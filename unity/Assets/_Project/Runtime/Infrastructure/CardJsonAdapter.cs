using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using TimeKey.Domain;
using UnityEngine;

namespace TimeKey.Infrastructure
{
    public static class CardJsonAdapter
    {
        public static CardDefinition Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException("Card JSON is required.");
            }

            var dto = Deserialize<CardJsonDto>(json);
            if (dto == null || string.IsNullOrWhiteSpace(dto.name))
            {
                throw new FormatException("Card JSON must contain a stable name.");
            }

            int numericId;
            if (!int.TryParse(dto.id, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericId))
            {
                throw new FormatException("Card JSON must contain a numeric id.");
            }

            var frontImage = ParseFrontImage(dto.front_image);
            ValidateEffectValueTokens(json, dto.effects);
            var effects = ParseEffects(json, dto.effects);
            var isClear = effects.Count == 1 && effects[0].Kind == CardEffectKind.Clear;

            try
            {
                return new CardDefinition(
                    dto.name,
                    numericId,
                    frontImage,
                    effects,
                    ParseRange(dto.effect_range),
                    ParseShape(dto.shape, isClear));
            }
            catch (ArgumentException exception)
            {
                throw new FormatException("Card JSON violates the card definition contract.", exception);
            }
        }

        public static bool TryParse(string json, out CardDefinition definition, out string error)
        {
            try
            {
                definition = Parse(json);
                error = null;
                return true;
            }
            catch (FormatException exception)
            {
                definition = null;
                error = exception.Message;
                return false;
            }
        }

        private static IReadOnlyList<CardEffect> ParseEffects(
            string json,
            EffectCommonJsonDto[] effects)
        {
            if (effects == null || effects.Length == 0)
            {
                throw new FormatException("Card JSON must contain at least one effect.");
            }

            var kinds = new CardEffectKind[effects.Length];
            var hasClear = false;
            for (var index = 0; index < effects.Length; index++)
            {
                var effect = effects[index];
                if (effect == null || !TryParseEffectKind(effect.type, out kinds[index]))
                {
                    throw new FormatException("Card JSON contains an unknown effect type.");
                }

                hasClear |= kinds[index] == CardEffectKind.Clear;
            }

            if (hasClear && effects.Length != 1)
            {
                throw new FormatException("Clear must be the card's only effect.");
            }

            return hasClear
                ? ParseClearEffect(json)
                : ParseNumericEffects(json, effects, kinds);
        }

        private static void ValidateEffectValueTokens(
            string json,
            EffectCommonJsonDto[] effects)
        {
            if (effects == null || effects.Length == 0)
            {
                throw new FormatException("Card JSON must contain at least one effect.");
            }

            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (Newtonsoft.Json.JsonException exception)
            {
                throw new FormatException("Card JSON is malformed.", exception);
            }

            var effectTokens = root["effects"] as JArray;
            if (effectTokens == null || effectTokens.Count != effects.Length)
            {
                throw new FormatException("Card JSON must contain an effects array.");
            }

            for (var index = 0; index < effectTokens.Count; index++)
            {
                var effectToken = effectTokens[index] as JObject;
                var valueToken = effectToken?["value"];
                if (valueToken == null)
                {
                    throw new FormatException("Card effects must contain a value.");
                }

                var type = effects[index]?.type;
                if (string.Equals(type, "clear", StringComparison.Ordinal))
                {
                    if (valueToken.Type != JTokenType.String)
                    {
                        throw new FormatException("Clear effect values must be strings.");
                    }
                }
                else if (valueToken.Type != JTokenType.Integer)
                {
                    throw new FormatException("Numeric card effect values must be JSON integers.");
                }
            }
        }

        private static IReadOnlyList<CardEffect> ParseNumericEffects(
            string json,
            EffectCommonJsonDto[] effects,
            CardEffectKind[] kinds)
        {
            var numericDto = Deserialize<NumericEffectsJsonDto>(json);
            if (numericDto == null ||
                numericDto.effects == null ||
                numericDto.effects.Length != effects.Length)
            {
                throw new FormatException("Card JSON contains an invalid numeric effect payload.");
            }

            var result = new List<CardEffect>(effects.Length);
            for (var index = 0; index < effects.Length; index++)
            {
                var numericEffect = numericDto.effects[index];
                if (numericEffect == null || numericEffect.value < 0)
                {
                    throw new FormatException("Numeric card effect values must be non-negative integers.");
                }

                var creationId = effects[index].creation;
                if (kinds[index] == CardEffectKind.Built)
                {
                    if (string.IsNullOrWhiteSpace(creationId))
                    {
                        throw new FormatException("Built effects require a creation ID.");
                    }
                }
                else if (!string.IsNullOrEmpty(creationId))
                {
                    throw new FormatException("Only Built effects may contain a creation ID.");
                }

                try
                {
                    result.Add(new CardEffect(kinds[index], numericEffect.value, creationId));
                }
                catch (ArgumentException exception)
                {
                    throw new FormatException("Card JSON contains an invalid numeric effect payload.", exception);
                }
            }

            return result;
        }

        private static IReadOnlyList<CardEffect> ParseClearEffect(string json)
        {
            var stringDto = Deserialize<StringEffectsJsonDto>(json);
            if (stringDto == null ||
                stringDto.effects == null ||
                stringDto.effects.Length != 1 ||
                stringDto.effects[0] == null)
            {
                throw new FormatException("Card JSON contains an invalid clear effect payload.");
            }

            var clearMask = ParseTimelineCells(stringDto.effects[0].value, false);
            try
            {
                return new[] { new CardEffect(CardEffectKind.Clear, clearMask) };
            }
            catch (ArgumentException exception)
            {
                throw new FormatException("Card JSON contains an invalid clear effect payload.", exception);
            }
        }

        private static bool TryParseEffectKind(string type, out CardEffectKind kind)
        {
            switch (type)
            {
                case "damage":
                    kind = CardEffectKind.Damage;
                    return true;
                case "elevation":
                    kind = CardEffectKind.Elevation;
                    return true;
                case "recover":
                    kind = CardEffectKind.Recover;
                    return true;
                case "built":
                    kind = CardEffectKind.Built;
                    return true;
                case "poison":
                    kind = CardEffectKind.Poison;
                    return true;
                case "clear":
                    kind = CardEffectKind.Clear;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        private static string ParseFrontImage(string frontImage)
        {
            if (string.IsNullOrWhiteSpace(frontImage))
            {
                throw new FormatException("Card JSON must contain a front image filename.");
            }

            var normalized = frontImage.Trim();
            if (normalized.IndexOf('/') >= 0 ||
                normalized.IndexOf('\\') >= 0 ||
                normalized.IndexOf(':') >= 0 ||
                normalized.Contains(".."))
            {
                throw new FormatException("Card front_image must be a plain filename.");
            }

            return normalized;
        }

        private static IReadOnlyList<HexCoord> ParseRange(string[] offsets)
        {
            if (offsets == null || offsets.Length == 0)
            {
                throw new FormatException("Card JSON must contain at least one range offset.");
            }

            var result = new List<HexCoord>(offsets.Length);
            for (var index = 0; index < offsets.Length; index++)
            {
                var parts = offsets[index] == null ? Array.Empty<string>() : offsets[index].Split(',');
                int q;
                int r;
                if (parts.Length != 2 ||
                    !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out q) ||
                    !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out r))
                {
                    throw new FormatException("Card JSON contains an invalid range offset.");
                }

                result.Add(new HexCoord(q, r));
            }

            return result;
        }

        private static IReadOnlyList<TimelineCell> ParseShape(string shape, bool isClear)
        {
            var parsedShape = ParseTimelineCells(shape, true);
            return isClear ? Array.Empty<TimelineCell>() : parsedShape;
        }

        private static IReadOnlyList<TimelineCell> ParseTimelineCells(
            string encoded,
            bool emptyFallsBackToSingleCell)
        {
            if (string.IsNullOrWhiteSpace(encoded))
            {
                throw new FormatException("Card JSON must contain a non-empty timeline matrix.");
            }

            var rows = encoded.Split(
                new[] { ',', ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length == 0)
            {
                throw new FormatException("Card JSON must contain a non-empty timeline matrix.");
            }

            var width = rows[0].Length;
            var occupied = new List<TimelineCell>();
            var minX = int.MaxValue;
            var minY = int.MaxValue;

            for (var y = 0; y < rows.Length; y++)
            {
                if (rows[y].Length != width)
                {
                    throw new FormatException("Timeline matrix rows must have equal widths.");
                }

                for (var x = 0; x < rows[y].Length; x++)
                {
                    var value = rows[y][x];
                    if (value != '0' && value != '1')
                    {
                        throw new FormatException("Timeline matrices may contain only 0 and 1.");
                    }

                    if (value == '1')
                    {
                        occupied.Add(new TimelineCell(x, y));
                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y);
                    }
                }
            }

            if (occupied.Count == 0)
            {
                if (emptyFallsBackToSingleCell)
                {
                    return new[] { new TimelineCell(0, 0) };
                }

                throw new FormatException("Clear masks must occupy at least one cell.");
            }

            var normalized = new List<TimelineCell>(occupied.Count);
            for (var index = 0; index < occupied.Count; index++)
            {
                normalized.Add(new TimelineCell(occupied[index].X - minX, occupied[index].Y - minY));
            }

            return normalized;
        }

        private static T Deserialize<T>(string json)
        {
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (ArgumentException exception)
            {
                throw new FormatException("Card JSON is malformed or contains a wrong payload type.", exception);
            }
        }

        [Serializable]
        private sealed class CardJsonDto
        {
            public string name = null;
            public string front_image = null;
            public EffectCommonJsonDto[] effects = null;
            public string[] effect_range = null;
            public string shape = null;
            public string id = null;
        }

        [Serializable]
        private sealed class EffectCommonJsonDto
        {
            public string type = null;
            public string creation = null;
        }

        [Serializable]
        private sealed class NumericEffectsJsonDto
        {
            public NumericEffectJsonDto[] effects = null;
        }

        [Serializable]
        private sealed class NumericEffectJsonDto
        {
            public int value = -1;
        }

        [Serializable]
        private sealed class StringEffectsJsonDto
        {
            public StringEffectJsonDto[] effects = null;
        }

        [Serializable]
        private sealed class StringEffectJsonDto
        {
            public string value = null;
        }

    }
}
