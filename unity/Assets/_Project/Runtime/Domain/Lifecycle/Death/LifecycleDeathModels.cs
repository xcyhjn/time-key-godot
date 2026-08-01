using System;

namespace TimeKey.Domain
{
    public enum LifecycleDeathPolicy
    {
        RemainBroken,
        Remove
    }

    public enum LifecycleMutationReason
    {
        TowerDecay,
        PoisonTurnStart
    }

    public sealed class LifecycleOccupantMutation
    {
        public LifecycleOccupantMutation(
            string runtimeId,
            HexCoord coordinate,
            TurnLifecyclePhase phase,
            LifecycleMutationReason reason,
            int expectedHp,
            int expectedPoisonStacks,
            int afterHp,
            int afterPoisonStacks,
            bool remove,
            LifecycleDeathPolicy? deathPolicy = null)
        {
            if (string.IsNullOrWhiteSpace(runtimeId))
            {
                throw new ArgumentException("A runtime occupant ID is required.", nameof(runtimeId));
            }

            if (expectedHp < 0 || expectedPoisonStacks < 0 ||
                afterHp < 0 || afterPoisonStacks < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedHp),
                    "Lifecycle occupant values cannot be negative.");
            }

            if (remove && (afterHp != 0 || afterPoisonStacks != 0))
            {
                throw new ArgumentException(
                    "A removed occupant must finish with zero HP and zero status stacks.");
            }

            RuntimeId = runtimeId.Trim();
            Coordinate = coordinate;
            Phase = phase;
            Reason = reason;
            ExpectedHp = expectedHp;
            ExpectedPoisonStacks = expectedPoisonStacks;
            AfterHp = afterHp;
            AfterPoisonStacks = afterPoisonStacks;
            Remove = remove;
            DeathPolicy = deathPolicy;
        }

        public string RuntimeId { get; }

        public HexCoord Coordinate { get; }

        public TurnLifecyclePhase Phase { get; }

        public LifecycleMutationReason Reason { get; }

        public int ExpectedHp { get; }

        public int ExpectedPoisonStacks { get; }

        public int AfterHp { get; }

        public int AfterPoisonStacks { get; }

        public bool Remove { get; }

        public LifecycleDeathPolicy? DeathPolicy { get; }
    }

    public sealed class LifecycleOccupantChangeResult
    {
        public LifecycleOccupantChangeResult(
            long sequence,
            LifecycleOccupantMutation mutation)
        {
            if (sequence <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }

            if (mutation == null)
            {
                throw new ArgumentNullException(nameof(mutation));
            }

            Sequence = sequence;
            RuntimeId = mutation.RuntimeId;
            Coordinate = mutation.Coordinate;
            Phase = mutation.Phase;
            Reason = mutation.Reason;
            BeforeHp = mutation.ExpectedHp;
            AfterHp = mutation.AfterHp;
            BeforePoisonStacks = mutation.ExpectedPoisonStacks;
            AfterPoisonStacks = mutation.AfterPoisonStacks;
            Removed = mutation.Remove;
            DeathPolicy = mutation.DeathPolicy;
        }

        public long Sequence { get; }

        public string RuntimeId { get; }

        public HexCoord Coordinate { get; }

        public TurnLifecyclePhase Phase { get; }

        public LifecycleMutationReason Reason { get; }

        public int BeforeHp { get; }

        public int AfterHp { get; }

        public int BeforePoisonStacks { get; }

        public int AfterPoisonStacks { get; }

        public bool Removed { get; }

        public LifecycleDeathPolicy? DeathPolicy { get; }

        public bool Died => BeforeHp > 0 && AfterHp == 0;
    }
}
