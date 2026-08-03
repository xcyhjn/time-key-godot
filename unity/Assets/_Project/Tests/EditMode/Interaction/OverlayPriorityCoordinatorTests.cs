using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Presentation.Interaction;

namespace TimeKey.Tests.EditMode.Interaction
{
    public sealed class OverlayPriorityCoordinatorTests
    {
        [Test]
        public void HigherPriorityReplacesLowerAndLowerCannotTakeOwnershipBack()
        {
            var coordinator = new OverlayPriorityCoordinator();
            Assert.That(
                coordinator.TryAcquire(
                    CombatOverlayOwner.IdleActionHover,
                    "action:a",
                    out var idle),
                Is.True);
            Assert.That(
                coordinator.TryAcquire(
                    CombatOverlayOwner.CardTargeting,
                    "card:i1",
                    out var targeting),
                Is.True);
            Assert.That(
                coordinator.TryAcquire(
                    CombatOverlayOwner.IdleActionHover,
                    "action:b",
                    out _),
                Is.False);
            Assert.That(coordinator.Owner, Is.EqualTo(CombatOverlayOwner.CardTargeting));
            Assert.That(coordinator.Release(idle), Is.False);
            Assert.That(coordinator.Release(targeting), Is.True);
            Assert.That(coordinator.IsEmpty, Is.True);
        }

        [Test]
        public void IdleTileInspectTogglesSameCellAndReplacesDifferentCell()
        {
            var port = new IdleTileInspectPort();
            var first = new HexCoord(0, 0);
            var second = new HexCoord(1, 0);
            Assert.That(port.Set(first), Is.True);
            Assert.That(port.State.Coordinate, Is.EqualTo(first));
            Assert.That(port.Toggle(first), Is.True);
            Assert.That(port.State.IsInspecting, Is.False);
            Assert.That(port.Toggle(first), Is.True);
            Assert.That(port.Toggle(second), Is.True);
            Assert.That(port.State.Coordinate, Is.EqualTo(second));
            Assert.That(port.Clear(), Is.True);
            Assert.That(port.Clear(), Is.False);
        }

        [Test]
        public void RightClickPolicySeparatesShortCancelFromOrbitDrag()
        {
            var policy = new RightClickGesturePolicy(8f);
            Assert.That(policy.Begin(new PointerPoint(10f, 10f), false), Is.True);
            Assert.That(
                policy.End(new PointerPoint(14f, 13f), false),
                Is.EqualTo(RightClickGestureResult.ShortClick));
            Assert.That(policy.Begin(new PointerPoint(10f, 10f), false), Is.True);
            policy.Move(new PointerPoint(20f, 10f));
            Assert.That(
                policy.End(new PointerPoint(20f, 10f), false),
                Is.EqualTo(RightClickGestureResult.Drag));
            Assert.That(policy.Begin(new PointerPoint(10f, 10f), true), Is.False);
        }
    }
}
