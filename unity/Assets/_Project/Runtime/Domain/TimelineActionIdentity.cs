using System;
using System.Globalization;

namespace TimeKey.Domain
{
    public readonly struct TimelineActionIdentity : IEquatable<TimelineActionIdentity>
    {
        private readonly string _value;

        public TimelineActionIdentity(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A timeline action identity is required.", nameof(value));
            }

            _value = value;
        }

        public string Value => _value;

        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public static TimelineActionIdentity FromSequence(long sequence, int ordinal)
        {
            if (sequence <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }

            if (ordinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }

            return new TimelineActionIdentity(string.Format(
                CultureInfo.InvariantCulture,
                "cycle:{0}/action:{1}",
                sequence,
                ordinal));
        }

        public bool Equals(TimelineActionIdentity other)
        {
            return StringComparer.Ordinal.Equals(_value, other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is TimelineActionIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        }

        public override string ToString()
        {
            return _value ?? string.Empty;
        }

        public static bool operator ==(TimelineActionIdentity left, TimelineActionIdentity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TimelineActionIdentity left, TimelineActionIdentity right)
        {
            return !left.Equals(right);
        }
    }
}
