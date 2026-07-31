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

        public TimelineAction(
            TimelineActorKind actorKind,
            string cardId,
            string targetId,
            TimelineCell origin,
            IReadOnlyList<TimelineCell> shape,
            int damage)
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

            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
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
            Damage = damage;
            _shape = new ReadOnlyCollection<TimelineCell>(copiedShape);
        }

        public TimelineActorKind ActorKind { get; }

        public string CardId { get; }

        public string TargetId { get; }

        public TimelineCell Origin { get; }

        public IReadOnlyList<TimelineCell> Shape => _shape;

        public int Damage { get; }

        public static TimelineAction FromCard(
            CardDefinition card,
            string targetId,
            TimelineCell origin)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            var damage = 0;
            for (var index = 0; index < card.Effects.Count; index++)
            {
                if (card.Effects[index].Kind == CardEffectKind.Damage)
                {
                    damage += card.Effects[index].Value;
                }
            }

            return new TimelineAction(
                TimelineActorKind.Player,
                card.StableId,
                targetId,
                origin,
                card.Shape,
                damage);
        }
    }
}
