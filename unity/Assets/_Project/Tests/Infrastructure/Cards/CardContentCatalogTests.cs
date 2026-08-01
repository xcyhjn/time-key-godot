using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Infrastructure.Cards;

namespace TimeKey.Tests.Infrastructure.Cards
{
    public sealed class CardContentCatalogTests
    {
        private static readonly string[] StableIds =
        {
            "earthquake",
            "lighting",
            "poison",
            "recover",
            "tornado",
            "tower",
            "wind"
        };

        [Test]
        public void FromJson_LoadsAllSevenRealCardsAndFrontImages()
        {
            var catalog = CardContentCatalog.FromJson(ReadRealDocuments());

            Assert.That(catalog.Cards.Select(card => card.StableId), Is.EqualTo(StableIds));
            Assert.That(catalog.Cards.Select(card => card.StableId).Distinct().Count(), Is.EqualTo(7));
            Assert.That(catalog.Entries.All(entry => !string.IsNullOrWhiteSpace(entry.FrontImage)), Is.True);
            Assert.That(GetEntry(catalog, "tower").ArtworkResourcePath,
                Is.EqualTo("Art/Battle/Cards/tower_card"));
            Assert.That(GetEntry(catalog, "poison").ArtworkResourcePath,
                Is.EqualTo("Art/Battle/Cards/poison_card"));
        }

        [Test]
        public void Constructor_DuplicateStableId_FailsExplicitly()
        {
            var card = CreateCard("duplicate", 20);

            var exception = Assert.Throws<ArgumentException>(
                () => new CardContentCatalog(new[] { card, card }));

            Assert.That(exception.Message, Does.Contain("Duplicate card stable ID: duplicate"));
        }

        [Test]
        public void FromJson_InvalidDocument_PropagatesTypedSchemaFailure()
        {
            const string missingFrontImage =
                "{\"name\":\"bad\",\"effects\":[{\"type\":\"damage\",\"value\":1}]," +
                "\"effect_range\":[\"0,0\"],\"shape\":\"1\",\"id\":\"20\"}";

            Assert.Throws<FormatException>(
                () => CardContentCatalog.FromJson(new[] { missingFrontImage }));
        }

        [Test]
        public void Constructor_AdditionalOrdinaryCard_NeedsNoRoutingChange()
        {
            var cards = CardContentCatalog.FromJson(ReadRealDocuments()).Cards.ToList();
            cards.Add(CreateCard("ordinary_extra", 20));

            var catalog = new CardContentCatalog(cards);

            Assert.That(catalog.Cards.Count, Is.EqualTo(8));
            Assert.That(catalog.TryGet("ordinary_extra", out var extra), Is.True);
            Assert.That(extra.FrontImage, Is.EqualTo("ordinary_extra.png"));
        }

        private static IReadOnlyList<string> ReadRealDocuments()
        {
            var root = Path.Combine(
                UnityEngine.Application.dataPath,
                "_Project",
                "Content",
                "Cards");
            return StableIds.Select(id => File.ReadAllText(Path.Combine(root, id + ".json"))).ToArray();
        }

        private static CardContentEntry GetEntry(CardContentCatalog catalog, string stableId)
        {
            Assert.That(catalog.TryGetEntry(stableId, out var entry), Is.True);
            return entry;
        }

        private static CardDefinition CreateCard(string stableId, int numericId)
        {
            return new CardDefinition(
                stableId,
                numericId,
                stableId + ".png",
                new[] { new CardEffect(CardEffectKind.Damage, 1) },
                new[] { new HexCoord(0, 0) },
                new[] { new TimelineCell(0, 0) });
        }
    }
}
