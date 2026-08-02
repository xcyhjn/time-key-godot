using System;
using System.Collections.Generic;
using NUnit.Framework;
using TimeKey.Application;
using TimeKey.Application.BattleFlow;
using TimeKey.Domain;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Deck;

namespace TimeKey.Tests.EditMode.Application.BattleFlow
{
    public sealed class BattleFlowNextTurnHookTests
    {
        [Test]
        public void InitialStart_DrawsFiveWithoutAdvancingRoundOrAwardingTimecoins()
        {
            var fixture = Fixture();
            fixture.Hook.PrepareInitialStart(1);

            var lifecycle = fixture.Runner.Run(TurnLifecycleRequest.InitialStart());

            Assert.That(lifecycle.Succeeded, Is.True);
            Assert.That(fixture.Hook.Current.Deck.Counts,
                Is.EqualTo(new DeckZoneCounts(7, 5, 0)));
            Assert.That(fixture.Hook.Current.Era, Is.EqualTo(1));
            Assert.That(fixture.Hook.Current.Phase, Is.EqualTo(1));
            Assert.That(fixture.Hook.Current.Timecoins, Is.EqualTo(0));
            Assert.That(fixture.Hook.LastResult.RoundResult, Is.Null);
        }

        [Test]
        public void EndTurn_DiscardsHandAwardsFrozenEmptyCellsAdvancesAndDrawsFive()
        {
            var fixture = StartedFixture();
            fixture.Hook.PrepareEndTurn(
                2,
                6,
                HandIds(fixture.Hook.Current.Hand),
                Array.Empty<TimelineActionPresentationSnapshot>());

            var lifecycle = fixture.Runner.Run(TurnLifecycleRequest.EndTurn(EmptyPlan()));

            Assert.That(lifecycle.Succeeded, Is.True);
            Assert.That(fixture.Hook.LastResult.DiscardResult.MovedCount, Is.EqualTo(5));
            Assert.That(fixture.Hook.LastResult.RoundResult.TimecoinsAwarded, Is.EqualTo(30));
            Assert.That(fixture.Hook.Current.Era, Is.EqualTo(1));
            Assert.That(fixture.Hook.Current.Phase, Is.EqualTo(2));
            Assert.That(fixture.Hook.Current.Timecoins, Is.EqualTo(30));
            Assert.That(fixture.Hook.Current.Deck.Counts,
                Is.EqualTo(new DeckZoneCounts(2, 5, 5)));
        }

        [Test]
        public void RepeatedTurns_RecycleDiscardDeterministicallyWhenDrawPileRunsOut()
        {
            var left = StartedFixture(seed: 731);
            var right = StartedFixture(seed: 731);

            RunEndTurn(left, sequence: 2);
            RunEndTurn(left, sequence: 3);
            RunEndTurn(right, sequence: 2);
            RunEndTurn(right, sequence: 3);

            Assert.That(left.Hook.LastResult.DrawResult.Shuffle.Occurred, Is.True);
            Assert.That(left.Hook.LastResult.DrawResult.Shuffle.MovedCardCount, Is.EqualTo(10));
            Assert.That(StableIds(left.Hook.Current.Hand),
                Is.EqualTo(StableIds(right.Hook.Current.Hand)));
            Assert.That(left.Hook.Current.Deck.Counts,
                Is.EqualTo(new DeckZoneCounts(7, 5, 0)));
        }

        [Test]
        public void DoubleEmptyDeck_IsSuccessfulTypedExhaustionAndKeepsEmptyZones()
        {
            var fixture = Fixture(new DeckState(
                Array.Empty<CardInstance>(),
                seed: 1,
                shuffleInitially: false));
            fixture.Hook.PrepareInitialStart(1);

            var lifecycle = fixture.Runner.Run(TurnLifecycleRequest.InitialStart());

            Assert.That(lifecycle.Succeeded, Is.True);
            Assert.That(fixture.Hook.LastResult.DrawResult.Succeeded, Is.False);
            Assert.That(fixture.Hook.LastResult.DrawResult.Reason,
                Is.EqualTo(DeckOperationReason.Exhausted));
            Assert.That(fixture.Hook.Current.Deck.Counts.Total, Is.EqualTo(0));
        }

