using System;
using System.Collections.Generic;

namespace TimeKey.Domain.Deck
{
    internal sealed class DeterministicDeckRandom
    {
        private ulong _state;

        public DeterministicDeckRandom(ulong seed)
        {
            _state = seed;
        }

        public void Shuffle<T>(IList<T> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            for (var index = values.Count - 1; index > 0; index--)
            {
                var swapIndex = NextIndex(index + 1);
                if (swapIndex == index)
                {
                    continue;
                }

                var value = values[index];
                values[index] = values[swapIndex];
                values[swapIndex] = value;
            }
        }

        private int NextIndex(int exclusiveMaximum)
        {
            var maximum = (ulong)exclusiveMaximum;
            var rejectionThreshold = unchecked(0UL - maximum) % maximum;
            ulong value;
            do
            {
                value = NextUInt64();
            }
            while (value < rejectionThreshold);

            return (int)(value % maximum);
        }

        private ulong NextUInt64()
        {
            _state += 0x9E3779B97F4A7C15UL;
            var value = _state;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
