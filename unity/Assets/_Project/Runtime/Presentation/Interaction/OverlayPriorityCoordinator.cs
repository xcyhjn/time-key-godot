using System;

namespace TimeKey.Presentation.Interaction
{
    public enum CombatOverlayOwner
    {
        None = 0,
        IdleActionHover = 10,
        Scheduling = 20,
        Drag = 30,
        Clear = 40,
        CardTargeting = 50,
        Resolving = 60,
        Disabled = 70
    }

    public readonly struct OverlayLease
    {
        internal OverlayLease(CombatOverlayOwner owner, string identity)
        {
            Owner = owner;
            Identity = identity;
        }

        public CombatOverlayOwner Owner { get; }

        public string Identity { get; }

        public bool IsValid => Owner != CombatOverlayOwner.None;
    }

    public sealed class OverlayPriorityCoordinator
    {
        private CombatOverlayOwner _owner;
        private string _identity;

        public event Action<CombatOverlayOwner, string> Changed;

        public CombatOverlayOwner Owner => _owner;

        public string Identity => _identity;

        public bool IsEmpty => _owner == CombatOverlayOwner.None;

        public bool TryAcquire(CombatOverlayOwner owner, string identity, out OverlayLease lease)
        {
            if (owner == CombatOverlayOwner.None)
            {
                throw new ArgumentException("None cannot acquire an overlay lease.", nameof(owner));
            }

            if (identity != null && string.IsNullOrWhiteSpace(identity))
            {
                throw new ArgumentException("Overlay identity must be null or non-empty.", nameof(identity));
            }

            if (!IsEmpty && owner < _owner)
            {
                lease = default;
                return false;
            }

            var changed = IsEmpty || owner != _owner ||
                !string.Equals(identity, _identity, StringComparison.Ordinal);
            _owner = owner;
            _identity = identity;
            lease = new OverlayLease(owner, identity);
            if (changed)
            {
                Changed?.Invoke(_owner, _identity);
            }

            return true;
        }

        public bool Release(OverlayLease lease)
        {
            if (!lease.IsValid || lease.Owner != _owner ||
                !string.Equals(lease.Identity, _identity, StringComparison.Ordinal))
            {
                return false;
            }

            Clear();
            return true;
        }

        public bool Release(CombatOverlayOwner owner, string identity = null)
        {
            if (_owner != owner ||
                (identity != null && !string.Equals(identity, _identity, StringComparison.Ordinal)))
            {
                return false;
            }

            Clear();
            return true;
        }

        public void Clear()
        {
            if (IsEmpty)
            {
                return;
            }

            _owner = CombatOverlayOwner.None;
            _identity = null;
            Changed?.Invoke(_owner, _identity);
        }

        public void ClearAll()
        {
            Clear();
        }
    }
}
