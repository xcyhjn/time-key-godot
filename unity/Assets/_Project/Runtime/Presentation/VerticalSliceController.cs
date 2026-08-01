using System;
using System.Collections;
using System.Collections.Generic;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Presentation.Bindings;
using TimeKey.Presentation.Cards;
using TimeKey.Presentation.Occupants;
using TimeKey.Presentation.Targeting;
using TimeKey.Presentation.Terrain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimeKey.Presentation
{
    public sealed class VerticalSliceController : MonoBehaviour
    {
        public const string LightingCardId = "lighting";
        public const string EarthquakeCardId = "earthquake";
        public const string TargetId = "target-01";
        public const int FixtureSeed = 731;
        public const float HexBlockHeight = 0.32f;

        private static readonly HexCoord TargetCoordinate = new HexCoord(1, 0);

        [Header("Stable Scene References")]
        [SerializeField] private Camera sceneCamera = null;
        [SerializeField] private BoardOrbitCameraController boardCamera = null;
        [SerializeField] private Light keyLight = null;
        [SerializeField] private Light fillLight = null;
        [SerializeField] private Renderer battlefieldGround = null;
        [SerializeField] private Transform boardRoot = null;
        [SerializeField] private Transform dynamicRoot = null;
        [SerializeField] private Transform targetAnchor = null;
        [SerializeField] private EventSystem sceneEventSystem = null;
        [SerializeField] private Canvas sceneCanvas = null;
        [SerializeField] private RectTransform hudRoot = null;
        [SerializeField] private RectTransform timelineRoot = null;
        [SerializeField] private Text statusText = null;
        [SerializeField] private Text targetText = null;
        [SerializeField] private Button resolveButton = null;
        [SerializeField] private CardHandHost cardHandHost = null;
        [SerializeField] private BoardRangePreview boardRangePreview = null;
        [SerializeField] private TimelinePlacementPreview timelinePlacementPreview = null;
        [SerializeField] private CombatPresentationBinding presentationBinding = null;

        [Header("Dynamic Prefabs")]
        [SerializeField] private GameObject hexColumnPrefab = null;
        [SerializeField] private GameObject grassBlockPrefab = null;
        [SerializeField] private GameObject dirtBlockPrefab = null;
        [SerializeField] private GameObject targetViewPrefab = null;

        private readonly Dictionary<HexCoord, BoardTileView> _tiles = new Dictionary<HexCoord, BoardTileView>();
        private readonly Dictionary<HexCoord, HexTileColumn> _columns = new Dictionary<HexCoord, HexTileColumn>();
        private GameObject _generatedRoot;
        private CombatApplicationSession _applicationSession;
        private CombatSliceState _state;
        private TimelineGrid _timeline;
        private ResolutionSnapshot _lastSnapshot;
        private Text _statusText;
        private Text _targetText;
        private Button _resolveButton;
        private Renderer _targetRenderer;
        private GameObject _targetObject;
        private Material _targetMaterial;
        private BoardTileView _selectedTile;
        private CardHandView _cardHandView;
        private CardHandHost _cardHandHost;
        private BoardRangePreview _boardRangePreview;
        private TimelinePlacementPreview _timelinePlacementPreview;
        private bool _initialized;
        private bool _viewsBound;

        public int CurrentTargetHp => _state == null ? 0 : _state.TargetHp;

        public bool EnemyIntentResolved => _lastSnapshot != null && _lastSnapshot.EnemyIntentResolved;

        public int TimelineSlotCount => presentationBinding == null
            ? 0
            : presentationBinding.TimelineCellCount;

        public int BoardTileCount => _tiles.Count;

        public int TimelineOccupiedCellCount => _timeline == null ? 0 : _timeline.OccupiedCellCount;

        public HexCoord? SelectedTile => _selectedTile == null ? (HexCoord?)null : _selectedTile.Coordinate;

        public Vector3 TargetWorldPosition => _targetObject == null ? Vector3.zero : _targetObject.transform.position;

        public Camera SceneCamera => sceneCamera;

        public Canvas SceneCanvas => sceneCanvas;

        public BoardOrbitCameraController BoardCamera => boardCamera;

        public CardHandView CardHand => _cardHandView;

        public CardHandHost CardHandHost => _cardHandHost;

        public string SelectedCardId =>
            _applicationSession == null ? null : _applicationSession.Current.SelectedStableId;

        public BoardRangePreview BoardRangePreview => _boardRangePreview;

        public TimelinePlacementPreview TimelinePreview => _timelinePlacementPreview;

        public HexTileColumn GetTileColumn(HexCoord coordinate)
        {
            EnsureBuilt();
            return _columns.TryGetValue(coordinate, out var column) ? column : null;
        }

        public CardPlaySessionState? CardPlayState => GetCompatibilityCardPlayState();

        private void Awake()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException(
                    "VerticalSliceController must be initialized by the serialized composition root.");
            }
        }

        private void OnEnable()
        {
            if (_initialized)
            {
                BindViews();
            }
        }

        private void OnDisable()
        {
            UnbindViews();
        }

        private void Update()
        {
            if (_generatedRoot == null ||
                boardCamera == null ||
                boardCamera.IsManipulating ||
                IsAnyCardDragging())
            {
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                TrySelectWorldAtScreenPoint(Input.mousePosition);
            }
        }

        private IEnumerator Start()
        {
            if (!HasCommandLineArgument("-timekeySmokeQuit"))
            {
                yield break;
            }

            yield return null;

            try
            {
                var arranged = SelectCard(LightingCardId) &&
                    SelectTarget(TargetId) &&
                    TryPlaceSelected(0, 0);
                var snapshot = arranged ? ResolveTimeline() : null;
                if (snapshot == null || snapshot.TargetHpAfter != 0 || !snapshot.EnemyIntentResolved)
                {
                    throw new InvalidOperationException("The Player smoke path did not satisfy the frozen slice contract.");
                }

                Debug.Log("TIMEKEY_PLAYER_SMOKE_PASS");
                UnityEngine.Application.Quit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                UnityEngine.Application.Quit(1);
            }
        }

        public void Initialize(
            CombatApplicationSession applicationSession,
            CombatSliceState state,
            TimelineGrid timeline,
            IReadOnlyList<TimelineAction> initialActions)
        {
            if (_initialized)
            {
                return;
            }

            _applicationSession = applicationSession ??
                throw new ArgumentNullException(nameof(applicationSession));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            BuildSceneGraph(initialActions ?? throw new ArgumentNullException(nameof(initialActions)));
        }

        public void BuildSceneGraph()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException(
                    "VerticalSliceController has not been initialized by composition.");
            }
        }

        private void BuildSceneGraph(IReadOnlyList<TimelineAction> initialActions)
        {
            if (_initialized)
            {
                return;
            }

            ValidateSerializedReferences();
            _generatedRoot = dynamicRoot.gameObject;
            _boardRangePreview = boardRangePreview;
            _timelinePlacementPreview = timelinePlacementPreview;
            _cardHandHost = cardHandHost;
            _statusText = statusText;
            _targetText = targetText;
            _resolveButton = resolveButton;

            BuildWorld();
            BuildInterface();
            for (var index = 0; index < initialActions.Count; index++)
            {
                RenderTimelineAction(initialActions[index]);
            }
            _initialized = true;
            BindViews();
            RefreshPresentation();
        }

        public bool SelectCard(string stableId)
        {
            EnsureBuilt();
            var result = _applicationSession.SelectCard(stableId);
            if (!result.Succeeded)
            {
                return false;
            }

            SetHandCardsActive(true);
            BoardCamera.InputEnabled = false;
            RefreshPresentation();
            return true;
        }

        public bool SelectTarget(string targetId)
        {
            return SelectTarget(targetId, TargetCoordinate);
        }

        public bool SelectTarget(string targetId, HexCoord coordinate)
        {
            EnsureBuilt();
            var current = _applicationSession.Current;
            if (current.SelectedCard == null ||
                string.IsNullOrWhiteSpace(targetId) ||
                current.RequiredTargetKind != CombatTargetKind.Entity)
            {
                return false;
            }

            var result = _applicationSession.SelectTarget(
                CombatTarget.ForEntity(targetId, coordinate));
            if (!result.Succeeded)
            {
                return false;
            }

            RefreshPresentation();
            return true;
        }

        public bool CancelSelectedCard()
        {
            EnsureBuilt();
            if (_applicationSession.Current.SelectedCard == null)
            {
                return false;
            }

            var result = _applicationSession.CancelCard();
            if (!result.Succeeded)
            {
                return false;
            }

            BoardCamera.InputEnabled = true;
            RefreshPresentation();
            return true;
        }

        public bool SelectTile(HexCoord coordinate)
        {
            EnsureBuilt();
            if (!_tiles.TryGetValue(coordinate, out var tile))
            {
                return false;
            }

            if (_applicationSession.Current.RequiredTargetKind == CombatTargetKind.Tile)
            {
                return SelectEarthquakeTarget(coordinate);
            }

            if (_selectedTile != null)
            {
                _selectedTile.SetSelected(false);
            }

            _selectedTile = tile;
            _selectedTile.SetSelected(true);
            SetStatus(string.Format("HEX {0},{1} selected.", coordinate.Q, coordinate.R));
            return true;
        }

        public bool SelectEarthquakeTarget(HexCoord coordinate)
        {
            EnsureBuilt();
            var current = _applicationSession.Current;
            if (current.SelectedCard == null ||
                current.RequiredTargetKind != CombatTargetKind.Tile ||
                !_tiles.ContainsKey(coordinate))
            {
                return false;
            }

            var result = _applicationSession.SelectTarget(CombatTarget.ForTile(coordinate));
            if (!result.Succeeded)
            {
                return false;
            }

            RefreshPresentation();
            return true;
        }

        public void SetBoardView(float yaw, float pitch = 48f, float distance = 15.5f)
        {
            EnsureBuilt();
            BoardCamera.SetView(yaw, pitch, distance, new Vector3(0f, 0.45f, 0f), true);
            foreach (var billboard in _generatedRoot.GetComponentsInChildren<CameraFacingBillboard>())
            {
                billboard.FaceCamera();
            }
        }

        public bool TrySelectWorldAtScreenPoint(Vector2 screenPoint)
        {
            EnsureBuilt();
            if (IsScreenPointOverInterface(screenPoint))
            {
                return false;
            }

            var ray = SceneCamera.ScreenPointToRay(screenPoint);
            var hits = Physics.RaycastAll(ray, SceneCamera.farClipPlane);
            if (hits.Length == 0)
            {
                return false;
            }

            var current = _applicationSession.Current;
            if (current.SelectedCard != null &&
                current.RequiredTargetKind == CombatTargetKind.Entity)
            {
                WorldTargetView nearestTarget = null;
                var nearestDistance = float.MaxValue;
                for (var index = 0; index < hits.Length; index++)
                {
                    var target = hits[index].collider.GetComponentInParent<WorldTargetView>();
                    if (target != null && hits[index].distance < nearestDistance)
                    {
                        nearestTarget = target;
                        nearestDistance = hits[index].distance;
                    }
                }

                return nearestTarget != null &&
                       _state.TryGetOccupant(nearestTarget.TargetId, out var occupant) &&
                       SelectTarget(nearestTarget.TargetId, occupant.Coordinate);
            }

            if (current.SelectedCard != null &&
                current.RequiredTargetKind != CombatTargetKind.Tile)
            {
                return false;
            }

            BoardTileView selectedTile = null;
            var closestScreenDistance = float.MaxValue;
            for (var index = 0; index < hits.Length; index++)
            {
                var tile = hits[index].collider.GetComponentInParent<BoardTileView>();
                if (tile == null || !_columns.TryGetValue(tile.Coordinate, out var column))
                {
                    continue;
                }

                var bounds = column.TopBounds;
                var topCenter = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
                var projected = SceneCamera.WorldToScreenPoint(topCenter);
                var distance = ((Vector2)projected - screenPoint).sqrMagnitude;
                if (distance < closestScreenDistance)
                {
                    selectedTile = tile;
                    closestScreenDistance = distance;
                }
            }

            return selectedTile != null && SelectTile(selectedTile.Coordinate);
        }

        public bool IsScreenPointOverInterface(Vector2 screenPoint)
        {
            if (SceneCanvas == null)
            {
                return false;
            }

            foreach (var graphic in SceneCanvas.GetComponentsInChildren<Graphic>(false))
            {
                if (graphic.raycastTarget &&
                    graphic.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(
                        graphic.rectTransform,
                        screenPoint,
                        SceneCanvas.worldCamera))
                {
                    return true;
                }
            }

            if (EventSystem.current == null)
            {
                return false;
            }

            var pointer = new PointerEventData(EventSystem.current) { position = screenPoint };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            foreach (var result in results)
            {
                var canvas = result.gameObject.GetComponentInParent<Canvas>();
                if (canvas == SceneCanvas)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryPlaceSelected(int column, int row)
        {
            EnsureBuilt();
            var current = _applicationSession.Current;
            if (current.SelectedCard == null || current.Phase == CombatSessionPhase.Committed)
            {
                return false;
            }

            var cell = new TimelineCell(column, row);
            var card = current.SelectedCard;
            if (current.InteractionMode == CombatInteractionMode.TimelineClear)
            {
                var clearPreview = _applicationSession.PreviewClear(cell);
                RefreshPresentation();
                if (!clearPreview.Succeeded || !clearPreview.IsPlacementValid)
                {
                    return false;
                }

                var clearCommit = _applicationSession.CommitClear();
                if (!clearCommit.Succeeded || clearCommit.ClearResult == null)
                {
                    SetStatus(card.StableId + " could not clear the timeline.");
                    return false;
                }

                presentationBinding.ApplyTimelineClearResult(clearCommit.ClearResult);
                SetHandCardsActive(false);
                BoardCamera.InputEnabled = true;
                RefreshPresentation();
                return true;
            }

            if (!current.Target.HasValue)
            {
                return false;
            }

            var preview = _applicationSession.PreviewTimeline(cell);
            RefreshPresentation();
            if (!preview.Succeeded || !preview.IsPlacementValid)
            {
                return false;
            }

            var commit = _applicationSession.CommitTimeline();
            if (!commit.Succeeded)
            {
                SetStatus(card.StableId + " could not be committed to the timeline.");
                return false;
            }

            var label = GetTimelineLabel(card);
            var color = card.Effects.Count > 0 && card.Effects[0].Kind == CardEffectKind.Elevation
                ? new Color(0.84f, 0.58f, 0.18f, 1f)
                : new Color(0.16f, 0.74f, 0.82f, 1f);
            presentationBinding.RenderTimelineAction(cell, card.Shape, label, color);
            SetHandCardsActive(false);
            BoardCamera.InputEnabled = true;
            RefreshPresentation();
            return true;
        }

        public bool PreviewTimelineSelected(int column, int row)
        {
            EnsureBuilt();
            var current = _applicationSession.Current;
            if (current.SelectedCard == null || current.Phase == CombatSessionPhase.Committed)
            {
                return false;
            }

            var cell = new TimelineCell(column, row);
            if (current.InteractionMode == CombatInteractionMode.TimelineClear)
            {
                var clearTransition = _applicationSession.PreviewClear(cell);
                RefreshPresentation();
                return clearTransition.Succeeded && clearTransition.IsPlacementValid;
            }

            if (!current.Target.HasValue)
            {
                return false;
            }

            var transition = _applicationSession.PreviewTimeline(cell);
            RefreshPresentation();
            return transition.Succeeded && transition.IsPlacementValid;
        }

        public void ClearTimelinePreview()
        {
            if (presentationBinding != null &&
                (_applicationSession == null ||
                 _applicationSession.Current.Phase != CombatSessionPhase.Committed))
            {
                presentationBinding.ClearTimelinePreview();
            }
        }

        public ResolutionSnapshot ResolveTimeline()
        {
            EnsureBuilt();
            if (_applicationSession.Current.Phase != CombatSessionPhase.Committed)
            {
                throw new InvalidOperationException("Place the player action before resolving.");
            }

            var result = _applicationSession.ResolveTimeline();
            if (!result.Succeeded || result.Resolution == null)
            {
                throw new InvalidOperationException(
                    "The committed player action could not be resolved: " + result.FailureReason);
            }

            _lastSnapshot = result.Resolution;
            ApplyTileEffects(_lastSnapshot);
            presentationBinding.ApplyOccupantEffects(_lastSnapshot.OccupantEffectResults);
            if (CurrentTargetHp == 0)
            {
                _targetMaterial.color = new Color(0.24f, 0.68f, 0.46f, 1f);
            }

            BoardCamera.InputEnabled = true;
            RefreshPresentation();
            return _lastSnapshot;
        }

        private void ApplyTileEffects(ResolutionSnapshot snapshot)
        {
            for (var index = 0; index < snapshot.EffectResults.Count; index++)
            {
                var result = snapshot.EffectResults[index];
                if (!_columns.TryGetValue(result.Coordinate, out var column))
                {
                    continue;
                }

                if (result.Removed)
                {
                    column.Clear();
                    continue;
                }

                column.ApplyLogicalLayerCount(result.AfterLayers);
                var tileView = _tiles[result.Coordinate];
                tileView.RefreshRenderers(ToArray(column.Renderers));
                _boardRangePreview.Register(result.Coordinate, tileView);
            }

            targetAnchor.position = _columns[TargetCoordinate].OccupantAnchor.position;
        }

        private void BuildWorld()
        {
            sceneCanvas.worldCamera = sceneCamera;
            boardCamera.Initialize(sceneCamera, new Vector3(0f, 0.45f, 0f), 32f, 48f, 15.5f);

            for (var q = -2; q <= 2; q++)
            {
                var minimumR = Mathf.Max(-2, -q - 2);
                var maximumR = Mathf.Min(2, -q + 2);
                for (var r = minimumR; r <= maximumR; r++)
                {
                    var elevation = (q == 1 && r == 0) || (q == -1 && r == 1) ? 1 : 0;
                    CreateTile(new HexCoord(q, r), elevation);
                }
            }

            targetAnchor.position = _columns[TargetCoordinate].OccupantAnchor.position;
            _targetObject = Instantiate(targetViewPrefab, targetAnchor, false);
            _targetObject.name = "Target-target-01";
            _targetObject.transform.localPosition = new Vector3(0f, 0.10f, 0f);
            _targetRenderer = _targetObject.GetComponentInChildren<Renderer>(true);
            if (_targetRenderer == null)
            {
                throw new InvalidOperationException("The target view Prefab must contain a Renderer.");
            }

            _targetMaterial = new Material(_targetRenderer.sharedMaterial);
            _targetRenderer.sharedMaterial = _targetMaterial;
            foreach (var target in _targetObject.GetComponentsInChildren<WorldTargetView>(true))
            {
                target.Initialize(TargetId);
            }

            foreach (var billboard in _targetObject.GetComponentsInChildren<CameraFacingBillboard>(true))
            {
                billboard.Initialize(sceneCamera);
            }

            var occupantView = _targetObject.GetComponent<CombatOccupantView>();
            if (occupantView == null ||
                !_state.TryGetOccupant(TargetId, TargetCoordinate, out var targetOccupant))
            {
                throw new InvalidOperationException(
                    "The target view Prefab or target occupant registration is incomplete.");
            }

            presentationBinding.RegisterExistingOccupant(targetOccupant, occupantView);
        }

        private void BuildInterface()
        {
            _cardHandView = _cardHandHost.GetCard(LightingCardId);
        }

        private void CreateTile(HexCoord coordinate, int elevation)
        {
            var tile = Instantiate(hexColumnPrefab, boardRoot, false);
            tile.name = string.Format("Hex-{0}-{1}", coordinate.Q, coordinate.R);
            tile.transform.position = HexToWorld(coordinate, 0f);
            var logicalLayerCount = elevation + 1;
            var column = tile.GetComponent<HexTileColumn>();
            if (column == null)
            {
                throw new InvalidOperationException("The hex-column Prefab must contain HexTileColumn.");
            }

            column.Initialize(grassBlockPrefab, dirtBlockPrefab, HexBlockHeight);
            column.ApplyLogicalLayerCount(logicalLayerCount);

            var tileView = column.GetComponent<BoardTileView>();
            if (tileView == null)
            {
                throw new InvalidOperationException("The hex-column Prefab must contain BoardTileView.");
            }

            tileView.Initialize(coordinate, ToArray(column.Renderers));
            _tiles.Add(coordinate, tileView);
            _columns.Add(coordinate, column);
            _state.Board.AddTile(coordinate, logicalLayerCount);
            _boardRangePreview.Register(coordinate, tileView);
            presentationBinding.RegisterOccupantColumn(coordinate, column);
        }

        private void HandleCardCancelRequested(string stableId)
        {
            var selectedStableId = SelectedCardId;
            if (selectedStableId != null &&
                string.Equals(stableId, selectedStableId, StringComparison.Ordinal))
            {
                CancelSelectedCard();
            }
        }

        private void HandleCardDragChanged(string stableId, Vector2 pointerPosition, CardDragPhase phase)
        {
            var selectedStableId = SelectedCardId;
            if (selectedStableId == null ||
                !string.Equals(stableId, selectedStableId, StringComparison.Ordinal))
            {
                return;
            }

            if (phase == CardDragPhase.Started || phase == CardDragPhase.Moved)
            {
                BoardCamera.InputEnabled = false;
                SetStatus(stableId.ToUpperInvariant() + " held. Choose a target and timeline position.");
            }
        }

        private void RenderTimelineAction(TimelineAction intent)
        {
            presentationBinding.RenderTimelineAction(
                intent.Origin,
                intent.Shape,
                "INTENT",
                new Color(0.78f, 0.27f, 0.25f, 1f));
        }

        private static Vector3 HexToWorld(HexCoord coordinate, float elevation)
        {
            const float radius = 1f;
            var x = 1.5f * radius * coordinate.Q;
            var z = Mathf.Sqrt(3f) * radius * (coordinate.R + (coordinate.Q / 2f));
            return new Vector3(x, elevation, z);
        }

        private static string GetTimelineLabel(CardDefinition card)
        {
            var label = card.StableId.ToUpperInvariant();
            return label.Length <= 5 ? label : label.Substring(0, 5);
        }

        private void SetHandCardsActive(bool active)
        {
            for (var index = 0; index < _cardHandHost.Cards.Count; index++)
            {
                _cardHandHost.Cards[index].gameObject.SetActive(active);
            }
        }

        private bool IsAnyCardDragging()
        {
            if (_cardHandHost == null)
            {
                return false;
            }

            for (var index = 0; index < _cardHandHost.Cards.Count; index++)
            {
                if (_cardHandHost.Cards[index].IsDragging)
                {
                    return true;
                }
            }

            return false;
        }

        private static Renderer[] ToArray(IReadOnlyList<Renderer> renderers)
        {
            var result = new Renderer[renderers.Count];
            for (var index = 0; index < renderers.Count; index++)
            {
                result[index] = renderers[index];
            }

            return result;
        }

        private void BindViews()
        {
            if (_viewsBound)
            {
                return;
            }

            presentationBinding.Bind();
            presentationBinding.CardSelected += HandleCardSelected;
            presentationBinding.CardCancelled += HandleCardCancelRequested;
            presentationBinding.CardDragChanged += HandleCardDragChanged;
            presentationBinding.TimelineSelected += HandleTimelineClicked;
            presentationBinding.TimelinePreviewRequested += HandleTimelinePointerEntered;
            presentationBinding.TimelinePreviewCleared += HandleTimelinePointerExited;
            presentationBinding.ResolveRequested += HandleResolveClicked;

            _viewsBound = true;
        }

        private void UnbindViews()
        {
            if (!_viewsBound)
            {
                return;
            }

            presentationBinding.CardSelected -= HandleCardSelected;
            presentationBinding.CardCancelled -= HandleCardCancelRequested;
            presentationBinding.CardDragChanged -= HandleCardDragChanged;
            presentationBinding.TimelineSelected -= HandleTimelineClicked;
            presentationBinding.TimelinePreviewRequested -= HandleTimelinePointerEntered;
            presentationBinding.TimelinePreviewCleared -= HandleTimelinePointerExited;
            presentationBinding.ResolveRequested -= HandleResolveClicked;
            presentationBinding.Unbind();

            _viewsBound = false;
        }

        private void HandleCardSelected(string stableId)
        {
            SelectCard(stableId);
        }

        private void HandleTimelineClicked(TimelineCell coordinate)
        {
            TryPlaceSelected(coordinate.X, coordinate.Y);
        }

        private void HandleTimelinePointerEntered(TimelineCell coordinate)
        {
            PreviewTimelineSelected(coordinate.X, coordinate.Y);
        }

        private void HandleTimelinePointerExited()
        {
            ClearTimelinePreview();
        }

        private void HandleResolveClicked()
        {
            ResolveTimeline();
        }

        private void ValidateSerializedReferences()
        {
            RequireReference(sceneCamera, nameof(sceneCamera));
            RequireReference(boardCamera, nameof(boardCamera));
            RequireReference(keyLight, nameof(keyLight));
            RequireReference(fillLight, nameof(fillLight));
            RequireReference(battlefieldGround, nameof(battlefieldGround));
            RequireReference(boardRoot, nameof(boardRoot));
            RequireReference(dynamicRoot, nameof(dynamicRoot));
            RequireReference(targetAnchor, nameof(targetAnchor));
            RequireReference(sceneEventSystem, nameof(sceneEventSystem));
            RequireReference(sceneCanvas, nameof(sceneCanvas));
            RequireReference(hudRoot, nameof(hudRoot));
            RequireReference(timelineRoot, nameof(timelineRoot));
            RequireReference(statusText, nameof(statusText));
            RequireReference(targetText, nameof(targetText));
            RequireReference(resolveButton, nameof(resolveButton));
            RequireReference(cardHandHost, nameof(cardHandHost));
            RequireReference(boardRangePreview, nameof(boardRangePreview));
            RequireReference(timelinePlacementPreview, nameof(timelinePlacementPreview));
            RequireReference(presentationBinding, nameof(presentationBinding));
            RequireReference(hexColumnPrefab, nameof(hexColumnPrefab));
            RequireReference(grassBlockPrefab, nameof(grassBlockPrefab));
            RequireReference(dirtBlockPrefab, nameof(dirtBlockPrefab));
            RequireReference(targetViewPrefab, nameof(targetViewPrefab));

            if (presentationBinding.TimelineCellCount !=
                TimelineGrid.DefaultWidth * TimelineGrid.DefaultHeight)
            {
                throw new InvalidOperationException(
                    "The serialized TimelinePresenter must contain exactly 36 cells.");
            }
        }

        private void OnValidate()
        {
            if (!gameObject.scene.IsValid())
            {
                return;
            }

            try
            {
                ValidateSerializedReferences();
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError(name + ": " + exception.Message, this);
            }
        }

        private static void RequireReference(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
            {
                throw new InvalidOperationException("Missing serialized reference: " + fieldName + ".");
            }
        }

        private void SetStatus(string value)
        {
            _statusText.text = value;
        }

        private void RefreshPresentation()
        {
            presentationBinding.Refresh(_applicationSession.Current);
        }

        private void EnsureBuilt()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException(
                    "VerticalSliceController has not been initialized by composition.");
            }
        }

        private CardPlaySessionState? GetCompatibilityCardPlayState()
        {
            if (_applicationSession == null ||
                _applicationSession.Current.SelectedCard == null)
            {
                return null;
            }

            switch (_applicationSession.Current.Phase)
            {
                case CombatSessionPhase.CardSelected:
                    return CardPlaySessionState.Idle;
                case CombatSessionPhase.TargetSelected:
                    return CardPlaySessionState.TargetSelected;
                case CombatSessionPhase.TimelinePreview:
                    return CardPlaySessionState.TimelinePreview;
                case CombatSessionPhase.Committed:
                    return CardPlaySessionState.Committed;
                default:
                    return null;
            }
        }

        private void OnDestroy()
        {
            UnbindViews();
            DestroyOwnedObject(_targetMaterial);
        }

        private static void DestroyOwnedObject(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }

        private static bool HasCommandLineArgument(string expected)
        {
            foreach (var argument in Environment.GetCommandLineArgs())
            {
                if (string.Equals(argument, expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
