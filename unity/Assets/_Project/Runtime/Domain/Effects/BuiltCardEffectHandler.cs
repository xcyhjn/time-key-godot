using System;
using System.Globalization;

namespace TimeKey.Domain
{
    public sealed class BuiltCardEffectHandler : ICardEffectHandler
    {
        private const string TowerCreationId = "tower";
        private const int TowerHp = 100;

        public CardEffectKind Kind => CardEffectKind.Built;

        public bool Supports(CardEffect effect)
        {
            return effect.Kind == Kind &&
                   effect.NumericAmount.HasValue &&
                   effect.Value == 1 &&
                   string.Equals(effect.CreationId, TowerCreationId, StringComparison.Ordinal) &&
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

            var coordinate = action.TargetCoord.Value;
            if (!state.Board.TryGetTile(coordinate, out _) ||
                state.TryGetOccupant(coordinate, out _))
            {
                return;
            }

            var tower = new CombatOccupantState(
                CreateTowerRuntimeId(coordinate),
                coordinate,
                TowerCreationId,
                CombatAttitude.Neutral,
                TowerHp,
                TowerHp,
                poisonStacks: 0,
                supportsHealth: true,
                supportsStatus: true,
                creationId: TowerCreationId);
            if (!state.TryAddOccupant(tower) ||
                !state.TryGetOccupant(tower.RuntimeId, coordinate, out var after))
            {
                return;
            }

            effectResults.AddOccupant(
                new OccupantEffectResult(CardEffectKind.Built, before: null, after));
        }

        private static string CreateTowerRuntimeId(HexCoord coordinate)
        {
            return string.Concat(
                "built:tower:",
                coordinate.Q.ToString(CultureInfo.InvariantCulture),
                ":",
                coordinate.R.ToString(CultureInfo.InvariantCulture));
        }
    }
}
