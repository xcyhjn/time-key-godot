using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Infrastructure;
using UnityEngine;

namespace TimeKey.Tests.Infrastructure
{
    public sealed class CardJsonAdapterTests
    {
        private static readonly CardExpectation[] RealCardExpectations =
        {
            new CardExpectation(
                "lighting",
                1,
                "lighting.png",
                CardEffectKind.Damage,
                100,
                null,
                new[] { new HexCoord(0, 0), new HexCoord(1, 0), new HexCoord(2, 0) },
                new[] { new TimelineCell(0, 0) },
                Array.Empty<TimelineCell>()),
            new CardExpectation(
                "earthquake",
                2,
                "earthquake.png",
                CardEffectKind.Elevation,
                2,
                null,
                new[]
                {
                    new HexCoord(0, 0),
                    new HexCoord(1, 0),
                    new HexCoord(1, -1),
                    new HexCoord(0, -1),
                    new HexCoord(-1, 0),
                    new HexCoord(-1, 1),
                    new HexCoord(0, 1)
                },
                new[] { new TimelineCell(0, 0), new TimelineCell(1, 0) },
                Array.Empty<TimelineCell>()),
            new CardExpectation(
                "wind",
                4,
                "wind.png",
                CardEffectKind.Clear,
                null,
                null,
                new[] { new HexCoord(0, 0) },
                Array.Empty<TimelineCell>(),
                new[]
                {
                    new TimelineCell(0, 0),
                    new TimelineCell(1, 0),
                    new TimelineCell(0, 1),
                    new TimelineCell(1, 1)
                }),
            new CardExpectation(
                "recover",
                5,
                "recover.png",
                CardEffectKind.Recover,
                100,
                null,
                new[] { new HexCoord(0, 0), new HexCoord(1, 0), new HexCoord(-1, 1) },
                new[]
                {
                    new TimelineCell(0, 0),
                    new TimelineCell(1, 0),
                    new TimelineCell(2, 0)
                },
                Array.Empty<TimelineCell>()),
            new CardExpectation(
                "tower",
                6,
                "tower_card.png",
                CardEffectKind.Built,
                1,
                "tower",
                new[] { new HexCoord(0, 0) },
                new[]
                {
                    new TimelineCell(1, 0),
                    new TimelineCell(0, 1),
                    new TimelineCell(1, 1),
                    new TimelineCell(2, 1)
                },
                Array.Empty<TimelineCell>()),
            new CardExpectation(
                "poison",
                7,
                "poison_card.png",
                CardEffectKind.Poison,
                2,
                null,
                new[] { new HexCoord(0, 0) },
                new[]
                {
                    new TimelineCell(0, 0),
                    new TimelineCell(1, 0),
                    new TimelineCell(0, 1),
                    new TimelineCell(1, 1),
                    new TimelineCell(2, 1)
                },
                Array.Empty<TimelineCell>()),
            new CardExpectation(
                "tornado",
                8,
                "tornado.png",
                CardEffectKind.Clear,
                null,
                null,
                new[] { new HexCoord(0, 0) },
                Array.Empty<TimelineCell>(),
                new[]
                {
                    new TimelineCell(0, 0),
                    new TimelineCell(1, 0),
                    new TimelineCell(2, 0),
                    new TimelineCell(3, 0),
                    new TimelineCell(4, 0),
                    new TimelineCell(5, 0),
                    new TimelineCell(6, 0),
                    new TimelineCell(7, 0),
                    new TimelineCell(8, 0),
                    new TimelineCell(9, 0),
                    new TimelineCell(10, 0),
                    new TimelineCell(11, 0)
                })
        };

