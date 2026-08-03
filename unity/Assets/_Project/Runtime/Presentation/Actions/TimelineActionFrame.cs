using System;
using System.Collections.Generic;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Presentation.Theming;
using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Presentation.Actions
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class TimelineActionFrame : MonoBehaviour
    {
        [SerializeField] private Image background = null;
        [SerializeField] private Image stripe = null;
        [SerializeField] private Outline outline = null;
        [SerializeField] private Text label = null;
        [SerializeField] private Text badge = null;
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private Image cellBackgroundTemplate = null;
        [SerializeField] private Image edgeTemplate = null;
        [SerializeField] private UiThemeScope themeScope = null;

        private readonly Vector3[] _corners = new Vector3[4];
        private readonly List<Image> _cellVisuals = new List<Image>();
        private readonly List<Image> _edgeVisuals = new List<Image>();
        private readonly List<TimelineCell> _visualOccupiedCells = new List<TimelineCell>();
        private readonly List<EdgeSegment> _edgeSegments = new List<EdgeSegment>();
        private readonly HashSet<TimelineCell> _occupiedCellSet = new HashSet<TimelineCell>();
        private TimelineActionPresentationSnapshot _snapshot;
        private IReadOnlyDictionary<TimelineCell, TimelineCellView> _cells;
        private RectTransform _layer;
        private Color _fillColor;
        private Color _edgeColor;
        private int _layoutSignature;
        private bool _hasLayoutSignature;
        private bool _layoutInvalidated;
        private bool _refreshingGeometry;
        private bool _highlighted;

        public TimelineActionIdentity ActionId => _snapshot == null ? default : _snapshot.ActionId;

        public TimelineActionPresentationSnapshot Snapshot => _snapshot;

        public TimelineActorKind ActorKind =>
            _snapshot == null ? default : _snapshot.ActorKind;

        public IReadOnlyList<TimelineCell> OccupiedCells =>
            _snapshot == null ? Array.Empty<TimelineCell>() : _snapshot.OccupiedCells;

        public IReadOnlyList<TimelineCell> VisualOccupiedCells => _visualOccupiedCells;

        public void Apply(
            TimelineActionPresentationSnapshot snapshot,
            IReadOnlyDictionary<TimelineCell, TimelineCellView> cells,
            RectTransform layer)
        {
            var snapshotChanged = !ReferenceEquals(_snapshot, snapshot);
            var bindingChanged = !ReferenceEquals(_cells, cells) || !ReferenceEquals(_layer, layer);
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (layer == null)
            {
                throw new ArgumentNullException(nameof(layer));
            }

            ValidateDependencies();
            _cells = cells;
            _layer = layer;
            var enemy = snapshot.ActorKind == TimelineActorKind.Enemy;
            var unsupported = snapshot.Validity == TimelineActionValidity.Unsupported;
            ApplyThemeColors(enemy, unsupported);
            background.color = _fillColor;
            background.enabled = false;
            stripe.gameObject.SetActive(enemy);
            label.text = snapshot.Display.Title;
            badge.text = enemy
                ? unsupported ? "敌方 / 无效果" : "敌方"
                : "玩家";
            outline.effectColor = _edgeColor;
            outline.enabled = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            _visualOccupiedCells.Clear();
            for (var index = 0; index < snapshot.OccupiedCells.Count; index++)
            {
                _visualOccupiedCells.Add(snapshot.OccupiedCells[index]);
            }

            var geometryChanged = snapshotChanged || bindingChanged || !_hasLayoutSignature;
            if (!geometryChanged && ComputeLayoutSignature() != _layoutSignature)
            {
                geometryChanged = true;
            }

            if (_layoutInvalidated || geometryChanged)
            {
                _layoutInvalidated = true;
                RefreshGeometry();
            }

            SetHighlighted(_highlighted);
        }

        private void ApplyThemeColors(bool enemy, bool unsupported)
        {
            if (themeScope == null)
            {
                themeScope = GetComponentInParent<UiThemeScope>();
            }

            var styleId = enemy ? UiStyleId.EnemyIntentFrame : UiStyleId.PlayerActionFrame;
            if (themeScope != null && themeScope.TryGet(styleId, out var style))
            {
                _fillColor = style.frame.fillColor;
                _edgeColor = unsupported
                    ? style.button.highlightedColor
                    : style.frame.borderColor;
                stripe.color = unsupported
                    ? style.button.highlightedColor
                    : style.frame.borderColor;
                label.color = style.text.normalColor;
                badge.color = style.text.normalColor;
                if (style.text.font != null)
                {
                    label.font = style.text.font;
                    badge.font = style.text.font;
                }

                return;
            }

            _fillColor = enemy
                ? new Color(0.42f, 0.10f, 0.11f, 0.42f)
                : new Color(0.04f, 0.38f, 0.40f, 0.38f);
            _edgeColor = unsupported
                ? new Color(1f, 0.72f, 0.18f, 1f)
                : enemy
                    ? new Color(1f, 0.34f, 0.28f, 1f)
                    : new Color(0.18f, 0.88f, 0.80f, 1f);
            stripe.color = unsupported
                ? new Color(1f, 0.72f, 0.18f, 0.90f)
                : new Color(0.95f, 0.31f, 0.27f, 0.90f);
        }

        public void SetHighlighted(bool highlighted)
        {
            _highlighted = highlighted;
            if (outline != null)
            {
                outline.effectDistance = highlighted
                    ? new Vector2(4f, -4f)
                    : new Vector2(2f, -2f);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = highlighted ? 1f : 0.86f;
            }
        }

        private void OnEnable()
        {
            if (_snapshot != null)
            {
                _layoutInvalidated = true;
            }
        }

        private void OnDisable()
        {
            SetInactive(_cellVisuals);
            SetInactive(_edgeVisuals);
            _hasLayoutSignature = false;
            _layoutInvalidated = true;
            _visualOccupiedCells.Clear();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_snapshot != null)
            {
                _layoutInvalidated = true;
            }
        }

        private void OnTransformParentChanged()
        {
            if (_snapshot != null)
            {
                _layoutInvalidated = true;
            }
        }

        private void LateUpdate()
        {
            if (_snapshot == null || _cells == null || _layer == null || _refreshingGeometry)
            {
                return;
            }

            if (_layoutInvalidated || !_hasLayoutSignature ||
                ComputeLayoutSignature() != _layoutSignature)
            {
                RefreshGeometry();
            }
        }

        private void Update()
        {
            if (_snapshot == null || stripe == null ||
                _snapshot.ActorKind != TimelineActorKind.Enemy || _highlighted)
            {
                return;
            }

            var color = stripe.color;
            color.a = 0.62f + (0.24f * (0.5f + (0.5f * Mathf.Sin(Time.unscaledTime * 4f))));
            stripe.color = color;
        }

        private void RefreshGeometry()
        {
            if (_refreshingGeometry || _snapshot == null || _cells == null || _layer == null)
            {
                return;
            }

            _refreshingGeometry = true;
            try
            {
                Canvas.ForceUpdateCanvases();
                var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                _occupiedCellSet.Clear();
                for (var index = 0; index < _snapshot.OccupiedCells.Count; index++)
                {
                    var coordinate = _snapshot.OccupiedCells[index];
                    if (!_cells.TryGetValue(coordinate, out var cell) || cell == null)
                    {
                        throw new InvalidOperationException("No serialized timeline cell exists for " + coordinate + ".");
                    }

                    _occupiedCellSet.Add(coordinate);
                    ((RectTransform)cell.transform).GetWorldCorners(_corners);
                    for (var cornerIndex = 0; cornerIndex < _corners.Length; cornerIndex++)
                    {
                        var local = (Vector2)_layer.InverseTransformPoint(_corners[cornerIndex]);
                        min = Vector2.Min(min, local);
                        max = Vector2.Max(max, local);
                    }
                }

                var rect = (RectTransform)transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.position = _layer.TransformPoint((min + max) * 0.5f);
                rect.sizeDelta = (max - min) + new Vector2(4f, 4f);
                BuildVisualCells(rect);
                BuildEdgeSegments(rect);
                _layoutSignature = ComputeLayoutSignature();
                _hasLayoutSignature = true;
                _layoutInvalidated = false;
            }
            finally
            {
                _refreshingGeometry = false;
            }
        }

        private void BuildVisualCells(RectTransform frameRect)
        {
            if (cellBackgroundTemplate != null)
            {
                cellBackgroundTemplate.gameObject.SetActive(false);
                EnsurePool(_cellVisuals, cellBackgroundTemplate, _snapshot.OccupiedCells.Count, "Cell");
            }

            for (var index = 0; index < _cellVisuals.Count; index++)
            {
                var visual = _cellVisuals[index];
                if (index >= _snapshot.OccupiedCells.Count || cellBackgroundTemplate == null)
                {
                    visual.gameObject.SetActive(false);
                    continue;
                }

                var coordinate = _snapshot.OccupiedCells[index];
                var cell = _cells[coordinate];
                visual.gameObject.SetActive(true);
                visual.color = _fillColor;
                visual.raycastTarget = false;
                PositionVisualOverCell(visual.rectTransform, (RectTransform)cell.transform, frameRect);
            }
        }

        private void BuildEdgeSegments(RectTransform frameRect)
        {
            _edgeSegments.Clear();
            for (var index = 0; index < _snapshot.OccupiedCells.Count; index++)
            {
                var cell = _snapshot.OccupiedCells[index];
                if (!_occupiedCellSet.Contains(new TimelineCell(cell.X - 1, cell.Y)))
                {
                    _edgeSegments.Add(new EdgeSegment(cell, EdgeSide.Left));
                }

                if (!_occupiedCellSet.Contains(new TimelineCell(cell.X + 1, cell.Y)))
                {
                    _edgeSegments.Add(new EdgeSegment(cell, EdgeSide.Right));
                }

                if (!_occupiedCellSet.Contains(new TimelineCell(cell.X, cell.Y - 1)))
                {
                    _edgeSegments.Add(new EdgeSegment(cell, EdgeSide.Bottom));
                }

                if (!_occupiedCellSet.Contains(new TimelineCell(cell.X, cell.Y + 1)))
                {
                    _edgeSegments.Add(new EdgeSegment(cell, EdgeSide.Top));
                }
            }

            if (edgeTemplate != null)
            {
                edgeTemplate.gameObject.SetActive(false);
                EnsurePool(_edgeVisuals, edgeTemplate, _edgeSegments.Count, "Edge");
            }

            for (var index = 0; index < _edgeVisuals.Count; index++)
            {
                var visual = _edgeVisuals[index];
                if (index >= _edgeSegments.Count || edgeTemplate == null)
                {
                    visual.gameObject.SetActive(false);
                    continue;
                }

                var segment = _edgeSegments[index];
                visual.gameObject.SetActive(true);
                visual.color = _edgeColor;
                visual.raycastTarget = false;
                PositionEdge(visual.rectTransform, segment, frameRect);
            }
        }

        private void EnsurePool(
            List<Image> pool,
            Image template,
            int required,
            string prefix)
        {
            while (pool.Count < required)
            {
                var visual = Instantiate(template, transform, false);
                visual.name = prefix + "-" + pool.Count;
                visual.raycastTarget = false;
                visual.gameObject.SetActive(false);
                pool.Add(visual);
            }
        }

        private void PositionVisualOverCell(
            RectTransform visual,
            RectTransform cell,
            RectTransform frameRect)
        {
            cell.GetWorldCorners(_corners);
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (var index = 0; index < _corners.Length; index++)
            {
                var local = (Vector2)frameRect.InverseTransformPoint(_corners[index]);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            SetLocalRect(visual, min, max);
        }

        private void PositionEdge(
            RectTransform visual,
            EdgeSegment segment,
            RectTransform frameRect)
        {
            var cell = _cells[segment.Cell];
            cell.transform.GetComponent<RectTransform>().GetWorldCorners(_corners);
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (var index = 0; index < _corners.Length; index++)
            {
                var local = (Vector2)frameRect.InverseTransformPoint(_corners[index]);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            var thickness = 2f;
            switch (segment.Side)
            {
                case EdgeSide.Left:
                    SetLocalRect(visual, new Vector2(min.x - thickness * 0.5f, min.y),
                        new Vector2(min.x + thickness * 0.5f, max.y));
                    break;
                case EdgeSide.Right:
                    SetLocalRect(visual, new Vector2(max.x - thickness * 0.5f, min.y),
                        new Vector2(max.x + thickness * 0.5f, max.y));
                    break;
                case EdgeSide.Bottom:
                    SetLocalRect(visual, new Vector2(min.x, min.y - thickness * 0.5f),
                        new Vector2(max.x, min.y + thickness * 0.5f));
                    break;
                default:
                    SetLocalRect(visual, new Vector2(min.x, max.y - thickness * 0.5f),
                        new Vector2(max.x, max.y + thickness * 0.5f));
                    break;
            }
        }

        private static void SetLocalRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (min + max) * 0.5f;
            rect.sizeDelta = max - min;
        }

        private int ComputeLayoutSignature()
        {
            var hash = 17;
            hash = Mix(hash, _layer == null ? 0 : _layer.GetInstanceID());
            MixRectTransform(ref hash, _layer);
            if (_snapshot == null || _cells == null)
            {
                return hash;
            }

            for (var index = 0; index < _snapshot.OccupiedCells.Count; index++)
            {
                if (_cells.TryGetValue(_snapshot.OccupiedCells[index], out var cell) && cell != null)
                {
                    MixRectTransform(ref hash, (RectTransform)cell.transform);
                }
            }

            return hash;
        }

        private static void MixRectTransform(ref int hash, RectTransform rect)
        {
            if (rect == null)
            {
                hash = Mix(hash, 0);
                return;
            }

            hash = Mix(hash, Mathf.RoundToInt(rect.rect.xMin * 1000f));
            hash = Mix(hash, Mathf.RoundToInt(rect.rect.yMin * 1000f));
            hash = Mix(hash, Mathf.RoundToInt(rect.rect.width * 1000f));
            hash = Mix(hash, Mathf.RoundToInt(rect.rect.height * 1000f));
            hash = Mix(hash, Mathf.RoundToInt(rect.anchoredPosition.x * 1000f));
            hash = Mix(hash, Mathf.RoundToInt(rect.anchoredPosition.y * 1000f));
            hash = Mix(hash, Mathf.RoundToInt(rect.position.x * 1000f));
            hash = Mix(hash, Mathf.RoundToInt(rect.position.y * 1000f));
            hash = Mix(hash, Mathf.RoundToInt(rect.lossyScale.x * 1000f));
            hash = Mix(hash, Mathf.RoundToInt(rect.lossyScale.y * 1000f));
        }

        private static int Mix(int hash, int value)
        {
            return unchecked((hash * 31) + value);
        }

        private static void SetInactive(List<Image> visuals)
        {
            for (var index = 0; index < visuals.Count; index++)
            {
                if (visuals[index] != null)
                {
                    visuals[index].gameObject.SetActive(false);
                }
            }
        }

        private void ValidateDependencies()
        {
            if (background == null || stripe == null || outline == null ||
                label == null || badge == null || canvasGroup == null)
            {
                throw new InvalidOperationException(name + " has incomplete serialized action-frame references.");
            }
        }

        private enum EdgeSide
        {
            Left,
            Right,
            Bottom,
            Top
        }

        private struct EdgeSegment
        {
            public EdgeSegment(TimelineCell cell, EdgeSide side)
            {
                Cell = cell;
                Side = side;
            }

            public TimelineCell Cell;
            public EdgeSide Side;
        }
    }
}
