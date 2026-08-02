using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain.Deck
{
    public static class StarterDeck
    {
        private static readonly string[] StableIds =
        {
            "lighting",
            "lighting",
            "earthquake",
            "earthquake",
            "recover",
            "wind",
            "wind",
            "recover",
            "tower",
            "tower",
            "poison",
            "poison"
        };

        public static IReadOnlyList<string> OrderedStableIds
        {
            get
            {
                var copy = new List<string>(StableIds.Length);
                for (var index = 0; index < StableIds.Length; index++)
                {
                    copy.Add(StableIds[index]);
                }

                return new ReadOnlyCollection<string>(copy);
            }
        }

        public static IReadOnlyList<CardInstance> CreateInstances(string identityScope)
        {
            if (string.IsNullOrWhiteSpace(identityScope))
            {
                throw new ArgumentException(
                    "A globally unique battle identity scope is required.",
                    nameof(identityScope));
            }

            var cards = new List<CardInstance>(StableIds.Length);
            for (var index = 0; index < StableIds.Length; index++)
            {
                cards.Add(new CardInstance(
                    StableIds[index],
                    CardInstanceId.FromOrdinal(identityScope, index)));
            }

            return new ReadOnlyCollection<CardInstance>(cards);
        }
    }
}
