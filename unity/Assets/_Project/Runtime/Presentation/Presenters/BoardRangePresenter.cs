using System;
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
