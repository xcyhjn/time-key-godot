using System;
using TimeKey.Application;
using TimeKey.Presentation.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Presentation.Presenters
{
    [DisallowMultipleComponent]
    public sealed class CombatHudPresenter : MonoBehaviour
    {
        [SerializeField] private Text statusText = null;
        [SerializeField] private Text targetText = null;
        [SerializeField] private Button resolveButton = null;

        private bool _isBound;

        public event Action ResolveRequested;

        public bool IsBound => _isBound;

        public void Bind()
        {
            ValidateDependencies();
            if (_isBound)
            {
                return;
            }

            resolveButton.onClick.AddListener(HandleResolveRequested);
            _isBound = true;
        }

        public void Unbind()
        {
            if (!_isBound)
            {
                return;
            }

            resolveButton.onClick.RemoveListener(HandleResolveRequested);
            _isBound = false;
        }

        public void Refresh(CombatSessionView state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            ValidateDependencies();
            resolveButton.interactable = state.Phase == CombatSessionPhase.Committed;
            statusText.text = GetStatus(state);
            targetText.text = GetTargetStatus(state);
        }

        private void OnEnable()
        {
            if (statusText != null && targetText != null && resolveButton != null)
            {
                Bind();
            }
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void ValidateDependencies()
        {
            if (statusText == null)
            {
                throw new InvalidOperationException(
                    name + " is missing serialized statusText reference.");
            }

            if (targetText == null)
            {
                throw new InvalidOperationException(
                    name + " is missing serialized targetText reference.");
            }

            if (resolveButton == null)
            {
                throw new InvalidOperationException(
                    name + " is missing serialized resolveButton reference.");
            }
        }

        private static string GetStatus(CombatSessionView state)
        {
            switch (state.Phase)
            {
                case CombatSessionPhase.CardSelected:
                    if (state.InteractionMode == CombatInteractionMode.TimelineClear)
                    {
                        return CombatChineseText.ClearSelected;
                    }

                    return CombatChineseText.CardSelected;
                case CombatSessionPhase.TargetSelected:
                    return CombatChineseText.TargetLocked;
                case CombatSessionPhase.TimelinePreview:
                    if (state.InteractionMode == CombatInteractionMode.TimelineClear)
                    {
                        return state.IsPlacementValid
                            ? CombatChineseText.ClearPositionValid
                            : CombatChineseText.ClearPositionOutOfBounds;
                    }

                    return state.IsPlacementValid
                        ? CombatChineseText.TimelinePositionValid
                        : CombatChineseText.TimelinePositionInvalid;
                case CombatSessionPhase.Committed:
                    return CombatChineseText.ActionPlaced;
                case CombatSessionPhase.Cancelled:
                    return CombatChineseText.CardCancelled;
                case CombatSessionPhase.Resolved:
                    if (state.LastClearResult != null)
                    {
                        return CombatChineseText.ClearResolved;
                    }

                    return CombatChineseText.Resolved;
                case CombatSessionPhase.Disposed:
                    return CombatChineseText.SessionClosed;
                default:
                    return CombatChineseText.SelectCard;
            }
        }

        private static string GetTargetStatus(CombatSessionView state)
        {
            if (state.LastClearResult != null)
            {
                return CombatChineseText.ClearRemoved(state.LastClearResult.RemovedActions.Count);
            }

            if (state.InteractionMode == CombatInteractionMode.TimelineClear)
            {
                return state.ClearPreview == null
                    ? CombatChineseText.ClearNoMapTarget
                    : CombatChineseText.ClearHits(state.ClearPreview.HitActions.Count);
            }

            if (state.LastResolution != null)
            {
                var occupantResults = state.LastResolution.OccupantEffectResults;
                if (occupantResults.Count > 0)
                {
                    var result = occupantResults[occupantResults.Count - 1];
                    if (result.After != null)
                    {
                        if (result.Before != null &&
                            result.After.PoisonStacks != result.Before.PoisonStacks)
                        {
                            return CombatChineseText.PoisonStatus(
                                result.After.CreationId,
                                result.After.RuntimeId,
                                result.After.PoisonStacks);
                        }

                        if (result.After.SupportsHealth)
                        {
                            return CombatChineseText.OccupantHealth(
                                result.After.CreationId,
                                result.After.RuntimeId,
                                result.After.Hp);
                        }
                    }
                }

                return CombatChineseText.TargetHealth(state.LastResolution.TargetHpAfter);
            }

            if (!state.Target.HasValue)
            {
                return CombatChineseText.NoTarget;
            }

            var target = state.Target.Value;
            return target.Kind == CombatTargetKind.Entity
                ? CombatChineseText.EntityLocked(target.EntityId)
                : CombatChineseText.TileLocked(target.Coordinate);
        }

        private void HandleResolveRequested()
        {
            ResolveRequested?.Invoke();
        }
    }
}
