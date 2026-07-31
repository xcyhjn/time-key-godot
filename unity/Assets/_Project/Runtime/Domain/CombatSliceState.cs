using System;

namespace TimeKey.Domain
{
    public sealed class CombatSliceState
    {
        public CombatSliceState(string targetId, int targetHp, int seed, int turn = 1)
        {
            if (string.IsNullOrWhiteSpace(targetId))
            {
                throw new ArgumentException("A target ID is required.", nameof(targetId));
            }

            if (targetHp < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetHp));
            }

            if (turn < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(turn));
            }

            TargetId = targetId;
            TargetHp = targetHp;
            Seed = seed;
            Turn = turn;
        }

        public string TargetId { get; }

        public int TargetHp { get; private set; }

        public int Seed { get; }

        public int Turn { get; }

        internal void ApplyDamage(string targetId, int damage)
        {
            if (targetId != TargetId || damage <= 0)
            {
                return;
            }

            TargetHp = Math.Max(0, TargetHp - damage);
        }
    }
}