        [TestCaseSource(nameof(RealCardExpectations))]
        public void Parse_RealFixture_PreservesContractFields(CardExpectation expected)
        {
            var definition = CardJsonAdapter.Parse(ReadFixture(expected.StableId));

            Assert.That(definition.StableId, Is.EqualTo(expected.StableId));
            Assert.That(definition.NumericId, Is.EqualTo(expected.NumericId));
            Assert.That(definition.FrontImage, Is.EqualTo(expected.FrontImage));
            Assert.That(definition.Effects.Count, Is.EqualTo(1));
            Assert.That(definition.Effects[0].Kind, Is.EqualTo(expected.EffectKind));
            Assert.That(definition.Effects[0].NumericAmount, Is.EqualTo(expected.NumericAmount));
            Assert.That(definition.Effects[0].CreationId, Is.EqualTo(expected.CreationId));
            Assert.That(definition.Effects[0].ClearMask, Is.EqualTo(expected.ClearMask));
            Assert.That(definition.Range, Is.EqualTo(expected.Range));
            Assert.That(definition.Shape, Is.EqualTo(expected.Shape));
        }

        [Test]
        public void Parse_LightingFixture_PreservesLegacyValueProperty()
        {
            var definition = CardJsonAdapter.Parse(ReadFixture("lighting"));

            Assert.That(definition.Effects[0].Value, Is.EqualTo(100));
        }

        [Test]
        public void Parse_IgnoresUnmappedChineseDescriptionFields()
        {
            const string json =
                "{\"name\":\"lighting\",\"front_image\":\"lighting.png\",\"效果\":\"unused\",\"时代\":\"1\"," +
                "\"effects\":[{\"type\":\"damage\",\"value\":100}]," +
                "\"effect_range\":[\"0,0\"],\"shape\":\"1\",\"id\":\"1\"}";

            var definition = CardJsonAdapter.Parse(json);

            Assert.That(definition.StableId, Is.EqualTo("lighting"));
        }

        [Test]
        public void TryParse_MissingStableId_ReturnsExplicitFailure()
        {
            const string json =
                "{\"front_image\":\"lighting.png\"," +
                "\"effects\":[{\"type\":\"damage\",\"value\":100}]," +
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
                "{\"name\":\"lighting\",\"front_image\":\"lighting.png\"," +
                "\"effects\":[{\"type\":\"teleport\",\"value\":1}]," +
                "\"effect_range\":[\"0,0\"],\"shape\":\"1\",\"id\":\"1\"}";

            Assert.Throws<FormatException>(() => CardJsonAdapter.Parse(json));
        }

        [TestCase("damage", "\"100\"")]
        [TestCase("elevation", "\"2\"")]
        [TestCase("recover", "null")]
        [TestCase("recover", "1.5")]
        [TestCase("poison", "-1")]
        [TestCase("clear", "1")]
        public void Parse_InvalidEffectValueTypeOrAmount_ThrowsFormatException(
            string effectType,
            string valueJson)
        {
            var json = CreateJson(effectType, "\"value\":" + valueJson);

            Assert.Throws<FormatException>(() => CardJsonAdapter.Parse(json));
        }

        [Test]
        public void Parse_MissingEffectValue_ThrowsFormatException()
        {
            var json = CreateJson("damage", "\"unused\":100");

            Assert.Throws<FormatException>(() => CardJsonAdapter.Parse(json));
        }

        [Test]
        public void Parse_MissingEffects_ThrowsFormatException()
        {
            const string json =
                "{\"name\":\"test\",\"front_image\":\"test.png\"," +
                "\"effect_range\":[\"0,0\"],\"shape\":\"1\",\"id\":\"1\"}";

            Assert.Throws<FormatException>(() => CardJsonAdapter.Parse(json));
        }

        [TestCase("\"creation\":\"\",\"value\":1")]
        [TestCase("\"value\":1")]
        public void Parse_BuiltWithoutCreation_ThrowsFormatException(string effectFields)
        {
            var json = CreateJson("built", effectFields);

            Assert.Throws<FormatException>(() => CardJsonAdapter.Parse(json));
        }

        [TestCase("\"value\":\"\"")]
        [TestCase("\"value\":\"0\"")]
        [TestCase("\"value\":\"10x\"")]
        [TestCase("\"value\":\"1,11\"")]
        public void Parse_InvalidClearMask_ThrowsFormatException(string effectFields)
        {
            var json = CreateJson("clear", effectFields, "0");

            Assert.Throws<FormatException>(() => CardJsonAdapter.Parse(json));
        }

