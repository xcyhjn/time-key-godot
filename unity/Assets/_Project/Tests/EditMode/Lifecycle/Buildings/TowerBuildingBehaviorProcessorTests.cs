using System.Linq;
using NUnit.Framework;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode.Lifecycle.Buildings
{
    public sealed class TowerBuildingBehaviorProcessorTests
    {
        [Test]
        public void Process_EmptySnapshotSucceedsWithoutMutation()
        {
            var store = new TestLifecycleOccupantStore();
            var processor = new TowerBuildingBehaviorProcessor(store);

            var result = processor.Process(1);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changes, Is.Empty);
            Assert.That(store.ApplyCalls, Is.Zero);
        }

        [Test]
        public void Process_SortsTowersByCoordinateThenRuntimeId()
        {
            var store = new TestLifecycleOccupantStore(
                Tower("z", 1, 0),
                Tower("b", 0, 0),
                Tower("a", 0, 0));
            var processor = new TowerBuildingBehaviorProcessor(store);

            var result = processor.Process(1);

            Assert.That(
                result.Changes.Select(change => change.RuntimeId),
                Is.EqualTo(new[] { "a", "b", "z" }));
            Assert.That(store.ApplyCalls, Is.EqualTo(1));
        }

        [Test]
        public void Process_TowerRunsInCreationCycleThenRemovesOnNextCycle()
        {
            var store = new TestLifecycleOccupantStore(Tower("tower", 0, 0));
            var processor = new TowerBuildingBehaviorProcessor(store);

            var first = processor.Process(1);
            var second = processor.Process(2);

            Assert.That(first.Changes.Single().BeforeHp, Is.EqualTo(100));
            Assert.That(first.Changes.Single().AfterHp, Is.EqualTo(50));
            Assert.That(first.Changes.Single().Removed, Is.False);
            Assert.That(second.Changes.Single().BeforeHp, Is.EqualTo(50));
            Assert.That(second.Changes.Single().AfterHp, Is.Zero);
            Assert.That(second.Changes.Single().Removed, Is.True);
            Assert.That(second.Changes.Single().DeathPolicy,
                Is.EqualTo(LifecycleDeathPolicy.Remove));
            Assert.That(store.Contains("tower"), Is.False);
        }

        [Test]
        public void Process_DeadTowerAndOrdinaryOccupantAreNotExecuted()
        {
            var store = new TestLifecycleOccupantStore(
                Tower("dead", 0, 0, hp: 0),
                TestLifecycleOccupantStore.Occupant("enemy", 1, 0));
            var processor = new TowerBuildingBehaviorProcessor(store);

            var result = processor.Process(1);

            Assert.That(result.Changes, Is.Empty);
            Assert.That(store.ApplyCalls, Is.Zero);
            Assert.That(store.Get("enemy").Hp, Is.EqualTo(100));
        }

        [Test]
        public void Process_RepeatedSequenceIsIdempotent()
        {
            var store = new TestLifecycleOccupantStore(Tower("tower", 0, 0));
            var processor = new TowerBuildingBehaviorProcessor(store);

            var first = processor.Process(7);
            var repeated = processor.Process(7);

            Assert.That(first.AlreadyProcessed, Is.False);
            Assert.That(repeated.Succeeded, Is.True);
            Assert.That(repeated.AlreadyProcessed, Is.True);
            Assert.That(repeated.Changes, Is.Empty);
            Assert.That(store.Get("tower").Hp, Is.EqualTo(50));
            Assert.That(store.CaptureCalls, Is.EqualTo(1));
            Assert.That(store.ApplyCalls, Is.EqualTo(1));
        }

        [Test]
        public void Process_RejectedAtomicMutationDoesNotLatchSequence()
        {
            var store = new TestLifecycleOccupantStore(Tower("tower", 0, 0))
            {
                RejectNextApply = true
            };
            var processor = new TowerBuildingBehaviorProcessor(store);

            var failed = processor.Process(3);
            var retried = processor.Process(3);

            Assert.That(failed.Failure, Is.EqualTo(TowerBehaviorFailure.StoreRejectedMutation));
            Assert.That(failed.Changes, Is.Empty);
            Assert.That(retried.Succeeded, Is.True);
            Assert.That(retried.AlreadyProcessed, Is.False);
            Assert.That(store.Get("tower").Hp, Is.EqualTo(50));
        }

        private static CombatOccupantState Tower(
            string runtimeId,
            int q,
            int r,
            int hp = 100)
        {
            return TestLifecycleOccupantStore.Occupant(
                runtimeId,
                q,
                r,
                hp,
                100,
                kind: "tower",
                creationId: "tower");
        }
    }
}
