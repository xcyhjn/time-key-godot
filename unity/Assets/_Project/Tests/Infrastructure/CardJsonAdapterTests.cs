using System;
using System.IO;
using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Infrastructure;
using UnityEngine;

namespace TimeKey.Tests.Infrastructure
{
    public sealed class CardJsonAdapterTests
    {
        [Test]
        public void Parse_RealLightingFixture_PreservesContractFields()
        {
            var definition = CardJsonAdapter.Parse(ReadFixture());

            Assert.That(definition.StableId, Is.EqualTo("lighting"));
            Assert.That(definition.NumericId, Is.EqualTo(1));
            Assert.That(definition.Effects.Count, Is.EqualTo(1));
            Assert.That(definition.Effects[0].Kind, Is.EqualTo(CardEffectKind.Damage));
            Assert.That(definition.Effects[0].Value, Is.EqualTo(100));
            Assert.That(
                definition.Range,
                Is.EqualTo(new[] { new HexCoord(0, 0), new HexCoord(1, 0), new HexCoord(2, 0) }));
            Assert.That(definition.Shape, Is.EqualTo(new[] { new TimelineCell(0, 0) }));
        }

        [Test]
        public void Parse_IgnoresUnmappedChineseDescriptionFields()
        {
            const string json =
                "{\"name\":\"lighting\",\"效果\":\"unused\",\"时代\":\"1\"," +
                "\"effects\":[{\"type\":\"damage\",\"value\":100}]," +
                "\"effect_range\":[\"0,0\"],\"shape\":\"1\",\"id\":\"1\"}";

            var definition = CardJsonAdapter.Parse(json);

            Assert.That(definition.StableId, Is.EqualTo("lighting"));
        }

        [Test]
        public void TryParse_MissingStableId_ReturnsExplicitFailure()
        {
            const string json =
                "{\"effects\":[{\"type\":\"damage\",\"value\":100}]," +
                "\"effect_range\":[\"0,0\"],\"shape\":\"1\",\"id\":\"1\"}";

            var succeeded = CardJsonAdapter.TryParse(json, out var definition, out var error);

            Assert.That(succeeded, Is.False);
            Assert.That(definition, Is.Null);
            Assert.That(error, Does.Contain("stable name"));
        }

        [Test]
        public void Parse_UnknownEffect_ThrowsFormatException()
        {
            const string json =
                "{\"name\":\"lighting\",\"effects\":[{\"type\":\"teleport\",\"value\":1}]," +
                "\"effect_range\":[\"0,0\"],\"shape\":\"1\",\"id\":\"1\"}";

            Assert.Throws<FormatException>(() => CardJsonAdapter.Parse(json));
        }

        [TestCase("1x")]
        [TestCase("x")]
        public void Parse_InvalidShape_ThrowsFormatException(string shape)
        {
            var json =
                "{\"name\":\"lighting\",\"effects\":[{\"type\":\"damage\",\"value\":100}]," +
                "\"effect_range\":[\"0,0\"],\"shape\":\"" + shape + "\",\"id\":\"1\"}";

            Assert.Throws<FormatException>(() => CardJsonAdapter.Parse(json));
        }

        [Test]
        public void Parse_EmptyOccupiedShape_FallsBackToSingleCell()
        {
            const string json =
                "{\"name\":\"wind\",\"effects\":[{\"type\":\"damage\",\"value\":0}]," +
                "\"effect_range\":[\"0,0\"],\"shape\":\"0\",\"id\":\"4\"}";

            var definition = CardJsonAdapter.Parse(json);

            Assert.That(definition.Shape, Is.EqualTo(new[] { new TimelineCell(0, 0) }));
        }

        [Test]
        public void Parse_ShapeRows_AcceptsWhitespaceSeparators()
        {
            const string json =
                "{\"name\":\"tower\",\"effects\":[{\"type\":\"damage\",\"value\":0}]," +
                "\"effect_range\":[\"0,0\"],\"shape\":\"010\\n111\",\"id\":\"6\"}";

            var definition = CardJsonAdapter.Parse(json);

            Assert.That(
                definition.Shape,
                Is.EqualTo(new[]
                {
                    new TimelineCell(1, 0),
                    new TimelineCell(0, 1),
                    new TimelineCell(1, 1),
                    new TimelineCell(2, 1)
                }));
        }

        private static string ReadFixture()
        {
            return File.ReadAllText(
                Path.Combine(Application.dataPath, "_Project", "Content", "Cards", "lighting.json"));
        }
    }
}
