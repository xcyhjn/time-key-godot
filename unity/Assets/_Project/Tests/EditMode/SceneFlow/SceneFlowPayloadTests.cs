using System.Collections.Generic;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.BattleFlow;

namespace TimeKey.Tests.EditMode.SceneFlow
{
    public sealed class SceneFlowPayloadTests
    {
        [Test]
        public void CombatLaunchPayload_DefensivelyCopiesDeckAndTargetsCombat()
        {
            var deck = new List<string> { "lighting", "recover" };
            var launch = Launch(deck);
            deck[0] = "mutated";

            Assert.That(launch.TargetScene, Is.EqualTo(SceneId.Combat));
            Assert.That(launch.DeckStableIds, Is.EqualTo(new[] { "lighting", "recover" }));
            Assert.That(launch.Fingerprint, Does.Contain("run-731"));
        }

        [Test]
        public void CombatLaunchPayload_FingerprintCoversCharacterChapterAndDeck()
        {
            var baseline = Launch(new[] { "lighting", "recover" });
            var differentCharacter = new CombatLaunchPayload(
                baseline.LaunchCorrelationId,
                baseline.RunId,
                baseline.RunSeed,
                baseline.Chapter,
                baseline.Era,
                baseline.Phase,
                baseline.RoomId,
                "different-character",
                baseline.Timecoins,
                baseline.BattleTag,
                baseline.BattleSeed,
                baseline.DeckStableIds);
            var differentDeck = Launch(new[] { "lighting", "wind" });

            Assert.That(differentCharacter.Fingerprint, Is.Not.EqualTo(baseline.Fingerprint));
            Assert.That(differentDeck.Fingerprint, Is.Not.EqualTo(baseline.Fingerprint));
        }

        [Test]
        public void CombatOutcome_VictoryRequiresClaimedRewardAndPreservesLaunchIdentity()
        {
            var launch = Launch(new[] { "lighting", "recover" });
            var settlement = Settlement(launch);
            settlement.TryResolve(1, BattleOutcome.VictorySettlement);
            var returnResult = settlement.TryCreateReturnBoundary(
                new BattleRoundLedger(1, 1, 0).Snapshot,
                launch.DeckStableIds);

            var beforeClaim = CombatOutcome.TryCreate(
                "outcome-1",
                launch,
                settlement.Snapshot,
                returnResult.Payload);
            settlement.TryClaimReward(2);
            var afterClaim = CombatOutcome.TryCreate(
                "outcome-1",
                launch,
                settlement.Snapshot,
                returnResult.Payload);

            Assert.That(beforeClaim.Failure, Is.EqualTo(CombatOutcomeFailure.RewardNotClaimed));
            Assert.That(afterClaim.Succeeded, Is.True);
            Assert.That(afterClaim.Outcome.RunId, Is.EqualTo(launch.RunId));
            Assert.That(afterClaim.Outcome.RoomId, Is.EqualTo(launch.RoomId));
            Assert.That(afterClaim.Outcome.LaunchCorrelationId,
                Is.EqualTo(launch.LaunchCorrelationId));
        }

        [Test]
        public void OutOfBattleShellState_AppliesOutcomeOnceAndRejectsConflict()
        {
            var launch = Launch(new[] { "lighting", "recover" });
            var outcome = VictoryOutcome(launch, "outcome-1");
            var shell = new OutOfBattleShellState(launch);

            var first = shell.TryApplyOutcome(outcome);
            var repeated = shell.TryApplyOutcome(outcome);
            var otherLaunch = new CombatLaunchPayload(
                launch.LaunchCorrelationId,
                launch.RunId,
                launch.RunSeed,
                launch.Chapter,
                launch.Era,
                launch.Phase,
                "room-2",
                launch.CharacterId,
                launch.Timecoins,
                launch.BattleTag,
                launch.BattleSeed,
                launch.DeckStableIds);
            var conflict = shell.TryApplyOutcome(VictoryOutcome(otherLaunch, "outcome-1"));

            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.WasAlreadyApplied, Is.False);
            Assert.That(repeated.Succeeded, Is.True);
            Assert.That(repeated.WasAlreadyApplied, Is.True);
            Assert.That(conflict.Failure, Is.EqualTo(CombatOutcomeApplyFailure.OutcomeConflict));
            Assert.That(shell.SettledRoomIds, Does.Contain(launch.RoomId));
        }

        [Test]
        public void CombatOutcome_RejectsMismatchedBattleIdentity()
        {
            var launch = Launch(new[] { "lighting" });
            var settlement = new BattleSettlementState(
                "different-battle",
                launch.BattleSeed,
                new BattleRewardEntry("reward", BattleRewardKind.Acquire, "获得卡牌"));
            settlement.TryResolve(1, BattleOutcome.Defeat);
            var boundary = settlement.TryCreateReturnBoundary(
                new BattleRoundLedger(1, 1, 0).Snapshot,
                launch.DeckStableIds);

            var result = CombatOutcome.TryCreate(
                "outcome-mismatch",
                launch,
                settlement.Snapshot,
                boundary.Payload);

            Assert.That(result.Failure, Is.EqualTo(CombatOutcomeFailure.LaunchMismatch));
        }

        private static CombatLaunchPayload Launch(IReadOnlyList<string> deck)
        {
            return new CombatLaunchPayload(
                "launch-731",
                "run-731",
                731,
                1,
                1,
                1,
                "room-1",
                "character-silver",
                0,
                "combat-vertical-slice",
                731,
                deck);
        }

        private static BattleSettlementState Settlement(CombatLaunchPayload launch)
        {
            return new BattleSettlementState(
                launch.BattleTag,
                launch.BattleSeed,
                new BattleRewardEntry("reward", BattleRewardKind.Acquire, "获得卡牌"));
        }

        private static CombatOutcome VictoryOutcome(
            CombatLaunchPayload launch,
            string outcomeCorrelationId)
        {
            var settlement = Settlement(launch);
            settlement.TryResolve(1, BattleOutcome.VictorySettlement);
            settlement.TryClaimReward(2);
            var boundary = settlement.TryCreateReturnBoundary(
                new BattleRoundLedger(1, 1, 0).Snapshot,
                launch.DeckStableIds);
            return CombatOutcome.TryCreate(
                outcomeCorrelationId,
                launch,
                settlement.Snapshot,
                boundary.Payload).Outcome;
        }
    }
}
