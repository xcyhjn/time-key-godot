using System.Linq;
using NUnit.Framework;
using TimeKey.Application;
using TimeKey.Application.Intents;
using TimeKey.Domain;
using TimeKey.Domain.Intents;

namespace TimeKey.Tests.EditMode.Intents
{
    public sealed class EnemyIntentApplicationServiceTests
    {
        [Test]
        public void ScheduledSnapshotUsesTheSameActionIdentityAndFrozenPayload()
        {
            var source = EnemyIntentSchedulerTests.Source(
                "village-1",
                new HexCoord(1, 0),
                priority: 999,
                shape: new[] { new TimelineCell(1, 0), new TimelineCell(2, 0) });
            var service = new EnemyIntentApplicationService();
            var scheduled = service.Generate(
                EnemyIntentSchedulerTests.World(new[] { source }),
                8,
                4,
                72).Scheduled.Single();

            var snapshot = service.CreateScheduledSnapshot(scheduled);

            Assert.That(snapshot.ActionId, Is.EqualTo(scheduled.Action.ActionId));
            Assert.That(snapshot.ActorKind, Is.EqualTo(TimelineActorKind.Enemy));
            Assert.That(snapshot.SourceId, Is.EqualTo("village-1"));
            Assert.That(snapshot.SourceCoord, Is.EqualTo(new HexCoord(1, 0)));
            Assert.That(snapshot.TargetId, Is.EqualTo("tile:0,0"));
            Assert.That(snapshot.Shape, Is.EqualTo(source.LiteralShape));
            Assert.That(snapshot.OccupiedCells, Is.EqualTo(scheduled.OccupiedCells));
            Assert.That(snapshot.Display.Title, Is.EqualTo("村庄意图"));
            Assert.That(snapshot.Validity, Is.EqualTo(TimelineActionValidity.Unsupported));
            Assert.That(
                snapshot.InvalidReason,
                Is.EqualTo(TimelineActionInvalidReason.UnsupportedSourceCommand));
            Assert.That(snapshot.ResolveState, Is.EqualTo(TimelineActionResolveState.Scheduled));
        }

        [Test]
        public void ResolvedUnsupportedCommandStaysObservableAndDoesNotBecomeDamageZero()
        {
            var source = EnemyIntentSchedulerTests.Source(
                "village-1",
                new HexCoord(1, 0));
            var world = EnemyIntentSchedulerTests.World(new[] { source });
            var service = new EnemyIntentApplicationService();
            var scheduled = service.Generate(world, 2, 0, 3).Scheduled.Single();

            var result = service.Resolve(scheduled, world);
            var snapshot = service.CreateResolvedSnapshot(scheduled, result);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.EffectApplied, Is.False);
            Assert.That(scheduled.Action.Effects, Is.Empty);
            Assert.That(snapshot.Validity, Is.EqualTo(TimelineActionValidity.Unsupported));
            Assert.That(
                snapshot.InvalidReason,
                Is.EqualTo(TimelineActionInvalidReason.UnsupportedSourceCommand));
            Assert.That(snapshot.ResolveState, Is.EqualTo(TimelineActionResolveState.Resolved));
        }

        [Test]
        public void InvalidResolutionProjectsRemovedSnapshotWithTypedReason()
        {
            var source = EnemyIntentSchedulerTests.Source(
                "village-1",
                new HexCoord(1, 0));
            var world = EnemyIntentSchedulerTests.World(new[] { source });
            var service = new EnemyIntentApplicationService();
            var scheduled = service.Generate(world, 2, 0, 3).Scheduled.Single();
            var currentWorld = EnemyIntentSchedulerTests.World(
                new[]
                {
                    EnemyIntentSchedulerTests.Source(
                        "replacement",
                        new HexCoord(1, 0))
                });

            var result = service.Resolve(scheduled, currentWorld);
            var snapshot = service.CreateResolvedSnapshot(scheduled, result);

            Assert.That(snapshot.ActionId, Is.EqualTo(scheduled.Action.ActionId));
            Assert.That(snapshot.Validity, Is.EqualTo(TimelineActionValidity.Invalid));
            Assert.That(snapshot.InvalidReason, Is.EqualTo(TimelineActionInvalidReason.MissingSource));
            Assert.That(snapshot.ResolveState, Is.EqualTo(TimelineActionResolveState.Removed));
            Assert.That(result.Removal.OccupiedCells, Is.EqualTo(snapshot.OccupiedCells));
        }
    }
}
