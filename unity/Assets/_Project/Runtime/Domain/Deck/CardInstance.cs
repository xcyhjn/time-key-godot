using System;
using System.Globalization;

namespace TimeKey.Domain.Deck
{
    public readonly struct CardInstanceId : IEquatable<CardInstanceId>
    {
        private readonly string _value;

        public CardInstanceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A card instance identity is required.", nameof(value));
            }

            _value = value.Trim();
        }

        public string Value => _value;

        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public static CardInstanceId FromOrdinal(string scope, int ordinal)
        {
            if (string.IsNullOrWhiteSpace(scope))
            {
                throw new ArgumentException("A card instance scope is required.", nameof(scope));
            }

            if (ordinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }

            return new CardInstanceId(string.Format(
                CultureInfo.InvariantCulture,
                "{0}/card:{1:D2}",
                scope.Trim(),
                ordinal));
        }

        public bool Equals(CardInstanceId other)
        {
            return StringComparer.Ordinal.Equals(_value, other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is CardInstanceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        }

        public override string ToString()
        {
            return _value ?? string.Empty;
        }

        public static bool operator ==(CardInstanceId left, CardInstanceId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CardInstanceId left, CardInstanceId right)
        {
            return !left.Equals(right);
        }
    }

    public sealed class CardInstance
    {
        public CardInstance(string stableId, CardInstanceId instanceId)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                throw new ArgumentException("A stable card ID is required.", nameof(stableId));
            }

            if (!instanceId.IsValid)
            {
                throw new ArgumentException("A card instance identity is required.", nameof(instanceId));
            }

            StableId = stableId.Trim();
            InstanceId = instanceId;
        }

        public string StableId { get; }

        public CardInstanceId InstanceId { get; }
    }
}
