using System;

namespace TimeKey.Domain
{
    public enum CombatAttitude
    {
        Player,
        Enemy,
        Neutral
    }

    public sealed class CombatOccupantSnapshot
    {
        internal CombatOccupantSnapshot(CombatOccupantState state)
        {
            RuntimeId = state.RuntimeId;
            Coordinate = state.Coordinate;
            Kind = state.Kind;
            CreationId = state.CreationId;
            Attitude = state.Attitude;
            Hp = state.Hp;
            MaxHp = state.MaxHp;
            PoisonStacks = state.PoisonStacks;
            SupportsHealth = state.SupportsHealth;
            SupportsStatus = state.SupportsStatus;
        }

        public string RuntimeId { get; }

        public HexCoord Coordinate { get; }

        public string Kind { get; }

        public string CreationId { get; }

        public CombatAttitude Attitude { get; }

        public int Hp { get; }

        public int MaxHp { get; }

        public int PoisonStacks { get; }

        public bool SupportsHealth { get; }

        public bool SupportsStatus { get; }

        public bool IsAlive => !SupportsHealth || Hp > 0;
    }

    public sealed class CombatOccupantState
    {
        public CombatOccupantState(
            string runtimeId,
            HexCoord coordinate,
            string kind,
            CombatAttitude attitude,
            int hp,
            int maxHp,
            int poisonStacks,
            bool supportsHealth,
            bool supportsStatus,
            string creationId = null)
        {
            if (string.IsNullOrWhiteSpace(runtimeId))
            {
                throw new ArgumentException("A runtime occupant ID is required.", nameof(runtimeId));
            }

            if (string.IsNullOrWhiteSpace(kind))
            {
                throw new ArgumentException("An occupant kind is required.", nameof(kind));
            }

            if (!Enum.IsDefined(typeof(CombatAttitude), attitude))
            {
                throw new ArgumentOutOfRangeException(nameof(attitude));
            }

            if (hp < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hp));
            }

            if (maxHp < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp));
            }

            if (supportsHealth)
            {
                if (maxHp == 0 || hp > maxHp)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxHp));
                }
            }
            else if (hp != 0 || maxHp != 0)
            {
                throw new ArgumentException("Occupants without health support must use zero HP values.");
            }

            if (poisonStacks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(poisonStacks));
            }

            if (!supportsStatus && poisonStacks != 0)
            {
                throw new ArgumentException("Occupants without status support cannot have poison stacks.");
            }

            RuntimeId = runtimeId.Trim();
            Coordinate = coordinate;
            Kind = kind.Trim();
            CreationId = string.IsNullOrWhiteSpace(creationId) ? null : creationId.Trim();
            Attitude = attitude;
            Hp = hp;
            MaxHp = maxHp;
            PoisonStacks = poisonStacks;
            SupportsHealth = supportsHealth;
            SupportsStatus = supportsStatus;
        }

        public string RuntimeId { get; }

        public HexCoord Coordinate { get; }

        public string Kind { get; }

        public string CreationId { get; }

        public CombatAttitude Attitude { get; }

        public int Hp { get; private set; }

        public int MaxHp { get; }

        public int PoisonStacks { get; private set; }

        public bool SupportsHealth { get; }

        public bool SupportsStatus { get; }

        internal CombatOccupantState Copy()
        {
            return new CombatOccupantState(
                RuntimeId,
                Coordinate,
                Kind,
                Attitude,
                Hp,
                MaxHp,
                PoisonStacks,
                SupportsHealth,
                SupportsStatus,
                CreationId);
        }

        internal CombatOccupantSnapshot Snapshot()
        {
            return new CombatOccupantSnapshot(this);
        }

        internal bool TrySetHealth(int hp)
        {
            if (!SupportsHealth || hp < 0 || hp > MaxHp)
            {
                return false;
            }

            Hp = hp;
            return true;
        }

        internal bool TryAddPoisonStacks(int amount)
        {
            if (!SupportsStatus || amount <= 0)
            {
                return false;
            }

            PoisonStacks = checked(PoisonStacks + amount);
            return true;
        }

        internal bool TrySetPoisonStacks(int stacks)
        {
            if (!SupportsStatus || stacks < 0)
            {
                return false;
            }

            PoisonStacks = stacks;
            return true;
        }
    }
}
