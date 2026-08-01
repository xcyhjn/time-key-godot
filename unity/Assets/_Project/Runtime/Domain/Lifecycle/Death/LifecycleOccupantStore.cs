using System.Collections.Generic;

namespace TimeKey.Domain
{
    public interface ILifecycleOccupantStore
    {
        // Returns the immutable phase-entry view used for the complete processor pass.
        IReadOnlyList<CombatOccupantSnapshot> CaptureOccupants();

        // Implementations must validate every expected value, then apply all mutations or none.
        // Remove mutations also clear the authoritative tile slot and all status registration.
        bool TryApply(
            IReadOnlyList<LifecycleOccupantMutation> mutations,
            out string failureReason);
    }
}