        [Test]
        public void ReplayingCompletedSequence_ReturnsSameResultWithoutMovingCardsOrAddingCoins()
        {
            var fixture = StartedFixture();
            fixture.Hook.PrepareEndTurn(
                2,
                30,
                HandIds(fixture.Hook.Current.Hand),
                Array.Empty<TimelineActionPresentationSnapshot>());

            var first = fixture.Hook.ExecutePrepared(
                2,
                TurnLifecycleRequestKind.EndTurn);
            var replay = fixture.Hook.ExecutePrepared(
                2,
                TurnLifecycleRequestKind.EndTurn);

            Assert.That(replay, Is.SameAs(first));
            Assert.That(fixture.Hook.Current.Timecoins, Is.EqualTo(6));
            Assert.That(fixture.Hook.Current.Deck.Counts,
                Is.EqualTo(new DeckZoneCounts(2, 5, 5)));
        }

        [Test]
        public void RoundOverflow_IsRejectedBeforeDiscardingTheHand()
        {
            var deck = DeckState.CreateStarter(731, "battle-overflow");
            deck.Draw(new DeckCommandId("setup/draw"), DeckState.FormalDrawRequest);
            var fixture = Fixture(
                deck,
                new BattleRoundLedger(
                    initialEra: 1,
                    initialPhase: 1,
                    initialTimecoins: int.MaxValue));
            var before = deck.Snapshot();
            fixture.Hook.PrepareEndTurn(
                1,
                35,
                HandIds(deck.Snapshot().Hand),
                Array.Empty<TimelineActionPresentationSnapshot>());

            var result = fixture.Hook.ExecutePrepared(
                1,
                TurnLifecycleRequestKind.EndTurn);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure,
                Is.EqualTo(BattleFlowHookFailure.RoundTransactionFailed));
            Assert.That(deck.Snapshot().Counts, Is.EqualTo(before.Counts));
            Assert.That(InstanceIds(deck.Snapshot().Hand),
                Is.EqualTo(InstanceIds(before.Hand)));
        }

        [TestCase(BattleOutcome.VictorySettlement)]
        [TestCase(BattleOutcome.Defeat)]
        public void TerminalBattle_RejectsHookWithoutDeckOrRoundMutation(BattleOutcome outcome)
        {
            var fixture = Fixture();
            fixture.Settlement.TryResolve(99, outcome);
            fixture.Hook.PrepareInitialStart(1);

            var lifecycle = fixture.Runner.Run(TurnLifecycleRequest.InitialStart());

            Assert.That(lifecycle.Succeeded, Is.False);
            Assert.That(lifecycle.Failure, Is.EqualTo(TurnLifecycleFailure.ReservedHookFailed));
            Assert.That(fixture.Hook.LastResult.Failure,
                Is.EqualTo(BattleFlowHookFailure.TerminalBattle));
            Assert.That(fixture.Hook.Current.Deck.Counts,
                Is.EqualTo(new DeckZoneCounts(12, 0, 0)));
            Assert.That(fixture.Hook.Current.Phase, Is.EqualTo(1));
            Assert.That(fixture.Hook.Current.Timecoins, Is.EqualTo(0));
        }

        [Test]
        public void EndTurn_KeepsImmutableActionDisplayReadableAfterHandIsDiscarded()
        {
            var fixture = StartedFixture();
            var snapshots = new List<TimelineActionPresentationSnapshot>
            {
                ActionSnapshot("action:2:0", "lighting", "落雷")
            };
            fixture.Hook.PrepareEndTurn(
                2,
                1,
                HandIds(fixture.Hook.Current.Hand),
                snapshots);
            snapshots.Clear();

            fixture.Runner.Run(TurnLifecycleRequest.EndTurn(EmptyPlan()));

            Assert.That(fixture.Hook.Current.DiscardPile.Count, Is.EqualTo(5));
            Assert.That(fixture.Hook.Current.ActionDisplaySnapshots.Count, Is.EqualTo(1));
            Assert.That(fixture.Hook.Current.ActionDisplaySnapshots[0].ActionId,
                Is.EqualTo(new TimelineActionIdentity("action:2:0")));
            Assert.That(fixture.Hook.Current.ActionDisplaySnapshots[0].CardStableId,
                Is.EqualTo("lighting"));
            Assert.That(fixture.Hook.Current.ActionDisplaySnapshots[0].Display.Title,
                Is.EqualTo("落雷"));
        }

