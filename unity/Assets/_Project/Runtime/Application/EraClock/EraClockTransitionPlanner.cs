using System;

namespace TimeKey.Application.EraClock
{
    public enum EraClockPhaseTransition
    {
        None,
        Advance,
        Rollover,
        Jump
    }

    public sealed class EraClockTransitionPlan
    {
        internal EraClockTransitionPlan(
            EraClockPresentationSnapshot previous,
            EraClockPresentationSnapshot current,
            EraClockPhaseTransition phaseTransition,
            bool shouldMoveAnchor)
        {
            Previous = previous;
            Current = current ?? throw new ArgumentNullException(nameof(current));
            PhaseTransition = phaseTransition;
            ShouldMoveAnchor = shouldMoveAnchor;
        }

        public EraClockPresentationSnapshot Previous { get; }

        public EraClockPresentationSnapshot Current { get; }

        public EraClockPhaseTransition PhaseTransition { get; }

        public bool ShouldMoveAnchor { get; }

        public bool ShouldAnimate =>
            PhaseTransition == EraClockPhaseTransition.Advance ||
            PhaseTransition == EraClockPhaseTransition.Rollover ||
            ShouldMoveAnchor;
    }

    public static class EraClockTransitionPlanner
    {
        public static EraClockTransitionPlan Create(
            EraClockPresentationSnapshot previous,
            EraClockPresentationSnapshot current)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            if (previous == null)
            {
                return new EraClockTransitionPlan(
                    previous: null,
                    current,
                    EraClockPhaseTransition.None,
                    shouldMoveAnchor: false);
            }

            EraClockPhaseTransition phaseTransition = ClassifyPhase(previous, current);
            return new EraClockTransitionPlan(
                previous,
                current,
                phaseTransition,
                previous.AnchorTarget != current.AnchorTarget);
        }

        private static EraClockPhaseTransition ClassifyPhase(
            EraClockPresentationSnapshot previous,
            EraClockPresentationSnapshot current)
        {
            if (previous.Era == current.Era && previous.Phase == current.Phase)
            {
                return EraClockPhaseTransition.None;
            }

            if (previous.Era == current.Era && current.Phase == previous.Phase + 1)
            {
                return EraClockPhaseTransition.Advance;
            }

            if (previous.Phase == 8 &&
                current.Phase == 1 &&
                current.Era == previous.Era + 1)
            {
                return EraClockPhaseTransition.Rollover;
            }

            return EraClockPhaseTransition.Jump;
        }
    }
}
