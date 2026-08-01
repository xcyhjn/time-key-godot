using System;
using TimeKey.Application;
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
                    return "CARD SELECTED  |  CHOOSE A TARGET";
                case CombatSessionPhase.TargetSelected:
                    return "TARGET LOCKED  |  CHOOSE A TIMELINE POSITION";
                case CombatSessionPhase.TimelinePreview:
                    return state.IsPlacementValid
                        ? "TIMELINE POSITION VALID  |  CLICK TO CONFIRM"
                        : "TIMELINE POSITION INVALID";
                case CombatSessionPhase.Committed:
                    return "ACTION PLACED  |  RESOLVE THE TIMELINE";
                case CombatSessionPhase.Cancelled:
                    return "CARD CANCELLED  |  SELECT A CARD";
                case CombatSessionPhase.Resolved:
                    return "RESOLVED  |  TIMELINE COMPLETE";
                case CombatSessionPhase.Disposed:
                    return "SESSION CLOSED";
                default:
                    return "SELECT A CARD";
            }
        }

        private static string GetTargetStatus(CombatSessionView state)
        {
            if (state.LastResolution != null)
            {
                return "TARGET  |  HP " + state.LastResolution.TargetHpAfter;
            }

            if (!state.Target.HasValue)
            {
                return "NO TARGET";
            }

            var target = state.Target.Value;
            return target.Kind == CombatTargetKind.Entity
                ? target.EntityId.ToUpperInvariant() + "  |  LOCKED"
                : string.Format(
                    "HEX {0},{1}  |  LOCKED",
                    target.Coordinate.Q,
                    target.Coordinate.R);
        }

        private void HandleResolveRequested()
        {
            ResolveRequested?.Invoke();
        }
    }
}
