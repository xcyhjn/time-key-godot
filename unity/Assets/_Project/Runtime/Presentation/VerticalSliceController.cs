using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        [SerializeField] private TextAsset lightingFixture = null;
        [SerializeField] private TextAsset earthquakeFixture = null;

        private readonly List<Button> _timelineButtons = new List<Button>(36);
        private readonly Dictionary<HexCoord, BoardTileView> _tiles = new Dictionary<HexCoord, BoardTileView>();
        private readonly Dictionary<HexCoord, HexTileColumn> _columns = new Dictionary<HexCoord, HexTileColumn>();
        private readonly Dictionary<string, CardDefinition> _cards =
            new Dictionary<string, CardDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> _cardSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private GameObject _generatedRoot;
        private CardDefinition _lightingCard;
        private CardDefinition _earthquakeCard;
        private CardPlaySession _cardPlaySession;
        private CombatSliceState _state;
        private TimelineGrid _timeline;
        private CardDefinition _selectedCard;
        private string _selectedTargetId;
        private TimelineCell? _playerCell;
        private ResolutionSnapshot _lastSnapshot;
        private Text _statusText;
        private Text _targetText;
        private Button _resolveButton;
        private Renderer _targetRenderer;
        private GameObject _targetObject;
        private Material _targetMaterial;
        private Material _groundMaterial;
        private Sprite _centerAltarSprite;
        private GameObject _grassTilePrefab;
        private GameObject _dirtTilePrefab;
        private BoardTileView _selectedTile;
        private CardHandView _cardHandView;
        private CardHandHost _cardHandHost;
        private BoardRangePreview _boardRangePreview;
        private TimelinePlacementPreview _timelinePlacementPreview;
        private Sprite _lightingCardSprite;
        private Sprite _earthquakeCardSprite;

        public int CurrentTargetHp => _state == null ? 0 : _state.TargetHp;

        public bool EnemyIntentResolved => _lastSnapshot != null && _lastSnapshot.EnemyIntentResolved;

        public int TimelineSlotCount => _timelineButtons.Count;

        public int BoardTileCount => _tiles.Count;

        public int TimelineOccupiedCellCount => _timeline == null ? 0 : _timeline.OccupiedCellCount;

        public HexCoord? SelectedTile => _selectedTile == null ? (HexCoord?)null : _selectedTile.Coordinate;

        public Vector3 TargetWorldPosition => _targetObject == null ? Vector3.zero : _targetObject.transform.position;

        public Camera SceneCamera { get; private set; }

        public Canvas SceneCanvas { get; private set; }

        public BoardOrbitCameraController BoardCamera { get; private set; }

        public CardHandView CardHand => _cardHandView;

        public CardHandHost CardHandHost => _cardHandHost;

        public string SelectedCardId => _selectedCard == null ? null : _selectedCard.StableId;

        public BoardRangePreview BoardRangePreview => _boardRangePreview;

        public TimelinePlacementPreview TimelinePreview => _timelinePlacementPreview;

        public HexTileColumn GetTileColumn(HexCoord coordinate)
        {
            EnsureBuilt();
            return _columns.TryGetValue(coordinate, out var column) ? column : null;
        }

        public CardPlaySessionState? CardPlayState =>
            _cardPlaySession == null ? (CardPlaySessionState?)null : _cardPlaySession.State;

        private void Awake()
        {
            BuildSceneGraph();
        }

        private void Update()
        {
            if (_generatedRoot == null ||
                BoardCamera == null ||
                BoardCamera.IsManipulating ||
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
                Application.Quit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Application.Quit(1);
            }
        }

        public void BuildSceneGraph()
        {
            if (_generatedRoot != null)
            {
                return;
            }

            if (lightingFixture == null || earthquakeFixture == null)
            {
                throw new InvalidOperationException("The lighting and earthquake card fixtures must be assigned.");
            }

            _lightingCard = CardJsonAdapter.Parse(lightingFixture.text);
            _earthquakeCard = CardJsonAdapter.Parse(earthquakeFixture.text);
            _cards.Add(_lightingCard.StableId, _lightingCard);
            _cards.Add(_earthquakeCard.StableId, _earthquakeCard);
            _state = new CombatSliceState(TargetId, 10, FixtureSeed, new CombatBoardState());
            _timeline = new TimelineGrid();

            _generatedRoot = new GameObject("GeneratedSlice");
            _generatedRoot.transform.SetParent(transform, false);
            _boardRangePreview = _generatedRoot.AddComponent<BoardRangePreview>();
            _timelinePlacementPreview = _generatedRoot.AddComponent<TimelinePlacementPreview>();

            BuildWorld();
            BuildInterface();
            PlaceEnemyIntent();
            SetStatus("Select LIGHTING or EARTHQUAKE to schedule an action.");
        }

        public bool SelectCard(string stableId)
        {
            EnsureBuilt();
            if (!_cards.TryGetValue(stableId, out var card) ||
                _playerCell.HasValue ||
                _lastSnapshot != null)
            {
                return false;
            }

            _selectedCard = card;
            _selectedTargetId = null;
            _boardRangePreview.Clear();
            _timelinePlacementPreview.Clear();
            _cardPlaySession = new CardPlaySession(card);
            SetHandCardsActive(true);
            RefreshHand(card.StableId, CardHandInteractionState.Selected);
            BoardCamera.InputEnabled = false;
            SetStatus(string.Equals(card.StableId, LightingCardId, StringComparison.Ordinal)
                ? "LIGHTING selected. Click the red target."
                : "EARTHQUAKE selected. Click a center hex.");
            return true;
        }

        public bool SelectTarget(string targetId)
        {
            EnsureBuilt();
            if (_selectedCard == null ||
                _cardPlaySession == null ||
                !string.Equals(_selectedCard.StableId, LightingCardId, StringComparison.Ordinal) ||
                !string.Equals(targetId, TargetId, StringComparison.Ordinal))
            {
                return false;
            }

            var transition = _cardPlaySession.SelectTarget(targetId, TargetCoordinate);
            if (!transition.Succeeded)
            {
                return false;
            }

            _selectedTargetId = targetId;
            _boardRangePreview.Show(TargetCoordinate, _selectedCard.Range);
            _timelinePlacementPreview.Clear();
            _cardHandHost.SetInteractionState(CardHandInteractionState.Targeting);
            _targetText.text = string.Format("TARGET 01  |  HP {0} / 10  |  LOCKED", CurrentTargetHp);
            SetStatus("Target locked. Place LIGHTING in an open timeline slot.");
            return true;
        }

        public bool CancelSelectedCard()
        {
            EnsureBuilt();
            if (_cardPlaySession == null)
            {
                return false;
            }

            var transition = _cardPlaySession.Cancel();
            if (!transition.Succeeded)
            {
                return false;
            }

            _selectedCard = null;
            _selectedTargetId = null;
            _cardPlaySession = null;
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

            if (_selectedCard != null &&
                string.Equals(_selectedCard.StableId, EarthquakeCardId, StringComparison.Ordinal))
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
            if (_selectedCard == null ||
                _cardPlaySession == null ||
                !string.Equals(_selectedCard.StableId, EarthquakeCardId, StringComparison.Ordinal) ||
                !_tiles.ContainsKey(coordinate))
            {
                return false;
            }

            var targetId = string.Format("hex-{0}-{1}", coordinate.Q, coordinate.R);
            var transition = _cardPlaySession.SelectTarget(targetId, coordinate);
            if (!transition.Succeeded)
            {
                return false;
            }

            _selectedTargetId = targetId;
            _boardRangePreview.Show(coordinate, _selectedCard.Range);
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

            if (_selectedCard != null &&
                string.Equals(_selectedCard.StableId, LightingCardId, StringComparison.Ordinal))
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

            if (_cardPlaySession != null &&
                (_selectedCard == null ||
                 !string.Equals(_selectedCard.StableId, EarthquakeCardId, StringComparison.Ordinal)))
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
            if (_selectedCard == null ||
                _cardPlaySession == null ||
                string.IsNullOrEmpty(_selectedTargetId) ||
                _playerCell.HasValue)
            {
                return false;
            }

            var cell = new TimelineCell(column, row);
            var preview = _cardPlaySession.PreviewTimeline(_timeline, cell);
            _timelinePlacementPreview.Show(cell, _selectedCard.Shape, preview.IsPlacementValid);
            _cardHandHost.SetInteractionState(CardHandInteractionState.Scheduling);
            if (!preview.IsPlacementValid)
            {
                SetStatus("That timeline slot is occupied or outside the 12 x 3 grid.");
                return false;
            }

            var commit = _cardPlaySession.Commit(_timeline);
            if (!commit.Succeeded)
            {
                SetStatus(_selectedCard.StableId + " could not be committed to the timeline.");
                return false;
            }

            _playerCell = cell;
            _boardRangePreview.Clear();
            _timelinePlacementPreview.Clear();
            var label = string.Equals(_selectedCard.StableId, LightingCardId, StringComparison.Ordinal)
                ? "LIGHT"
                : "QUAKE";
            var color = string.Equals(_selectedCard.StableId, LightingCardId, StringComparison.Ordinal)
                ? new Color(0.16f, 0.74f, 0.82f, 1f)
                : new Color(0.84f, 0.58f, 0.18f, 1f);
            for (var index = 0; index < _selectedCard.Shape.Count; index++)
            {
                SetTimelineCell(cell + _selectedCard.Shape[index], label, color);
            }
            _resolveButton.interactable = true;
            _cardHandHost.SetInteractionState(CardHandInteractionState.Disabled);
            SetHandCardsActive(false);
            BoardCamera.InputEnabled = true;
            SetStatus(_selectedCard.StableId.ToUpperInvariant() + " placed. Resolve the timeline.");
            return true;
        }

        public bool PreviewTimelineSelected(int column, int row)
        {
            EnsureBuilt();
            if (_selectedCard == null ||
                _cardPlaySession == null ||
                string.IsNullOrEmpty(_selectedTargetId) ||
                _playerCell.HasValue)
            {
                return false;
            }

            var cell = new TimelineCell(column, row);
            var transition = _cardPlaySession.PreviewTimeline(_timeline, cell);
            _timelinePlacementPreview.Show(cell, _selectedCard.Shape, transition.IsPlacementValid);
            _cardHandHost.SetInteractionState(CardHandInteractionState.Scheduling);
            SetStatus(transition.IsPlacementValid
                ? "Timeline position is legal. Click to confirm " + _selectedCard.StableId.ToUpperInvariant() + "."
                : "Timeline position conflicts or falls outside the 12 x 3 grid.");
            return transition.IsPlacementValid;
        }

        public void ClearTimelinePreview()
        {
            if (_timelinePlacementPreview != null && !_playerCell.HasValue)
            {
                _timelinePlacementPreview.Clear();
            }
        }

        public ResolutionSnapshot ResolveTimeline()
        {
            EnsureBuilt();
            if (!_playerCell.HasValue)
            {
                throw new InvalidOperationException("Place the player action before resolving.");
            }

            var resolvedCardId = _selectedCard.StableId;
            _lastSnapshot = _timeline.Resolve(_state);
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
            _selectedCard = null;
            _selectedTargetId = null;
            _cardPlaySession = null;
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
        }

        private void BuildWorld()
        {
            var cameraObject = new GameObject("SliceCamera");
            cameraObject.transform.SetParent(_generatedRoot.transform, false);
            SceneCamera = cameraObject.AddComponent<Camera>();
            SceneCamera.orthographic = false;
            SceneCamera.fieldOfView = 38f;
            SceneCamera.clearFlags = CameraClearFlags.SolidColor;
            SceneCamera.backgroundColor = new Color(0.035f, 0.047f, 0.055f, 1f);
            SceneCamera.nearClipPlane = 0.1f;
            SceneCamera.farClipPlane = 100f;
            BoardCamera = cameraObject.AddComponent<BoardOrbitCameraController>();
            BoardCamera.Initialize(SceneCamera, new Vector3(0f, 0.45f, 0f), 32f, 48f, 15.5f);

            var lightObject = new GameObject("KeyLight");
            lightObject.transform.SetParent(_generatedRoot.transform, false);
            var keyLight = lightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.92f, 0.78f, 1f);
            keyLight.intensity = 1.4f;
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

            var fillObject = new GameObject("FillLight");
            fillObject.transform.SetParent(_generatedRoot.transform, false);
            var fillLight = fillObject.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.35f, 0.66f, 0.92f, 1f);
            fillLight.intensity = 0.65f;
            fillObject.transform.rotation = Quaternion.Euler(36f, 145f, 0f);

            _groundMaterial = CreateUnlitMaterial(new Color(0.055f, 0.07f, 0.065f, 1f));
            _grassTilePrefab = LoadTilePrefab("Art/Battle/Models/HexTile_Grass");
            _dirtTilePrefab = LoadTilePrefab("Art/Battle/Models/HexTile_Dirt");
            _centerAltarSprite = CreateOriginalSprite("Art/Battle/center_altar");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "BattlefieldGround";
            ground.transform.SetParent(_generatedRoot.transform, false);
            ground.transform.position = new Vector3(0f, -0.04f, 0f);
            ground.transform.localScale = new Vector3(2.1f, 1f, 2.1f);
            ground.GetComponent<Renderer>().sharedMaterial = _groundMaterial;

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

            _targetObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _targetObject.name = "Target-target-01";
            _targetObject.transform.SetParent(_columns[TargetCoordinate].OccupantAnchor, false);
            _targetObject.transform.localPosition = new Vector3(0f, 0.10f, 0f);
            _targetObject.transform.localScale = new Vector3(0.58f, 0.10f, 0.58f);
            _targetMaterial = CreateMaterial(new Color(0.86f, 0.24f, 0.24f, 1f));
            _targetRenderer = _targetObject.GetComponent<Renderer>();
            _targetRenderer.sharedMaterial = _targetMaterial;
            _targetObject.AddComponent<WorldTargetView>().Initialize(TargetId);

            var targetArt = new GameObject("OriginalArt-center_altar", typeof(SpriteRenderer), typeof(BoxCollider));
            targetArt.transform.SetParent(_columns[TargetCoordinate].OccupantAnchor, false);
            targetArt.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            targetArt.transform.localScale = Vector3.one * 0.62f;
            var targetSpriteRenderer = targetArt.GetComponent<SpriteRenderer>();
            targetSpriteRenderer.sprite = _centerAltarSprite;
            targetSpriteRenderer.color = Color.white;
            targetSpriteRenderer.sortingOrder = 100;
            targetSpriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            targetArt.GetComponent<BoxCollider>().size = new Vector3(2.25f, 2.25f, 0.16f);
            targetArt.AddComponent<WorldTargetView>().Initialize(TargetId);
            targetArt.AddComponent<CameraFacingBillboard>().Initialize(SceneCamera);
        }

        private void BuildInterface()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(_generatedRoot.transform, false);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            var canvasObject = new GameObject("SliceCanvas");
            canvasObject.transform.SetParent(_generatedRoot.transform, false);
            SceneCanvas = canvasObject.AddComponent<Canvas>();
            SceneCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            SceneCanvas.worldCamera = SceneCamera;
            SceneCanvas.planeDistance = 1f;
            canvasObject.AddComponent<GraphicRaycaster>();
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var header = CreatePanel(
                SceneCanvas.transform,
                "Header",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(24f, -104f),
                new Vector2(-24f, -24f),
                new Color(0.06f, 0.08f, 0.09f, 1f));
            CreateText(header.transform, "Title", "TIME KEY  /  COMBAT BOARD", 28, TextAnchor.MiddleLeft,
                new Vector2(22f, 8f), new Vector2(-22f, -38f));
            _statusText = CreateText(header.transform, "Status", string.Empty, 20, TextAnchor.MiddleLeft,
                new Vector2(22f, 42f), new Vector2(-22f, -8f));

            var timelinePanel = CreatePanel(
                SceneCanvas.transform,
                "Timeline",
                new Vector2(0.18f, 0.73f),
                new Vector2(0.82f, 0.90f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.07f, 0.09f, 0.10f, 1f));
            var grid = timelinePanel.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(20, 20, 22, 18);
            grid.cellSize = new Vector2(88f, 44f);
            grid.spacing = new Vector2(4f, 5f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = TimelineGrid.DefaultWidth;

            for (var row = 0; row < TimelineGrid.DefaultHeight; row++)
            {
                for (var column = 0; column < TimelineGrid.DefaultWidth; column++)
                {
                    var capturedColumn = column;
                    var capturedRow = row;
                    var button = CreateButton(
                        timelinePanel.transform,
                        string.Format("Slot-{0}-{1}", column, row),
                        string.Format("{0:00}", column + 1),
                        new Color(0.16f, 0.19f, 0.20f, 1f));
                    button.onClick.AddListener(() => TryPlaceSelected(capturedColumn, capturedRow));
                    AddTimelinePreviewEvents(button, capturedColumn, capturedRow);
                    _timelinePlacementPreview.Register(
                        new TimelineCell(column, row),
                        button.targetGraphic);
                    _timelineButtons.Add(button);
                }
            }

            _lightingCardSprite = LoadCardSprite(_lightingCard);
            _earthquakeCardSprite = LoadCardSprite(_earthquakeCard);
            _cardSprites.Add(_lightingCard.StableId, _lightingCardSprite);
            _cardSprites.Add(_earthquakeCard.StableId, _earthquakeCardSprite);
            var cardHandObject = new GameObject("CardHandHost", typeof(RectTransform));
            cardHandObject.transform.SetParent(SceneCanvas.transform, false);
            _cardHandHost = cardHandObject.AddComponent<CardHandHost>();
            RefreshHand(null, CardHandInteractionState.Idle);
            _cardHandView = _cardHandHost.GetCard(LightingCardId);
            _cardHandHost.CardSelected += stableId =>
            {
                SelectCard(stableId);
            };
            _cardHandHost.CardCancelRequested += HandleCardCancelRequested;
            _cardHandHost.CardDragChanged += HandleCardDragChanged;

            var detailPanel = CreatePanel(
                SceneCanvas.transform,
                "DetailPanel",
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-414f, 24f),
                new Vector2(-24f, 224f),
                new Color(0.08f, 0.10f, 0.11f, 0.96f));
            _targetText = CreateText(detailPanel.transform, "TargetStatus", "TARGET 01  |  HP 10 / 10", 20,
                TextAnchor.MiddleLeft, new Vector2(20f, 112f), new Vector2(-20f, -18f));
            CreateText(detailPanel.transform, "Intent", "ENEMY INTENT  /  SLOT 03-B", 18,
                TextAnchor.MiddleLeft, new Vector2(20f, 72f), new Vector2(-20f, -60f));
            _resolveButton = CreateButton(detailPanel.transform, "Resolve", "RESOLVE TIMELINE", new Color(0.70f, 0.25f, 0.20f, 1f));
            SetRect(_resolveButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero,
                new Vector2(20f, 18f), new Vector2(370f, 64f));
            _resolveButton.interactable = false;
            _resolveButton.onClick.AddListener(() => ResolveTimeline());
        }

        private void CreateTile(HexCoord coordinate, int elevation)
        {
            var tile = new GameObject(string.Format("Hex-{0}-{1}", coordinate.Q, coordinate.R));
            tile.transform.SetParent(_generatedRoot.transform, false);
            tile.transform.position = HexToWorld(coordinate, 0f);
            var logicalLayerCount = elevation + 1;
            var column = tile.AddComponent<HexTileColumn>();
            column.Initialize(_grassTilePrefab, _dirtTilePrefab, HexBlockHeight);
            column.ApplyLogicalLayerCount(logicalLayerCount);

            var tileView = tile.AddComponent<BoardTileView>();
            tileView.Initialize(coordinate, ToArray(column.Renderers));
            _tiles.Add(coordinate, tileView);
            _columns.Add(coordinate, column);
            _state.Board.AddTile(coordinate, logicalLayerCount);
            _boardRangePreview.Register(coordinate, tileView);
        }

        private void AddTimelinePreviewEvents(Button button, int column, int row)
        {
            var trigger = button.gameObject.AddComponent<EventTrigger>();
            trigger.triggers = new List<EventTrigger.Entry>();

            var enter = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter,
                callback = new EventTrigger.TriggerEvent()
            };
            enter.callback.AddListener(_ => PreviewTimelineSelected(column, row));
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit,
                callback = new EventTrigger.TriggerEvent()
            };
            exit.callback.AddListener(_ => ClearTimelinePreview());
            trigger.triggers.Add(exit);
        }

        private void HandleCardCancelRequested(string stableId)
        {
            if (_selectedCard != null &&
                string.Equals(stableId, _selectedCard.StableId, StringComparison.Ordinal))
            {
                CancelSelectedCard();
            }
        }

        private void HandleCardDragChanged(string stableId, Vector2 pointerPosition, CardDragPhase phase)
        {
            if (_selectedCard == null ||
                !string.Equals(stableId, _selectedCard.StableId, StringComparison.Ordinal))
            {
                return;
            }

            if (phase == CardDragPhase.Started || phase == CardDragPhase.Moved)
            {
                BoardCamera.InputEnabled = false;
                SetStatus(stableId.ToUpperInvariant() + " held. Choose a target and timeline position.");
            }
        }

        private void PlaceEnemyIntent()
        {
            var origin = new TimelineCell(2, 1);
            var intent = new TimelineAction(
                TimelineActorKind.Enemy,
                "enemy-intent",
                TargetId,
                origin,
                new[] { new TimelineCell(0, 0) },
                0);
            if (!_timeline.TryPlace(intent))
            {
                throw new InvalidOperationException("The frozen enemy intent could not be placed.");
            }

            SetTimelineCell(origin, "INTENT", new Color(0.78f, 0.27f, 0.25f, 1f));
        }

        private void SetTimelineCell(TimelineCell cell, string label, Color color)
        {
            var index = (cell.Y * TimelineGrid.DefaultWidth) + cell.X;
            var button = _timelineButtons[index];
            button.GetComponentInChildren<Text>().text = label;
            SetButtonColor(button, color);
        }

        private static Vector3 HexToWorld(HexCoord coordinate, float elevation)
        {
            const float radius = 1f;
            var x = 1.5f * radius * coordinate.Q;
            var z = Mathf.Sqrt(3f) * radius * (coordinate.R + (coordinate.Q / 2f));
            return new Vector3(x, elevation, z);
        }

        private static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                throw new InvalidOperationException("No compatible lit shader is available.");
            }

            var material = new Material(shader) { color = color };
            material.enableInstancing = true;
            return material;
        }

        private static Material CreateUnlitMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                throw new InvalidOperationException("No compatible unlit shader is available.");
            }

            var material = new Material(shader) { color = color };
            material.enableInstancing = true;
            return material;
        }

        private static Sprite CreateOriginalSprite(string resourcePath)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Original project art is missing: " + resourcePath);
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = texture.name + "-runtime-sprite";
            return sprite;
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

        private static GameObject LoadTilePrefab(string resourcePath)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Hex tile model is missing: " + resourcePath);
            }

            return prefab;
        }

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            panel.GetComponent<Image>().color = color;
            return rect;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.92f, 0.94f, 0.90f, 1f);
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, offsetMin, offsetMax);
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Color color)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            SetButtonColor(button, color);
            CreateText(buttonObject.transform, "Label", label, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void SetButtonColor(Button button, Color color)
        {
            button.targetGraphic.color = color;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.90f, 0.96f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.82f, 0.86f, 1f);
            colors.disabledColor = new Color(0.42f, 0.44f, 0.45f, 0.75f);
            button.colors = colors;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void SetStatus(string value)
        {
            _statusText.text = value;
        }

        private void EnsureBuilt()
        {
            if (_generatedRoot == null)
            {
                BuildSceneGraph();
            }
        }

        private void OnDestroy()
        {
            DestroyOwnedObject(_targetMaterial);
            DestroyOwnedObject(_groundMaterial);
            DestroyOwnedObject(_centerAltarSprite);
            DestroyOwnedObject(_lightingCardSprite);
            DestroyOwnedObject(_earthquakeCardSprite);
        }

        private static void DestroyOwnedObject(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
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
