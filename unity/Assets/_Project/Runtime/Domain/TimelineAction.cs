using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain
{
    public enum TimelineActorKind
    {
        Enemy,
        Player
    }

    public sealed class TimelineAction
    {
        private readonly ReadOnlyCollection<TimelineCell> _shape;
        private readonly ReadOnlyCollection<CardEffect> _effects;
        private readonly ReadOnlyCollection<HexCoord> _effectRange;

        public TimelineAction(
            TimelineActorKind actorKind,
            string cardId,
            string targetId,
            TimelineCell origin,
            IReadOnlyList<TimelineCell> shape,
            int damage)
            : this(
                actorKind,
                cardId,
                targetId,
                origin,
                shape,
                null,
                CreateDamageEffects(damage),
                Array.Empty<HexCoord>())
        {
        }

        public TimelineAction(
            TimelineActorKind actorKind,
            string cardId,
            string targetId,
            TimelineCell origin,
            IReadOnlyList<TimelineCell> shape,
            HexCoord? targetCoord,
            IReadOnlyList<CardEffect> effects,
            IReadOnlyList<HexCoord> effectRange)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("A card or intent ID is required.", nameof(cardId));
            }

            if (string.IsNullOrWhiteSpace(targetId))
            {
                throw new ArgumentException("A target ID is required.", nameof(targetId));
            }

            if (shape == null || shape.Count == 0)
            {
                throw new ArgumentException("An action must occupy at least one cell.", nameof(shape));
            }

            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            if (effectRange == null)
            {
                throw new ArgumentNullException(nameof(effectRange));
            }

            var copiedShape = new List<TimelineCell>(shape.Count);
            var uniqueCells = new HashSet<TimelineCell>();
            for (var index = 0; index < shape.Count; index++)
            {
                if (!uniqueCells.Add(shape[index]))
                {
                    throw new ArgumentException("An action shape cannot contain duplicate cells.", nameof(shape));
                }

                copiedShape.Add(shape[index]);
            }

            ActorKind = actorKind;
            CardId = cardId;
            TargetId = targetId;
            Origin = origin;
            TargetCoord = targetCoord;
            _shape = new ReadOnlyCollection<TimelineCell>(copiedShape);
            _effects = Copy(effects);
            _effectRange = Copy(effectRange);
        }

        public TimelineActorKind ActorKind { get; }

        public string CardId { get; }

        public string TargetId { get; }

        public TimelineCell Origin { get; }

        public IReadOnlyList<TimelineCell> Shape => _shape;

        public HexCoord? TargetCoord { get; }

        public IReadOnlyList<CardEffect> Effects => _effects;

        public IReadOnlyList<HexCoord> EffectRange => _effectRange;

        public int Damage
        {
            get
            {
                var damage = 0;
                for (var index = 0; index < _effects.Count; index++)
                {
                    if (_effects[index].Kind == CardEffectKind.Damage)
                    {
                        damage += _effects[index].Value;
                    }
                }

                return damage;
            }
        }

        public static TimelineAction FromCard(
            CardDefinition card,
            string targetId,
            TimelineCell origin)
        {
            return FromCard(card, targetId, null, origin);
        }

        public static TimelineAction FromCard(
            CardDefinition card,
            string targetId,
            HexCoord? targetCoord,
            TimelineCell origin)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            for (var index = 0; index < card.Effects.Count; index++)
            {
                if (card.Effects[index].Kind == CardEffectKind.Clear)
                {
                    throw new ArgumentException(
                        "Clear cards do not create ordinary timeline actions.",
                        nameof(card));
                }
            }

            return new TimelineAction(
                TimelineActorKind.Player,
                card.StableId,
                targetId,
                origin,
                card.Shape,
                targetCoord,
                card.Effects,
                card.Range);
        }

        private static ReadOnlyCollection<T> Copy<T>(IReadOnlyList<T> source)
        {
            var result = new List<T>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                result.Add(source[index]);
            }

            return new ReadOnlyCollection<T>(result);
        }

        private static IReadOnlyList<CardEffect> CreateDamageEffects(int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            return damage == 0
                ? Array.Empty<CardEffect>()
                : new[] { new CardEffect(CardEffectKind.Damage, damage) };
        }
    }
}
