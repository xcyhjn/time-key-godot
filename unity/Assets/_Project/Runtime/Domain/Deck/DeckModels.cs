using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain.Deck
{
    public readonly struct DeckCommandId : IEquatable<DeckCommandId>
    {
        private readonly string _value;

        public DeckCommandId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A deck command identity is required.", nameof(value));
            }

            _value = value.Trim();
        }

        public string Value => _value;

        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public bool Equals(DeckCommandId other)
        {
            return StringComparer.Ordinal.Equals(_value, other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is DeckCommandId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        }

        public override string ToString()
        {
            return _value ?? string.Empty;
        }
    }

    public enum DeckOperationReason
    {
        None,
        InvalidCommand,
        InvalidRequest,
        DuplicateCommand,
        CardNotInHand,
        HandLimitReached,
        PartialHandLimit,
        Exhausted,
        PartialExhausted
    }

    public readonly struct DeckZoneCounts : IEquatable<DeckZoneCounts>
    {
        public DeckZoneCounts(int drawPile, int hand, int discardPile)
        {
            DrawPile = drawPile;
            Hand = hand;
            DiscardPile = discardPile;
        }

        public int DrawPile { get; }

        public int Hand { get; }

        public int DiscardPile { get; }

        public int Total => DrawPile + Hand + DiscardPile;

        public bool Equals(DeckZoneCounts other)
        {
            return DrawPile == other.DrawPile &&
                   Hand == other.Hand &&
                   DiscardPile == other.DiscardPile;
        }

        public override bool Equals(object obj)
        {
            return obj is DeckZoneCounts other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = DrawPile;
                hashCode = (hashCode * 397) ^ Hand;
                hashCode = (hashCode * 397) ^ DiscardPile;
                return hashCode;
            }
        }
    }

    public sealed class DeckShuffleInfo
    {
        private static readonly ReadOnlyCollection<CardInstanceId> EmptyIds =
            new ReadOnlyCollection<CardInstanceId>(new List<CardInstanceId>());

        internal DeckShuffleInfo(bool occurred, IReadOnlyList<CardInstanceId> movedInstanceIds)
        {
            Occurred = occurred;
            MovedInstanceIds = Copy(movedInstanceIds);
        }

        public bool Occurred { get; }

        public int MovedCardCount => MovedInstanceIds.Count;

        public IReadOnlyList<CardInstanceId> MovedInstanceIds { get; }

        internal static DeckShuffleInfo None()
        {
            return new DeckShuffleInfo(false, EmptyIds);
        }

        private static ReadOnlyCollection<CardInstanceId> Copy(
            IReadOnlyList<CardInstanceId> source)
        {
            var copy = new List<CardInstanceId>(source == null ? 0 : source.Count);
            if (source != null)
            {
                for (var index = 0; index < source.Count; index++)
                {
                    copy.Add(source[index]);
                }
            }

            return new ReadOnlyCollection<CardInstanceId>(copy);
        }
    }

    public sealed class DeckMoveResult
    {
        internal DeckMoveResult(
            bool succeeded,
            DeckOperationReason reason,
            int requestedCount,
            DeckZoneCounts before,
            DeckZoneCounts after,
            IReadOnlyList<CardInstanceId> movedInstanceIds,
            DeckShuffleInfo shuffle)
        {
            Succeeded = succeeded;
            Reason = reason;
            RequestedCount = requestedCount;
            Before = before;
            After = after;
            MovedInstanceIds = Copy(movedInstanceIds);
            Shuffle = shuffle ?? DeckShuffleInfo.None();
        }

        public bool Succeeded { get; }

        public DeckOperationReason Reason { get; }

        public int RequestedCount { get; }

        public int MovedCount => MovedInstanceIds.Count;

        public DeckZoneCounts Before { get; }

        public DeckZoneCounts After { get; }

        public IReadOnlyList<CardInstanceId> MovedInstanceIds { get; }

        public DeckShuffleInfo Shuffle { get; }

        private static ReadOnlyCollection<CardInstanceId> Copy(
            IReadOnlyList<CardInstanceId> source)
        {
            var copy = new List<CardInstanceId>(source == null ? 0 : source.Count);
            if (source != null)
            {
                for (var index = 0; index < source.Count; index++)
                {
                    copy.Add(source[index]);
                }
            }

            return new ReadOnlyCollection<CardInstanceId>(copy);
        }
    }

    public sealed class DeckSnapshot
    {
        internal DeckSnapshot(
            IReadOnlyList<CardInstance> drawPile,
            IReadOnlyList<CardInstance> hand,
            IReadOnlyList<CardInstance> discardPile)
        {
            DrawPile = Copy(drawPile);
            Hand = Copy(hand);
            DiscardPile = Copy(discardPile);
        }

        public IReadOnlyList<CardInstance> DrawPile { get; }

        public IReadOnlyList<CardInstance> Hand { get; }

        public IReadOnlyList<CardInstance> DiscardPile { get; }

        public DeckZoneCounts Counts =>
            new DeckZoneCounts(DrawPile.Count, Hand.Count, DiscardPile.Count);

        private static ReadOnlyCollection<CardInstance> Copy(IReadOnlyList<CardInstance> source)
        {
            var copy = new List<CardInstance>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                copy.Add(source[index]);
            }

            return new ReadOnlyCollection<CardInstance>(copy);
        }
    }
}