        [Test]
        public void SameSequenceWithDifferentFrozenPayload_IsTypedConflictWithoutMutation()
        {
            var fixture = StartedFixture();
            var first = fixture.Hook.PrepareEndTurn(
                2,
                2,
                HandIds(fixture.Hook.Current.Hand),
                new[] { ActionSnapshot("action:2:0", "lighting", "落雷") });

            var conflict = fixture.Hook.PrepareEndTurn(
                2,
                3,
                HandIds(fixture.Hook.Current.Hand),
                new[] { ActionSnapshot("action:2:0", "lighting", "落雷") });

            Assert.That(first.Succeeded, Is.True);
            Assert.That(conflict.Succeeded, Is.False);
            Assert.That(conflict.Failure, Is.EqualTo(BattleFlowHookFailure.SequenceConflict));
            Assert.That(fixture.Hook.Current.Deck.Counts,
                Is.EqualTo(new DeckZoneCounts(7, 5, 0)));
            Assert.That(fixture.Hook.Current.Timecoins, Is.EqualTo(0));
        }

        private static void RunEndTurn(FixtureState fixture, long sequence)
        {
            fixture.Hook.PrepareEndTurn(
                sequence,
                occupiedCellCount: 36,
                HandIds(fixture.Hook.Current.Hand),
                Array.Empty<TimelineActionPresentationSnapshot>());
            var result = fixture.Runner.Run(
                TurnLifecycleRequest.EndTurn(EmptyPlan()));
            Assert.That(result.Succeeded, Is.True);
        }

        private static FixtureState StartedFixture(ulong seed = 731)
        {
            var fixture = Fixture(DeckState.CreateStarter(seed, "battle-test"));
            fixture.Hook.PrepareInitialStart(1);
            var result = fixture.Runner.Run(TurnLifecycleRequest.InitialStart());
            Assert.That(result.Succeeded, Is.True);
            return fixture;
        }

        private static FixtureState Fixture(
            DeckState deck = null,
            BattleRoundLedger rounds = null)
        {
            var settlement = new BattleSettlementState(
                "battle-test",
                731,
                new BattleRewardEntry(
                    "reward-test",
                    BattleRewardKind.Acquire,
                    "获得卡牌"));
            var hook = new BattleFlowNextTurnHook(
                deck ?? DeckState.CreateStarter(731, "battle-test"),
                rounds ?? new BattleRoundLedger(),
                settlement);
            return new FixtureState(
                hook,
                settlement,
                new TurnLifecycleRunner(nextTurnHook: hook));
        }

        private static TimelineActionPlan EmptyPlan()
        {
            return new TimelineActionPlan(Array.Empty<TimelineActionPlanEntry>());
        }

        private static TimelineActionPresentationSnapshot ActionSnapshot(
            string actionId,
            string cardStableId,
            string title)
        {
            return new TimelineActionPresentationSnapshot(
                new TimelineActionIdentity(actionId),
                TimelineActorKind.Player,
                priority: 0,
                sourceId: "player",
                sourceCoord: null,
                targetId: "target",
                targetCoord: new HexCoord(0, 0),
                cardStableId,
                effectStableId: cardStableId,
                new TimelineActionDisplayPayload(title, "详情"),
                new TimelineCell(0, 0),
                new[] { new TimelineCell(0, 0) },
                new[] { new TimelineCell(0, 0) },
                Array.Empty<HexCoord>(),
                TimelineActionValidity.Valid,
                TimelineActionInvalidReason.None,
                TimelineActionResolveState.Scheduled);
        }

        private static IReadOnlyList<string> StableIds(IReadOnlyList<CardInstance> cards)
        {
            var values = new List<string>(cards.Count);
            for (var index = 0; index < cards.Count; index++)
            {
                values.Add(cards[index].StableId);
            }

            return values;
        }

        private static IReadOnlyList<CardInstanceId> InstanceIds(
            IReadOnlyList<CardInstance> cards)
        {
            var values = new List<CardInstanceId>(cards.Count);
            for (var index = 0; index < cards.Count; index++)
            {
                values.Add(cards[index].InstanceId);
            }

            return values;
        }

        private static IReadOnlyList<CardInstanceId> HandIds(
            IReadOnlyList<CardInstance> cards)
        {
            return InstanceIds(cards);
        }

        private sealed class FixtureState
        {
            public FixtureState(
                BattleFlowNextTurnHook hook,
                BattleSettlementState settlement,
                TurnLifecycleRunner runner)
            {
                Hook = hook;
                Settlement = settlement;
                Runner = runner;
            }

            public BattleFlowNextTurnHook Hook { get; }

            public BattleSettlementState Settlement { get; }

            public TurnLifecycleRunner Runner { get; }
        }
    }
}
