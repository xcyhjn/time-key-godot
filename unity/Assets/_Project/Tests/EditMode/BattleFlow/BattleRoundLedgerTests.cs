using NUnit.Framework;
using TimeKey.Domain.BattleFlow;

namespace TimeKey.Tests.EditMode.BattleFlow
{
    public sealed class BattleRoundLedgerTests
    {
        [Test]
        public void NewLedger_StartsAtEraOnePhaseOneWithoutAdvancing()
        {
            var ledger = new BattleRoundLedger();

            Assert.That(ledger.Snapshot.Era, Is.EqualTo(1));
            Assert.That(ledger.Snapshot.Phase, Is.EqualTo(1));
            Assert.That(ledger.Snapshot.Timecoins, Is.Zero);
        }

        [TestCase(0, 36)]
        [TestCase(13, 23)]
        [TestCase(36, 0)]
        public void AdvanceTurn_AwardsOneTimecoinPerEmptyTimelineCell(
            int occupiedCellCount,
            int expectedAward)
        {
            var ledger = new BattleRoundLedger();

            var result = ledger.Apply(
                RoundTransaction.AdvanceTurn(1, occupiedCellCount));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.TimecoinsAwarded, Is.EqualTo(expectedAward));
            Assert.That(result.After.Timecoins, Is.EqualTo(expectedAward));
            Assert.That(result.After.Era, Is.EqualTo(1));
            Assert.That(result.After.Phase, Is.EqualTo(2));
        }

        [Test]
        public void AdvanceTurn_UsesOccupiedCellsRatherThanActionCount()
        {
            var ledger = new BattleRoundLedger();

            var result = ledger.Apply(RoundTransaction.AdvanceTurn(1, 3));

            Assert.That(result.TimecoinsAwarded, Is.EqualTo(33));
        }

        [Test]
        public void AdvanceTurn_PhaseEightRollsIntoNextEraAndContinues()
        {
            var ledger = new BattleRoundLedger(initialEra: 1, initialPhase: 7);

            var phaseEight = ledger.Apply(RoundTransaction.AdvanceTurn(1, 36));
            var nextEra = ledger.Apply(RoundTransaction.AdvanceTurn(2, 36));
            var nextPhase = ledger.Apply(RoundTransaction.AdvanceTurn(3, 36));

            Assert.That(phaseEight.After.Era, Is.EqualTo(1));
            Assert.That(phaseEight.After.Phase, Is.EqualTo(8));
            Assert.That(nextEra.After.Era, Is.EqualTo(2));
            Assert.That(nextEra.After.Phase, Is.EqualTo(1));
            Assert.That(nextPhase.After.Era, Is.EqualTo(2));
            Assert.That(nextPhase.After.Phase, Is.EqualTo(2));
        }

        [Test]
        public void SameSequenceAndPayload_ReturnsFirstResultWithoutMutatingAgain()
        {
            var ledger = new BattleRoundLedger();
            var command = RoundTransaction.AdvanceTurn(7, 30);

            var first = ledger.Apply(command);
            var replay = ledger.Apply(command);

            Assert.That(replay, Is.SameAs(first));
            Assert.That(ledger.Snapshot.Phase, Is.EqualTo(2));
            Assert.That(ledger.Snapshot.Timecoins, Is.EqualTo(6));
        }

        [Test]
        public void SameSequenceWithDifferentPayload_IsTypedConflictWithoutSideEffects()
        {
            var ledger = new BattleRoundLedger();
            ledger.Apply(RoundTransaction.AdvanceTurn(7, 30));
            var before = ledger.Snapshot;

            var conflict = ledger.Apply(RoundTransaction.AdvanceTurn(7, 29));

            Assert.That(conflict.Succeeded, Is.False);
            Assert.That(conflict.Failure, Is.EqualTo(RoundTransactionFailure.SequenceConflict));
            Assert.That(ledger.Snapshot, Is.SameAs(before));
        }

        [Test]
        public void AwardOverflow_FailsWithoutAdvancingRoundOrBalance()
        {
            var ledger = new BattleRoundLedger(initialTimecoins: int.MaxValue - 35);
            var before = ledger.Snapshot;

            var result = ledger.Apply(RoundTransaction.AdvanceTurn(1, 0));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(RoundTransactionFailure.ArithmeticOverflow));
            Assert.That(ledger.Snapshot, Is.SameAs(before));
        }

        [Test]
        public void EraOverflow_FailsBeforeAwardingTimecoins()
        {
            var ledger = new BattleRoundLedger(
                initialEra: int.MaxValue,
                initialPhase: BattleRoundLedger.PhasesPerEra,
                initialTimecoins: 4);
            var before = ledger.Snapshot;

            var result = ledger.Apply(RoundTransaction.AdvanceTurn(1, 35));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(RoundTransactionFailure.ArithmeticOverflow));
            Assert.That(ledger.Snapshot, Is.SameAs(before));
        }

        [TestCase(-1)]
        [TestCase(37)]
        public void InvalidOccupiedCellCount_FailsWithoutSideEffects(int occupiedCellCount)
        {
            var ledger = new BattleRoundLedger(initialTimecoins: 5);
            var before = ledger.Snapshot;

            var result = ledger.Apply(
                RoundTransaction.AdvanceTurn(1, occupiedCellCount));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(RoundTransactionFailure.InvalidOccupiedCellCount));
            Assert.That(ledger.Snapshot, Is.SameAs(before));
        }

        [Test]
        public void SpendTimecoins_SucceedsExactlyOnceForASequence()
        {
            var ledger = new BattleRoundLedger(initialTimecoins: 10);
            var command = RoundTransaction.SpendTimecoins(4, 6);

            var first = ledger.Apply(command);
            var replay = ledger.Apply(command);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.TimecoinsSpent, Is.EqualTo(6));
            Assert.That(replay, Is.SameAs(first));
            Assert.That(ledger.Snapshot.Timecoins, Is.EqualTo(4));
            Assert.That(ledger.Snapshot.Era, Is.EqualTo(1));
            Assert.That(ledger.Snapshot.Phase, Is.EqualTo(1));
        }

        [TestCase(0, RoundTransactionFailure.InvalidAmount)]
        [TestCase(-1, RoundTransactionFailure.InvalidAmount)]
        [TestCase(11, RoundTransactionFailure.InsufficientTimecoins)]
        public void InvalidSpend_FailsWithoutSideEffects(
            int amount,
            RoundTransactionFailure expectedFailure)
        {
            var ledger = new BattleRoundLedger(initialTimecoins: 10);
            var before = ledger.Snapshot;

            var result = ledger.Apply(RoundTransaction.SpendTimecoins(1, amount));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(expectedFailure));
            Assert.That(ledger.Snapshot, Is.SameAs(before));
        }
    }
}