        [Test]
        public void Parse_ClearMixedWithResolveEffect_ThrowsFormatException()
        {
            const string json =
                "{\"name\":\"mixed\",\"front_image\":\"mixed.png\"," +
                "\"effects\":[{\"type\":\"clear\",\"value\":\"1\"},{\"type\":\"damage\",\"value\":1}]," +
                "\"effect_range\":[\"0,0\"],\"shape\":\"0\",\"id\":\"9\"}";

            Assert.Throws<FormatException>(() => CardJsonAdapter.Parse(json));
        }

        [TestCase("")]
        [TestCase("../lighting.png")]
        [TestCase("cards/lighting.png")]
        [TestCase("cards\\lighting.png")]
        [TestCase("/lighting.png")]
        public void Parse_InvalidFrontImage_ThrowsFormatException(string frontImage)
        {
            var json =
                "{\"name\":\"lighting\",\"front_image\":\"" + frontImage.Replace("\\", "\\\\") + "\"," +
                "\"effects\":[{\"type\":\"damage\",\"value\":100}]," +
                "\"effect_range\":[\"0,0\"],\"shape\":\"1\",\"id\":\"1\"}";

            Assert.Throws<FormatException>(() => CardJsonAdapter.Parse(json));
        }

        [TestCase("1x")]
        [TestCase("x")]
        public void Parse_InvalidShape_ThrowsFormatException(string shape)
        {
            var json = CreateJson("damage", "\"value\":100", shape);

            Assert.Throws<FormatException>(() => CardJsonAdapter.Parse(json));
        }

        [Test]
        public void Parse_EmptyOccupiedShape_FallsBackToSingleCellForResolveCard()
        {
            var definition = CardJsonAdapter.Parse(CreateJson("damage", "\"value\":0", "0"));

            Assert.That(definition.Shape, Is.EqualTo(new[] { new TimelineCell(0, 0) }));
        }

        [Test]
        public void Parse_ShapeRows_AcceptsWhitespaceSeparators()
        {
            var definition = CardJsonAdapter.Parse(
                CreateJson("built", "\"creation\":\"tower\",\"value\":1", "010\\n111"));

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

        private static string CreateJson(string effectType, string effectFields, string shape = "1")
        {
            return
                "{\"name\":\"test\",\"front_image\":\"test.png\"," +
                "\"effects\":[{\"type\":\"" + effectType + "\"," + effectFields + "}]," +
                "\"effect_range\":[\"0,0\"],\"shape\":\"" + shape + "\",\"id\":\"1\"}";
        }

        private static string ReadFixture(string stableId)
        {
            return File.ReadAllText(
                Path.Combine(UnityEngine.Application.dataPath, "_Project", "Content", "Cards", stableId + ".json"));
        }

        public sealed class CardExpectation
        {
            public CardExpectation(
                string stableId,
                int numericId,
                string frontImage,
                CardEffectKind effectKind,
                int? numericAmount,
                string creationId,
                IReadOnlyList<HexCoord> range,
                IReadOnlyList<TimelineCell> shape,
                IReadOnlyList<TimelineCell> clearMask)
            {
                StableId = stableId;
                NumericId = numericId;
                FrontImage = frontImage;
                EffectKind = effectKind;
                NumericAmount = numericAmount;
                CreationId = creationId;
                Range = range;
                Shape = shape;
                ClearMask = clearMask;
            }

            public string StableId { get; }

            public int NumericId { get; }

            public string FrontImage { get; }

            public CardEffectKind EffectKind { get; }

            public int? NumericAmount { get; }

            public string CreationId { get; }

            public IReadOnlyList<HexCoord> Range { get; }

            public IReadOnlyList<TimelineCell> Shape { get; }

            public IReadOnlyList<TimelineCell> ClearMask { get; }

            public override string ToString()
            {
                return StableId;
            }
        }
    }
}
