using System;
using System.Collections.Generic;
using TimeKey.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Presentation.Targeting
{
    public sealed class TimelinePlacementPreview : MonoBehaviour
    {
        private readonly Dictionary<TimelineCell, Graphic> _cells =
            new Dictionary<TimelineCell, Graphic>();
        private readonly Dictionary<TimelineCell, Outline> _outlines =
            new Dictionary<TimelineCell, Outline>();
        private readonly Dictionary<TimelineCell, Color> _savedColors =
            new Dictionary<TimelineCell, Color>();
        private readonly List<TimelineCell> _activeCoordinates = new List<TimelineCell>();
        private readonly List<TimelineCell> _missingCoordinates = new List<TimelineCell>();

        public event Action<TimelineCell> MissingCoordinate;

        public IReadOnlyList<TimelineCell> ActiveCoordinates => _activeCoordinates;

        public IReadOnlyList<TimelineCell> MissingCoordinates => _missingCoordinates;

        public int RegisteredCount => _cells.Count;

        public bool IsShowing => _activeCoordinates.Count > 0 || _missingCoordinates.Count > 0;

        public bool IsValid { get; private set; }

        public void Register(TimelineCell coordinate, Graphic cellView)
        {
            if (cellView == null)
            {
                throw new ArgumentNullException(nameof(cellView));
            }

            Clear();
            _cells[coordinate] = cellView;
            var outline = cellView.GetComponent<Outline>();
            if (outline == null)
            {
                outline = cellView.gameObject.AddComponent<Outline>();
            }

            outline.enabled = false;
            outline.effectColor = new Color(1f, 0.86f, 0.24f, 1f);
            outline.effectDistance = new Vector2(3f, 3f);
            _outlines[coordinate] = outline;
        }

        public void Show(TimelineCell origin, IEnumerable<TimelineCell> shape, bool isValid)
        {
            if (shape == null)
            {
                throw new ArgumentNullException(nameof(shape));
            }

            Clear();
            IsValid = isValid;
            var projected = new HashSet<TimelineCell>();
            foreach (var offset in shape)
            {
                projected.Add(origin + offset);
            }

            var color = isValid
                ? TargetPreviewPalette.TimelineValid
                : TargetPreviewPalette.TimelineInvalid;
            foreach (var coordinate in projected)
            {
                if (_cells.TryGetValue(coordinate, out var graphic) && graphic != null)
                {
                    _savedColors[coordinate] = graphic.color;
                    graphic.color = color;
                    if (!isValid && _outlines.TryGetValue(coordinate, out var outline))
                    {
                        outline.enabled = true;
                    }
                    _activeCoordinates.Add(coordinate);
                }
                else
                {
                    _missingCoordinates.Add(coordinate);
                }
            }

            _activeCoordinates.Sort(CompareCoordinates);
            _missingCoordinates.Sort(CompareCoordinates);
            for (var index = 0; index < _missingCoordinates.Count; index++)
            {
                MissingCoordinate?.Invoke(_missingCoordinates[index]);
            }
        }

        public void Clear()
        {
            foreach (var saved in _savedColors)
            {
                if (_cells.TryGetValue(saved.Key, out var graphic) && graphic != null)
                {
                    graphic.color = saved.Value;
                }
            }

            foreach (var outline in _outlines.Values)
            {
                if (outline != null)
                {
                    outline.enabled = false;
                }
            }

            _savedColors.Clear();
            _activeCoordinates.Clear();
            _missingCoordinates.Clear();
            IsValid = false;
        }

        private void OnDisable()
        {
            Clear();
        }

        private static int CompareCoordinates(TimelineCell left, TimelineCell right)
        {
            var xComparison = left.X.CompareTo(right.X);
            return xComparison != 0 ? xComparison : left.Y.CompareTo(right.Y);
        }
    }
}
