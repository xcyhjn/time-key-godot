using System.Linq;
using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Domain.Intents;

namespace TimeKey.Tests.EditMode.Intents
{
    public sealed class EnemyIntentResolverTests
    {
        [Test]
        public void Resolve_EmptySourceCommandIsSuccessfulExplicitNoEffect()
        {
            var source = Source(shape: new[]
            {
                new TimelineCell(1, 0),
                new TimelineCell(2, 0)
            });
            var world = EnemyIntentSchedulerTests.World(new[] { source });
            var scheduled = Schedule(world, width: 3, height: 1);

            var result = new EnemyIntentResolver().Resolve(scheduled, world);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.EffectApplied, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(EnemyIntentResolveOutcome.UnsupportedSourceCommand));
            Assert.That(result.InvalidReason, Is.EqualTo(EnemyIntentInvalidReason.None));
            Assert.That(result.Removal.ActionId, Is.EqualTo(scheduled.Action.ActionId));
            Assert.That(result.Removal.OccupiedCells, Is.EqualTo(scheduled.OccupiedCells));
        }

        [Test]
        public void Resolve_NonEmptyUnregisteredCommandIsExplicitUnsupportedEffect()
        {
            var source = Source(command: "expand 1");
            var world = EnemyIntentSchedulerTests.World(new[] { source });
            var result = new EnemyIntentResolver().Resolve(Schedule(world), world);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.EffectApplied, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(EnemyIntentResolveOutcome.UnsupportedEffect));
        }

        [Test]
        public void Resolve_SourceReplacementAtSameCoordinateIsMissingSource()
        {
            var original = Source();
            var scheduled = Schedule(EnemyIntentSchedulerTests.World(new[] { original }));
            var replacement = EnemyIntentSchedulerTests.Source(
                "replacement",
                original.Coordinate);
            var current = EnemyIntentSchedulerTests.World(new[] { replacement });

            var result = new EnemyIntentResolver().Resolve(scheduled, current);

            AssertInvalid(result, EnemyIntentInvalidReason.MissingSource, scheduled);
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void Resolve_RechecksSourceAliveAndCapability(bool isAlive, bool canGenerate)
        {
            var original = Source();
            var scheduled = Schedule(EnemyIntentSchedulerTests.World(new[] { original }));
            var changed = Source(isAlive: isAlive, canGenerate: canGenerate);

            var result = new EnemyIntentResolver().Resolve(
                scheduled,
                EnemyIntentSchedulerTests.World(new[] { changed }));

            AssertInvalid(result, EnemyIntentInvalidReason.SourceUnavailable, scheduled);
        }

        [Test]
        public void Resolve_RechecksTargetTileAndDoesNotSilentlyRetarget()
        {
            var original = Source();
            var scheduled = Schedule(EnemyIntentSchedulerTests.World(new[] { original }));
            var movedTarget = Source(
                target: new EnemyIntentTargetSnapshot("tile:2,0", new HexCoord(2, 0)));

            var movedResult = new EnemyIntentResolver().Resolve(
                scheduled,
                EnemyIntentSchedulerTests.World(new[] { movedTarget }));
            var missingResult = new EnemyIntentResolver().Resolve(
                scheduled,
                EnemyIntentSchedulerTests.World(
                    new[] { original },
                    tiles: new[] { original.Coordinate }));

            AssertInvalid(movedResult, EnemyIntentInvalidReason.TargetUnavailable, scheduled);
            AssertInvalid(missingResult, EnemyIntentInvalidReason.MissingTarget, scheduled);
        }

        [TestCase("replacement", 100, CombatAttitude.Enemy)]
        [TestCase("target", 0, CombatAttitude.Enemy)]
        [TestCase("target", 100, CombatAttitude.Player)]
        public void Resolve_RechecksTargetIdentityLifeAndAttitude(
            string runtimeId,
            int hp,
            CombatAttitude attitude)
        {
            var target = new EnemyIntentTargetSnapshot(
                "target",
                new HexCoord(0, 0),
                true,
                "target",
                CombatAttitude.Enemy);
            var original = Source(target: target);
            var originalOccupant = Occupant("target", 100, CombatAttitude.Enemy);
            var scheduled = Schedule(EnemyIntentSchedulerTests.World(
                new[] { original },
                new[] { originalOccupant }));
            var currentOccupant = Occupant(runtimeId, hp, attitude);

            var result = new EnemyIntentResolver().Resolve(
                scheduled,
                EnemyIntentSchedulerTests.World(
                    new[] { original },
                    new[] { currentOccupant }));

            AssertInvalid(result, EnemyIntentInvalidReason.TargetUnavailable, scheduled);
        }

        [Test]
        public void Resolve_RechecksLiteralShapeAndEffectDefinition()
        {
            var original = Source();
            var scheduled = Schedule(EnemyIntentSchedulerTests.World(new[] { original }));
            var shapeChanged = Source(shape: new[]
            {
                new TimelineCell(0, 0),
                new TimelineCell(1, 0)
            });
            var effectChanged = Source(effectStableId: "changed-effect");

            var shapeResult = new EnemyIntentResolver().Resolve(
                scheduled,
                EnemyIntentSchedulerTests.World(new[] { shapeChanged }));
            var effectResult = new EnemyIntentResolver().Resolve(
                scheduled,
                EnemyIntentSchedulerTests.World(new[] { effectChanged }));

            AssertInvalid(shapeResult, EnemyIntentInvalidReason.ShapeMismatch, scheduled);
            AssertInvalid(effectResult, EnemyIntentInvalidReason.EffectMismatch, scheduled);
        }

        [Test]
        public void Resolve_TopologyChangeThatAltersEffectRangeIsEffectMismatch()
        {
            var original = Source(range: new[]
            {
                new HexCoord(0, 0),
                new HexCoord(2, 0)
            });
            var originalWorld = EnemyIntentSchedulerTests.World(new[] { original });
            var scheduled = Schedule(originalWorld);
            var currentWorld = EnemyIntentSchedulerTests.World(
                new[] { original },
                tiles: new[] { original.Coordinate, new HexCoord(0, 0) });

            var result = new EnemyIntentResolver().Resolve(scheduled, currentWorld);

            AssertInvalid(result, EnemyIntentInvalidReason.EffectMismatch, scheduled);
        }

        private static EnemyIntentScheduledAction Schedule(
            EnemyIntentWorldSnapshot world,
            int width = 12,
            int height = 3)
        {
            return new EnemyIntentScheduler().Generate(
                world,
                5,
                2,
                91,
                width: width,
                height: height).Scheduled.Single();
        }

        private static EnemyIntentSourceSnapshot Source(
            bool isAlive = true,
            bool canGenerate = true,
            EnemyIntentTargetSnapshot target = null,
            TimelineCell[] shape = null,
            HexCoord[] range = null,
            string command = null,
            string effectStableId = "enemy-intent-effect")
        {
            return EnemyIntentSchedulerTests.Source(
                "source",
                new HexCoord(1, 0),
                isAlive: isAlive,
                canGenerate: canGenerate,
                target: target,
                shape: shape,
                range: range,
                command: command,
                effectStableId: effectStableId);
        }

        private static CombatOccupantSnapshot Occupant(
            string runtimeId,
            int hp,
            CombatAttitude attitude)
        {
            var coordinate = new HexCoord(0, 0);
            var state = new CombatSliceState(
                runtimeId,
                1,
                new CombatBoardState(),
                new[]
                {
                    new CombatOccupantState(
                        runtimeId,
                        coordinate,
                        "target",
                        attitude,
                        hp,
                        100,
                        0,
                        true,
                        true)
                });
            Assert.That(state.TryGetOccupant(runtimeId, coordinate, out var snapshot), Is.True);
            return snapshot;
        }

        private static void AssertInvalid(
            EnemyIntentResolveResult result,
            EnemyIntentInvalidReason reason,
            EnemyIntentScheduledAction scheduled)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.EffectApplied, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(EnemyIntentResolveOutcome.RemovedInvalid));
            Assert.That(result.InvalidReason, Is.EqualTo(reason));
            Assert.That(result.Removal.ActionId, Is.EqualTo(scheduled.Action.ActionId));
            Assert.That(result.Removal.OccupiedCells, Is.EqualTo(scheduled.OccupiedCells));
        }
    }
}
