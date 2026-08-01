using System;
using TimeKey.Domain;
using TimeKey.Application;
using TimeKey.Presentation.Targeting;
using UnityEngine;

namespace TimeKey.Presentation.Presenters
{
    [DisallowMultipleComponent]
    public sealed class BoardRangePresenter : MonoBehaviour
    {
        [SerializeField] private BoardRangePreview rangePreview = null;

        public void Refresh(CombatSessionView state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            ValidateDependencies();
            var shouldShow = state.Phase == CombatSessionPhase.TargetSelected ||
                state.Phase == CombatSessionPhase.TimelinePreview;
            if (!shouldShow || state.SelectedCard == null || !state.Target.HasValue)
            {
                rangePreview.Clear();
                return;
            }

            rangePreview.Show(state.Target.Value.Coordinate, state.SelectedCard.Range);
        }

        public void HighlightAction(TimelineActionPresentationSnapshot action, bool highlighted)
        {
            ValidateDependencies();
            if (!highlighted || action == null)
            {
                rangePreview.Clear();
                return;
            }

            var center = action.TargetCoord ?? action.SourceCoord;
            if (!center.HasValue)
            {
                rangePreview.Clear();
                return;
            }

            rangePreview.Show(
                center.Value,
                action.EffectRange.Count == 0
                    ? new[] { new HexCoord(0, 0) }
                    : action.EffectRange);
        }

        private void OnDisable()
        {
            if (rangePreview != null)
            {
                rangePreview.Clear();
            }
        }

        private void ValidateDependencies()
        {
            if (rangePreview == null)
            {
                throw new InvalidOperationException(
                    name + " is missing serialized rangePreview reference.");
            }
        }
    }
}
