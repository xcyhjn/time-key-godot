using System;

namespace TimeKey.Domain.OverworldMovement
{
    public readonly struct MapNodeId : IEquatable<MapNodeId>
    {
        public MapNodeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A map node identity is required.", nameof(value));
            }

            Value = value.Trim();
        }

        public string Value { get; }

        public bool Equals(MapNodeId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is MapNodeId && Equals((MapNodeId)obj);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(MapNodeId left, MapNodeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MapNodeId left, MapNodeId right)
        {
            return !left.Equals(right);
        }
    }
}
