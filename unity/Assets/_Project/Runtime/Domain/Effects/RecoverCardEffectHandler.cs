namespace TimeKey.Domain
{
    public sealed class RecoverCardEffectHandler : ICardEffectHandler
    {
        public CardEffectKind Kind => CardEffectKind.Recover;

        public bool Supports(CardEffect effect)
        {
            return effect.Kind == Kind &&
                   effect.NumericAmount.HasValue &&
                   effect.Value > 0 &&
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
                before.Hp >= before.MaxHp)
            {
                return;
            }

            var missingHp = before.MaxHp - before.Hp;
            var recoveredHp = effect.Value >= missingHp
                ? before.MaxHp
                : before.Hp + effect.Value;
            if (!state.TrySetOccupantHealth(
                    before.RuntimeId,
                    before.Coordinate,
                    recoveredHp) ||
                !state.TryGetOccupant(
                    before.RuntimeId,
                    before.Coordinate,
                    out var after))
            {
                return;
            }

            effectResults.AddOccupant(
                new OccupantEffectResult(CardEffectKind.Recover, before, after));
        }
    }
}
