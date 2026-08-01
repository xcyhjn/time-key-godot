using System;
using System.Collections.Generic;
using System.Globalization;

namespace TimeKey.Domain.Intents
{
    public sealed class EnemyIntentScheduler
    {
        public const int DefaultMaximumIntentCount = 5;

        public EnemyIntentGenerationResult Generate(
            EnemyIntentWorldSnapshot world,
            long lifecycleSequence,
            int startingActionOrdinal,
            int seed,
            IReadOnlyList<TimelineCell> occupiedCells = null,
            int width = TimelineGrid.DefaultWidth,
            int height = TimelineGrid.DefaultHeight,
            int maximumIntentCount = DefaultMaximumIntentCount)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (lifecycleSequence <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lifecycleSequence));
            }

            if (startingActionOrdinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingActionOrdinal));
            }

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (maximumIntentCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumIntentCount));
            }

            var occupied = new HashSet<TimelineCell>();
            occupiedCells = occupiedCells ?? Array.Empty<TimelineCell>();
            for (var index = 0; index < occupiedCells.Count; index++)
            {
                if (!Contains(occupiedCells[index], width, height))
                {
                    throw new ArgumentOutOfRangeException(nameof(occupiedCells));
                }

                occupied.Add(occupiedCells[index]);
            }

            var sources = new List<EnemyIntentSourceSnapshot>(world.Sources);
            sources.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

            var candidates = new List<EnemyIntentCandidate>();
            var rejected = new List<EnemyIntentRejection>();
            for (var index = 0; index < sources.Count; index++)
            {
                var source = sources[index];
                var actionId = TimelineActionIdentity.FromSequence(
                    lifecycleSequence,
                    checked(startingActionOrdinal + index));
                var reason = EnemyIntentRules.ValidateSource(world, source, out var effectRange);
                if (reason != EnemyIntentInvalidReason.None)
                {
                    rejected.Add(new EnemyIntentRejection(actionId, source.RuntimeId, reason));
                    continue;
                }

                candidates.Add(new EnemyIntentCandidate(actionId, source, effectRange));
            }

            candidates.Sort((left, right) => CompareCandidates(left, right, seed));
            var scheduled = new List<EnemyIntentScheduledAction>();
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (scheduled.Count >= maximumIntentCount)
                {
                    rejected.Add(new EnemyIntentRejection(
                        candidate.ActionId,
                        candidate.Source.RuntimeId,
                        EnemyIntentInvalidReason.GenerationLimitReached));
                    continue;
                }

                var validOrigins = FindOrigins(
                    candidate.Source.LiteralShape,
                    occupied,
                    width,
                    height);
                if (validOrigins.Count == 0)
                {
                    var reason = FindOrigins(
                            candidate.Source.LiteralShape,
                            new HashSet<TimelineCell>(),
                            width,
                            height).Count == 0
                        ? EnemyIntentInvalidReason.OutOfBounds
                        : EnemyIntentInvalidReason.TimelineConflict;
                    rejected.Add(new EnemyIntentRejection(
                        candidate.ActionId,
                        candidate.Source.RuntimeId,
                        reason));
                    continue;
                }

                var origin = SelectOrigin(candidate, validOrigins, seed);
                var absoluteCells = new List<TimelineCell>(candidate.Source.LiteralShape.Count);
                for (var shapeIndex = 0;
                     shapeIndex < candidate.Source.LiteralShape.Count;
                     shapeIndex++)
                {
                    var cell = origin + candidate.Source.LiteralShape[shapeIndex];
                    absoluteCells.Add(cell);
                    occupied.Add(cell);
                }

                scheduled.Add(new EnemyIntentScheduledAction(candidate, origin, absoluteCells));
            }

            return new EnemyIntentGenerationResult(scheduled, rejected);
        }

        private static int CompareCandidates(
            EnemyIntentCandidate left,
            EnemyIntentCandidate right,
            int seed)
        {
            var priority = right.Source.Priority.CompareTo(left.Source.Priority);
            if (priority != 0)
            {
                return priority;
            }

            var leftRank = StableRank(seed, "candidate", left.ActionId.Value);
            var rightRank = StableRank(seed, "candidate", right.ActionId.Value);
            var rank = leftRank.CompareTo(rightRank);
            return rank != 0
                ? rank
                : StringComparer.Ordinal.Compare(left.ActionId.Value, right.ActionId.Value);
        }

        private static List<TimelineCell> FindOrigins(
            IReadOnlyList<TimelineCell> shape,
            HashSet<TimelineCell> occupied,
            int width,
            int height)
        {
            var result = new List<TimelineCell>();
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var origin = new TimelineCell(x, y);
                    var canPlace = true;
                    for (var index = 0; index < shape.Count; index++)
                    {
                        var cell = origin + shape[index];
                        if (!Contains(cell, width, height) || occupied.Contains(cell))
                        {
                            canPlace = false;
                            break;
                        }
                    }

                    if (canPlace)
                    {
                        result.Add(origin);
                    }
                }
            }

            return result;
        }

        private static TimelineCell SelectOrigin(
            EnemyIntentCandidate candidate,
            IReadOnlyList<TimelineCell> origins,
            int seed)
        {
            var selected = origins[0];
            var selectedRank = StableRank(seed, "origin", OriginKey(candidate.ActionId, selected));
            for (var index = 1; index < origins.Count; index++)
            {
                var rank = StableRank(seed, "origin", OriginKey(candidate.ActionId, origins[index]));
                if (rank < selectedRank)
                {
                    selected = origins[index];
                    selectedRank = rank;
                }
            }

            return selected;
        }

        private static string OriginKey(TimelineActionIdentity actionId, TimelineCell origin)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}/{2}",
                actionId.Value,
                origin.X,
                origin.Y);
        }

        private static uint StableRank(int seed, string scope, string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                hash = Mix(hash, (byte)seed);
                hash = Mix(hash, (byte)(seed >> 8));
                hash = Mix(hash, (byte)(seed >> 16));
                hash = Mix(hash, (byte)(seed >> 24));
                hash = Mix(hash, scope);
                hash = Mix(hash, value);
                return hash;
            }
        }

        private static uint Mix(uint hash, string value)
        {
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                hash = Mix(hash, (byte)character);
                hash = Mix(hash, (byte)(character >> 8));
            }

            return hash;
        }

        private static uint Mix(uint hash, byte value)
        {
            unchecked
            {
                return (hash ^ value) * 16777619u;
            }
        }

        private static bool Contains(TimelineCell cell, int width, int height)
        {
            return cell.X >= 0 && cell.X < width && cell.Y >= 0 && cell.Y < height;
        }
    }
}
