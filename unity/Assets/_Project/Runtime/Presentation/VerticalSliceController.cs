using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Infrastructure;
using TimeKey.Presentation.Cards;
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

        [Header("Content")]
        [SerializeField] private TextAsset lightingFixture = null;
        [SerializeField] private TextAsset earthquakeFixture = null;

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
        [SerializeField] private List<TimelineCellView> timelineCells = new List<TimelineCellView>(36);

        [Header("Dynamic Prefabs")]
        [SerializeField] private GameObject hexColumnPrefab = null;
        [SerializeField] private GameObject grassBlockPrefab = null;
        [SerializeField] private GameObject dirtBlockPrefab = null;
        [SerializeField] private GameObject targetViewPrefab = null;

        private readonly Dictionary<HexCoord, BoardTileView> _tiles = new Dictionary<HexCoord, BoardTileView>();
        private readonly Dictionary<HexCoord, HexTileColumn> _columns = new Dictionary<HexCoord, HexTileColumn>();
        private readonly Dictionary<string, Sprite> _cardSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private GameObject _generatedRoot;
        private CardDefinition _lightingCard;
        private CardDefinition _earthquakeCard;
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
        private Sprite _lightingCardSprite;
        private Sprite _earthquakeCardSprite;
        private bool _initialized;
        private bool _viewsBound;

        public int CurrentTargetHp => _state == null ? 0 : _state.TargetHp;

        public bool EnemyIntentResolved => _lastSnapshot != null && _lastSnapshot.EnemyIntentResolved;

        public int TimelineSlotCount => timelineCells.Count;

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
            BuildSceneGraph();
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

        public void BuildSceneGraph()
        {
            if (_initialized)
            {
                return;
            }

            ValidateSerializedReferences();

            _lightingCard = CardJsonAdapter.Parse(lightingFixture.text);
            _earthquakeCard = CardJsonAdapter.Parse(earthquakeFixture.text);
            _state = new CombatSliceState(TargetId, 10, FixtureSeed, new CombatBoardState());
            _timeline = new TimelineGrid();

            _generatedRoot = dynamicRoot.gameObject;
            _boardRangePreview = boardRangePreview;
            _timelinePlacementPreview = timelinePlacementPreview;
            _cardHandHost = cardHandHost;
            _statusText = statusText;
            _targetText = targetText;
            _resolveButton = resolveButton;

            BuildWorld();
            var enemyIntent = CreateEnemyIntent();
            _applicationSession = new CombatApplicationSession(
                new ControllerCardCatalog(new[] { _lightingCard, _earthquakeCard }),
                _state,
                _timeline,
                new[] { enemyIntent });
            BuildInterface();
            RenderEnemyIntent(enemyIntent);
            SetStatus("Select LIGHTING or EARTHQUAKE to schedule an action.");
            _initialized = true;
            BindViews();
        }

        public bool SelectCard(string stableId)
        {
            EnsureBuilt();
            var result = _applicationSession.SelectCard(stableId);
            if (!result.Succeeded)
            {
                return false;
            }

            var card = _applicationSession.Current.SelectedCard;
            _boardRangePreview.Clear();
            _timelinePlacementPreview.Clear();
            SetHandCardsActive(true);
            RefreshHand(card.StableId, CardHandInteractionState.Selected);
            BoardCamera.InputEnabled = false;
            SetStatus(result.RequiredTargetKind == CombatTargetKind.Entity
                ? card.StableId.ToUpperInvariant() + " selected. Click the red target."
                : card.StableId.ToUpperInvariant() + " selected. Click a center hex.");
            return true;
        }

        public bool SelectTarget(string targetId)
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
                CombatTarget.ForEntity(targetId, TargetCoordinate));
            if (!result.Succeeded)
            {
                return false;
            }

            _boardRangePreview.Show(TargetCoordinate, current.SelectedCard.Range);
            _timelinePlacementPreview.Clear();
            _cardHandHost.SetInteractionState(CardHandInteractionState.Targeting);
            _targetText.text = string.Format("TARGET 01  |  HP {0} / 10  |  LOCKED", CurrentTargetHp);
            SetStatus("Target locked. Place LIGHTING in an open timeline slot.");
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

            _boardRangePreview.Clear();
            _timelinePlacementPreview.Clear();
            RefreshHand(null, CardHandInteractionState.Idle);
            BoardCamera.InputEnabled = true;
            _targetText.text = string.Format("TARGET 01  |  HP {0} / 10", CurrentTargetHp);
            SetStatus("Card cancelled. Select LIGHTING or EARTHQUAKE.");
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

            _boardRangePreview.Show(coordinate, current.SelectedCard.Range);
            _timelinePlacementPreview.Clear();
            _cardHandHost.SetInteractionState(CardHandInteractionState.Targeting);
            SetStatus("EARTHQUAKE center locked. Place its two-cell shape on the timeline.");
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

                return nearestTarget != null && SelectTarget(nearestTarget.TargetId);
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
            if (current.SelectedCard == null ||
                !current.Target.HasValue ||
                current.Phase == CombatSessionPhase.Committed)
            {
                return false;
            }

            var cell = new TimelineCell(column, row);
            var card = current.SelectedCard;
            var preview = _applicationSession.PreviewTimeline(cell);
            _timelinePlacementPreview.Show(cell, card.Shape, preview.IsPlacementValid);
            _cardHandHost.SetInteractionState(CardHandInteractionState.Scheduling);
            if (!preview.Succeeded || !preview.IsPlacementValid)
            {
                SetStatus("That timeline slot is occupied or outside the 12 x 3 grid.");
                return false;
            }

            var commit = _applicationSession.CommitTimeline();
            if (!commit.Succeeded)
            {
                SetStatus(card.StableId + " could not be committed to the timeline.");
                return false;
            }

            _boardRangePreview.Clear();
            _timelinePlacementPreview.Clear();
            var label = string.Equals(card.StableId, LightingCardId, StringComparison.Ordinal)
                ? "LIGHT"
                : "QUAKE";
            var color = string.Equals(card.StableId, LightingCardId, StringComparison.Ordinal)
                ? new Color(0.16f, 0.74f, 0.82f, 1f)
                : new Color(0.84f, 0.58f, 0.18f, 1f);
            for (var index = 0; index < card.Shape.Count; index++)
            {
                SetTimelineCell(cell + card.Shape[index], label, color);
            }
            _resolveButton.interactable = true;
            _cardHandHost.SetInteractionState(CardHandInteractionState.Disabled);
            SetHandCardsActive(false);
            BoardCamera.InputEnabled = true;
            SetStatus(card.StableId.ToUpperInvariant() + " placed. Resolve the timeline.");
            return true;
        }

        public bool PreviewTimelineSelected(int column, int row)
        {
            EnsureBuilt();
            var current = _applicationSession.Current;
            if (current.SelectedCard == null ||
                !current.Target.HasValue ||
                current.Phase == CombatSessionPhase.Committed)
            {
                return false;
            }

            var cell = new TimelineCell(column, row);
            var transition = _applicationSession.PreviewTimeline(cell);
            _timelinePlacementPreview.Show(
                cell,
                current.SelectedCard.Shape,
                transition.IsPlacementValid);
            _cardHandHost.SetInteractionState(CardHandInteractionState.Scheduling);
            SetStatus(transition.IsPlacementValid
                ? "Timeline position is legal. Click to confirm " + current.SelectedCard.StableId.ToUpperInvariant() + "."
                : "Timeline position conflicts or falls outside the 12 x 3 grid.");
            return transition.Succeeded && transition.IsPlacementValid;
        }

        public void ClearTimelinePreview()
        {
            if (_timelinePlacementPreview != null &&
                (_applicationSession == null ||
                 _applicationSession.Current.Phase != CombatSessionPhase.Committed))
            {
                _timelinePlacementPreview.Clear();
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

            var resolvedCardId = result.StableId;
            _lastSnapshot = result.Resolution;
            _boardRangePreview.Clear();
            _timelinePlacementPreview.Clear();
            ApplyTileEffects(_lastSnapshot);
            _targetText.text = string.Format(
                "TARGET 01  |  HP {0} / 10{1}",
                CurrentTargetHp,
                CurrentTargetHp == 0 ? "  |  DISABLED" : string.Empty);
            if (CurrentTargetHp == 0)
            {
                _targetMaterial.color = new Color(0.24f, 0.68f, 0.46f, 1f);
            }

            _resolveButton.interactable = false;
            BoardCamera.InputEnabled = true;
            SetStatus(string.Equals(resolvedCardId, LightingCardId, StringComparison.Ordinal)
                ? "RESOLVED  |  LIGHTING: 10 -> 0 HP  |  ENEMY INTENT: PROCESSED"
                : "RESOLVED  |  EARTHQUAKE: RANGE +2 LAYERS  |  ENEMY INTENT: PROCESSED");
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
        }

        private void BuildInterface()
        {
            for (var index = 0; index < timelineCells.Count; index++)
            {
                var cell = timelineCells[index];
                _timelinePlacementPreview.Register(cell.Coordinate, cell.Graphic);
            }

            _lightingCardSprite = LoadCardSprite(_lightingCard);
            _earthquakeCardSprite = LoadCardSprite(_earthquakeCard);
            _cardSprites.Add(_lightingCard.StableId, _lightingCardSprite);
            _cardSprites.Add(_earthquakeCard.StableId, _earthquakeCardSprite);
            RefreshHand(null, CardHandInteractionState.Idle);
            _cardHandView = _cardHandHost.GetCard(LightingCardId);
            _resolveButton.interactable = false;
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

        private static TimelineAction CreateEnemyIntent()
        {
            var origin = new TimelineCell(2, 1);
            return new TimelineAction(
                TimelineActorKind.Enemy,
                "enemy-intent",
                TargetId,
                origin,
                new[] { new TimelineCell(0, 0) },
                0);
        }

        private void RenderEnemyIntent(TimelineAction intent)
        {
            SetTimelineCell(intent.Origin, "INTENT", new Color(0.78f, 0.27f, 0.25f, 1f));
        }

        private void SetTimelineCell(TimelineCell cell, string label, Color color)
        {
            for (var index = 0; index < timelineCells.Count; index++)
            {
                if (timelineCells[index].Coordinate.Equals(cell))
                {
                    timelineCells[index].SetContent(label, color);
                    return;
                }
            }

            throw new InvalidOperationException("No serialized timeline cell exists for " + cell + ".");
        }

        private static Vector3 HexToWorld(HexCoord coordinate, float elevation)
        {
            const float radius = 1f;
            var x = 1.5f * radius * coordinate.Q;
            var z = Mathf.Sqrt(3f) * radius * (coordinate.R + (coordinate.Q / 2f));
            return new Vector3(x, elevation, z);
        }

        private static Sprite LoadCardSprite(CardDefinition card)
        {
            var resourceName = Path.GetFileNameWithoutExtension(card.FrontImage);
            var resourcePath = "Art/Battle/Cards/" + resourceName;
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                var importedSprite = Resources.Load<Sprite>(resourcePath);
                texture = importedSprite == null ? null : importedSprite.texture;
            }

            if (texture == null)
            {
                throw new InvalidOperationException("Card artwork is missing: " + card.FrontImage);
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = resourceName + "-runtime-sprite";
            return sprite;
        }

        private void RefreshHand(string selectedStableId, CardHandInteractionState state)
        {
            _cardHandHost.Build(new[]
            {
                new CardViewModel(
                    _lightingCard.StableId,
                    _cardSprites[_lightingCard.StableId],
                    string.Equals(selectedStableId, _lightingCard.StableId, StringComparison.Ordinal),
                    true),
                new CardViewModel(
                    _earthquakeCard.StableId,
                    _cardSprites[_earthquakeCard.StableId],
                    string.Equals(selectedStableId, _earthquakeCard.StableId, StringComparison.Ordinal),
                    true)
            });
            _cardHandHost.SetInteractionState(state);
            _cardHandHost.ApplyVisualStateImmediate();
            _cardHandView = _cardHandHost.GetCard(LightingCardId);
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

            _cardHandHost.CardSelected += HandleCardSelected;
            _cardHandHost.CardCancelRequested += HandleCardCancelRequested;
            _cardHandHost.CardDragChanged += HandleCardDragChanged;
            _resolveButton.onClick.AddListener(HandleResolveClicked);
            for (var index = 0; index < timelineCells.Count; index++)
            {
                timelineCells[index].Clicked += HandleTimelineClicked;
                timelineCells[index].PointerEntered += HandleTimelinePointerEntered;
                timelineCells[index].PointerExited += HandleTimelinePointerExited;
            }

            _viewsBound = true;
        }

        private void UnbindViews()
        {
            if (!_viewsBound)
            {
                return;
            }

            _cardHandHost.CardSelected -= HandleCardSelected;
            _cardHandHost.CardCancelRequested -= HandleCardCancelRequested;
            _cardHandHost.CardDragChanged -= HandleCardDragChanged;
            _resolveButton.onClick.RemoveListener(HandleResolveClicked);
            for (var index = 0; index < timelineCells.Count; index++)
            {
                timelineCells[index].Clicked -= HandleTimelineClicked;
                timelineCells[index].PointerEntered -= HandleTimelinePointerEntered;
                timelineCells[index].PointerExited -= HandleTimelinePointerExited;
            }

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
            RequireReference(lightingFixture, nameof(lightingFixture));
            RequireReference(earthquakeFixture, nameof(earthquakeFixture));
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
            RequireReference(hexColumnPrefab, nameof(hexColumnPrefab));
            RequireReference(grassBlockPrefab, nameof(grassBlockPrefab));
            RequireReference(dirtBlockPrefab, nameof(dirtBlockPrefab));
            RequireReference(targetViewPrefab, nameof(targetViewPrefab));

            if (timelineCells == null || timelineCells.Count != TimelineGrid.DefaultWidth * TimelineGrid.DefaultHeight)
            {
                throw new InvalidOperationException("timelineCells must contain exactly 36 serialized cells.");
            }

            var coordinates = new HashSet<TimelineCell>();
            for (var index = 0; index < timelineCells.Count; index++)
            {
                RequireReference(timelineCells[index], nameof(timelineCells) + "[" + index + "]");
                if (timelineCells[index].Graphic == null || !coordinates.Add(timelineCells[index].Coordinate))
                {
                    throw new InvalidOperationException("timelineCells contains an incomplete or duplicate coordinate.");
                }
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

        private void EnsureBuilt()
        {
            if (!_initialized)
            {
                BuildSceneGraph();
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
            _applicationSession?.Dispose();
            DestroyOwnedObject(_targetMaterial);
            DestroyOwnedObject(_lightingCardSprite);
            DestroyOwnedObject(_earthquakeCardSprite);
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

        private sealed class ControllerCardCatalog : ICardCatalog
        {
            private readonly Dictionary<string, CardDefinition> _cardsById =
                new Dictionary<string, CardDefinition>(StringComparer.Ordinal);

            public ControllerCardCatalog(IReadOnlyList<CardDefinition> cards)
            {
                Cards = cards ?? throw new ArgumentNullException(nameof(cards));
                for (var index = 0; index < cards.Count; index++)
                {
                    var card = cards[index] ??
                        throw new ArgumentException("Catalog cards cannot contain null.", nameof(cards));
                    if (!_cardsById.TryAdd(card.StableId, card))
                    {
                        throw new ArgumentException(
                            "Duplicate card stable ID: " + card.StableId + ".",
                            nameof(cards));
                    }
                }
            }

            public IReadOnlyList<CardDefinition> Cards { get; }

            public bool TryGet(string stableId, out CardDefinition card)
            {
                return _cardsById.TryGetValue(stableId, out card);
            }
        }
    }
}
