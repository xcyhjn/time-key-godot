using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Domain.Intents;

namespace TimeKey.Tests.EditMode.Intents
{
    public sealed class EnemyIntentSchedulerTests
    {
        [Test]
        public void Generate_FiltersUnavailableSourcesWithTypedReasons()
        {
            var dead = Source("dead", new HexCoord(1, 0), isAlive: false);
            var disabled = Source("disabled", new HexCoord(2, 0), canGenerate: false);
            var missingTarget = Source(
                "missing-target",
                new HexCoord(3, 0),
                target: new EnemyIntentTargetSnapshot("outside", new HexCoord(99, 99)));
            var result = Generate(World(new[] { dead, disabled, missingTarget }));

            Assert.That(result.Scheduled, Is.Empty);
            Assert.That(
                result.Rejected.Select(value => value.SourceRuntimeId),
                Is.EquivalentTo(new[] { "dead", "disabled", "missing-target" }));
            Assert.That(
                result.Rejected.Single(value => value.SourceRuntimeId == "dead").Reason,
                Is.EqualTo(EnemyIntentInvalidReason.SourceUnavailable));
            Assert.That(
                result.Rejected.Single(value => value.SourceRuntimeId == "missing-target").Reason,
                Is.EqualTo(EnemyIntentInvalidReason.MissingTarget));
        }

        [Test]
        public void Generate_PlacesHigherPriorityBeforeLowerPriority()
        {
            var low = Source("low", new HexCoord(1, 0), priority: 0);
            var high = Source("high", new HexCoord(2, 0), priority: 999);

            var result = Generate(World(new[] { low, high }), width: 2, height: 1);

            Assert.That(
                result.Scheduled.Select(value => value.Candidate.Source.RuntimeId),
                Is.EqualTo(new[] { "high", "low" }));
        }

        [Test]
        public void Generate_SameSeedIsReproducibleAndIndependentOfSourceOrder()
        {
            var sources = new[]
            {
                Source("a", new HexCoord(1, 0)),
                Source("b", new HexCoord(2, 0)),
                Source("c", new HexCoord(3, 0)),
                Source("d", new HexCoord(4, 0))
            };
            var reversed = sources.Reverse().ToArray();

            var first = Generate(World(sources), seed: 8721, width: 8, height: 2);
            var second = Generate(World(reversed), seed: 8721, width: 8, height: 2);

            Assert.That(Signature(first), Is.EqualTo(Signature(second)));
        }

        [Test]
        public void Generate_DifferentSeedChangesTieSelectionOrOrigin()
        {
            var sources = new[]
            {
                Source("a", new HexCoord(1, 0)),
                Source("b", new HexCoord(2, 0)),
                Source("c", new HexCoord(3, 0)),
                Source("d", new HexCoord(4, 0)),
                Source("e", new HexCoord(5, 0))
            };

            var first = Generate(World(sources), seed: 11, width: 12, height: 3);
            var second = Generate(World(sources), seed: 7919, width: 12, height: 3);

            Assert.That(Signature(first), Is.Not.EqualTo(Signature(second)));
        }

        [Test]
        public void Generate_PreservesVillageLeadingZeroLiteralShape()
        {
            var village = Source(
                "village",
                new HexCoord(1, 0),
                shape: new[]
                {
                    new TimelineCell(1, 0),
                    new TimelineCell(2, 0)
                });

            var scheduled = Generate(
                World(new[] { village }),
                width: 3,
                height: 1).Scheduled.Single();

            Assert.That(scheduled.Origin, Is.EqualTo(new TimelineCell(0, 0)));
            Assert.That(scheduled.OccupiedCells, Is.EqualTo(new[]
            {
                new TimelineCell(1, 0),
                new TimelineCell(2, 0)
            }));
            Assert.That(scheduled.OccupiedCells, Has.None.EqualTo(new TimelineCell(0, 0)));
            Assert.That(scheduled.Action.Shape, Is.EqualTo(village.LiteralShape));
        }

        [Test]
        public void Generate_RejectsShapeThatCannotFitTimelineBounds()
        {
            var source = Source(
                "outside",
                new HexCoord(1, 0),
                shape: new[] { new TimelineCell(3, 0) });

            var result = Generate(World(new[] { source }), width: 3, height: 1);

            Assert.That(result.Scheduled, Is.Empty);
            Assert.That(result.Rejected.Single().Reason, Is.EqualTo(EnemyIntentInvalidReason.OutOfBounds));
        }

