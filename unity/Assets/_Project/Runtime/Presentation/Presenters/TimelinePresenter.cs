using System;
using System.Collections.Generic;
using TimeKey.Application;
using TimeKey.Domain;
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

        private readonly Dictionary<TimelineCell, TimelineCellView> _cellsByCoordinate =
            new Dictionary<TimelineCell, TimelineCellView>();
        private bool _isBound;

        public event Action<TimelineCell> TimelineSelected;
        public event Action<TimelineCell> TimelinePreviewRequested;
        public event Action TimelinePreviewCleared;

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
            TimelinePreviewRequested?.Invoke(coordinate);
        }

        private void HandleTimelinePreviewCleared()
        {
            TimelinePreviewCleared?.Invoke();
        }
    }
}
