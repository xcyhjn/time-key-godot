using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain
{
    public enum CardEffectKind
    {
        Damage,
        Elevation,
        Recover,
        Built,
        Poison,
        Clear
    }

    public readonly struct CardEffect
    {
        private static readonly ReadOnlyCollection<TimelineCell> EmptyClearMask =
            new ReadOnlyCollection<TimelineCell>(new List<TimelineCell>());

        private readonly ReadOnlyCollection<TimelineCell> _clearMask;

        public CardEffect(CardEffectKind kind, int value)
            : this(kind, value, null, null)
        {
        }

        public CardEffect(CardEffectKind kind, int value, string creationId)
            : this(kind, value, creationId, null)
        {
        }

        public CardEffect(CardEffectKind kind, IReadOnlyList<TimelineCell> clearMask)
            : this(kind, null, null, clearMask)
        {
        }

        private CardEffect(
            CardEffectKind kind,
            int? numericAmount,
            string creationId,
            IReadOnlyList<TimelineCell> clearMask)
        {
            if (!Enum.IsDefined(typeof(CardEffectKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (kind == CardEffectKind.Clear)
            {
                if (numericAmount.HasValue || !string.IsNullOrEmpty(creationId))
                {
                    throw new ArgumentException("Clear effects cannot contain numeric or creation payloads.");
                }

                _clearMask = CopyAndValidateClearMask(clearMask);
            }
            else
            {
                if (!numericAmount.HasValue || numericAmount.Value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(numericAmount));
                }

                if (kind == CardEffectKind.Built)
                {
                    if (string.IsNullOrWhiteSpace(creationId))
                    {
                        throw new ArgumentException("Built effects require a creation ID.", nameof(creationId));
                    }

                    creationId = creationId.Trim();
                }
                else if (!string.IsNullOrEmpty(creationId))
                {
                    throw new ArgumentException("Only Built effects may contain a creation ID.", nameof(creationId));
                }

                _clearMask = EmptyClearMask;
            }

            Kind = kind;
            NumericAmount = numericAmount;
            CreationId = creationId;
        }

        public CardEffectKind Kind { get; }

        public int? NumericAmount { get; }

        public int Value => NumericAmount.GetValueOrDefault();

        public string CreationId { get; }

        public IReadOnlyList<TimelineCell> ClearMask => _clearMask ?? EmptyClearMask;

        private static ReadOnlyCollection<TimelineCell> CopyAndValidateClearMask(
            IReadOnlyList<TimelineCell> clearMask)
        {
            if (clearMask == null || clearMask.Count == 0)
            {
                throw new ArgumentException("Clear effects require a non-empty mask.", nameof(clearMask));
            }

            var copiedMask = new List<TimelineCell>(clearMask.Count);
            var uniqueCells = new HashSet<TimelineCell>();
            var minX = int.MaxValue;
            var minY = int.MaxValue;

            for (var index = 0; index < clearMask.Count; index++)
            {
                var cell = clearMask[index];
                if (cell.X < 0 || cell.Y < 0 || !uniqueCells.Add(cell))
                {
                    throw new ArgumentException(
                        "Clear masks must contain unique, non-negative cells.",
                        nameof(clearMask));
                }

                minX = Math.Min(minX, cell.X);
                minY = Math.Min(minY, cell.Y);
                copiedMask.Add(cell);
            }

            if (minX != 0 || minY != 0)
            {
                throw new ArgumentException("Clear masks must be normalized to the origin.", nameof(clearMask));
            }

            return new ReadOnlyCollection<TimelineCell>(copiedMask);
        }
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
            : this(stableId, numericId, null, effects, range, shape, false)
        {
        }

        public CardDefinition(
            string stableId,
            int numericId,
            string frontImage,
            IReadOnlyList<CardEffect> effects,
            IReadOnlyList<HexCoord> range,
            IReadOnlyList<TimelineCell> shape)
            : this(stableId, numericId, frontImage, effects, range, shape, true)
        {
        }

        private CardDefinition(
            string stableId,
            int numericId,
            string frontImage,
            IReadOnlyList<CardEffect> effects,
            IReadOnlyList<HexCoord> range,
            IReadOnlyList<TimelineCell> shape,
            bool requireFrontImage)
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

            if (shape == null)
            {
                throw new ArgumentNullException(nameof(shape));
            }

            if (requireFrontImage)
            {
                frontImage = NormalizeFrontImage(frontImage);
            }

            var copiedEffects = Copy(effects);
            var hasClearEffect = false;
            for (var index = 0; index < copiedEffects.Count; index++)
            {
                hasClearEffect |= copiedEffects[index].Kind == CardEffectKind.Clear;
            }

            if (hasClearEffect && (copiedEffects.Count != 1 || shape.Count != 0))
            {
                throw new ArgumentException(
                    "Clear must be the card's only effect and cannot use a normal shape.",
                    nameof(effects));
            }

            if (!hasClearEffect && shape.Count == 0)
            {
                throw new ArgumentException("A card shape must occupy at least one cell.", nameof(shape));
            }

            StableId = stableId;
            NumericId = numericId;
            FrontImage = frontImage;
            _effects = new ReadOnlyCollection<CardEffect>(copiedEffects);
            _range = new ReadOnlyCollection<HexCoord>(Copy(range));
            _shape = new ReadOnlyCollection<TimelineCell>(Copy(shape));
        }

        public string StableId { get; }

        public int NumericId { get; }

        public string FrontImage { get; }

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

        private static string NormalizeFrontImage(string frontImage)
        {
            if (string.IsNullOrWhiteSpace(frontImage))
            {
                throw new ArgumentException("A front image filename is required.", nameof(frontImage));
            }

            var normalized = frontImage.Trim();
            if (normalized.IndexOf('/') >= 0 ||
                normalized.IndexOf('\\') >= 0 ||
                normalized.IndexOf(':') >= 0 ||
                normalized.Contains(".."))
            {
                throw new ArgumentException("The front image must be a plain filename.", nameof(frontImage));
            }

            return normalized;
        }
    }
}
