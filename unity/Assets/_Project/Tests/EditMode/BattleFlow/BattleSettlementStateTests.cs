using System.Collections.Generic;
using NUnit.Framework;
using TimeKey.Domain.BattleFlow;

namespace TimeKey.Tests.EditMode.BattleFlow
{
    public sealed class BattleSettlementStateTests
    {
        [Test]
        public void Victory_IsTerminalAndCreatesOneRewardEntry()
        {
            var settlement = Settlement();

            var result = settlement.TryResolve(1, BattleOutcome.VictorySettlement);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Snapshot.Outcome, Is.EqualTo(BattleOutcome.VictorySettlement));
            Assert.That(result.Snapshot.IsInputLocked, Is.True);
            Assert.That(result.Snapshot.RewardEntry.EntryStableId, Is.EqualTo("reward-entry"));
        }

        [Test]
        public void Defeat_IsTerminalWithoutARewardEntry()
        {
            var settlement = Settlement();

            var result = settlement.TryResolve(1, BattleOutcome.Defeat);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Snapshot.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(result.Snapshot.IsInputLocked, Is.True);
            Assert.That(result.Snapshot.RewardEntry, Is.Null);
        }

        [TestCase(0, BattleOutcome.VictorySettlement, BattleSettlementFailure.InvalidSequence)]
        [TestCase(-1, BattleOutcome.Defeat, BattleSettlementFailure.InvalidSequence)]
        [TestCase(1, BattleOutcome.Active, BattleSettlementFailure.InvalidOutcome)]
        [TestCase(1, (BattleOutcome)99, BattleSettlementFailure.InvalidOutcome)]
        public void InvalidResolutionRequest_IsTypedAndHasNoSideEffects(
            long sequence,
            BattleOutcome outcome,
            BattleSettlementFailure expectedFailure)
        {
            var settlement = Settlement();
            var before = settlement.Snapshot;

            var result = settlement.TryResolve(sequence, outcome);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(expectedFailure));
            Assert.That(settlement.Snapshot, Is.SameAs(before));
        }

        [TestCase(BattleOutcome.VictorySettlement, BattleOutcome.Defeat)]
        [TestCase(BattleOutcome.Defeat, BattleOutcome.VictorySettlement)]
        public void OppositeTerminalOutcome_IsRejectedWithoutReplacingFirst(
            BattleOutcome firstOutcome,
            BattleOutcome competingOutcome)
        {
            var settlement = Settlement();
            settlement.TryResolve(1, firstOutcome);

            var conflict = settlement.TryResolve(2, competingOutcome);

            Assert.That(conflict.Succeeded, Is.False);
            Assert.That(conflict.Failure, Is.EqualTo(BattleSettlementFailure.OutcomeConflict));
            Assert.That(settlement.Snapshot.Outcome, Is.EqualTo(firstOutcome));
        }

        [Test]
        public void RepeatingSameOutcome_ReturnsOriginalResolutionWithoutIssuingAnotherReward()
        {
            var settlement = Settlement();
            var first = settlement.TryResolve(1, BattleOutcome.VictorySettlement);

            var replay = settlement.TryResolve(2, BattleOutcome.VictorySettlement);

            Assert.That(replay, Is.SameAs(first));
            Assert.That(replay.Snapshot.RewardEntry, Is.SameAs(first.Snapshot.RewardEntry));
        }

        [Test]
        public void ReusingResolutionSequenceForDifferentOutcome_IsTypedSequenceConflict()
        {
            var settlement = Settlement();
            settlement.TryResolve(5, BattleOutcome.VictorySettlement);

            var conflict = settlement.TryResolve(5, BattleOutcome.Defeat);

            Assert.That(conflict.Succeeded, Is.False);
            Assert.That(conflict.Failure, Is.EqualTo(BattleSettlementFailure.SequenceConflict));
            Assert.That(settlement.Snapshot.Outcome, Is.EqualTo(BattleOutcome.VictorySettlement));
        }

        [Test]
        public void RewardCanBeClaimedOnceAndSameSequenceIsIdempotent()
        {
            var settlement = Settlement();
            settlement.TryResolve(1, BattleOutcome.VictorySettlement);

            var first = settlement.TryClaimReward(2);
            var replay = settlement.TryClaimReward(2);
            var duplicate = settlement.TryClaimReward(3);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(replay, Is.SameAs(first));
            Assert.That(duplicate.Succeeded, Is.False);
            Assert.That(duplicate.Failure, Is.EqualTo(BattleSettlementFailure.RewardAlreadyClaimed));
            Assert.That(settlement.Snapshot.IsRewardClaimed, Is.True);
        }

        [Test]
        public void DefeatCannotClaimVictoryReward()
        {
            var settlement = Settlement();
            settlement.TryResolve(1, BattleOutcome.Defeat);

            var result = settlement.TryClaimReward(2);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(BattleSettlementFailure.RewardUnavailable));
            Assert.That(settlement.Snapshot.IsRewardClaimed, Is.False);
        }

        [Test]
        public void VictoryReturnBoundaryCarriesTypedSnapshotAndCopiesDeckIds()
        {
            var settlement = Settlement();
            settlement.TryResolve(1, BattleOutcome.VictorySettlement);
            var deck = new List<string> { "lighting", "lighting", "tower" };

            var result = settlement.TryCreateReturnBoundary(
                new BattleRoundSnapshot(3, 7, 42),
                deck);
            deck[0] = "changed";
            deck.Clear();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Payload.Outcome, Is.EqualTo(BattleOutcome.VictorySettlement));
            Assert.That(result.Payload.Completion, Is.EqualTo(BattleReturnCompletion.VictoryCompleted));
            Assert.That(result.Payload.IsCompleted, Is.True);
            Assert.That(result.Payload.Era, Is.EqualTo(3));
            Assert.That(result.Payload.Phase, Is.EqualTo(7));
            Assert.That(result.Payload.Timecoins, Is.EqualTo(42));
            Assert.That(result.Payload.BattleTag, Is.EqualTo("battle-alpha"));
            Assert.That(result.Payload.BattleSeed, Is.EqualTo(9123));
            Assert.That(
                result.Payload.DeckStableIds,
                Is.EqualTo(new[] { "lighting", "lighting", "tower" }));
        }

        [Test]
        public void DefeatReturnBoundaryNeverMasqueradesAsCompleted()
        {
            var settlement = Settlement();
            settlement.TryResolve(1, BattleOutcome.Defeat);

            var result = settlement.TryCreateReturnBoundary(
                new BattleRoundSnapshot(1, 2, 5),
                new[] { "poison" });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Payload.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(result.Payload.Completion, Is.EqualTo(BattleReturnCompletion.Defeat));
            Assert.That(result.Payload.IsCompleted, Is.False);
        }

        [Test]
        public void ActiveBattleCannotCreateReturnBoundary()
        {
            var settlement = Settlement();

            var result = settlement.TryCreateReturnBoundary(
                new BattleRoundSnapshot(1, 1, 0),
                new[] { "wind" });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(BattleReturnFailure.OutcomeNotResolved));
            Assert.That(result.Payload, Is.Null);
            Assert.That(settlement.Snapshot.Outcome, Is.EqualTo(BattleOutcome.Active));
        }

        [Test]
        public void InvalidDeckSnapshotFailsWithoutChangingSettlement()
        {
            var settlement = Settlement();
            settlement.TryResolve(1, BattleOutcome.VictorySettlement);
            var before = settlement.Snapshot;

            var result = settlement.TryCreateReturnBoundary(
                new BattleRoundSnapshot(1, 2, 5),
                new[] { "valid", " " });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(BattleReturnFailure.InvalidDeckStableId));
            Assert.That(result.Payload, Is.Null);
            Assert.That(settlement.Snapshot, Is.SameAs(before));
        }

        private static BattleSettlementState Settlement()
        {
            return new BattleSettlementState(
                "battle-alpha",
                9123,
                new BattleRewardEntry(
                    "reward-entry",
                    BattleRewardKind.Acquire,
                    "获得卡牌"));
        }
    }
}
