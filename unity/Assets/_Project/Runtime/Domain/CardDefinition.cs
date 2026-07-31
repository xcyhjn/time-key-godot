using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain
{
    public enum CardEffectKind
    {
        Damage
    }

    public readonly struct CardEffect
    {
        public CardEffect(CardEffectKind kind, int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Kind = kind;
            Value = value;
        }

        public CardEffectKind Kind { get; }

        public int Value { get; }
    }

    public sealed class CardDefinition
    {
        private readonly ReadOnlyCollection<CardEffect> _effects;
        private readonly ReadOnlyCollection<HexCoord> _range;
        private readonly ReadOnlyCollection<TimelineCell> _shape;

        public CardDefinition(
            string stableId,
            int numericId,
            IReadOnlyList<CardEffect> effects,
            IReadOnlyList<HexCoord> range,
            IReadOnlyList<TimelineCell> shape)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                throw new ArgumentException("A stable card ID is required.", nameof(stableId));
            }

            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            if (range == null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            if (shape == null || shape.Count == 0)
            {
                throw new ArgumentException("A card shape must occupy at least one cell.", nameof(shape));
            }

            StableId = stableId;
            NumericId = numericId;
            _effects = new ReadOnlyCollection<CardEffect>(Copy(effects));
            _range = new ReadOnlyCollection<HexCoord>(Copy(range));
            _shape = new ReadOnlyCollection<TimelineCell>(Copy(shape));
        }

        public string StableId { get; }

        public int NumericId { get; }

        public IReadOnlyList<CardEffect> Effects => _effects;

        public IReadOnlyList<HexCoord> Range => _range;

        public IReadOnlyList<TimelineCell> Shape => _shape;

        private static List<T> Copy<T>(IReadOnlyList<T> source)
        {
            var result = new List<T>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                result.Add(source[index]);
            }

            return result;
        }
    }
}