        [Test]
        public void Generate_DistinguishesTimelineConflictFromOutOfBounds()
        {
            var source = Source(
                "blocked",
                new HexCoord(1, 0),
                shape: new[] { new TimelineCell(0, 0), new TimelineCell(1, 0) });
            var scheduler = new EnemyIntentScheduler();

            var result = scheduler.Generate(
                World(new[] { source }),
                4,
                0,
                22,
                new[] { new TimelineCell(0, 0) },
                width: 2,
                height: 1);

            Assert.That(result.Scheduled, Is.Empty);
            Assert.That(
                result.Rejected.Single().Reason,
                Is.EqualTo(EnemyIntentInvalidReason.TimelineConflict));
        }

        [Test]
        public void Generate_StopsAtFiveAndReturnsTypedLimitRejections()
        {
            var sources = Enumerable.Range(0, 7)
                .Select(index => Source("source-" + index, new HexCoord(index + 1, 0)))
                .ToArray();

            var result = Generate(World(sources), width: 12, height: 3);

            Assert.That(result.Scheduled, Has.Count.EqualTo(5));
            Assert.That(
                result.Rejected.Count(value =>
                    value.Reason == EnemyIntentInvalidReason.GenerationLimitReached),
                Is.EqualTo(2));
        }

        [Test]
        public void Generate_UsesExistingTimelineActionIdentityAndDeepCopiesInputData()
        {
            var shape = new List<TimelineCell> { new TimelineCell(0, 0) };
            var range = new List<HexCoord> { new HexCoord(0, 0) };
            var source = Source("stable", new HexCoord(1, 0), shape: shape, range: range);
            var scheduled = Generate(
                World(new[] { source }),
                sequence: 7,
                startingOrdinal: 12).Scheduled.Single();

            shape[0] = new TimelineCell(9, 9);
            range.Clear();

            Assert.That(
                scheduled.Action.ActionId,
                Is.EqualTo(TimelineActionIdentity.FromSequence(7, 12)));
            Assert.That(scheduled.Action.ActorKind, Is.EqualTo(TimelineActorKind.Enemy));
            Assert.That(scheduled.Action.SourceId, Is.EqualTo("stable"));
            Assert.That(scheduled.Action.Shape, Is.EqualTo(new[] { new TimelineCell(0, 0) }));
            Assert.That(scheduled.Candidate.EffectRange, Is.EqualTo(new[] { new HexCoord(0, 0) }));
        }

        private static EnemyIntentGenerationResult Generate(
            EnemyIntentWorldSnapshot world,
            long sequence = 3,
            int startingOrdinal = 0,
            int seed = 57,
            int width = 12,
            int height = 3)
        {
            return new EnemyIntentScheduler().Generate(
                world,
                sequence,
                startingOrdinal,
                seed,
                width: width,
                height: height);
        }

        private static string Signature(EnemyIntentGenerationResult result)
        {
            return string.Join(
                "|",
                result.Scheduled.Select(value =>
                    value.Candidate.Source.RuntimeId + ":" +
                    value.Origin.X + "," + value.Origin.Y));
        }

        internal static EnemyIntentWorldSnapshot World(
            IReadOnlyList<EnemyIntentSourceSnapshot> sources,
            IReadOnlyList<CombatOccupantSnapshot> occupants = null,
            IReadOnlyList<HexCoord> tiles = null)
        {
            tiles = tiles ?? Enumerable.Range(0, 12)
                .Select(index => new HexCoord(index, 0))
                .ToArray();
            return new EnemyIntentWorldSnapshot(tiles, sources, occupants);
        }

        internal static EnemyIntentSourceSnapshot Source(
            string runtimeId,
            HexCoord sourceCoord,
            int priority = 0,
            bool isAlive = true,
            bool canGenerate = true,
            EnemyIntentTargetSnapshot target = null,
            IReadOnlyList<TimelineCell> shape = null,
            IReadOnlyList<HexCoord> range = null,
            string command = null,
            string effectStableId = "enemy-intent-effect")
        {
            return new EnemyIntentSourceSnapshot(
                runtimeId,
                sourceCoord,
                "village",
                isAlive,
                canGenerate,
                "village-expand",
                priority,
                target ?? new EnemyIntentTargetSnapshot("tile:0,0", new HexCoord(0, 0)),
                shape ?? new[] { new TimelineCell(0, 0) },
                new EnemyIntentEffectSnapshot(
                    effectStableId,
                    command,
                    range ?? new[] { new HexCoord(0, 0) }),
                new EnemyIntentDisplayData(
                    "村庄意图",
                    "向相邻空地扩建 1 格",
                    "village",
                    "村庄",
                    "相邻空地"));
        }
    }
}
