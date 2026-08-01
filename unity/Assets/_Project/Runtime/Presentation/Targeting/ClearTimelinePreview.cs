using System;
using System.Collections.Generic;
using TimeKey.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Presentation.Targeting
{
    [DisallowMultipleComponent]
    public sealed class ClearTimelinePreview : MonoBehaviour
    {
        private static readonly Color EmptyColor = new Color(0.34f, 0.78f, 0.96f, 1f);
        private static readonly Color OccupiedColor = new Color(0.24f, 0.90f, 0.36f, 1f);
        private static readonly Color InvalidColor = new Color(0.86f, 0.24f, 0.22f, 1f);
        private static readonly Color BorderColor = new Color(0.04f, 0.06f, 0.08f, 1f);

        private readonly Dictionary<TimelineCell, TimelineCellView> _cells =
            new Dictionary<TimelineCell, TimelineCellView>();
        private readonly Dictionary<TimelineCell, SavedAppearance> _saved =
            new Dictionary<TimelineCell, SavedAppearance>();
        private readonly List<TimelineCell> _activeCoordinates = new List<TimelineCell>();
        private readonly List<TimelineCell> _missingCoordinates = new List<TimelineCell>();

        public IReadOnlyList<TimelineCell> ActiveCoordinates => _activeCoordinates;

        public IReadOnlyList<TimelineCell> MissingCoordinates => _missingCoordinates;

        public int RegisteredCount => _cells.Count;

        public bool IsShowing => _activeCoordinates.Count > 0 || _missingCoordinates.Count > 0;

        public bool IsInBounds { get; private set; }

        public void Register(TimelineCell coordinate, TimelineCellView cellView)
        {
            if (cellView == null)
            {
                throw new ArgumentNullException(nameof(cellView));
            }

            Clear();
            _cells[coordinate] = cellView;
            EnsureOutline(cellView).enabled = false;
        }

        public void Show(TimelineClearPreview preview)
        {
            if (preview == null)
            {
                throw new ArgumentNullException(nameof(preview));
            }

            Clear();
            IsInBounds = preview.IsInBounds;
            for (var index = 0; index < preview.Cells.Count; index++)
            {
                var cellPreview = preview.Cells[index];
                if (!_cells.TryGetValue(cellPreview.Coordinate, out var cellView) ||
                    cellView == null)
                {
                    _missingCoordinates.Add(cellPreview.Coordinate);
                    continue;
                }

                var outline = EnsureOutline(cellView);
                _saved[cellPreview.Coordinate] = new SavedAppearance(
                    cellView.DisplayColor,
                    cellView.DisplayText,
                    outline.enabled,
                    outline.effectColor,
                    outline.effectDistance);

                var state = preview.IsInBounds
                    ? cellPreview.State
                    : TimelineClearCellState.OutOfBounds;
                switch (state)
                {
                    case TimelineClearCellState.Empty:
                        cellView.SetPreviewContent("○", EmptyColor);
                        ConfigureOutline(outline, new Vector2(2f, 2f));
                        break;
                    case TimelineClearCellState.Occupied:
                        cellView.SetPreviewContent("HIT", OccupiedColor);
                        ConfigureOutline(outline, new Vector2(4f, 4f));
                        break;
                    case TimelineClearCellState.OutOfBounds:
                        cellView.SetPreviewContent("!", InvalidColor);
                        ConfigureOutline(outline, new Vector2(4f, 4f));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(cellPreview.State));
                }

                _activeCoordinates.Add(cellPreview.Coordinate);
            }

            _activeCoordinates.Sort(CompareCoordinates);
            _missingCoordinates.Sort(CompareCoordinates);
        }

        public void Clear()
        {
            foreach (var saved in _saved)
            {
                if (!_cells.TryGetValue(saved.Key, out var cellView) || cellView == null)
                {
                    continue;
                }

                cellView.SetPreviewContent(saved.Value.Text, saved.Value.Color);
                var outline = EnsureOutline(cellView);
                outline.enabled = saved.Value.OutlineEnabled;
                outline.effectColor = saved.Value.OutlineColor;
                outline.effectDistance = saved.Value.OutlineDistance;
            }

            _saved.Clear();
            _activeCoordinates.Clear();
            _missingCoordinates.Clear();
            IsInBounds = false;
        }

        private void OnDisable()
        {
            Clear();
        }

        private static Outline EnsureOutline(TimelineCellView cellView)
        {
            var outline = cellView.Graphic.GetComponent<Outline>();
            return outline == null ? cellView.Graphic.gameObject.AddComponent<Outline>() : outline;
        }

        private static void ConfigureOutline(Outline outline, Vector2 distance)
        {
            outline.enabled = true;
            outline.effectColor = BorderColor;
            outline.effectDistance = distance;
        }

        private static int CompareCoordinates(TimelineCell left, TimelineCell right)
        {
            var xComparison = left.X.CompareTo(right.X);
            return xComparison != 0 ? xComparison : left.Y.CompareTo(right.Y);
        }

        private readonly struct SavedAppearance
        {
            public SavedAppearance(
                Color color,
                string text,
                bool outlineEnabled,
                Color outlineColor,
                Vector2 outlineDistance)
            {
                Color = color;
                Text = text;
                OutlineEnabled = outlineEnabled;
                OutlineColor = outlineColor;
                OutlineDistance = outlineDistance;
            }

            public Color Color { get; }

            public string Text { get; }

            public bool OutlineEnabled { get; }

            public Color OutlineColor { get; }

            public Vector2 OutlineDistance { get; }
        }
    }
}
