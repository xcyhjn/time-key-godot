namespace TimeKey.Domain
{
    public sealed class PoisonCardEffectHandler : ICardEffectHandler
    {
        private const int AppliedStacks = 2;

        public CardEffectKind Kind => CardEffectKind.Poison;

        public bool Supports(CardEffect effect)
        {
            return effect.Kind == Kind &&
                   effect.NumericAmount.HasValue &&
                   effect.Value == AppliedStacks &&
                   string.IsNullOrEmpty(effect.CreationId) &&
                   effect.ClearMask.Count == 0;
        }

        public void Apply(
            CombatSliceState state,
            TimelineAction action,
            CardEffect effect,
            CardEffectResultBuffer effectResults)
        {
            if (!action.TargetCoord.HasValue || action.EffectRange.Count == 0)
            {
                return;
            }

            if (!state.TryGetOccupant(
                    action.TargetId,
                    action.TargetCoord.Value,
                    out var before) ||
                !before.SupportsHealth ||
                before.Hp <= 0 ||
                !before.SupportsStatus)
            {
                return;
            }

            if (!state.TryAddOccupantPoisonStacks(
                    before.RuntimeId,
                    before.Coordinate,
                    AppliedStacks) ||
                !state.TryGetOccupant(
                    before.RuntimeId,
                    before.Coordinate,
                    out var after))
            {
                return;
            }

            effectResults.AddOccupant(
                new OccupantEffectResult(CardEffectKind.Poison, before, after));
        }
    }
}
