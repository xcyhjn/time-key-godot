using System;
using System.Linq;
using NUnit.Framework;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode.Lifecycle.Statuses
{
    public sealed class PoisonTurnStartProcessorTests
    {
        [Test]
        public void Process_EmptySnapshotSucceedsWithoutMutation()
        {
            var store = new TestLifecycleOccupantStore();
            var processor = new PoisonTurnStartProcessor(store);

            var result = processor.Process(1);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.SpreadResults, Is.Empty);
            Assert.That(result.DamageResults, Is.Empty);
            Assert.That(result.DecayResults, Is.Empty);
            Assert.That(store.ApplyCalls, Is.Zero);
        }

        [Test]
        public void Process_SingleSourceSpreadsToAllSixOccupiedAxialNeighbors()
        {
            var store = new TestLifecycleOccupantStore(
                Occupant("source", 0, 0, poison: 1),
                Occupant("east", 1, 0),
                Occupant("north-east", 1, -1),
                Occupant("north-west", 0, -1),
                Occupant("west", -1, 0),
                Occupant("south-west", -1, 1),
                Occupant("south-east", 0, 1));
            var processor = new PoisonTurnStartProcessor(store);

            var result = processor.Process(1);

            Assert.That(result.SpreadResults, Has.Count.EqualTo(6));
            Assert.That(result.SpreadResults.Select(item => item.TargetRuntimeId),
                Is.EquivalentTo(new[]
                {
                    "east", "north-east", "north-west", "west", "south-west", "south-east"
                }));
            Assert.That(store.Get("source").Hp, Is.EqualTo(90));
            Assert.That(store.Get("source").PoisonStacks, Is.Zero);
            Assert.That(store.Get("east").PoisonStacks, Is.EqualTo(1));
        }

        [Test]
        public void Process_MultipleOldSourcesAggregateBeforeOneAtomicCommit()
        {
            var store = new TestLifecycleOccupantStore(
                Occupant("left", -1, 0, poison: 1),
                Occupant("upper", 0, -1, poison: 1),
                Occupant("target", 0, 0));
            var processor = new PoisonTurnStartProcessor(store);

            var result = processor.Process(1);

            Assert.That(
                result.SpreadResults.Count(item => item.TargetRuntimeId == "target"),
                Is.EqualTo(2));
            Assert.That(store.Get("target").PoisonStacks, Is.EqualTo(2));
            Assert.That(store.ApplyCalls, Is.EqualTo(1));
            Assert.That(store.LastMutations.Select(item => item.RuntimeId),
                Is.EqualTo(new[] { "left", "upper", "target" }));
        }

        [Test]
        public void Process_EmptyBoundaryDeadAndStatusUnsupportedNeighborsAreFiltered()
        {
            var store = new TestLifecycleOccupantStore(
                Occupant("source", 0, 0, poison: 1),
                Occupant("dead", 1, 0, hp: 0),
                Occupant(
                    "unsupported",
                    0,
                    1,
                    supportsStatus: false));
            var processor = new PoisonTurnStartProcessor(store);

            var result = processor.Process(1);

            Assert.That(result.SpreadResults, Is.Empty);
            Assert.That(store.Get("dead").PoisonStacks, Is.Zero);
            Assert.That(store.Get("unsupported").PoisonStacks, Is.Zero);
        }

        [Test]
        public void Process_DamageUsesSnapshotMaxHpAndStacksWithExactCeiling()
        {
            var store = new TestLifecycleOccupantStore(
                Occupant("source", 0, 0, hp: 101, maxHp: 101, poison: 2));
            var processor = new PoisonTurnStartProcessor(store);

            var result = processor.Process(1);

            var damage = result.DamageResults.Single();
            Assert.That(damage.SnapshotPoisonStacks, Is.EqualTo(2));
            Assert.That(damage.RequestedDamage, Is.EqualTo(21));
            Assert.That(damage.AppliedDamage, Is.EqualTo(21));
            Assert.That(damage.AfterHp, Is.EqualTo(80));
            Assert.That(store.Get("source").PoisonStacks, Is.EqualTo(1));
        }

        [Test]
        public void Process_NewInfectionDoesNotSpreadTakeDamageOrDecayInSameCycle()
        {
            var store = new TestLifecycleOccupantStore(
                Occupant("source", 0, 0, poison: 1),
                Occupant("new", 1, 0),
                Occupant("beyond", 2, 0));
            var processor = new PoisonTurnStartProcessor(store);

            var result = processor.Process(1);

            Assert.That(result.SpreadResults.Select(item => item.TargetRuntimeId),
                Is.EqualTo(new[] { "new" }));
            Assert.That(result.DamageResults.Select(item => item.RuntimeId),
                Is.EqualTo(new[] { "source" }));
            Assert.That(result.DecayResults.Select(item => item.RuntimeId),
                Is.EqualTo(new[] { "source" }));
            Assert.That(store.Get("new").Hp, Is.EqualTo(100));
            Assert.That(store.Get("new").PoisonStacks, Is.EqualTo(1));
            Assert.That(store.Get("beyond").PoisonStacks, Is.Zero);
        }

        [Test]
        public void Process_OldSourcesRetainNeighborSpreadButDecayExactlyOnce()
        {
            var store = new TestLifecycleOccupantStore(
                Occupant("left", 0, 0, poison: 1),
                Occupant("right", 1, 0, poison: 1));
            var processor = new PoisonTurnStartProcessor(store);

            var result = processor.Process(1);

            Assert.That(result.DecayResults.Select(item => item.BeforeDecayStacks),
                Is.EqualTo(new[] { 2, 2 }));
            Assert.That(result.DecayResults.Select(item => item.AfterStacks),
                Is.EqualTo(new[] { 1, 1 }));
            Assert.That(store.Get("left").PoisonStacks, Is.EqualTo(1));
            Assert.That(store.Get("right").PoisonStacks, Is.EqualTo(1));
        }

        [Test]
        public void Process_StackOverflowFailsBeforeAnyMutation()
        {
            var store = new TestLifecycleOccupantStore(
                Occupant("source", 0, 0, poison: 1),
                Occupant("overflow", 1, 0, poison: int.MaxValue));
            var processor = new PoisonTurnStartProcessor(store);

            var result = processor.Process(1);

            Assert.That(result.Failure, Is.EqualTo(PoisonTurnStartFailure.StackOverflow));
            Assert.That(result.OccupantChanges, Is.Empty);
            Assert.That(store.ApplyCalls, Is.Zero);
            Assert.That(store.Get("source").Hp, Is.EqualTo(100));
            Assert.That(store.Get("overflow").PoisonStacks, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void Process_DeathRemovesTowerButKeepsOrdinaryEnemyBrokenAndClearsStatus()
        {
            var store = new TestLifecycleOccupantStore(
                Occupant(
                    "tower",
                    0,
                    0,
                    hp: 1,
                    maxHp: 10,
                    poison: 1,
                    kind: "tower",
                    creationId: "tower"),
                Occupant("enemy", 3, 0, hp: 1, maxHp: 10, poison: 1));
            var processor = new PoisonTurnStartProcessor(store);

            var result = processor.Process(1);

            Assert.That(store.ApplyCalls, Is.EqualTo(1));
            Assert.That(store.Contains("tower"), Is.False);
            Assert.That(store.Contains("enemy"), Is.True);
            Assert.That(store.Get("enemy").Hp, Is.Zero);
            Assert.That(store.Get("enemy").PoisonStacks, Is.Zero);
            Assert.That(
                result.OccupantChanges.Single(item => item.RuntimeId == "tower").Removed,
                Is.True);
            Assert.That(
                result.OccupantChanges.Single(item => item.RuntimeId == "enemy").DeathPolicy,
                Is.EqualTo(LifecycleDeathPolicy.RemainBroken));
            Assert.That(result.DecayResults, Is.All.Matches<PoisonDecayResult>(
                item => item.SkippedBecauseDead && item.AfterStacks == 0));
        }

        [Test]
        public void Process_ResultsAndMutationBatchUseStableCoordinateOrder()
        {
            var store = new TestLifecycleOccupantStore(
                Occupant("z", 2, 0, poison: 1),
                Occupant("b", 0, 1, poison: 1),
                Occupant("a", 0, 0, poison: 1));
            var processor = new PoisonTurnStartProcessor(store);

            var result = processor.Process(1);

            Assert.That(result.DamageResults.Select(item => item.RuntimeId),
                Is.EqualTo(new[] { "a", "b", "z" }));
            Assert.That(result.DecayResults.Select(item => item.RuntimeId),
                Is.EqualTo(new[] { "a", "b", "z" }));
            Assert.That(store.LastMutations.Select(item => item.RuntimeId),
                Is.EqualTo(new[] { "a", "b", "z" }));
        }

        [Test]
        public void Process_RepeatedSequenceDoesNotTickAgain()
        {
            var store = new TestLifecycleOccupantStore(
                Occupant("source", 0, 0, poison: 2));
            var processor = new PoisonTurnStartProcessor(store);

            var first = processor.Process(9);
            var repeated = processor.Process(9);

            Assert.That(first.AlreadyProcessed, Is.False);
            Assert.That(repeated.AlreadyProcessed, Is.True);
            Assert.That(repeated.OccupantChanges, Is.Empty);
            Assert.That(store.Get("source").Hp, Is.EqualTo(80));
            Assert.That(store.Get("source").PoisonStacks, Is.EqualTo(1));
            Assert.That(store.CaptureCalls, Is.EqualTo(1));
            Assert.That(store.ApplyCalls, Is.EqualTo(1));
        }

        [Test]
        public void Process_RejectedAtomicBatchPublishesNoPassResultsAndCanRetry()
        {
            var store = new TestLifecycleOccupantStore(
                Occupant("source", 0, 0, poison: 1),
                Occupant("target", 1, 0))
            {
                RejectNextApply = true
            };
            var processor = new PoisonTurnStartProcessor(store);

            var failed = processor.Process(4);
            var retried = processor.Process(4);

            Assert.That(failed.Failure,
                Is.EqualTo(PoisonTurnStartFailure.StoreRejectedMutation));
            Assert.That(failed.SpreadResults, Is.Empty);
            Assert.That(failed.DamageResults, Is.Empty);
            Assert.That(failed.DecayResults, Is.Empty);
            Assert.That(failed.OccupantChanges, Is.Empty);
            Assert.That(retried.Succeeded, Is.True);
            Assert.That(retried.AlreadyProcessed, Is.False);
            Assert.That(store.Get("target").PoisonStacks, Is.EqualTo(1));
        }

        [Test]
        public void Runner_EndTurnExecutesTowerBeforePoisonThroughSharedLifecycle()
        {
            var store = new TestLifecycleOccupantStore(
                Occupant(
                    "tower",
                    0,
                    0,
                    hp: 100,
                    maxHp: 100,
                    poison: 1,
                    kind: "tower",
                    creationId: "tower"));
            var tower = new TowerBuildingBehaviorProcessor(store);
            var poison = new PoisonTurnStartProcessor(store);
            var runner = new TurnLifecycleRunner(
                buildingBehaviorProcessor: tower,
                turnStartStatusProcessor: poison);

            var result = runner.Run(TurnLifecycleRequest.EndTurn(
                new TimelineActionPlan(Array.Empty<TimelineActionPlanEntry>())));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(tower.LastResult.Changes.Single().BeforeHp, Is.EqualTo(100));
            Assert.That(tower.LastResult.Changes.Single().AfterHp, Is.EqualTo(50));
            Assert.That(poison.LastResult.DamageResults.Single().BeforeHp, Is.EqualTo(50));
            Assert.That(poison.LastResult.DamageResults.Single().AfterHp, Is.EqualTo(40));
            Assert.That(store.Get("tower").Hp, Is.EqualTo(40));
        }

        private static CombatOccupantState Occupant(
            string runtimeId,
            int q,
            int r,
            int hp = 100,
            int maxHp = 100,
            int poison = 0,
            bool supportsStatus = true,
            string kind = "enemy",
            string creationId = null)
        {
            return TestLifecycleOccupantStore.Occupant(
                runtimeId,
                q,
                r,
                hp,
                maxHp,
                poison,
                supportsHealth: true,
                supportsStatus: supportsStatus,
                kind: kind,
                creationId: creationId);
        }
    }
}
