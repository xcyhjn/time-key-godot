using NUnit.Framework;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode.Lifecycle.Death
{
    public sealed class LifecycleDeathPolicyTests
    {
        [TestCase("tower", null, LifecycleDeathPolicy.Remove)]
        [TestCase("building", "tower", LifecycleDeathPolicy.Remove)]
        [TestCase("radar-underling", null, LifecycleDeathPolicy.Remove)]
        [TestCase("enemy", "radar-underling", LifecycleDeathPolicy.Remove)]
        [TestCase("enemy", null, LifecycleDeathPolicy.RemainBroken)]
        public void Resolve_UsesFrozenTypedRemovalPolicy(
            string kind,
            string creationId,
            LifecycleDeathPolicy expected)
        {
            var store = new TestLifecycleOccupantStore(
                TestLifecycleOccupantStore.Occupant(
                    "subject",
                    0,
                    0,
                    kind: kind,
                    creationId: creationId));
            var occupant = store.Get("subject");

            var result = new DefaultLifecycleDeathPolicyResolver().Resolve(occupant);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void RemovalMutation_RequiresZeroHealthAndStatus()
        {
            Assert.That(
                () => new LifecycleOccupantMutation(
                    "subject",
                    new HexCoord(0, 0),
                    TurnLifecyclePhase.RunningBuildingBehaviors,
                    LifecycleMutationReason.TowerDecay,
                    50,
                    2,
                    0,
                    2,
                    remove: true,
                    deathPolicy: LifecycleDeathPolicy.Remove),
                Throws.ArgumentException);
        }
    }
}
