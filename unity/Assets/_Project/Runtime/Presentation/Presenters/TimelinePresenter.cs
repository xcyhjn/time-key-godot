using System;
using System.Collections.Generic;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Presentation.Actions;
using TimeKey.Presentation.Targeting;
using UnityEngine;

namespace TimeKey.Presentation.Presenters
{
    [DisallowMultipleComponent]
    public sealed class TimelinePresenter : MonoBehaviour
    {
        [SerializeField] private TimelinePlacementPreview timelinePreview = null;
        [SerializeField] private ClearTimelinePreview clearTimelinePreview = null;
        [SerializeField] private List<TimelineCellView> timelineCells =
            new List<TimelineCellView>();
        [SerializeField] private RectTransform actionLayer = null;
        [SerializeField] private TimelineActionFrame actionFramePrefab = null;

        private readonly Dictionary<TimelineCell, TimelineCellView> _cellsByCoordinate =
            new Dictionary<TimelineCell, TimelineCellView>();
        private readonly Dictionary<TimelineActionIdentity, TimelineActionFrame> _framesByActionId =
            new Dictionary<TimelineActionIdentity, TimelineActionFrame>();
        private readonly Dictionary<TimelineCell, TimelineActionIdentity> _actionIdsByCell =
            new Dictionary<TimelineCell, TimelineActionIdentity>();
        private readonly Dictionary<TimelineActionIdentity, TimelineActionPresentationSnapshot>
            _snapshotsByActionId =
                new Dictionary<TimelineActionIdentity, TimelineActionPresentationSnapshot>();
        private TimelineActionIdentity? _highlightedActionId;
        private bool _isBound;

        public event Action<TimelineCell> TimelineSelected;
        public event Action<TimelineCell> TimelinePreviewRequested;
        public event Action TimelinePreviewCleared;
        public event Action<TimelineActionPresentationSnapshot, bool> ActionHovered;

        public bool IsBound => _isBound;

        public int CellCount => timelineCells == null ? 0 : timelineCells.Count;

        public void Bind()
        {
            ValidateDependencies();
            if (_isBound)
            {
                return;
            }

            _cellsByCoordinate.Clear();
            for (var index = 0; index < timelineCells.Count; index++)
            {
                var cell = timelineCells[index];
                _cellsByCoordinate.Add(cell.Coordinate, cell);
                timelinePreview.Register(cell.Coordinate, cell.Graphic);
                clearTimelinePreview.Register(cell.Coordinate, cell);
                cell.Clicked += HandleTimelineSelected;
                cell.PointerEntered += HandleTimelinePreviewRequested;
                cell.PointerExited += HandleTimelinePreviewCleared;
            }

            _isBound = true;
        }

        public void Unbind()
        {
            if (!_isBound)
            {
                return;
            }

            for (var index = 0; index < timelineCells.Count; index++)
            {
                var cell = timelineCells[index];
                if (cell == null)
                {
                    continue;
                }

                cell.Clicked -= HandleTimelineSelected;
                cell.PointerEntered -= HandleTimelinePreviewRequested;
                cell.PointerExited -= HandleTimelinePreviewCleared;
            }

            _cellsByCoordinate.Clear();
            _isBound = false;
        }

        public void Refresh(CombatSessionView state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            ValidateDependencies();
            RenderScheduledActions(state.TimelineActions);
            if (state.InteractionMode == CombatInteractionMode.TimelineClear &&
                state.ClearPreview != null)
            {
                timelinePreview.Clear();
                clearTimelinePreview.Show(state.ClearPreview);
                return;
            }

            clearTimelinePreview.Clear();
            if (state.Phase != CombatSessionPhase.TimelinePreview ||
                state.SelectedCard == null ||
                !state.TimelineOrigin.HasValue)
            {
                timelinePreview.Clear();
                return;
            }

            timelinePreview.Show(
                state.TimelineOrigin.Value,
                state.SelectedCard.Shape,
                state.IsPlacementValid);
        }

        public void RenderAction(
            TimelineCell origin,
            IReadOnlyList<TimelineCell> shape,
            string label,
            Color color)
        {
            if (shape == null)
            {
                throw new ArgumentNullException(nameof(shape));
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("A timeline label is required.", nameof(label));
            }

            if (!_isBound)
            {
                Bind();
            }

            for (var index = 0; index < shape.Count; index++)
            {
                var coordinate = origin + shape[index];
                TimelineCellView cell;
                if (!_cellsByCoordinate.TryGetValue(coordinate, out cell))
                {
                    throw new InvalidOperationException(
                        "No serialized timeline cell exists for " + coordinate + ".");
                }

                cell.SetContent(label, color);
            }
        }

