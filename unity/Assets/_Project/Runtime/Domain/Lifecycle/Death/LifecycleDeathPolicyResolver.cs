using System;

namespace TimeKey.Domain
{
    public interface ILifecycleDeathPolicyResolver
    {
        LifecycleDeathPolicy Resolve(CombatOccupantSnapshot occupant);
    }

    public sealed class DefaultLifecycleDeathPolicyResolver : ILifecycleDeathPolicyResolver
    {
        public const string TowerKind = "tower";
        public const string RadarUnderlingKind = "radar-underling";

        public LifecycleDeathPolicy Resolve(CombatOccupantSnapshot occupant)
        {
            if (occupant == null)
            {
                throw new ArgumentNullException(nameof(occupant));
            }

            return Matches(occupant, TowerKind) || Matches(occupant, RadarUnderlingKind)
                ? LifecycleDeathPolicy.Remove
                : LifecycleDeathPolicy.RemainBroken;
        }

        public static bool IsTower(CombatOccupantSnapshot occupant)
        {
            return occupant != null && Matches(occupant, TowerKind);
        }

        private static bool Matches(CombatOccupantSnapshot occupant, string stableKind)
        {
            return string.Equals(occupant.Kind, stableKind, StringComparison.Ordinal) ||
                   string.Equals(occupant.CreationId, stableKind, StringComparison.Ordinal);
        }
    }
}
