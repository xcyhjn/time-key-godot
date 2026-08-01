using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TimeKey.Application;
using TimeKey.Domain;

namespace TimeKey.Infrastructure.Cards
{
    public sealed class CardContentCatalog : ICardCatalog
    {
        private readonly ReadOnlyCollection<CardDefinition> _cards;
        private readonly ReadOnlyCollection<CardContentEntry> _entries;
        private readonly Dictionary<string, CardContentEntry> _entriesByStableId;

        public CardContentCatalog(IReadOnlyList<CardDefinition> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            if (cards.Count == 0)
            {
                throw new ArgumentException("At least one card is required.", nameof(cards));
            }

            var copiedCards = new List<CardDefinition>(cards.Count);
            var entries = new List<CardContentEntry>(cards.Count);
            _entriesByStableId = new Dictionary<string, CardContentEntry>(StringComparer.Ordinal);
            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index] ??
                    throw new ArgumentException("Card definitions cannot contain null.", nameof(cards));
                var entry = new CardContentEntry(card);
                if (!_entriesByStableId.TryAdd(card.StableId, entry))
                {
                    throw new ArgumentException(
                        "Duplicate card stable ID: " + card.StableId + ".",
                        nameof(cards));
                }

                copiedCards.Add(card);
                entries.Add(entry);
            }

            _cards = new ReadOnlyCollection<CardDefinition>(copiedCards);
            _entries = new ReadOnlyCollection<CardContentEntry>(entries);
        }

        public IReadOnlyList<CardDefinition> Cards => _cards;

        public IReadOnlyList<CardContentEntry> Entries => _entries;

        public static CardContentCatalog FromJson(IReadOnlyList<string> documents)
        {
            if (documents == null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            var cards = new List<CardDefinition>(documents.Count);
            for (var index = 0; index < documents.Count; index++)
            {
                cards.Add(CardJsonAdapter.Parse(documents[index]));
            }

            return new CardContentCatalog(cards);
        }

        public bool TryGet(string stableId, out CardDefinition card)
        {
            CardContentEntry entry;
            if (stableId != null && _entriesByStableId.TryGetValue(stableId, out entry))
            {
                card = entry.Definition;
                return true;
            }

            card = null;
            return false;
        }

        public bool TryGetEntry(string stableId, out CardContentEntry entry)
        {
            if (stableId != null && _entriesByStableId.TryGetValue(stableId, out entry))
            {
                return true;
            }

            entry = null;
            return false;
        }
    }
}