        public void ClearPreview()
        {
            ValidateDependencies();
            timelinePreview.Clear();
            clearTimelinePreview.Clear();
        }

        public void ApplyClearResult(TimelineClearResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!result.Succeeded)
            {
                return;
            }

            if (!_isBound)
            {
                Bind();
            }

            ClearPreview();
            for (var actionIndex = 0; actionIndex < result.RemovedActions.Count; actionIndex++)
            {
                var action = result.RemovedActions[actionIndex];
                RemoveActionFrame(action.ActionId);
                for (var cellIndex = 0; cellIndex < action.OccupiedCells.Count; cellIndex++)
                {
                    if (_cellsByCoordinate.TryGetValue(action.OccupiedCells[cellIndex], out var cell))
                    {
                        cell.ClearContent();
                    }
                }
            }
        }

        private void OnEnable()
        {
            if (timelinePreview != null &&
                clearTimelinePreview != null &&
                timelineCells != null &&
                timelineCells.Count > 0)
            {
                Bind();
            }
        }

        private void OnDisable()
        {
            Unbind();
            if (timelinePreview != null)
            {
                timelinePreview.Clear();
            }

            if (clearTimelinePreview != null)
            {
                clearTimelinePreview.Clear();
            }

            ClearActionFrames();
        }

        private void ValidateDependencies()
        {
            if (timelinePreview == null)
            {
                throw new InvalidOperationException(
                    name + " is missing serialized timelinePreview reference.");
            }

            if (clearTimelinePreview == null)
            {
                throw new InvalidOperationException(
                    name + " is missing serialized clearTimelinePreview reference.");
            }

            if (timelineCells == null || timelineCells.Count == 0)
            {
                throw new InvalidOperationException(
                    name + " is missing serialized timelineCells references.");
            }

            if (actionLayer == null || actionFramePrefab == null)
            {
                throw new InvalidOperationException(
                    name + " is missing serialized action-frame references.");
            }

            for (var index = 0; index < timelineCells.Count; index++)
            {
                if (timelineCells[index] == null)
                {
                    throw new InvalidOperationException(
                        name + " contains a null serialized timelineCells reference.");
                }
            }

            var coordinates = new HashSet<TimelineCell>();
            for (var index = 0; index < timelineCells.Count; index++)
            {
                if (!coordinates.Add(timelineCells[index].Coordinate))
                {
                    throw new InvalidOperationException(
                        name + " contains duplicate timeline coordinate " +
                        timelineCells[index].Coordinate + ".");
                }
            }
        }

        private void HandleTimelineSelected(TimelineCell coordinate)
        {
            TimelineSelected?.Invoke(coordinate);
        }

        private void HandleTimelinePreviewRequested(TimelineCell coordinate)
        {
            if (_actionIdsByCell.TryGetValue(coordinate, out var actionId) &&
                _snapshotsByActionId.TryGetValue(actionId, out var snapshot))
            {
                HighlightAction(actionId, true);
                ActionHovered?.Invoke(snapshot, true);
                return;
            }

            TimelinePreviewRequested?.Invoke(coordinate);
        }

        private void HandleTimelinePreviewCleared()
        {
            if (_highlightedActionId.HasValue &&
                _snapshotsByActionId.TryGetValue(_highlightedActionId.Value, out var snapshot))
            {
                HighlightAction(_highlightedActionId.Value, false);
                ActionHovered?.Invoke(snapshot, false);
            }

            TimelinePreviewCleared?.Invoke();
        }

        public void HighlightAction(TimelineActionIdentity actionId, bool highlighted)
        {
            if (_highlightedActionId.HasValue &&
                _framesByActionId.TryGetValue(_highlightedActionId.Value, out var previous))
            {
                previous.SetHighlighted(false);
            }

            _highlightedActionId = highlighted ? actionId : (TimelineActionIdentity?)null;
            if (_framesByActionId.TryGetValue(actionId, out var frame))
            {
                frame.SetHighlighted(highlighted);
            }
        }

