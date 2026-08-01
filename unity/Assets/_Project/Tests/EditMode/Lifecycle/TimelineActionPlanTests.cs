using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode.Lifecycle
{
    public sealed class TimelineActionPlanTests
    {
        [Test]
        public void ResolutionOrder_MixesActorsByColumnThenRow()
        {
            var plan = new TimelineActionPlan(new[]
            {
                Entry("column-two", TimelineActorKind.Player, "shared", 2, 0),
                Entry("row-two", TimelineActorKind.Enemy, "enemy-intent", 1, 2),
                Entry("row-zero", TimelineActorKind.Player, "shared", 1, 0)
            });

            var succeeded = plan.TryGetResolutionOrder(
                out var ordered,
                out var failure,
                out var reason);

            Assert.That(succeeded, Is.True, reason);
            Assert.That(failure, Is.EqualTo(TimelinePlanFailure.None));
            Assert.That(
                ordered.Select(action => action.ActionId.Value),
                Is.EqualTo(new[] { "row-zero", "row-two", "column-two" }));
            Assert.That(
                ordered.Select(action => action.ActorKind),
                Is.EqualTo(new[]
                {
                    TimelineActorKind.Player,
                    TimelineActorKind.Enemy,
                    TimelineActorKind.Player
                }));
        }

        [Test]
        public void MultiCellAction_IsOneOrderedAction()
        {
            var plan = new TimelineActionPlan(new[]
            {
                new TimelineActionPlanEntry(
                    new TimelineActionIdentity("multi"),
                    TimelineActorKind.Player,
                    "lighting",
                    new TimelineCell(3, 0),
                    new[]
                    {
                        new TimelineCell(3, 0),
                        new TimelineCell(4, 0),
                        new TimelineCell(4, 1)
                    })
            });

            Assert.That(
                plan.TryGetResolutionOrder(out var ordered, out _, out _),
                Is.True);
            Assert.That(ordered, Has.Count.EqualTo(1));
            Assert.That(ordered[0].OccupiedCells, Has.Count.EqualTo(3));
        }

        [Test]
        public void SameDisplayStableId_WithDifferentActionIds_RemainsDistinct()
        {
            var plan = new TimelineActionPlan(new[]
            {
                Entry("first", TimelineActorKind.Player, "lighting", 0, 0),
                Entry("second", TimelineActorKind.Player, "lighting", 1, 0)
            });

            Assert.That(
                plan.TryGetResolutionOrder(out var ordered, out _, out _),
                Is.True);
            Assert.That(ordered, Has.Count.EqualTo(2));
            Assert.That(ordered.Select(action => action.DisplayStableId).Distinct().Count(), Is.EqualTo(1));
            Assert.That(ordered.Select(action => action.ActionId).Distinct().Count(), Is.EqualTo(2));
        }

        [Test]
        public void DuplicateActionId_IsAPlanFailureBeforeResolution()
        {
            var plan = new TimelineActionPlan(new[]
            {
                Entry("duplicate", TimelineActorKind.Player, "lighting", 0, 0),
                Entry("duplicate", TimelineActorKind.Enemy, "enemy-intent", 1, 0)
            });

            var succeeded = plan.TryGetResolutionOrder(
                out var ordered,
                out var failure,
                out var reason);

            Assert.That(succeeded, Is.False);
            Assert.That(failure, Is.EqualTo(TimelinePlanFailure.DuplicateActionIdentity));
            Assert.That(reason, Does.Contain("duplicate"));
            Assert.That(ordered, Is.Empty);
        }

        [Test]
        public void PlanAndEntry_CopyInputCollections()
        {
            var cells = new List<TimelineCell> { new TimelineCell(0, 0) };
            var entry = new TimelineActionPlanEntry(
                new TimelineActionIdentity("stable"),
                TimelineActorKind.Player,
                "lighting",
                new TimelineCell(0, 0),
                cells);
            var entries = new List<TimelineActionPlanEntry> { entry };
            var plan = new TimelineActionPlan(entries);

            cells[0] = new TimelineCell(11, 2);
            entries.Clear();

            Assert.That(plan.Actions, Has.Count.EqualTo(1));
            Assert.That(plan.Actions[0].OccupiedCells[0], Is.EqualTo(new TimelineCell(0, 0)));
        }

        private static TimelineActionPlanEntry Entry(
            string actionId,
            TimelineActorKind actorKind,
            string displayStableId,
            int x,
            int y)
        {
            var cell = new TimelineCell(x, y);
            return new TimelineActionPlanEntry(
                new TimelineActionIdentity(actionId),
                actorKind,
                displayStableId,
                cell,
                new[] { cell });
        }
    }
}
