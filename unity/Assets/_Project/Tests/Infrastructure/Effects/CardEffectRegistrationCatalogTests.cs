using System;
using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Infrastructure.Effects;

namespace TimeKey.Tests.Infrastructure.Effects
{
    public sealed class CardEffectRegistrationCatalogTests
    {
        [Test]
        public void VerticalSlice_RegistersDamageElevationAndRecoverByKind()
        {
            var catalog = CardEffectRegistrationCatalog.CreateVerticalSlice();

            Assert.That(catalog.Supports(CardEffectKind.Damage), Is.True);
            Assert.That(catalog.Supports(CardEffectKind.Elevation), Is.True);
            Assert.That(catalog.Supports(CardEffectKind.Recover), Is.True);
            Assert.That(catalog.Supports(CreateCard(CardEffectKind.Damage)), Is.True);
            Assert.That(catalog.Supports(CreateCard(CardEffectKind.Recover)), Is.True);
            Assert.That(catalog.Supports(CreateCard(CardEffectKind.Poison)), Is.False);
        }

        [Test]
        public void Constructor_DuplicateKind_FailsExplicitly()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new CardEffectRegistrationCatalog(
                    new[]
                    {
                        new CardEffectRegistration(CardEffectKind.Damage),
                        new CardEffectRegistration(CardEffectKind.Damage)
                    }));

            Assert.That(exception.Message, Does.Contain("Duplicate effect registration: Damage"));
        }

        private static CardDefinition CreateCard(CardEffectKind kind)
        {
            return new CardDefinition(
                "test_" + kind,
                30,
                "test.png",
                new[] { new CardEffect(kind, 1) },
                new[] { new HexCoord(0, 0) },
                new[] { new TimelineCell(0, 0) });
        }
    }
}
