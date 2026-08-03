using System;

namespace TimeKey.Application.EraClock
{
    public enum EraClockApplyStatus
    {
        Accepted,
        Repeated,
        Stale,
        SequenceConflict
    }

    public enum EraClockMachineState
    {
        Unbound,
        Settled,
        PhaseTransition,
        RolloverTransition,
        AnchorTransition
    }

    public sealed class EraClockApplyResult
    {
        internal EraClockApplyResult(
            EraClockApplyStatus status,
            EraClockTransitionPlan plan)
        {
            Status = status;
            Plan = plan;
        }

        public EraClockApplyStatus Status { get; }

        public EraClockTransitionPlan Plan { get; }

        public bool Accepted => Status == EraClockApplyStatus.Accepted;
    }

    public sealed class EraClockStateMachine
    {
        public EraClockPresentationSnapshot CurrentSnapshot { get; private set; }

        public EraClockMachineState State { get; private set; } =
            EraClockMachineState.Unbound;

        public EraClockApplyResult Apply(EraClockPresentationSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (CurrentSnapshot != null)
            {
                if (snapshot.Sequence < CurrentSnapshot.Sequence)
                {
                    return new EraClockApplyResult(EraClockApplyStatus.Stale, plan: null);
                }

                if (snapshot.Sequence == CurrentSnapshot.Sequence)
                {
                    return new EraClockApplyResult(
                        snapshot.Equals(CurrentSnapshot)
                            ? EraClockApplyStatus.Repeated
                            : EraClockApplyStatus.SequenceConflict,
                        plan: null);
                }
            }

            EraClockTransitionPlan plan = EraClockTransitionPlanner.Create(
                CurrentSnapshot,
                snapshot);
            CurrentSnapshot = snapshot;
            State = StateFor(plan);
            return new EraClockApplyResult(EraClockApplyStatus.Accepted, plan);
        }

        public bool Complete(long sequence)
        {
            return Settle(sequence);
        }

        public bool Cancel(long sequence)
        {
            return Settle(sequence);
        }

        private bool Settle(long sequence)
        {
            if (CurrentSnapshot == null || CurrentSnapshot.Sequence != sequence)
            {
                return false;
            }

            State = EraClockMachineState.Settled;
            return true;
        }

        private static EraClockMachineState StateFor(EraClockTransitionPlan plan)
        {
            if (plan.PhaseTransition == EraClockPhaseTransition.Rollover)
            {
                return EraClockMachineState.RolloverTransition;
            }

            if (plan.PhaseTransition == EraClockPhaseTransition.Advance)
            {
                return EraClockMachineState.PhaseTransition;
            }

            if (plan.ShouldMoveAnchor)
            {
                return EraClockMachineState.AnchorTransition;
            }

            return EraClockMachineState.Settled;
        }
    }
}
