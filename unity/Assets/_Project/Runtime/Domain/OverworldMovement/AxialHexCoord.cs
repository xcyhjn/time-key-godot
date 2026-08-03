using System;

namespace TimeKey.Domain.OverworldMovement
{
    public readonly struct AxialHexCoord : IEquatable<AxialHexCoord>
    {
        public AxialHexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int Q { get; }

        public int R { get; }

        public int DistanceTo(AxialHexCoord other)
        {
            var dq = Q - other.Q;
            var dr = R - other.R;
            return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2;
        }

        public bool IsAdjacentTo(AxialHexCoord other)
        {
            return DistanceTo(other) == 1;
        }

        public bool Equals(AxialHexCoord other)
        {
            return Q == other.Q && R == other.R;
        }

        public override bool Equals(object obj)
        {
            return obj is AxialHexCoord && Equals((AxialHexCoord)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Q * 397) ^ R;
            }
        }

        public override string ToString()
        {
            return "(" + Q + "," + R + ")";
        }
    }
}