        public void HighlightCardActions(string stableId, bool highlighted)
        {
            foreach (var pair in _snapshotsByActionId)
            {
                if (string.Equals(pair.Value.CardStableId, stableId, StringComparison.Ordinal))
                {
                    HighlightAction(pair.Key, highlighted);
                }
            }
        }

        public void RemoveActionsBySource(string sourceRuntimeId)
        {
            if (string.IsNullOrWhiteSpace(sourceRuntimeId))
            {
                return;
            }

            var actionIds = new List<TimelineActionIdentity>();
            foreach (var pair in _snapshotsByActionId)
            {
                if (string.Equals(
                        pair.Value.SourceId,
                        sourceRuntimeId,
                        StringComparison.Ordinal))
                {
                    actionIds.Add(pair.Key);
                }
            }

            for (var index = 0; index < actionIds.Count; index++)
            {
                RemoveActionFrame(actionIds[index]);
            }
        }

        private void RenderScheduledActions(
            IReadOnlyList<TimelineActionPresentationSnapshot> snapshots)
        {
            if (!_isBound)
            {
                Bind();
            }

            for (var index = 0; index < timelineCells.Count; index++)
            {
                timelineCells[index].ClearContent();
            }

            _actionIdsByCell.Clear();
            _snapshotsByActionId.Clear();
            var retained = new HashSet<TimelineActionIdentity>();
            for (var index = 0; index < snapshots.Count; index++)
            {
                var snapshot = snapshots[index];
                retained.Add(snapshot.ActionId);
                _snapshotsByActionId[snapshot.ActionId] = snapshot;
                for (var cellIndex = 0; cellIndex < snapshot.OccupiedCells.Count; cellIndex++)
                {
                    var coordinate = snapshot.OccupiedCells[cellIndex];
                    _actionIdsByCell[coordinate] = snapshot.ActionId;
                    if (_cellsByCoordinate.TryGetValue(coordinate, out var cell))
                    {
                        cell.SetContent(
                            snapshot.Display.Title,
                            snapshot.ActorKind == TimelineActorKind.Enemy
                                ? new Color(0.48f, 0.13f, 0.14f, 1f)
                                : new Color(0.06f, 0.42f, 0.43f, 1f));
                    }
                }

                if (!_framesByActionId.TryGetValue(snapshot.ActionId, out var frame))
                {
                    frame = Instantiate(actionFramePrefab, actionLayer, false);
                    frame.name = "Action-" + snapshot.ActionId.Value.Replace('/', '-').Replace(':', '-');
                    _framesByActionId.Add(snapshot.ActionId, frame);
                }

                frame.Apply(snapshot, _cellsByCoordinate, actionLayer);
            }

            var staleIds = new List<TimelineActionIdentity>();
            foreach (var pair in _framesByActionId)
            {
                if (!retained.Contains(pair.Key))
                {
                    staleIds.Add(pair.Key);
                }
            }

            for (var index = 0; index < staleIds.Count; index++)
            {
                RemoveActionFrame(staleIds[index]);
            }

            actionLayer.SetAsLastSibling();
        }

        private void RemoveActionFrame(TimelineActionIdentity actionId)
        {
            if (_framesByActionId.TryGetValue(actionId, out var frame))
            {
                _framesByActionId.Remove(actionId);
                if (frame != null)
                {
                    DestroyActionFrame(frame);
                }
            }

            _snapshotsByActionId.Remove(actionId);
            var staleCells = new List<TimelineCell>();
            foreach (var pair in _actionIdsByCell)
            {
                if (pair.Value == actionId)
                {
                    staleCells.Add(pair.Key);
                }
            }

            for (var index = 0; index < staleCells.Count; index++)
            {
                _actionIdsByCell.Remove(staleCells[index]);
            }
        }

        private void ClearActionFrames()
        {
            foreach (var frame in _framesByActionId.Values)
            {
                if (frame != null)
                {
                    DestroyActionFrame(frame);
                }
            }

            _framesByActionId.Clear();
            _snapshotsByActionId.Clear();
            _actionIdsByCell.Clear();
            _highlightedActionId = null;
        }

        private static void DestroyActionFrame(TimelineActionFrame frame)
        {
            frame.gameObject.SetActive(false);
            if (UnityEngine.Application.isPlaying)
            {
                Destroy(frame.gameObject);
            }
            else
            {
                DestroyImmediate(frame.gameObject);
            }
        }
    }
}
