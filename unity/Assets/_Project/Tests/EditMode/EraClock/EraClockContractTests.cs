using System;
using NUnit.Framework;
using TimeKey.Application.EraClock;
using TimeKey.Application.SceneFlow;

namespace TimeKey.Tests.EditMode.EraClock
{
    public sealed class EraClockContractTests
    {
        [TestCase(0, 1, 0)]
        [TestCase(1, 0, 0)]
        [TestCase(1, 9, 0)]
        [TestCase(1, 1, -1)]
        public void SnapshotRejectsInvalidAuthoritativeValues(int era, int phase, long sequence)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EraClockPresentationSnapshot(
                    era,
                    phase,
                    sequence,
                    EraClockAnchorTarget.Center));
        }

        [Test]
        public void OutOfBattleAdapterCopiesAuthoritativeEraAndPhase()
        {
            var start = new RunStartPayload(
                RunStartKind.NewGame,
                "era-clock-run",
                731,
                string.Empty,
                chapter: 1,
                era: 3,
                phase: 7,
                timecoins: 9,
                new[] { "card-a" });
            var state = new OutOfBattleShellState(start);

            EraClockPresentationSnapshot snapshot = EraClockSnapshotAdapter.FromOutOfBattle(
                state,
                sequence: 42,
                EraClockAnchorTarget.Hud);

            Assert.That(snapshot.Era, Is.EqualTo(3));
            Assert.That(snapshot.Phase, Is.EqualTo(7));
            Assert.That(snapshot.Sequence, Is.EqualTo(42));
            Assert.That(snapshot.AnchorTarget, Is.EqualTo(EraClockAnchorTarget.Hud));
            Assert.That(snapshot.IsInputLocked, Is.False);
            Assert.That(snapshot.IsRevealed, Is.True);
        }

        [Test]
        public void RunStartAdapterCopiesAuthoritativeEraAndPhase()
        {
            var start = new RunStartPayload(
                RunStartKind.NewGame,
                "era-clock-run-start",
                731,
                string.Empty,
                chapter: 1,
                era: 4,
                phase: 6,
                timecoins: 9,
                new[] { "card-a" });

            EraClockPresentationSnapshot snapshot = EraClockSnapshotAdapter.FromRunStart(
                start,
                sequence: 43,
                EraClockAnchorTarget.Center);

            Assert.That(snapshot.Era, Is.EqualTo(4));
            Assert.That(snapshot.Phase, Is.EqualTo(6));
            Assert.That(snapshot.Sequence, Is.EqualTo(43));
            Assert.That(snapshot.AnchorTarget, Is.EqualTo(EraClockAnchorTarget.Center));
        }

        [Test]
        public void SnapshotPreservesInputAndRevealMetadataWithoutOwningInputState()
        {
            var snapshot = new EraClockPresentationSnapshot(
                era: 2,
                phase: 5,
                sequence: 6,
                EraClockAnchorTarget.Hud,
                isInputLocked: true,
                isRevealed: false);

            Assert.That(snapshot.IsInputLocked, Is.True);
            Assert.That(snapshot.IsRevealed, Is.False);
        }

        [Test]
        public void PlannerClassifiesAdjacentPhaseAndRequestsAnimation()
        {
            EraClockTransitionPlan plan = EraClockTransitionPlanner.Create(
                Snapshot(era: 2, phase: 3, sequence: 1),
                Snapshot(era: 2, phase: 4, sequence: 2));

            Assert.That(plan.PhaseTransition, Is.EqualTo(EraClockPhaseTransition.Advance));
            Assert.That(plan.ShouldMoveAnchor, Is.False);
            Assert.That(plan.ShouldAnimate, Is.True);
        }

        [Test]
        public void PlannerClassifiesEightToOneAsRollover()
        {
            EraClockTransitionPlan plan = EraClockTransitionPlanner.Create(
                Snapshot(era: 4, phase: 8, sequence: 10),
                Snapshot(era: 5, phase: 1, sequence: 11));

            Assert.That(plan.PhaseTransition, Is.EqualTo(EraClockPhaseTransition.Rollover));
            Assert.That(plan.ShouldAnimate, Is.True);
        }

        [Test]
        public void PlannerSnapsNonAdjacentAuthoritativeJump()
        {
            EraClockTransitionPlan plan = EraClockTransitionPlanner.Create(
                Snapshot(era: 1, phase: 2, sequence: 4),
                Snapshot(era: 3, phase: 6, sequence: 5));

            Assert.That(plan.PhaseTransition, Is.EqualTo(EraClockPhaseTransition.Jump));
            Assert.That(plan.ShouldAnimate, Is.False);
        }

        [Test]
        public void PlannerCanCombinePhaseAndAnchorTransitions()
        {
            EraClockTransitionPlan plan = EraClockTransitionPlanner.Create(
                Snapshot(era: 1, phase: 7, sequence: 1),
                Snapshot(era: 1, phase: 8, sequence: 2, EraClockAnchorTarget.Hud));

            Assert.That(plan.PhaseTransition, Is.EqualTo(EraClockPhaseTransition.Advance));
            Assert.That(plan.ShouldMoveAnchor, Is.True);
            Assert.That(plan.ShouldAnimate, Is.True);
        }

        [Test]
        public void StateMachineRejectsStaleConflictAndExactRepeat()
        {
            var machine = new EraClockStateMachine();
            EraClockPresentationSnapshot current = Snapshot(era: 2, phase: 4, sequence: 8);

            Assert.That(machine.Apply(current).Status, Is.EqualTo(EraClockApplyStatus.Accepted));
            Assert.That(machine.Apply(current).Status, Is.EqualTo(EraClockApplyStatus.Repeated));
            Assert.That(
                machine.Apply(Snapshot(era: 2, phase: 3, sequence: 7)).Status,
                Is.EqualTo(EraClockApplyStatus.Stale));
            Assert.That(
                machine.Apply(Snapshot(era: 2, phase: 5, sequence: 8)).Status,
                Is.EqualTo(EraClockApplyStatus.SequenceConflict));
            Assert.That(machine.CurrentSnapshot, Is.SameAs(current));
        }

        [Test]
        public void StateMachineTracksRolloverUntilLatestTransitionCompletes()
        {
            var machine = new EraClockStateMachine();
            machine.Apply(Snapshot(era: 2, phase: 8, sequence: 20));

            EraClockApplyResult rollover = machine.Apply(
                Snapshot(era: 3, phase: 1, sequence: 21));

            Assert.That(rollover.Status, Is.EqualTo(EraClockApplyStatus.Accepted));
            Assert.That(machine.State, Is.EqualTo(EraClockMachineState.RolloverTransition));
            Assert.That(machine.Complete(sequence: 20), Is.False);
            Assert.That(machine.State, Is.EqualTo(EraClockMachineState.RolloverTransition));
            Assert.That(machine.Complete(sequence: 21), Is.True);
            Assert.That(machine.State, Is.EqualTo(EraClockMachineState.Settled));
        }

        [Test]
        public void NewerSnapshotWithSameVisualStateAdvancesSequenceWithoutAnimation()
        {
            var machine = new EraClockStateMachine();
            machine.Apply(Snapshot(era: 2, phase: 6, sequence: 30));

            EraClockApplyResult result = machine.Apply(Snapshot(era: 2, phase: 6, sequence: 31));

            Assert.That(result.Status, Is.EqualTo(EraClockApplyStatus.Accepted));
            Assert.That(result.Plan.ShouldAnimate, Is.False);
            Assert.That(machine.CurrentSnapshot.Sequence, Is.EqualTo(31));
            Assert.That(machine.State, Is.EqualTo(EraClockMachineState.Settled));
        }

        [Test]
        public void CancelOnlySettlesTheLatestTransition()
        {
            var machine = new EraClockStateMachine();
            machine.Apply(Snapshot(era: 1, phase: 1, sequence: 1));
            machine.Apply(Snapshot(era: 1, phase: 2, sequence: 2));

            Assert.That(machine.Cancel(sequence: 1), Is.False);
            Assert.That(machine.State, Is.EqualTo(EraClockMachineState.PhaseTransition));
            Assert.That(machine.Cancel(sequence: 2), Is.True);
            Assert.That(machine.State, Is.EqualTo(EraClockMachineState.Settled));
        }

        private static EraClockPresentationSnapshot Snapshot(
            int era,
            int phase,
            long sequence,
            EraClockAnchorTarget anchorTarget = EraClockAnchorTarget.Center)
        {
            return new EraClockPresentationSnapshot(era, phase, sequence, anchorTarget);
        }
    }
}
