using System;
using System.Collections;
using System.Collections.Generic;
using TimeKey.Application.Overworld;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.Overworld;
using TimeKey.Domain.OverworldMovement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimeKey.Presentation.OverworldMovement
{
    [DisallowMultipleComponent]
    public sealed class OverworldMovementPresenter : MonoBehaviour
    {
        [SerializeField] private OverworldNodeView[] nodeViews;
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private RectTransform edgeHost;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private RectTransform mapHost;
        [SerializeField] private Camera mapCamera;
        [SerializeField] private OverworldMapInputController mapInput;
        [SerializeField] private Text feedbackLabel;
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private Vector2 axialStep = new Vector2(180f, 160f);
        [SerializeField] private float staggerY = 80f;
        [SerializeField] private Vector2 runtimeLayerStep = new Vector2(220f, 145f);
        [SerializeField] private float runtimeEdgeThickness = 6f;

        private readonly List<GameObject> _runtimeNodeObjects = new List<GameObject>();
        private readonly List<GameObject> _runtimeEdgeObjects = new List<GameObject>();
        private readonly Dictionary<MapNodeId, Vector2> _runtimePositions =
            new Dictionary<MapNodeId, Vector2>();

        private OverworldMovementModel _model;
        private OverworldMapProjection _authoritativeMap;
        private OverworldChapterSnapshot _authoritativeChapter;
        private bool _bound;
        private bool _transitionLocked;
        private Coroutine _moveRoutine;
        private MoveTicket _activeTicket;
        private string _restoredCurrentNodeId;
        private string _selectedNodeId;
        private string _confirmingNodeId;
        private string _movingNodeId;
        private readonly HashSet<string> _restoredSettledNodeIds =
            new HashSet<string>(StringComparer.Ordinal);

        public event Action<MapNodeId> ArrivalCommitted;
        public event Action<MoveFailureReason> MoveRejected;

        public OverworldMovementSnapshot Snapshot => _model == null ? null : _model.Snapshot();
        public OverworldChapterSnapshot AuthoritativeSnapshot => _authoritativeChapter;
        public bool IsAuthoritative => _authoritativeMap != null;
        public bool IsMoving => _moveRoutine != null;
        public bool IsTransitionLocked => _transitionLocked;
        public string SelectedNodeId => _selectedNodeId ?? string.Empty;

        public bool TryGetNodeType(MapNodeId nodeId, out MapNodeType nodeType)
        {
            var view = Find(nodeId);
            if (view == null)
            {
                nodeType = MapNodeType.Normal;
                return false;
            }

            nodeType = view.NodeType;
            return true;
        }

        public bool TryGetRoomType(MapNodeId nodeId, out OverworldRoomType roomType)
        {
            var view = Find(nodeId);
            if (view == null)
            {
                roomType = OverworldRoomType.Battle;
                return false;
            }

            roomType = view.RoomType;
            return true;
        }

        private void OnEnable()
        {
            Rebind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        public void BindAuthoritativeMap(
            OverworldMapProjection map,
            OverworldChapterSnapshot chapter)
        {
            _authoritativeMap = map ?? throw new ArgumentNullException(nameof(map));
            _authoritativeChapter = chapter ?? throw new ArgumentNullException(nameof(chapter));
            _selectedNodeId = null;
            _confirmingNodeId = null;
            _movingNodeId = null;
            BuildRuntimeGraph();
            Rebind();
            SetFeedback("选择相邻的可用节点");
            PlaceMarker(chapter.CurrentNodeId);
            RefreshVisuals();
        }

        public void ApplyAuthoritativeSnapshot(OverworldChapterSnapshot chapter)
        {
            if (_authoritativeMap == null)
            {
                throw new InvalidOperationException("No authoritative map is bound.");
            }

            _authoritativeChapter = chapter ?? throw new ArgumentNullException(nameof(chapter));
            _selectedNodeId = null;
            _confirmingNodeId = null;
            _movingNodeId = null;
            PlaceMarker(chapter.CurrentNodeId);
            SetFeedback(chapter.ChapterCompleted
                ? "本章节已完成"
                : "选择相邻的可用节点");
            RefreshVisuals();
        }

        public void Rebind()
        {
            Unbind();
            if (nodeViews == null || nodeViews.Length == 0)
            {
                return;
            }

            foreach (var view in nodeViews)
            {
                if (view != null)
                {
                    view.Clicked += OnNodeClicked;
                }
            }

            if (_authoritativeMap != null)
            {
                _model = null;
                _bound = true;
                RefreshVisuals();
                return;
            }

            var domainNodes = new TimeKey.Domain.OverworldMovement.OverworldMapNode[
                nodeViews.Length];
            var current = nodeViews[0].Id;
            for (var index = 0; index < nodeViews.Length; index++)
            {
                var view = nodeViews[index];
                domainNodes[index] = new TimeKey.Domain.OverworldMovement.OverworldMapNode(
                    view.Id,
                    view.Coordinate,
                    view.NodeType);
                if (!string.IsNullOrWhiteSpace(_restoredCurrentNodeId) &&
                    view.Id.Value == _restoredCurrentNodeId)
                {
                    current = view.Id;
                }
            }

            _model = new OverworldMovementModel(domainNodes, current);
            foreach (var settled in _restoredSettledNodeIds)
            {
                _model.MarkSettled(new MapNodeId(settled));
            }

            _bound = true;
            SetFeedback("选择相邻节点移动");
            RefreshVisuals();
            PlaceMarker(current);
        }

        public void SetTransitionLocked(bool locked)
        {
            _transitionLocked = locked;
            if (mapInput != null)
            {
                mapInput.SetInteractionLocked(locked || IsMoving);
            }

            RefreshVisuals();
        }

        public void ApplySceneFlowState(
            string currentNodeId,
            IEnumerable<string> settledNodeIds)
        {
            if (_authoritativeMap != null)
            {
                return;
            }

            _restoredCurrentNodeId = currentNodeId;
            _restoredSettledNodeIds.Clear();
            if (settledNodeIds != null)
            {
                foreach (var settled in settledNodeIds)
                {
                    if (!string.IsNullOrWhiteSpace(settled))
                    {
                        _restoredSettledNodeIds.Add(settled);
                    }
                }
            }

            if (isActiveAndEnabled)
            {
                Rebind();
            }
        }

        public bool RequestRoomPreparation(MapNodeId nodeId)
        {
            if (_authoritativeMap != null)
            {
                if (IsMoving || _transitionLocked ||
                    !string.Equals(_selectedNodeId, nodeId.Value, StringComparison.Ordinal))
                {
                    return false;
                }

                _confirmingNodeId = nodeId.Value;
                RefreshVisuals();
                return true;
            }

            if (_model == null || IsMoving || _transitionLocked ||
                nodeId != _model.CurrentNodeId)
            {
                return false;
            }

            _model.MarkRoomPending();
            RefreshVisuals();
            return true;
        }

        public void SetRoomInteractionState(
            MapNodeId nodeId,
            bool selected,
            bool confirming)
        {
            if (_authoritativeMap == null ||
                !string.Equals(_selectedNodeId, nodeId.Value, StringComparison.Ordinal))
            {
                return;
            }

            _confirmingNodeId = confirming ? nodeId.Value : null;
            if (!selected && !confirming)
            {
                CancelAuthoritativeSelection();
                return;
            }

            RefreshVisuals();
        }

        public bool CancelAuthoritativeSelection()
        {
            if (_authoritativeMap == null || string.IsNullOrEmpty(_selectedNodeId) || IsMoving)
            {
                return false;
            }

            var selectedView = Find(new MapNodeId(_selectedNodeId));
            _selectedNodeId = null;
            _confirmingNodeId = null;
            PlaceMarker(_authoritativeChapter.CurrentNodeId);
            SetFeedback("已取消节点选择");
            RefreshVisuals();
            if (EventSystem.current != null && selectedView != null)
            {
                EventSystem.current.SetSelectedGameObject(selectedView.gameObject);
            }

            return true;
        }

        public void FocusFirstAvailable()
        {
            if (EventSystem.current == null || nodeViews == null)
            {
                return;
            }

            foreach (var view in nodeViews)
            {
                if (view != null && view.State == OverworldNodeVisualState.Available)
                {
                    EventSystem.current.SetSelectedGameObject(view.gameObject);
                    return;
                }
            }
        }

        private void Unbind()
        {
            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
                _moveRoutine = null;
            }

            if (_activeTicket != null && _model != null)
            {
                _model.CancelOrFail(_activeTicket);
                _activeTicket = null;
            }

            if (!_bound || nodeViews == null)
            {
                return;
            }

            foreach (var view in nodeViews)
            {
                if (view != null)
                {
                    view.Clicked -= OnNodeClicked;
                }
            }

            _bound = false;
        }

        private void OnNodeClicked(OverworldNodeView view)
        {
            TryMove(view.Id);
        }

        public bool TryMove(MapNodeId nodeId)
        {
            if (IsMoving || _transitionLocked || SceneInputLockState.IsLocked ||
                (mapInput != null && mapInput.ShouldSuppressClick))
            {
                return false;
            }

            if (_authoritativeMap != null)
            {
                return TrySelectAuthoritative(nodeId);
            }

            if (_model == null)
            {
                return false;
            }

            var command = new MoveCommand(
                "ui-" + nodeId.Value + "-" + _model.Revision,
                _model.Revision,
                _model.CurrentNodeId,
                nodeId);
            var result = _model.RequestMove(command);
            if (!result.Succeeded)
            {
                Reject(result.Failure);
                return false;
            }

            _activeTicket = result.Ticket;
            SetFeedback("移动中...");
            if (mapInput != null)
            {
                mapInput.SetInteractionLocked(true);
            }

            _moveRoutine = StartCoroutine(AnimateLegacyMove(result.Ticket));
            return true;
        }

        private bool TrySelectAuthoritative(MapNodeId nodeId)
        {
            var node = FindAuthoritativeNode(nodeId);
            if (node == null)
            {
                Reject(MoveFailureReason.MissingNode);
                return false;
            }

            if (nodeId == _authoritativeChapter.CurrentNodeId)
            {
                Reject(MoveFailureReason.SameNode);
                return false;
            }

            if (!node.IsAvailable)
            {
                Reject(node.IsSettled
                    ? MoveFailureReason.Settled
                    : MoveFailureReason.Locked);
                return false;
            }

            _movingNodeId = nodeId.Value;
            _selectedNodeId = null;
            _confirmingNodeId = null;
            SetFeedback("移动至 " + OverworldNodeView.RoomTypeText(node.RoomType));
            if (mapInput != null)
            {
                mapInput.SetInteractionLocked(true);
            }

            _moveRoutine = StartCoroutine(AnimateAuthoritativeSelection(nodeId));
            RefreshVisuals();
            return true;
        }

        private IEnumerator AnimateAuthoritativeSelection(MapNodeId targetId)
        {
            var target = Find(targetId);
            if (target == null || playerMarker == null)
            {
                FinishAuthoritativeMoveFailure();
                yield break;
            }

            var start = playerMarker.anchoredPosition;
            var end = PositionFor(targetId);
            var elapsed = 0f;
            while (elapsed < Mathf.Max(0f, moveDuration))
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, moveDuration));
                playerMarker.anchoredPosition = Vector2.LerpUnclamped(
                    start,
                    end,
                    Mathf.Sin(t * Mathf.PI * 0.5f));
                yield return null;
            }

            playerMarker.anchoredPosition = end;
            _selectedNodeId = targetId.Value;
            _movingNodeId = null;
            _moveRoutine = null;
            if (mapInput != null)
            {
                mapInput.SetInteractionLocked(_transitionLocked);
            }

            SetFeedback("已选择 " + targetId.Value);
            RefreshVisuals();
            ArrivalCommitted?.Invoke(targetId);
        }

        private IEnumerator AnimateLegacyMove(MoveTicket ticket)
        {
            var source = Find(ticket.Command.SourceNodeId);
            var target = Find(ticket.Command.TargetNodeId);
            if (source == null || target == null || playerMarker == null)
            {
                _model.CancelOrFail(ticket);
                _activeTicket = null;
                _moveRoutine = null;
                RefreshVisuals();
                yield break;
            }

            var start = playerMarker.anchoredPosition;
            var end = PositionFor(target.Coordinate);
            var cameraStart = mapCamera == null ? Vector3.zero : mapCamera.transform.position;
            var cameraEnd = WorldPositionFor(target.Coordinate);
            var elapsed = 0f;
            while (elapsed < Mathf.Max(0f, moveDuration))
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, moveDuration));
                var eased = Mathf.Sin(t * Mathf.PI * 0.5f);
                playerMarker.anchoredPosition = Vector2.LerpUnclamped(start, end, eased);
                if (mapCamera != null)
                {
                    mapCamera.transform.position = Vector3.LerpUnclamped(
                        cameraStart,
                        cameraEnd,
                        eased);
                }

                yield return null;
            }

            playerMarker.anchoredPosition = end;
            if (mapCamera != null)
            {
                mapCamera.transform.position = cameraEnd;
            }

            _model.CommitArrival(ticket);
            _restoredCurrentNodeId = _model.CurrentNodeId.Value;
            _activeTicket = null;
            _moveRoutine = null;
            if (mapInput != null)
            {
                mapInput.SetInteractionLocked(_transitionLocked);
            }

            RefreshVisuals();
            SetFeedback("已到达 " + _model.CurrentNodeId.Value);
            ArrivalCommitted?.Invoke(_model.CurrentNodeId);
        }

        private void FinishAuthoritativeMoveFailure()
        {
            _movingNodeId = null;
            _moveRoutine = null;
            if (mapInput != null)
            {
                mapInput.SetInteractionLocked(_transitionLocked);
            }

            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (_authoritativeMap != null)
            {
                RefreshAuthoritativeVisuals();
                return;
            }

            if (_model == null || nodeViews == null)
            {
                return;
            }

            var snapshot = _model.Snapshot();
            foreach (var view in nodeViews)
            {
                var state = OverworldNodeVisualState.Locked;
                OverworldMapNodeSnapshot nodeSnapshot = null;
                foreach (var node in snapshot.Nodes)
                {
                    if (node.Id == view.Id)
                    {
                        nodeSnapshot = node;
                        break;
                    }
                }

                if (nodeSnapshot != null && nodeSnapshot.IsSettled)
                {
                    state = OverworldNodeVisualState.Settled;
                }
                else if (view.Id == snapshot.CurrentNodeId)
                {
                    state = snapshot.Phase == MovementPhase.Moving
                        ? OverworldNodeVisualState.Moving
                        : OverworldNodeVisualState.Current;
                }
                else if (nodeSnapshot != null)
                {
                    state = nodeSnapshot.IsAvailable && !_transitionLocked
                        ? OverworldNodeVisualState.Available
                        : OverworldNodeVisualState.Locked;
                }

                view.SetVisualState(state);
            }
        }

        private void RefreshAuthoritativeVisuals()
        {
            if (_authoritativeChapter == null || nodeViews == null)
            {
                return;
            }

            foreach (var view in nodeViews)
            {
                var node = FindAuthoritativeNode(view.Id);
                var state = OverworldNodeVisualState.Locked;
                if (node != null)
                {
                    if (view.Id == _authoritativeChapter.CurrentNodeId)
                    {
                        state = OverworldNodeVisualState.Current;
                    }
                    else if (node.IsSettled)
                    {
                        state = OverworldNodeVisualState.Settled;
                    }
                    else if (string.Equals(
                                 _confirmingNodeId,
                                 view.Id.Value,
                                 StringComparison.Ordinal))
                    {
                        state = OverworldNodeVisualState.Confirming;
                    }
                    else if (string.Equals(
                                 _movingNodeId,
                                 view.Id.Value,
                                 StringComparison.Ordinal))
                    {
                        state = OverworldNodeVisualState.Moving;
                    }
                    else if (string.Equals(
                                 _selectedNodeId,
                                 view.Id.Value,
                                 StringComparison.Ordinal))
                    {
                        state = OverworldNodeVisualState.Selected;
                    }
                    else if (node.IsAvailable && !_transitionLocked)
                    {
                        state = OverworldNodeVisualState.Available;
                    }
                    else if (node.IsVisited)
                    {
                        state = OverworldNodeVisualState.Visited;
                    }
                }

                view.SetVisualState(state);
            }
        }

        private void BuildRuntimeGraph()
        {
            Unbind();
            ClearRuntimeGraph();
            if (mapHost == null || edgeHost == null || nodePrefab == null)
            {
                throw new InvalidOperationException(
                    name + " is missing the runtime map host, edge host, or node prefab.");
            }

            var layerCounts = new Dictionary<int, int>();
            var layerIndices = new Dictionary<int, int>();
            var maximumLayer = 0;
            foreach (var node in _authoritativeMap.Nodes)
            {
                if (!layerCounts.ContainsKey(node.Layer))
                {
                    layerCounts.Add(node.Layer, 0);
                    layerIndices.Add(node.Layer, 0);
                }

                layerCounts[node.Layer]++;
                maximumLayer = Mathf.Max(maximumLayer, node.Layer);
            }

            var views = new List<OverworldNodeView>(_authoritativeMap.Nodes.Count);
            foreach (var node in _authoritativeMap.Nodes)
            {
                var index = layerIndices[node.Layer];
                layerIndices[node.Layer] = index + 1;
                var count = layerCounts[node.Layer];
                var position = new Vector2(
                    (node.Layer - maximumLayer * 0.5f) * runtimeLayerStep.x,
                    (index - (count - 1) * 0.5f) * runtimeLayerStep.y);
                var instance = Instantiate(nodePrefab, mapHost);
                instance.SetActive(true);
                var rect = instance.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = node.RoomType == OverworldRoomType.Boss
                    ? new Vector2(176f, 126f)
                    : new Vector2(156f, 112f);
                var view = instance.GetComponent<OverworldNodeView>();
                if (view == null)
                {
                    throw new InvalidOperationException("The overworld node prefab has no view.");
                }

                view.ConfigureRuntime(
                    node.Id,
                    node.Layer,
                    node.Slot,
                    node.RoomType,
                    OverworldNodeView.RoomTypeText(node.RoomType) + " " +
                    (node.Layer + 1) + "-" + (node.Slot + 1));
                var button = instance.GetComponent<Button>();
                if (button != null)
                {
                    var navigation = button.navigation;
                    navigation.mode = Navigation.Mode.Automatic;
                    button.navigation = navigation;
                }

                _runtimePositions.Add(node.Id, position);
                _runtimeNodeObjects.Add(instance);
                views.Add(view);
            }

            foreach (var edge in _authoritativeMap.Edges)
            {
                CreateRuntimeEdge(edge);
            }

            edgeHost.SetAsFirstSibling();
            if (playerMarker != null)
            {
                playerMarker.SetAsLastSibling();
            }

            nodeViews = views.ToArray();
            var width = Mathf.Max(1080f, (maximumLayer + 1) * runtimeLayerStep.x + 260f);
            mapHost.sizeDelta = new Vector2(width, 480f);
        }

        private void CreateRuntimeEdge(OverworldMapEdgeProjection edge)
        {
            if (!_runtimePositions.TryGetValue(edge.From, out var from) ||
                !_runtimePositions.TryGetValue(edge.To, out var to))
            {
                return;
            }

            var edgeObject = new GameObject(
                "Edge-" + edge.From.Value + "-" + edge.To.Value,
                typeof(RectTransform),
                typeof(Image));
            edgeObject.transform.SetParent(edgeHost, false);
            var rect = edgeObject.GetComponent<RectTransform>();
            var delta = to - from;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, runtimeEdgeThickness);
            rect.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            var image = edgeObject.GetComponent<Image>();
            image.color = new Color(0.42f, 0.82f, 0.76f, 0.58f);
            image.raycastTarget = false;
            _runtimeEdgeObjects.Add(edgeObject);
        }

        private void ClearRuntimeGraph()
        {
            foreach (var edge in _runtimeEdgeObjects)
            {
                if (edge != null)
                {
                    Destroy(edge);
                }
            }

            foreach (var node in _runtimeNodeObjects)
            {
                if (node != null)
                {
                    Destroy(node);
                }
            }

            _runtimeEdgeObjects.Clear();
            _runtimeNodeObjects.Clear();
            _runtimePositions.Clear();
            nodeViews = Array.Empty<OverworldNodeView>();
        }

        private OverworldNodeStateSnapshot FindAuthoritativeNode(MapNodeId nodeId)
        {
            if (_authoritativeChapter == null)
            {
                return null;
            }

            foreach (var node in _authoritativeChapter.Nodes)
            {
                if (node.Id == nodeId)
                {
                    return node;
                }
            }

            return null;
        }

        private void Reject(MoveFailureReason failure)
        {
            MoveRejected?.Invoke(failure);
            SetFeedback(FailureText(failure));
            RefreshVisuals();
        }

        private void SetFeedback(string value)
        {
            if (feedbackLabel != null)
            {
                feedbackLabel.text = value;
            }
        }

        private static string FailureText(MoveFailureReason reason)
        {
            switch (reason)
            {
                case MoveFailureReason.SameNode:
                    return "当前位置无需移动";
                case MoveFailureReason.NonAdjacent:
                    return "只能移动到相邻节点";
                case MoveFailureReason.Locked:
                    return "节点尚未解锁";
                case MoveFailureReason.Settled:
                    return "节点已经结算";
                case MoveFailureReason.Reentrant:
                    return "移动尚未结束";
                default:
                    return "当前无法移动";
            }
        }

        private void PlaceMarker(MapNodeId nodeId)
        {
            if (playerMarker == null)
            {
                return;
            }

            playerMarker.anchoredPosition = _authoritativeMap != null
                ? PositionFor(nodeId)
                : Find(nodeId) == null
                    ? playerMarker.anchoredPosition
                    : PositionFor(Find(nodeId).Coordinate);
        }

        private OverworldNodeView Find(MapNodeId id)
        {
            if (nodeViews == null)
            {
                return null;
            }

            foreach (var view in nodeViews)
            {
                if (view != null && view.Id == id)
                {
                    return view;
                }
            }

            return null;
        }

        private Vector2 PositionFor(MapNodeId nodeId)
        {
            return _runtimePositions.TryGetValue(nodeId, out var position)
                ? position
                : Vector2.zero;
        }

        private Vector2 PositionFor(AxialHexCoord coordinate)
        {
            return new Vector2(
                coordinate.Q * axialStep.x,
                coordinate.R * axialStep.y + coordinate.Q * staggerY);
        }

        private Vector3 WorldPositionFor(AxialHexCoord coordinate)
        {
            var local = PositionFor(coordinate);
            return mapHost == null ? local : mapHost.TransformPoint(local);
        }
    }
}
