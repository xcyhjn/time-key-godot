using System;
using TimeKey.Domain;

namespace TimeKey.Application
{
    public enum CombatTargetKind
    {
        Entity,
        Tile
    }

    public readonly struct CombatTarget : IEquatable<CombatTarget>
    {
        private CombatTarget(CombatTargetKind kind, string entityId, HexCoord coordinate)
        {
            Kind = kind;
            EntityId = entityId;
            Coordinate = coordinate;
        }

        public CombatTargetKind Kind { get; }

        public string EntityId { get; }

        public HexCoord Coordinate { get; }

        public static CombatTarget ForEntity(string entityId, HexCoord coordinate)
        {
            if (string.IsNullOrWhiteSpace(entityId))
            {
                throw new ArgumentException("An entity target ID is required.", nameof(entityId));
            }

            return new CombatTarget(CombatTargetKind.Entity, entityId, coordinate);
        }

        public static CombatTarget ForTile(HexCoord coordinate)
        {
            return new CombatTarget(CombatTargetKind.Tile, null, coordinate);
        }

        public bool Equals(CombatTarget other)
        {
            return Kind == other.Kind &&
                   string.Equals(EntityId, other.EntityId, StringComparison.Ordinal) &&
                   Coordinate == other.Coordinate;
        }

        public override bool Equals(object obj)
        {
            return obj is CombatTarget other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ (EntityId == null ? 0 : EntityId.GetHashCode());
                hashCode = (hashCode * 397) ^ Coordinate.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(CombatTarget left, CombatTarget right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CombatTarget left, CombatTarget right)
        {
            return !left.Equals(right);
        }
    }
}
