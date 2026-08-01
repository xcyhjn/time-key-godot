using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain.Intents
{
    public enum EnemyIntentInvalidReason
    {
        None,
        MissingSource,
        SourceUnavailable,
        MissingTarget,
        TargetUnavailable,
        ShapeMismatch,
        EffectMismatch,
        OutOfBounds,
        TimelineConflict,
        GenerationLimitReached
    }

    public enum EnemyIntentResolveOutcome
    {
        RemovedInvalid,
        UnsupportedSourceCommand,
        UnsupportedEffect
    }

    public sealed class EnemyIntentDisplayData
    {
        public EnemyIntentDisplayData(
            string title,
            string description,
            string iconStableId = null,
            string sourceLabel = null,
            string targetLabel = null)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("An intent display title is required.", nameof(title));
            }

            Title = title.Trim();
            Description = description ?? string.Empty;
            IconStableId = Optional(iconStableId, nameof(iconStableId));
            SourceLabel = Optional(sourceLabel, nameof(sourceLabel));
            TargetLabel = Optional(targetLabel, nameof(targetLabel));
        }

        public string Title { get; }

        public string Description { get; }

        public string IconStableId { get; }

        public string SourceLabel { get; }

        public string TargetLabel { get; }

        private static string Optional(string value, string parameterName)
        {
            if (value != null && string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Optional display values cannot be whitespace.", parameterName);
            }

            return value;
        }
    }

    public sealed class EnemyIntentEffectSnapshot
    {
        private readonly ReadOnlyCollection<HexCoord> _rangeOffsets;

        public EnemyIntentEffectSnapshot(
            string stableId,
            string sourceCommand,
            IReadOnlyList<HexCoord> rangeOffsets)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                throw new ArgumentException("An intent effect stable ID is required.", nameof(stableId));
            }

            StableId = stableId.Trim();
            SourceCommand = string.IsNullOrWhiteSpace(sourceCommand) ? null : sourceCommand.Trim();
            _rangeOffsets = Copy(rangeOffsets ?? Array.Empty<HexCoord>());
        }

        public string StableId { get; }

        public string SourceCommand { get; }

        public IReadOnlyList<HexCoord> RangeOffsets => _rangeOffsets;

        private static ReadOnlyCollection<HexCoord> Copy(IReadOnlyList<HexCoord> source)
        {
            var values = new List<HexCoord>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                values.Add(source[index]);
            }

            return new ReadOnlyCollection<HexCoord>(values);
        }
    }

    public sealed class EnemyIntentTargetSnapshot
    {
        public EnemyIntentTargetSnapshot(
            string targetId,
            HexCoord coordinate,
            bool requiresOccupant = false,
            string occupantRuntimeId = null,
            CombatAttitude? requiredAttitude = null,
            bool requiresLivingOccupant = true)
        {
            if (string.IsNullOrWhiteSpace(targetId))
            {
                throw new ArgumentException("An intent target ID is required.", nameof(targetId));
            }

            if (requiresOccupant && string.IsNullOrWhiteSpace(occupantRuntimeId))
            {
                throw new ArgumentException(
                    "An occupant target requires a runtime ID.",
                    nameof(occupantRuntimeId));
            }

            if (!requiresOccupant && occupantRuntimeId != null)
            {
                throw new ArgumentException(
                    "A tile-only target cannot carry an occupant runtime ID.",
                    nameof(occupantRuntimeId));
            }

            TargetId = targetId.Trim();
            Coordinate = coordinate;
            RequiresOccupant = requiresOccupant;
            OccupantRuntimeId = occupantRuntimeId == null ? null : occupantRuntimeId.Trim();
            RequiredAttitude = requiredAttitude;
            RequiresLivingOccupant = requiresLivingOccupant;
        }

        public string TargetId { get; }

        public HexCoord Coordinate { get; }

        public bool RequiresOccupant { get; }

        public string OccupantRuntimeId { get; }

        public CombatAttitude? RequiredAttitude { get; }

        public bool RequiresLivingOccupant { get; }
    }

    public sealed class EnemyIntentSourceSnapshot
    {
        private readonly ReadOnlyCollection<TimelineCell> _shape;

        public EnemyIntentSourceSnapshot(
            string runtimeId,
            HexCoord coordinate,
            string sourceKind,
            bool isAlive,
            bool canGenerateIntent,
            string intentStableId,
            int priority,
            EnemyIntentTargetSnapshot target,
            IReadOnlyList<TimelineCell> literalShape,
            EnemyIntentEffectSnapshot effect,
            EnemyIntentDisplayData display)
        {
            if (string.IsNullOrWhiteSpace(runtimeId))
            {
                throw new ArgumentException("An intent source runtime ID is required.", nameof(runtimeId));
            }

            if (string.IsNullOrWhiteSpace(sourceKind))
            {
                throw new ArgumentException("An intent source kind is required.", nameof(sourceKind));
            }

            if (string.IsNullOrWhiteSpace(intentStableId))
            {
                throw new ArgumentException("An intent stable ID is required.", nameof(intentStableId));
            }

            if (priority < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(priority));
            }

            RuntimeId = runtimeId.Trim();
            Coordinate = coordinate;
            SourceKind = sourceKind.Trim();
            IsAlive = isAlive;
            CanGenerateIntent = canGenerateIntent;
            IntentStableId = intentStableId.Trim();
            Priority = priority;
            Target = target;
            _shape = Copy(literalShape ?? Array.Empty<TimelineCell>());
            Effect = effect;
            Display = display;
        }

        public string RuntimeId { get; }

        public HexCoord Coordinate { get; }

        public string SourceKind { get; }

        public bool IsAlive { get; }

        public bool CanGenerateIntent { get; }

        public string IntentStableId { get; }

        public int Priority { get; }

        public EnemyIntentTargetSnapshot Target { get; }

        public IReadOnlyList<TimelineCell> LiteralShape => _shape;

        public EnemyIntentEffectSnapshot Effect { get; }

        public EnemyIntentDisplayData Display { get; }

        private static ReadOnlyCollection<TimelineCell> Copy(IReadOnlyList<TimelineCell> source)
        {
            var values = new List<TimelineCell>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                values.Add(source[index]);
            }

            return new ReadOnlyCollection<TimelineCell>(values);
        }
    }

    public sealed class EnemyIntentWorldSnapshot
    {
        private readonly HashSet<HexCoord> _tiles;
        private readonly Dictionary<string, EnemyIntentSourceSnapshot> _sources;
        private readonly Dictionary<string, CombatOccupantSnapshot> _occupantsById;
        private readonly Dictionary<HexCoord, CombatOccupantSnapshot> _occupantsByCoordinate;
        private readonly ReadOnlyCollection<EnemyIntentSourceSnapshot> _orderedSources;

        public EnemyIntentWorldSnapshot(
            IReadOnlyList<HexCoord> tiles,
            IReadOnlyList<EnemyIntentSourceSnapshot> sources,
            IReadOnlyList<CombatOccupantSnapshot> occupants = null)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            _tiles = new HashSet<HexCoord>();
            for (var index = 0; index < tiles.Count; index++)
            {
                _tiles.Add(tiles[index]);
            }

            _sources = new Dictionary<string, EnemyIntentSourceSnapshot>(StringComparer.Ordinal);
            var sourceCoordinates = new HashSet<HexCoord>();
            var copiedSources = new List<EnemyIntentSourceSnapshot>(sources.Count);
            for (var index = 0; index < sources.Count; index++)
            {
                var source = sources[index] ??
                    throw new ArgumentException("Intent sources cannot contain null entries.", nameof(sources));
                if (_sources.ContainsKey(source.RuntimeId) || !sourceCoordinates.Add(source.Coordinate))
                {
                    throw new ArgumentException(
                        "Intent sources require unique runtime IDs and coordinates.",
                        nameof(sources));
                }

                _sources.Add(source.RuntimeId, source);
                copiedSources.Add(source);
            }

            _orderedSources = new ReadOnlyCollection<EnemyIntentSourceSnapshot>(copiedSources);
            _occupantsById =
                new Dictionary<string, CombatOccupantSnapshot>(StringComparer.Ordinal);
            _occupantsByCoordinate = new Dictionary<HexCoord, CombatOccupantSnapshot>();
            occupants = occupants ?? Array.Empty<CombatOccupantSnapshot>();
            for (var index = 0; index < occupants.Count; index++)
            {
                var occupant = occupants[index] ??
                    throw new ArgumentException("Target occupants cannot contain null entries.", nameof(occupants));
                if (_occupantsById.ContainsKey(occupant.RuntimeId) ||
                    _occupantsByCoordinate.ContainsKey(occupant.Coordinate))
                {
                    throw new ArgumentException(
                        "Target occupants require unique runtime IDs and coordinates.",
                        nameof(occupants));
                }

                _occupantsById.Add(occupant.RuntimeId, occupant);
                _occupantsByCoordinate.Add(occupant.Coordinate, occupant);
            }
        }

        public IReadOnlyList<EnemyIntentSourceSnapshot> Sources => _orderedSources;

        public bool ContainsTile(HexCoord coordinate)
        {
            return _tiles.Contains(coordinate);
        }

        public bool TryGetSource(string runtimeId, out EnemyIntentSourceSnapshot source)
        {
            source = null;
            return !string.IsNullOrWhiteSpace(runtimeId) &&
                   _sources.TryGetValue(runtimeId, out source);
        }

        public bool TryGetOccupant(
            string runtimeId,
            HexCoord coordinate,
            out CombatOccupantSnapshot occupant)
        {
            occupant = null;
            return !string.IsNullOrWhiteSpace(runtimeId) &&
                   _occupantsByCoordinate.TryGetValue(coordinate, out occupant) &&
                   string.Equals(occupant.RuntimeId, runtimeId, StringComparison.Ordinal);
        }
    }

    public sealed class EnemyIntentCandidate
    {
        private readonly ReadOnlyCollection<HexCoord> _effectRange;

        internal EnemyIntentCandidate(
            TimelineActionIdentity actionId,
            EnemyIntentSourceSnapshot source,
            IReadOnlyList<HexCoord> effectRange)
        {
            ActionId = actionId;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            _effectRange = Copy(effectRange ?? throw new ArgumentNullException(nameof(effectRange)));
        }

        public TimelineActionIdentity ActionId { get; }

        public EnemyIntentSourceSnapshot Source { get; }

        public EnemyIntentTargetSnapshot Target => Source.Target;

        public IReadOnlyList<HexCoord> EffectRange => _effectRange;

        private static ReadOnlyCollection<HexCoord> Copy(IReadOnlyList<HexCoord> source)
        {
            var values = new List<HexCoord>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                values.Add(source[index]);
            }

            return new ReadOnlyCollection<HexCoord>(values);
        }
    }

    public sealed class EnemyIntentScheduledAction
    {
        private readonly ReadOnlyCollection<TimelineCell> _occupiedCells;

        internal EnemyIntentScheduledAction(
            EnemyIntentCandidate candidate,
            TimelineCell origin,
            IReadOnlyList<TimelineCell> occupiedCells)
        {
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            Origin = origin;
            _occupiedCells = Copy(occupiedCells);
            Action = new TimelineAction(
                candidate.ActionId,
                TimelineActorKind.Enemy,
                candidate.Source.Priority,
                candidate.Source.RuntimeId,
                candidate.Source.Coordinate,
                candidate.Source.IntentStableId,
                candidate.Target.TargetId,
                origin,
                candidate.Source.LiteralShape,
                candidate.Target.Coordinate,
                Array.Empty<CardEffect>(),
                candidate.EffectRange);
        }

        public EnemyIntentCandidate Candidate { get; }

        public TimelineCell Origin { get; }

        public TimelineAction Action { get; }

        public IReadOnlyList<TimelineCell> OccupiedCells => _occupiedCells;

        private static ReadOnlyCollection<TimelineCell> Copy(IReadOnlyList<TimelineCell> source)
        {
            var values = new List<TimelineCell>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                values.Add(source[index]);
            }

            return new ReadOnlyCollection<TimelineCell>(values);
        }
    }

    public sealed class EnemyIntentRejection
    {
        internal EnemyIntentRejection(
            TimelineActionIdentity actionId,
            string sourceRuntimeId,
            EnemyIntentInvalidReason reason)
        {
            ActionId = actionId;
            SourceRuntimeId = sourceRuntimeId;
            Reason = reason;
        }

        public TimelineActionIdentity ActionId { get; }

        public string SourceRuntimeId { get; }

        public EnemyIntentInvalidReason Reason { get; }
    }

    public sealed class EnemyIntentGenerationResult
    {
        private readonly ReadOnlyCollection<EnemyIntentScheduledAction> _scheduled;
        private readonly ReadOnlyCollection<EnemyIntentRejection> _rejected;

        internal EnemyIntentGenerationResult(
            IReadOnlyList<EnemyIntentScheduledAction> scheduled,
            IReadOnlyList<EnemyIntentRejection> rejected)
        {
            _scheduled = Copy(scheduled);
            _rejected = Copy(rejected);
        }

        public IReadOnlyList<EnemyIntentScheduledAction> Scheduled => _scheduled;

        public IReadOnlyList<EnemyIntentRejection> Rejected => _rejected;

        private static ReadOnlyCollection<T> Copy<T>(IReadOnlyList<T> source)
        {
            var values = new List<T>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                values.Add(source[index]);
            }

            return new ReadOnlyCollection<T>(values);
        }
    }

    public sealed class EnemyIntentRemovalSnapshot
    {
        private readonly ReadOnlyCollection<TimelineCell> _occupiedCells;

        internal EnemyIntentRemovalSnapshot(
            TimelineActionIdentity actionId,
            IReadOnlyList<TimelineCell> occupiedCells)
        {
            ActionId = actionId;
            _occupiedCells = new ReadOnlyCollection<TimelineCell>(
                new List<TimelineCell>(occupiedCells));
        }

        public TimelineActionIdentity ActionId { get; }

        public IReadOnlyList<TimelineCell> OccupiedCells => _occupiedCells;
    }

    public sealed class EnemyIntentResolveResult
    {
        internal EnemyIntentResolveResult(
            TimelineActionIdentity actionId,
            bool succeeded,
            EnemyIntentResolveOutcome outcome,
            EnemyIntentInvalidReason invalidReason,
            EnemyIntentRemovalSnapshot removal)
        {
            ActionId = actionId;
            Succeeded = succeeded;
            Outcome = outcome;
            InvalidReason = invalidReason;
            Removal = removal ?? throw new ArgumentNullException(nameof(removal));
        }

        public TimelineActionIdentity ActionId { get; }

        public bool Succeeded { get; }

        public bool EffectApplied => false;

        public EnemyIntentResolveOutcome Outcome { get; }

        public EnemyIntentInvalidReason InvalidReason { get; }

        public EnemyIntentRemovalSnapshot Removal { get; }
    }

    internal static class EnemyIntentRules
    {
        public static EnemyIntentInvalidReason ValidateSource(
            EnemyIntentWorldSnapshot world,
            EnemyIntentSourceSnapshot source,
            out IReadOnlyList<HexCoord> effectRange)
        {
            effectRange = Array.Empty<HexCoord>();
            if (!world.ContainsTile(source.Coordinate) || !source.IsAlive || !source.CanGenerateIntent)
            {
                return EnemyIntentInvalidReason.SourceUnavailable;
            }

            if (source.Target == null)
            {
                return EnemyIntentInvalidReason.MissingTarget;
            }

            var targetReason = ValidateTarget(world, source.Target);
            if (targetReason != EnemyIntentInvalidReason.None)
            {
                return targetReason;
            }

            if (!HasUniqueShapeCells(source.LiteralShape))
            {
                return EnemyIntentInvalidReason.ShapeMismatch;
            }

            if (source.Effect == null || source.Effect.RangeOffsets.Count == 0)
            {
                return EnemyIntentInvalidReason.EffectMismatch;
            }

            var range = new List<HexCoord>(source.Effect.RangeOffsets.Count);
            for (var index = 0; index < source.Effect.RangeOffsets.Count; index++)
            {
                var offset = source.Effect.RangeOffsets[index];
                var coordinate = new HexCoord(
                    source.Target.Coordinate.Q + offset.Q,
                    source.Target.Coordinate.R + offset.R);
                if (world.ContainsTile(coordinate))
                {
                    range.Add(coordinate);
                }
            }

            if (range.Count == 0)
            {
                return EnemyIntentInvalidReason.EffectMismatch;
            }

            if (source.Display == null)
            {
                return EnemyIntentInvalidReason.SourceUnavailable;
            }

            effectRange = new ReadOnlyCollection<HexCoord>(range);
            return EnemyIntentInvalidReason.None;
        }

        public static EnemyIntentInvalidReason ValidateTarget(
            EnemyIntentWorldSnapshot world,
            EnemyIntentTargetSnapshot target)
        {
            if (!world.ContainsTile(target.Coordinate))
            {
                return EnemyIntentInvalidReason.MissingTarget;
            }

            if (!target.RequiresOccupant)
            {
                return EnemyIntentInvalidReason.None;
            }

            if (!world.TryGetOccupant(
                    target.OccupantRuntimeId,
                    target.Coordinate,
                    out var occupant))
            {
                return EnemyIntentInvalidReason.TargetUnavailable;
            }

            if (target.RequiresLivingOccupant && !occupant.IsAlive)
            {
                return EnemyIntentInvalidReason.TargetUnavailable;
            }

            if (target.RequiredAttitude.HasValue &&
                occupant.Attitude != target.RequiredAttitude.Value)
            {
                return EnemyIntentInvalidReason.TargetUnavailable;
            }

            return EnemyIntentInvalidReason.None;
        }

        public static bool SequenceEqual<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            var comparer = EqualityComparer<T>.Default;
            for (var index = 0; index < left.Count; index++)
            {
                if (!comparer.Equals(left[index], right[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasUniqueShapeCells(IReadOnlyList<TimelineCell> shape)
        {
            if (shape == null || shape.Count == 0)
            {
                return false;
            }

            var cells = new HashSet<TimelineCell>();
            for (var index = 0; index < shape.Count; index++)
            {
                if (!cells.Add(shape[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
