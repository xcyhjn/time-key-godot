using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TimeKey.Domain;
using TimeKey.Domain.Deck;

namespace TimeKey.Application
{
    public enum TimelineActionValidity
    {
        Valid,
        Invalid,
        Unsupported
    }

    public enum TimelineActionInvalidReason
    {
        None,
        MissingSource,
        SourceUnavailable,
        MissingTarget,
        TargetUnavailable,
        ShapeMismatch,
        TimelineConflict,
        OutOfBounds,
        UnsupportedSourceCommand,
        UnsupportedEffect
    }

    public enum TimelineActionResolveState
    {
        Preview,
        Scheduled,
        Resolving,
        Resolved,
        Removed
    }

    public sealed class TimelineActionDisplayPayload
    {
        public TimelineActionDisplayPayload(
            string title,
            string description,
            string iconStableId = null,
            string sourceLabel = null,
            string targetLabel = null)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("A display title is required.", nameof(title));
            }

            Title = title;
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

    public sealed class TimelineActionPresentationSnapshot
    {
        private readonly ReadOnlyCollection<TimelineCell> _shape;
        private readonly ReadOnlyCollection<TimelineCell> _occupiedCells;
        private readonly ReadOnlyCollection<HexCoord> _effectRange;

        public TimelineActionPresentationSnapshot(
            TimelineActionIdentity actionId,
            TimelineActorKind actorKind,
            int priority,
            string sourceId,
            HexCoord? sourceCoord,
            string targetId,
            HexCoord? targetCoord,
            string cardStableId,
            string effectStableId,
            TimelineActionDisplayPayload display,
            TimelineCell origin,
            IReadOnlyList<TimelineCell> shape,
            IReadOnlyList<TimelineCell> occupiedCells,
            IReadOnlyList<HexCoord> effectRange,
            TimelineActionValidity validity,
            TimelineActionInvalidReason invalidReason,
            TimelineActionResolveState resolveState,
            CardInstanceId? cardInstanceId = null)
        {
            if (!actionId.IsValid)
            {
                throw new ArgumentException("A valid action identity is required.", nameof(actionId));
            }

            if (priority < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(priority));
            }

            sourceId = Optional(sourceId, nameof(sourceId));
            targetId = Optional(targetId, nameof(targetId));
            cardStableId = Optional(cardStableId, nameof(cardStableId));
            effectStableId = Optional(effectStableId, nameof(effectStableId));
            if (cardStableId == null && effectStableId == null)
            {
                throw new ArgumentException("A card or effect stable ID is required.");
            }

            if (display == null)
            {
                throw new ArgumentNullException(nameof(display));
            }

            ValidateValidity(validity, invalidReason);

            ActionId = actionId;
            ActorKind = actorKind;
            Priority = priority;
            SourceId = sourceId;
            SourceCoord = sourceCoord;
            TargetId = targetId;
            TargetCoord = targetCoord;
            CardStableId = cardStableId;
            EffectStableId = effectStableId;
            CardInstanceId = cardInstanceId;
            Display = display;
            Origin = origin;
            _shape = CopyRequired(shape, nameof(shape));
            _occupiedCells = CopyRequired(occupiedCells, nameof(occupiedCells));
            _effectRange = Copy(effectRange ?? throw new ArgumentNullException(nameof(effectRange)));
            Validity = validity;
            InvalidReason = invalidReason;
            ResolveState = resolveState;
        }

        public TimelineActionIdentity ActionId { get; }

        public TimelineActorKind ActorKind { get; }

        public int Priority { get; }

        public string SourceId { get; }

        public HexCoord? SourceCoord { get; }

        public string TargetId { get; }

        public HexCoord? TargetCoord { get; }

        public string CardStableId { get; }

        public CardInstanceId? CardInstanceId { get; }

        public string EffectStableId { get; }

        public TimelineActionDisplayPayload Display { get; }

        public TimelineCell Origin { get; }

        public IReadOnlyList<TimelineCell> Shape => _shape;

        public IReadOnlyList<TimelineCell> OccupiedCells => _occupiedCells;

        public IReadOnlyList<HexCoord> EffectRange => _effectRange;

        public TimelineActionValidity Validity { get; }

        public TimelineActionInvalidReason InvalidReason { get; }

        public TimelineActionResolveState ResolveState { get; }

        private static void ValidateValidity(
            TimelineActionValidity validity,
            TimelineActionInvalidReason invalidReason)
        {
            if (validity == TimelineActionValidity.Valid &&
                invalidReason != TimelineActionInvalidReason.None)
            {
                throw new ArgumentException("Valid actions cannot have an invalid reason.", nameof(invalidReason));
            }

            if (validity != TimelineActionValidity.Valid &&
                invalidReason == TimelineActionInvalidReason.None)
            {
                throw new ArgumentException("Invalid and unsupported actions require a typed reason.", nameof(invalidReason));
            }

            var isUnsupportedReason =
                invalidReason == TimelineActionInvalidReason.UnsupportedSourceCommand ||
                invalidReason == TimelineActionInvalidReason.UnsupportedEffect;
            if (validity == TimelineActionValidity.Unsupported && !isUnsupportedReason)
            {
                throw new ArgumentException("Unsupported actions require an unsupported reason.", nameof(invalidReason));
            }

            if (validity == TimelineActionValidity.Invalid && isUnsupportedReason)
            {
                throw new ArgumentException("Unsupported reasons require Unsupported validity.", nameof(invalidReason));
            }
        }

        private static string Optional(string value, string parameterName)
        {
            if (value != null && string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Optional stable IDs cannot be whitespace.", parameterName);
            }

            return value;
        }

        private static ReadOnlyCollection<T> CopyRequired<T>(
            IReadOnlyList<T> source,
            string parameterName)
        {
            if (source == null || source.Count == 0)
            {
                throw new ArgumentException("At least one value is required.", parameterName);
            }

            return Copy(source);
        }

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
}
