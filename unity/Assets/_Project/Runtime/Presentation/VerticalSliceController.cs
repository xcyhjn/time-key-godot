using System;
using System.Collections;
using System.Collections.Generic;
using TimeKey.Application;
using TimeKey.Application.BattleFlow;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain;
using TimeKey.Domain.BattleFlow;
using TimeKey.Presentation.Bindings;
using TimeKey.Presentation.Cards;
using TimeKey.Presentation.Localization;
using TimeKey.Presentation.Occupants;
using TimeKey.Presentation.Targeting;
using TimeKey.Presentation.Terrain;
using TimeKey.Presentation.Interaction;
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
        private CardHandHost _cardHandHost;
        private BoardRangePreview _boardRangePreview;
        private TimelinePlacementPreview _timelinePlacementPreview;
        private bool _initialized;
        private bool _viewsBound;
        private long _settlementCommandSequence = 1;
        private readonly RightClickGesturePolicy _rightClickPolicy = new RightClickGesturePolicy(8f);
        private Vector2 _lastInspectScreenPoint;
        private float _lastInspectYaw;
        private bool _hasLastInspectScreenPoint;

        public int CurrentTargetHp => _state == null ? 0 : _state.TargetHp;

        public bool EnemyIntentResolved => _lastSnapshot != null && _lastSnapshot.EnemyIntentResolved;

        public int TimelineSlotCount => presentationBinding == null
            ? 0
            : presentationBinding.TimelineCellCount;

        public int BoardTileCount => _tiles.Count;

        public int TimelineOccupiedCellCount => _timeline == null ? 0 : _timeline.OccupiedCellCount;

        public IReadOnlyList<TimelineActionPresentationSnapshot> TimelineActions =>
            _applicationSession == null
                ? Array.Empty<TimelineActionPresentationSnapshot>()
                : _applicationSession.Current.TimelineActions;

        public HexCoord? SelectedTile => _selectedTile == null ? (HexCoord?)null : _selectedTile.Coordinate;

        public Vector3 TargetWorldPosition => _targetObject == null ? Vector3.zero : _targetObject.transform.position;

        public Camera SceneCamera => sceneCamera;

        public Canvas SceneCanvas => sceneCanvas;

        public BoardOrbitCameraController BoardCamera => boardCamera;

        public CardHandView CardHand
        {
            get
            {
                if (_cardHandHost == null)
                {
                    return null;
                }

                var selected = _cardHandHost.GetCard(_cardHandHost.SelectedViewId);
                return selected != null
                    ? selected
                    : _cardHandHost.Cards.Count > 0
                        ? _cardHandHost.Cards[0]
                        : null;
            }
        }

        public CardHandHost CardHandHost => _cardHandHost;

        public string SelectedCardId =>
            _applicationSession == null ? null : _applicationSession.Current.SelectedStableId;

        public BattleFlowPresentationSnapshot BattleFlow =>
            _applicationSession == null ? null : _applicationSession.BattleFlowCurrent;

        public BattleFlowHookResult LastBattleFlowResult =>
            _applicationSession == null ? null : _applicationSession.LastBattleFlowResult;

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
            ClearInspectedTile();
            _rightClickPolicy.Cancel();
            UnbindViews();
        }

        private void Update()
        {
            if (SceneInputLockState.IsLocked)
            {
                _rightClickPolicy.Cancel();
                ClearInspectedTile();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_applicationSession != null && _applicationSession.Current.SelectedCard != null)
                {
                    CancelSelectedCard();
                }
                else
                {
                    ClearInspectedTile();
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                _rightClickPolicy.Begin(
                    new PointerPoint(Input.mousePosition.x, Input.mousePosition.y),
                    IsScreenPointOverInterface(Input.mousePosition));
            }

            if (Input.GetMouseButton(1))
            {
                _rightClickPolicy.Move(
                    new PointerPoint(Input.mousePosition.x, Input.mousePosition.y));
            }

            if (Input.GetMouseButtonUp(1))
            {
                var gesture = _rightClickPolicy.End(
                    new PointerPoint(Input.mousePosition.x, Input.mousePosition.y),
                    IsScreenPointOverInterface(Input.mousePosition));
                if (gesture == RightClickGestureResult.ShortClick)
                {
                    ClearInspectedTile();
                }
            }

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
                if (BattleFlow == null ||
                    BattleFlow.Era != 1 ||
                    BattleFlow.Phase != 1 ||
                    BattleFlow.Timecoins != 0 ||
                    BattleFlow.DrawPile.Count != 7 ||
                    BattleFlow.Hand.Count != 5 ||
                    BattleFlow.DiscardPile.Count != 0)
                {
                    throw new InvalidOperationException(
                        "The Player smoke path did not start from the frozen deck and round state.");
                }

                var firstTurn = SelectCard("recover") &&
                    SelectTarget(TargetId) &&
                    TryPlaceSelected(0, 0);
                if (!firstTurn || TimelineOccupiedCellCount != 5 || ResolveTimeline() == null)
                {
                    throw new InvalidOperationException(
                        "The Player smoke path could not advance to the second frozen hand.");
                }

                if (BattleFlow.Phase != 2 ||
                    BattleFlow.Timecoins != 31 ||
                    BattleFlow.DrawPile.Count != 2 ||
                    BattleFlow.Hand.Count != 5 ||
                    BattleFlow.DiscardPile.Count != 5 ||
                    LastBattleFlowResult == null ||
                    LastBattleFlowResult.DrawResult == null ||
                    LastBattleFlowResult.DrawResult.Shuffle.Occurred)
                {
                    throw new InvalidOperationException(
                        "The Player smoke path did not complete the first formal turn exactly once.");
                }

                var arranged = SelectCard(LightingCardId) &&
                    SelectTarget(TargetId);
                var committedInstanceId = SelectedCardViewId;
                arranged = arranged && TryPlaceSelected(0, 0);
                var actionSnapshotSurvived = false;
                for (var index = 0; index < TimelineActions.Count; index++)
                {
                    var action = TimelineActions[index];
                    if (action.ActorKind == TimelineActorKind.Player &&
                        string.Equals(
                            action.CardStableId,
                            LightingCardId,
                            StringComparison.Ordinal) &&
                        action.Display != null &&
                        !string.IsNullOrWhiteSpace(action.Display.Title))
                    {
                        actionSnapshotSurvived = true;
                        break;
                    }
                }

                var committedInstanceLeftHand = true;
                for (var index = 0; index < BattleFlow.Hand.Count; index++)
                {
                    if (string.Equals(
                            BattleFlow.Hand[index].InstanceId.ToString(),
                            committedInstanceId,
                            StringComparison.Ordinal))
                    {
                        committedInstanceLeftHand = false;
                        break;
                    }
                }

                if (!arranged ||
                    string.IsNullOrWhiteSpace(committedInstanceId) ||
                    !committedInstanceLeftHand ||
                    !actionSnapshotSurvived ||
                    TimelineOccupiedCellCount != 3 ||
                    BattleFlow.Hand.Count != 4 ||
                    BattleFlow.DiscardPile.Count != 6)
                {
                    throw new InvalidOperationException(
                        "The Player smoke path did not preserve action display identity after the card instance left hand.");
                }

                var snapshot = arranged ? ResolveTimeline() : null;
                if (snapshot == null ||
                    snapshot.TargetHpAfter != 0 ||
                    snapshot.EnemyIntentResolved ||
                    BattleFlow.Phase != 3 ||
                    BattleFlow.Timecoins != 64 ||
                    BattleFlow.DrawPile.Count != 7 ||
                    BattleFlow.Hand.Count != 5 ||
                    BattleFlow.DiscardPile.Count != 0 ||
                    LastBattleFlowResult == null ||
                    LastBattleFlowResult.DrawResult == null ||
                    !LastBattleFlowResult.DrawResult.Shuffle.Occurred ||
                    BattleFlow.Settlement.Outcome != BattleOutcome.VictorySettlement ||
                    !BattleFlow.IsInputLocked ||
                    BattleFlow.Settlement.RewardEntry == null)
                {
                    throw new InvalidOperationException("The Player smoke path did not satisfy the frozen slice contract.");
                }

                var oppositeOutcome = ResolveBattleOutcome(BattleOutcome.Defeat);
                if (oppositeOutcome.Succeeded ||
                    oppositeOutcome.Failure != BattleSettlementFailure.OutcomeConflict)
                {
                    throw new InvalidOperationException(
                        "The Player smoke path did not reject the opposite terminal outcome.");
                }

                var boundary = CreateBattleReturnBoundary();
                if (!boundary.Succeeded ||
                    boundary.Payload.Outcome != BattleOutcome.VictorySettlement ||
                    boundary.Payload.Completion != BattleReturnCompletion.VictoryCompleted ||
                    boundary.Payload.Era != 1 ||
                    boundary.Payload.Phase != 3 ||
                    boundary.Payload.Timecoins != 64 ||
                    boundary.Payload.DeckStableIds.Count != 12 ||
                    !string.Equals(
                        boundary.Payload.BattleTag,
                        "combat-vertical-slice",
                        StringComparison.Ordinal) ||
                    boundary.Payload.BattleSeed != FixtureSeed)
                {
                    throw new InvalidOperationException(
                        "The Player smoke path did not produce the typed battle return boundary.");
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

            ClearInspectedTile();
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

            ClearInspectedTile();
            BoardCamera.InputEnabled = true;
            RefreshPresentation();
            return true;
        }

        public BattleSettlementResult ResolveBattleOutcome(BattleOutcome outcome)
        {
            EnsureBuilt();
            var result = _applicationSession.TryResolveBattleOutcome(
                checked(_settlementCommandSequence++),
                outcome);
            RefreshPresentation();
            return result;
        }

        public BattleReturnResult CreateBattleReturnBoundary()
        {
            EnsureBuilt();
            return _applicationSession.TryCreateBattleReturnBoundary();
        }

        public bool SelectTile(HexCoord coordinate)
        {
            EnsureBuilt();
            if (SceneInputLockState.IsLocked ||
                !_tiles.TryGetValue(coordinate, out var tile))
            {
                return false;
            }

            if (_applicationSession.Current.RequiredTargetKind == CombatTargetKind.Tile)
            {
                return SelectEarthquakeTarget(coordinate);
            }

            if (_applicationSession.Current.SelectedCard != null ||
                (_applicationSession.Current.Phase != CombatSessionPhase.Idle &&
                 _applicationSession.Current.Phase != CombatSessionPhase.Cancelled))
            {
                return false;
            }

            if (_selectedTile != null)
            {
                _selectedTile.SetSelected(false);
            }

            _selectedTile = tile;
            _selectedTile.SetSelected(true);
            presentationBinding.SetInspectedTile(coordinate);
            SetStatus(CombatChineseText.TileSelected(coordinate));
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
                ClearInspectedTile();
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

            if (selectedTile == null)
            {
                ClearInspectedTile();
                return false;
            }

            if (_selectedTile != null && _selectedTile.Coordinate.Equals(selectedTile.Coordinate))
            {
                var moved = !_hasLastInspectScreenPoint ||
                    (screenPoint - _lastInspectScreenPoint).sqrMagnitude > 16f ||
                    Mathf.Abs(Mathf.DeltaAngle(boardCamera.Yaw, _lastInspectYaw)) > 0.5f;
                if (!moved)
                {
                    ClearInspectedTile();
                    return true;
                }
            }

            var selected = SelectTile(selectedTile.Coordinate);
            if (selected)
            {
                _lastInspectScreenPoint = screenPoint;
                _lastInspectYaw = boardCamera.Yaw;
                _hasLastInspectScreenPoint = true;
            }

            return selected;
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
                    SetStatus(CombatChineseText.ClearFailed(card.StableId));
                    return false;
                }

                presentationBinding.ApplyTimelineClearResult(clearCommit.ClearResult);
                ClearInspectedTile();
                SetHandCardsActive(true);
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
                SetStatus(CombatChineseText.CommitFailed(card.StableId));
                return false;
            }

            SetHandCardsActive(true);
            ClearInspectedTile();
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

            if (_applicationSession != null &&
                _applicationSession.Current.Phase == CombatSessionPhase.TimelinePreview)
            {
                var cancel = _applicationSession.CancelCard();
                if (cancel.Succeeded)
                {
                    ClearInspectedTile();
                    SetHandCardsActive(true);
                    BoardCamera.InputEnabled = true;
                    RefreshPresentation();
                }
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
            ClearInspectedTile();
            ApplyTileEffects(_lastSnapshot);
            presentationBinding.ApplyOccupantEffects(_lastSnapshot.OccupantEffectResults);
            presentationBinding.ApplyLifecycleChanges(
                _applicationSession.LastLifecycleChanges);
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
            if (_state.Board.TryGetTile(coordinate, out var existingTile))
            {
                if (existingTile.LogicalLayerCount != logicalLayerCount)
                {
                    throw new InvalidOperationException(
                        "The authored board height does not match the lifecycle board snapshot.");
                }
            }
            else
            {
                _state.Board.AddTile(coordinate, logicalLayerCount);
            }
            _boardRangePreview.Register(coordinate, tileView);
            presentationBinding.RegisterOccupantColumn(coordinate, column);
        }

        private void HandleCardCancelRequested(string stableId)
        {
            if (SceneInputLockState.IsLocked)
            {
                return;
            }

            var selectedViewId = SelectedCardViewId;
            if (selectedViewId != null &&
                string.Equals(stableId, selectedViewId, StringComparison.Ordinal))
            {
                CancelSelectedCard();
            }
        }

        private void HandleCardDragChanged(string stableId, Vector2 pointerPosition, CardDragPhase phase)
        {
            if (SceneInputLockState.IsLocked)
            {
                return;
            }

            var selectedViewId = SelectedCardViewId;
            if (selectedViewId == null ||
                !string.Equals(stableId, selectedViewId, StringComparison.Ordinal))
            {
                return;
            }

            if (phase == CardDragPhase.Started || phase == CardDragPhase.Moved)
            {
                BoardCamera.InputEnabled = false;
                SetStatus(CombatChineseText.CardHeld(SelectedCardId));
            }
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
            return CombatChineseText.GetTimelineLabel(card.StableId);
        }

        private void SetHandCardsActive(bool active)
        {
            for (var index = 0; index < _cardHandHost.Cards.Count; index++)
            {
                _cardHandHost.Cards[index].gameObject.SetActive(true);
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
            presentationBinding.BattleRewardRequested += HandleBattleRewardRequested;

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
            presentationBinding.BattleRewardRequested -= HandleBattleRewardRequested;
            presentationBinding.Unbind();

            _viewsBound = false;
        }

        private void ClearInspectedTile()
        {
            if (_selectedTile != null)
            {
                _selectedTile.SetSelected(false);
                _selectedTile = null;
            }

            _hasLastInspectScreenPoint = false;

            if (presentationBinding != null)
            {
                presentationBinding.ClearInspectedTile();
            }
        }

        private void HandleCardSelected(string stableId)
        {
            if (SceneInputLockState.IsLocked)
            {
                return;
            }

            SelectCard(stableId);
        }

        private void HandleTimelineClicked(TimelineCell coordinate)
        {
            if (SceneInputLockState.IsLocked)
            {
                return;
            }

            TryPlaceSelected(coordinate.X, coordinate.Y);
        }

        private void HandleTimelinePointerEntered(TimelineCell coordinate)
        {
            if (SceneInputLockState.IsLocked)
            {
                return;
            }

            PreviewTimelineSelected(coordinate.X, coordinate.Y);
        }

        private void HandleTimelinePointerExited()
        {
            if (SceneInputLockState.IsLocked)
            {
                return;
            }

            ClearTimelinePreview();
        }

        private void HandleResolveClicked()
        {
            if (SceneInputLockState.IsLocked)
            {
                return;
            }

            ResolveTimeline();
        }

        private void HandleBattleRewardRequested(BattleRewardEntry rewardEntry)
        {
            if (SceneInputLockState.IsLocked)
            {
                return;
            }

            var currentReward = BattleFlow == null
                ? null
                : BattleFlow.Settlement.RewardEntry;
            if (currentReward == null || rewardEntry == null ||
                !string.Equals(
                    currentReward.EntryStableId,
                    rewardEntry.EntryStableId,
                    StringComparison.Ordinal))
            {
                return;
            }

            _applicationSession.TryClaimBattleReward(
                checked(_settlementCommandSequence++));
            RefreshPresentation();
        }

        private string SelectedCardViewId
        {
            get
            {
                if (_applicationSession == null)
                {
                    return null;
                }

                var current = _applicationSession.Current;
                return current.SelectedCardInstanceId.HasValue
                    ? current.SelectedCardInstanceId.Value.ToString()
                    : current.SelectedStableId;
            }
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
