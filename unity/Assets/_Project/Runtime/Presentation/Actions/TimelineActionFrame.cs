using System;
using System.Collections.Generic;
using TimeKey.Application;
using TimeKey.Domain;
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

        private TimelineActionPresentationSnapshot _snapshot;
        private bool _highlighted;

        public TimelineActionIdentity ActionId => _snapshot == null ? default : _snapshot.ActionId;

        public TimelineActionPresentationSnapshot Snapshot => _snapshot;

        public TimelineActorKind ActorKind =>
            _snapshot == null ? default : _snapshot.ActorKind;

        public IReadOnlyList<TimelineCell> OccupiedCells =>
            _snapshot == null ? Array.Empty<TimelineCell>() : _snapshot.OccupiedCells;

        public void Apply(
            TimelineActionPresentationSnapshot snapshot,
            IReadOnlyDictionary<TimelineCell, TimelineCellView> cells,
            RectTransform layer)
        {
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
            PositionOverCells(snapshot.OccupiedCells, cells, layer);
            var enemy = snapshot.ActorKind == TimelineActorKind.Enemy;
            var unsupported = snapshot.Validity == TimelineActionValidity.Unsupported;
            background.color = enemy
                ? new Color(0.42f, 0.10f, 0.11f, 0.42f)
                : new Color(0.04f, 0.38f, 0.40f, 0.38f);
            stripe.gameObject.SetActive(enemy);
            stripe.color = unsupported
                ? new Color(1f, 0.72f, 0.18f, 0.90f)
                : new Color(0.95f, 0.31f, 0.27f, 0.90f);
            label.text = snapshot.Display.Title;
            badge.text = enemy
                ? unsupported ? "敌方 / 无效果" : "敌方"
                : "玩家";
            outline.effectColor = unsupported
                ? new Color(1f, 0.72f, 0.18f, 1f)
                : enemy
                    ? new Color(1f, 0.34f, 0.28f, 1f)
                    : new Color(0.18f, 0.88f, 0.80f, 1f);
            SetHighlighted(_highlighted);
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

        private void PositionOverCells(
            IReadOnlyList<TimelineCell> occupiedCells,
            IReadOnlyDictionary<TimelineCell, TimelineCellView> cells,
            RectTransform layer)
        {
            Canvas.ForceUpdateCanvases();
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            var corners = new Vector3[4];
            for (var index = 0; index < occupiedCells.Count; index++)
            {
                if (!cells.TryGetValue(occupiedCells[index], out var cell) || cell == null)
                {
                    throw new InvalidOperationException("No serialized timeline cell exists for " + occupiedCells[index] + ".");
                }

                ((RectTransform)cell.transform).GetWorldCorners(corners);
                for (var cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
                {
                    var local = (Vector2)layer.InverseTransformPoint(corners[cornerIndex]);
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                }
            }

            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (min + max) * 0.5f;
            rect.sizeDelta = (max - min) + new Vector2(4f, 4f);
        }

        private void ValidateDependencies()
        {
            if (background == null || stripe == null || outline == null ||
                label == null || badge == null || canvasGroup == null)
            {
                throw new InvalidOperationException(name + " has incomplete serialized action-frame references.");
            }
        }
    }
}
