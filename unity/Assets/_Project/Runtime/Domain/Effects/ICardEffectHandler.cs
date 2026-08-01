using System.Collections.Generic;

namespace TimeKey.Domain
{
    public interface ICardEffectHandler
    {
        CardEffectKind Kind { get; }

        void Apply(
            CombatSliceState state,
            TimelineAction action,
            CardEffect effect,
            ICollection<TileEffectResult> effectResults);
    }

    public sealed class UnsupportedCardEffectException : System.InvalidOperationException
    {
        public UnsupportedCardEffectException(CardEffectKind kind)
            : base("No card effect handler is registered for " + kind + ".")
        {
            Kind = kind;
        }

        public CardEffectKind Kind { get; }
    }

    internal sealed class DamageCardEffectHandler : ICardEffectHandler
    {
        public CardEffectKind Kind => CardEffectKind.Damage;

        public void Apply(
            CombatSliceState state,
            TimelineAction action,
            CardEffect effect,
            ICollection<TileEffectResult> effectResults)
        {
            state.ApplyDamage(action.TargetId, effect.Value);
        }
    }

    internal sealed class ElevationCardEffectHandler : ICardEffectHandler
    {
        public CardEffectKind Kind => CardEffectKind.Elevation;

        public void Apply(
            CombatSliceState state,
            TimelineAction action,
            CardEffect effect,
            ICollection<TileEffectResult> effectResults)
        {
            if (!action.TargetCoord.HasValue)
            {
                throw new System.InvalidOperationException("Elevation effects require a tile target.");
            }

            for (var index = 0; index < action.EffectRange.Count; index++)
            {
                var offset = action.EffectRange[index];
                var coordinate = new HexCoord(
                    action.TargetCoord.Value.Q + offset.Q,
                    action.TargetCoord.Value.R + offset.R);
                if (state.Board.TryApplyElevation(coordinate, effect.Value, out var result))
                {
                    effectResults.Add(result);
                }
            }
        }
    }
}
