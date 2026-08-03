using System;
using System.Collections;
using TimeKey.Application.EraClock;
using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Presentation.EraClock
{
    public enum EraClockPresenterState
    {
        Unbound,
        Settled,
        PhaseTransition,
        RolloverTransition,
        AnchorTransition,
        Disabled
    }

    public enum EraClockCompletionReason
    {
        Immediate,
        Completed,
        Cancelled,
        Rebound,
        Disabled
    }

    public enum EraClockJumpMode
    {
        Snap
    }

    [Serializable]
    public sealed class EraClockViewBindings
    {
        [SerializeField] private RectTransform clockRoot;
        [SerializeField] private RawImage clockFace;
        [SerializeField] private RawImage clockRing;
        [SerializeField] private RectTransform pointerPivot;
        [SerializeField] private RawImage pointerImage;
        [SerializeField] private Image progressFill;
        [SerializeField] private Text eraLabel;
        [SerializeField] private Text phaseLabel;
        [SerializeField] private RectTransform centerAnchor;
        [SerializeField] private RectTransform hudAnchor;
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private CanvasGroup rolloverPulse;

        public EraClockViewBindings(
            RectTransform clockRoot,
            RawImage clockFace,
            RawImage clockRing,
            RectTransform pointerPivot,
            RawImage pointerImage,
            Image progressFill,
            Text eraLabel,
            Text phaseLabel,
            RectTransform centerAnchor,
            RectTransform hudAnchor,
            CanvasGroup rootGroup,
            CanvasGroup rolloverPulse)
        {
            this.clockRoot = clockRoot;
            this.clockFace = clockFace;
            this.clockRing = clockRing;
            this.pointerPivot = pointerPivot;
            this.pointerImage = pointerImage;
            this.progressFill = progressFill;
            this.eraLabel = eraLabel;
            this.phaseLabel = phaseLabel;
            this.centerAnchor = centerAnchor;
            this.hudAnchor = hudAnchor;
            this.rootGroup = rootGroup;
            this.rolloverPulse = rolloverPulse;
        }

        public RectTransform ClockRoot => clockRoot;

        public RawImage ClockFace => clockFace;

        public RawImage ClockRing => clockRing;

        public RectTransform PointerPivot => pointerPivot;

        public RawImage PointerImage => pointerImage;

        public Image ProgressFill => progressFill;

        public Text EraLabel => eraLabel;

        public Text PhaseLabel => phaseLabel;

        public RectTransform CenterAnchor => centerAnchor;

        public RectTransform HudAnchor => hudAnchor;

        public CanvasGroup RootGroup => rootGroup;

        public CanvasGroup RolloverPulse => rolloverPulse;

        public bool IsComplete =>
            clockRoot != null &&
            clockFace != null &&
            clockRing != null &&
            pointerPivot != null &&
            pointerImage != null &&
            progressFill != null &&
            eraLabel != null &&
            phaseLabel != null &&
            centerAnchor != null &&
            hudAnchor != null &&
            rootGroup != null &&
            rolloverPulse != null;

        public void Validate()
        {
            if (!IsComplete)
            {
                throw new InvalidOperationException(
                    "EraClock requires ClockRoot, ClockFace, ClockRing, PointerPivot, " +
                    "PointerImage, EraProgress, EraLabel, PhaseLabel, CenterAnchor, " +
                    "HudAnchor, RootGroup, and RolloverPulse references.");
            }
        }

        public RectTransform AnchorFor(EraClockAnchorTarget target)
        {
            return target == EraClockAnchorTarget.Center ? centerAnchor : hudAnchor;
        }

        public void DisableRaycasts()
        {
            clockFace.raycastTarget = false;
            clockRing.raycastTarget = false;
            pointerImage.raycastTarget = false;
            progressFill.raycastTarget = false;
            eraLabel.raycastTarget = false;
            phaseLabel.raycastTarget = false;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
            rolloverPulse.interactable = false;
            rolloverPulse.blocksRaycasts = false;
        }
    }

    [Serializable]
    public sealed class EraClockAnimationSettings
    {
        [SerializeField] private float phaseDuration = 0.24f;
        [SerializeField] private float rolloverDuration = 0.64f;
        [SerializeField] private float anchorDuration = 1f;
        [SerializeField] private float centerScale = 1f;
        [SerializeField] private float hudScale = 0.46f;
        [SerializeField] private float pointerDegreesPerPhase = 45f;
        [SerializeField] private bool clockwise = true;
        [SerializeField, Range(0.05f, 0.8f)] private float rolloverEndpointFraction = 0.35f;
        [SerializeField, Range(0.1f, 0.95f)] private float rolloverPulseEndFraction = 0.65f;
        [SerializeField] private EraClockJumpMode jumpMode = EraClockJumpMode.Snap;
        [SerializeField] private AnimationCurve easing = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 3f),
            new Keyframe(1f, 1f, 0f, 0f));

        public EraClockAnimationSettings(
            float phaseDuration,
            float rolloverDuration,
            float anchorDuration,
            float centerScale = 1f,
            float hudScale = 0.46f)
        {
            this.phaseDuration = phaseDuration;
            this.rolloverDuration = rolloverDuration;
            this.anchorDuration = anchorDuration;
            this.centerScale = centerScale;
            this.hudScale = hudScale;
        }

        public float PhaseDuration => Mathf.Max(0f, phaseDuration);

        public float RolloverDuration => Mathf.Max(0f, rolloverDuration);

        public float AnchorDuration => Mathf.Max(0f, anchorDuration);

        public float PointerDegreesPerPhase => Mathf.Max(0f, pointerDegreesPerPhase);

        public float PointerDirection => clockwise ? -1f : 1f;

        public float RolloverEndpointFraction => Mathf.Clamp(
            rolloverEndpointFraction,
            0.05f,
            0.8f);

        public float RolloverPulseEndFraction => Mathf.Clamp(
            rolloverPulseEndFraction,
            RolloverEndpointFraction + 0.05f,
            0.95f);

        public EraClockJumpMode JumpMode => jumpMode;

        public float EvaluateEasing(float value)
        {
            return easing == null
                ? Mathf.Clamp01(value)
                : easing.Evaluate(Mathf.Clamp01(value));
        }

        public float ScaleFor(EraClockAnchorTarget target)
        {
            return Mathf.Max(0f, target == EraClockAnchorTarget.Center
                ? centerScale
                : hudScale);
        }
    }

    public sealed class EraClockPresenter : MonoBehaviour
    {
        [SerializeField] private EraClockViewBindings bindings;
        [SerializeField] private EraClockAnimationSettings animationSettings =
            new EraClockAnimationSettings(0.24f, 0.64f, 1f);

        private EraClockStateMachine stateMachine = new EraClockStateMachine();
        private Coroutine activeTransition;
        private int transitionGeneration;

        public event Action<EraClockPresentationSnapshot, EraClockCompletionReason>
            TransitionFinished;

        public EraClockPresenterState State { get; private set; } =
            EraClockPresenterState.Unbound;

        public EraClockPresentationSnapshot CurrentSnapshot =>
            stateMachine.CurrentSnapshot;

        public EraClockCompletionReason LastCompletionReason { get; private set; }

        public void Rebind(
            EraClockViewBindings viewBindings,
            EraClockAnimationSettings settings = null)
        {
            if (viewBindings == null)
            {
                throw new ArgumentNullException(nameof(viewBindings));
            }

            viewBindings.Validate();
            StopActiveTransition();
            bindings = viewBindings;
            if (settings != null)
            {
                animationSettings = settings;
            }

            if (animationSettings == null)
            {
                animationSettings = new EraClockAnimationSettings(0.24f, 0.64f, 1f);
            }

            bindings.DisableRaycasts();
            bindings.RolloverPulse.alpha = 0f;
            if (CurrentSnapshot != null)
            {
                SnapTo(CurrentSnapshot);
                stateMachine.Cancel(CurrentSnapshot.Sequence);
                State = EraClockPresenterState.Settled;
                Finish(CurrentSnapshot, EraClockCompletionReason.Rebound);
            }
        }

        public EraClockApplyResult ApplySnapshot(EraClockPresentationSnapshot snapshot)
        {
            EnsureReady();
            EraClockApplyResult result = stateMachine.Apply(snapshot);
            if (!result.Accepted)
            {
                return result;
            }

            StopActiveTransition();
            UpdateLabels(snapshot);
            EraClockTransitionPlan plan = result.Plan;
            if (!snapshot.IsRevealed || !plan.ShouldAnimate || TotalDuration(plan) <= 0f)
            {
                SnapTo(snapshot);
                stateMachine.Complete(snapshot.Sequence);
                State = EraClockPresenterState.Settled;
                Finish(snapshot, EraClockCompletionReason.Immediate);
                return result;
            }

            SetState(plan);
            int generation = transitionGeneration;
            activeTransition = StartCoroutine(RunTransition(plan, generation));
            return result;
        }

        public void CancelAndSnap()
        {
            StopActiveTransition();
            if (CurrentSnapshot == null || bindings == null || !bindings.IsComplete)
            {
                return;
            }

            SnapTo(CurrentSnapshot);
            stateMachine.Cancel(CurrentSnapshot.Sequence);
            State = EraClockPresenterState.Settled;
            Finish(CurrentSnapshot, EraClockCompletionReason.Cancelled);
        }

        public void RefreshLayout()
        {
            if (State == EraClockPresenterState.Settled &&
                CurrentSnapshot != null &&
                bindings != null &&
                bindings.IsComplete)
            {
                SetAnchorTerminal(CurrentSnapshot);
            }
        }

        private IEnumerator RunTransition(EraClockTransitionPlan plan, int generation)
        {
            EraClockPresentationSnapshot snapshot = plan.Current;
            float phaseDuration = PhaseDuration(plan);
            float anchorDuration = plan.ShouldMoveAnchor
                ? animationSettings.AnchorDuration
                : 0f;
            float totalDuration = Mathf.Max(phaseDuration, anchorDuration);
            float elapsed = 0f;
            float startAngle = bindings.PointerPivot.localEulerAngles.z;
            if (startAngle > 180f)
            {
                startAngle -= 360f;
            }

            float startProgress = bindings.ProgressFill.fillAmount;
            Vector3 startPosition = bindings.ClockRoot.position;
            Vector3 startScale = bindings.ClockRoot.localScale;

            while (elapsed < totalDuration)
            {
                if (generation != transitionGeneration)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                if (phaseDuration > 0f)
                {
                    float phaseT = Mathf.Clamp01(elapsed / phaseDuration);
                    ApplyPhaseFrame(plan, startAngle, startProgress, phaseT);
                }

                if (anchorDuration > 0f)
                {
                    float anchorT = animationSettings.EvaluateEasing(
                        Mathf.Clamp01(elapsed / anchorDuration));
                    RectTransform target = bindings.AnchorFor(snapshot.AnchorTarget);
                    bindings.ClockRoot.position = Vector3.LerpUnclamped(
                        startPosition,
                        target.position,
                        anchorT);
                    float targetScale = animationSettings.ScaleFor(snapshot.AnchorTarget);
                    bindings.ClockRoot.localScale = Vector3.LerpUnclamped(
                        startScale,
                        Vector3.one * targetScale,
                        anchorT);
                }

                yield return null;
            }

            if (generation != transitionGeneration)
            {
                yield break;
            }

            activeTransition = null;
            SnapTo(snapshot);
            stateMachine.Complete(snapshot.Sequence);
            State = EraClockPresenterState.Settled;
            Finish(snapshot, EraClockCompletionReason.Completed);
        }

        private void ApplyPhaseFrame(
            EraClockTransitionPlan plan,
            float startAngle,
            float startProgress,
            float t)
        {
            EraClockPresentationSnapshot snapshot = plan.Current;
            if (plan.PhaseTransition == EraClockPhaseTransition.Advance)
            {
                float easedT = animationSettings.EvaluateEasing(t);
                SetPointerAngle(Mathf.LerpUnclamped(
                    startAngle,
                    AngleFor(snapshot.Phase),
                    easedT));
                bindings.ProgressFill.fillAmount = Mathf.LerpUnclamped(
                    startProgress,
                    snapshot.NormalizedProgress,
                    easedT);
                return;
            }

            if (plan.PhaseTransition != EraClockPhaseTransition.Rollover)
            {
                return;
            }

            float endpointFraction = animationSettings.RolloverEndpointFraction;
            float pulseEndFraction = animationSettings.RolloverPulseEndFraction;
            if (t < endpointFraction)
            {
                float endpointT = animationSettings.EvaluateEasing(t / endpointFraction);
                SetPointerAngle(Mathf.LerpUnclamped(
                    startAngle,
                    animationSettings.PointerDirection * 360f,
                    endpointT));
                bindings.ProgressFill.fillAmount = Mathf.LerpUnclamped(
                    startProgress,
                    1f,
                    endpointT);
                bindings.RolloverPulse.alpha = 0f;
                return;
            }

            if (t < pulseEndFraction)
            {
                float pulseT = (t - endpointFraction) /
                    (pulseEndFraction - endpointFraction);
                SetPointerAngle(0f);
                bindings.ProgressFill.fillAmount = 0f;
                bindings.RolloverPulse.alpha = Mathf.Sin(pulseT * Mathf.PI);
                return;
            }

            float settleT = animationSettings.EvaluateEasing(
                (t - pulseEndFraction) / (1f - pulseEndFraction));
            SetPointerAngle(Mathf.LerpUnclamped(0f, AngleFor(snapshot.Phase), settleT));
            bindings.ProgressFill.fillAmount = Mathf.LerpUnclamped(
                0f,
                snapshot.NormalizedProgress,
                settleT);
            bindings.RolloverPulse.alpha = 1f - settleT;
        }

        private void SnapTo(EraClockPresentationSnapshot snapshot)
        {
            UpdateLabels(snapshot);
            SetPointerAngle(AngleFor(snapshot.Phase));
            bindings.ProgressFill.fillAmount = snapshot.NormalizedProgress;
            bindings.RolloverPulse.alpha = 0f;
            bindings.RootGroup.alpha = snapshot.IsRevealed ? 1f : 0f;
            SetAnchorTerminal(snapshot);
        }

        private void SetAnchorTerminal(EraClockPresentationSnapshot snapshot)
        {
            RectTransform anchor = bindings.AnchorFor(snapshot.AnchorTarget);
            bindings.ClockRoot.position = anchor.position;
            bindings.ClockRoot.localScale =
                Vector3.one * animationSettings.ScaleFor(snapshot.AnchorTarget);
        }

        private void UpdateLabels(EraClockPresentationSnapshot snapshot)
        {
            bindings.EraLabel.text = $"第{snapshot.Era}时代";
            bindings.PhaseLabel.text = $"{snapshot.Phase} / 8";
        }

        private void SetState(EraClockTransitionPlan plan)
        {
            if (plan.PhaseTransition == EraClockPhaseTransition.Rollover)
            {
                State = EraClockPresenterState.RolloverTransition;
            }
            else if (plan.PhaseTransition == EraClockPhaseTransition.Advance)
            {
                State = EraClockPresenterState.PhaseTransition;
            }
            else
            {
                State = EraClockPresenterState.AnchorTransition;
            }
        }

        private float TotalDuration(EraClockTransitionPlan plan)
        {
            return Mathf.Max(
                PhaseDuration(plan),
                plan.ShouldMoveAnchor ? animationSettings.AnchorDuration : 0f);
        }

        private float PhaseDuration(EraClockTransitionPlan plan)
        {
            if (plan.PhaseTransition == EraClockPhaseTransition.Advance)
            {
                return animationSettings.PhaseDuration;
            }

            return plan.PhaseTransition == EraClockPhaseTransition.Rollover
                ? animationSettings.RolloverDuration
                : 0f;
        }

        private void StopActiveTransition()
        {
            transitionGeneration++;
            if (activeTransition != null)
            {
                StopCoroutine(activeTransition);
                activeTransition = null;
            }
        }

        private void EnsureReady()
        {
            if (bindings == null)
            {
                throw new InvalidOperationException("EraClock view bindings have not been assigned.");
            }

            bindings.Validate();
            if (animationSettings == null)
            {
                animationSettings = new EraClockAnimationSettings(0.24f, 0.64f, 1f);
            }
        }

        private void Finish(
            EraClockPresentationSnapshot snapshot,
            EraClockCompletionReason reason)
        {
            LastCompletionReason = reason;
            TransitionFinished?.Invoke(snapshot, reason);
        }

        private float AngleFor(int phase)
        {
            return (phase - 1) *
                animationSettings.PointerDegreesPerPhase *
                animationSettings.PointerDirection;
        }

        private void SetPointerAngle(float angle)
        {
            bindings.PointerPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnDisable()
        {
            StopActiveTransition();
            if (CurrentSnapshot != null && bindings != null && bindings.IsComplete)
            {
                SnapTo(CurrentSnapshot);
                stateMachine.Cancel(CurrentSnapshot.Sequence);
                Finish(CurrentSnapshot, EraClockCompletionReason.Disabled);
            }

            State = EraClockPresenterState.Disabled;
        }

        private void OnEnable()
        {
            if (State == EraClockPresenterState.Disabled &&
                CurrentSnapshot != null &&
                bindings != null &&
                bindings.IsComplete)
            {
                SnapTo(CurrentSnapshot);
                State = EraClockPresenterState.Settled;
            }
        }

        private void LateUpdate()
        {
            RefreshLayout();
        }

        private void OnDestroy()
        {
            StopActiveTransition();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                CancelAndSnap();
            }
            else
            {
                RefreshLayout();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshLayout();
        }
    }
}
