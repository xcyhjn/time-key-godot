namespace TimeKey.Domain
{
    public interface ICardEffectHandler
    {
        CardEffectKind Kind { get; }

        bool Supports(CardEffect effect);

        void Apply(
            CombatSliceState state,
            TimelineAction action,
            CardEffect effect,
            CardEffectResultBuffer effectResults);
    }

    public sealed class UnsupportedCardEffectException : System.InvalidOperationException
    {
        public UnsupportedCardEffectException(CardEffectKind kind)
            : base("No card effect handler is registered for " + kind + ".")
        {
            Kind = kind;
        }

        public UnsupportedCardEffectException(CardEffect effect)
            : base("No card effect handler supports the payload for " + effect.Kind + ".")
        {
            Kind = effect.Kind;
        }

        public CardEffectKind Kind { get; }
    }

    internal sealed class DamageCardEffectHandler : ICardEffectHandler
    {
        public CardEffectKind Kind => CardEffectKind.Damage;

        public bool Supports(CardEffect effect)
        {
            return effect.Kind == Kind && effect.NumericAmount.HasValue;
        }

        public void Apply(
            CombatSliceState state,
            TimelineAction action,
            CardEffect effect,
            CardEffectResultBuffer effectResults)
        {
            state.ApplyDamage(action.TargetId, effect.Value);
        }
    }

    internal sealed class ElevationCardEffectHandler : ICardEffectHandler
    {
        public CardEffectKind Kind => CardEffectKind.Elevation;

        public bool Supports(CardEffect effect)
        {
            return effect.Kind == Kind && effect.NumericAmount.HasValue;
        }

        public void Apply(
            CombatSliceState state,
            TimelineAction action,
            CardEffect effect,
            CardEffectResultBuffer effectResults)
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
                    effectResults.AddTile(result);
                }
            }
        }
    }
}
