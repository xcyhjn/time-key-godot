using System;

namespace TimeKey.Domain
{
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int Q { get; }

        public int R { get; }

        public bool Equals(HexCoord other)
        {
            return Q == other.Q && R == other.R;
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Q * 397) ^ R;
            }
        }

        public static bool operator ==(HexCoord left, HexCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HexCoord left, HexCoord right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct TimelineCell : IEquatable<TimelineCell>
    {
        public TimelineCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        public bool Equals(TimelineCell other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is TimelineCell other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public static TimelineCell operator +(TimelineCell left, TimelineCell right)
        {
            return new TimelineCell(left.X + right.X, left.Y + right.Y);
        }

        public static bool operator ==(TimelineCell left, TimelineCell right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TimelineCell left, TimelineCell right)
        {
            return !left.Equals(right);
        }
    }
}
