using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.OverworldMovement;

namespace TimeKey.Presentation.OverworldMovement
{
    [DisallowMultipleComponent]
    public sealed class OverworldMovementPresenter : MonoBehaviour
    {
        [SerializeField] private OverworldNodeView[] nodeViews;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private RectTransform mapHost;
        [SerializeField] private Camera mapCamera;
        [SerializeField] private OverworldMapInputController mapInput;
        [SerializeField] private Text feedbackLabel;
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private Vector2 axialStep = new Vector2(180f, 160f);
        [SerializeField] private float staggerY = 80f;

        private OverworldMovementModel _model;
        private bool _bound;
        private bool _transitionLocked;
        private Coroutine _moveRoutine;
        private MoveTicket _activeTicket;
        private string _restoredCurrentNodeId;
        private readonly HashSet<string> _restoredSettledNodeIds =
            new HashSet<string>(StringComparer.Ordinal);

        public event Action<MapNodeId> ArrivalCommitted;
        public event Action<MoveFailureReason> MoveRejected;

        public OverworldMovementSnapshot Snapshot => _model == null ? null : _model.Snapshot();
        public bool IsMoving => _moveRoutine != null;
        public bool IsTransitionLocked => _transitionLocked;

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

        private void OnEnable()
        {
            Rebind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        public void Rebind()
        {
            Unbind();
            if (nodeViews == null || nodeViews.Length == 0)
            {
                return;
            }

            var domainNodes = new OverworldMapNode[nodeViews.Length];
            var current = nodeViews[0].Id;
            for (var index = 0; index < nodeViews.Length; index++)
            {
                var view = nodeViews[index];
                domainNodes[index] = new OverworldMapNode(
                    view.Id,
                    view.Coordinate,
                    view.NodeType);
                if (!string.IsNullOrWhiteSpace(_restoredCurrentNodeId) &&
                    view.Id.Value == _restoredCurrentNodeId)
                {
                    current = view.Id;
                }

                view.Clicked += OnNodeClicked;
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
            if (_model == null || IsMoving || _transitionLocked ||
                nodeId != _model.CurrentNodeId)
            {
                return false;
            }

            _model.MarkRoomPending();
            RefreshVisuals();
            return true;
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
            if (_model == null || IsMoving || _transitionLocked ||
                SceneInputLockState.IsLocked ||
                (mapInput != null && mapInput.ShouldSuppressClick))
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
                MoveRejected?.Invoke(result.Failure);
                SetFeedback(FailureText(result.Failure));
                RefreshVisuals();
                return false;
            }

            _activeTicket = result.Ticket;
            SetFeedback("移动中…");
            if (mapInput != null)
            {
                mapInput.SetInteractionLocked(true);
            }

            _moveRoutine = StartCoroutine(AnimateMove(result.Ticket));
            return true;
        }

        private IEnumerator AnimateMove(MoveTicket ticket)
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
            var cameraStart = mapCamera == null
                ? Vector3.zero
                : mapCamera.transform.position;
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

        private void RefreshVisuals()
        {
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
                else
                {
                    foreach (var node in snapshot.Nodes)
                    {
                        if (node.Id != view.Id)
                        {
                            continue;
                        }

                        state = node.IsSettled
                            ? OverworldNodeVisualState.Settled
                            : node.IsAvailable && !_transitionLocked
                                ? OverworldNodeVisualState.Available
                                : OverworldNodeVisualState.Locked;
                        break;
                    }
                }

                view.SetVisualState(state);
            }
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
            var view = Find(nodeId);
            if (view != null && playerMarker != null)
            {
                playerMarker.anchoredPosition = PositionFor(view.Coordinate);
            }
        }

        private OverworldNodeView Find(MapNodeId id)
        {
            foreach (var view in nodeViews)
            {
                if (view != null && view.Id == id)
                {
                    return view;
                }
            }

            return null;
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
