using System;
using System.Collections.Generic;
using System.Globalization;
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

            CardJsonDto dto;
            try
            {
                dto = JsonUtility.FromJson<CardJsonDto>(json);
            }
            catch (ArgumentException exception)
            {
                throw new FormatException("Card JSON is malformed.", exception);
            }

            if (dto == null || string.IsNullOrWhiteSpace(dto.name))
            {
                throw new FormatException("Card JSON must contain a stable name.");
            }

            int numericId;
            if (!int.TryParse(dto.id, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericId))
            {
                throw new FormatException("Card JSON must contain a numeric id.");
            }

            return new CardDefinition(
                dto.name,
                numericId,
                ParseEffects(dto.effects),
                ParseRange(dto.effect_range),
                ParseShape(dto.shape));
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

        private static IReadOnlyList<CardEffect> ParseEffects(EffectJsonDto[] effects)
        {
            if (effects == null || effects.Length == 0)
            {
                throw new FormatException("Card JSON must contain at least one effect.");
            }

            var result = new List<CardEffect>(effects.Length);
            for (var index = 0; index < effects.Length; index++)
            {
                var effect = effects[index];
                if (effect == null || !string.Equals(effect.type, "damage", StringComparison.Ordinal))
                {
                    throw new FormatException("Card JSON contains an unknown effect type.");
                }

                if (effect.value < 0)
                {
                    throw new FormatException("Card effect values cannot be negative.");
                }

                result.Add(new CardEffect(CardEffectKind.Damage, effect.value));
            }

            return result;
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

        private static IReadOnlyList<TimelineCell> ParseShape(string shape)
        {
            if (string.IsNullOrWhiteSpace(shape))
            {
                throw new FormatException("Card JSON must contain a shape.");
            }

            var rows = shape.Split(
                new[] { ',', ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length == 0)
            {
                throw new FormatException("Card JSON must contain a shape.");
            }
            var occupied = new List<TimelineCell>();
            var minX = int.MaxValue;
            var minY = int.MaxValue;

            for (var y = 0; y < rows.Length; y++)
            {
                for (var x = 0; x < rows[y].Length; x++)
                {
                    var value = rows[y][x];
                    if (value != '0' && value != '1')
                    {
                        throw new FormatException("Card shapes may contain only 0 and 1.");
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
                return new[] { new TimelineCell(0, 0) };
            }

            var normalized = new List<TimelineCell>(occupied.Count);
            for (var index = 0; index < occupied.Count; index++)
            {
                normalized.Add(new TimelineCell(occupied[index].X - minX, occupied[index].Y - minY));
            }

            return normalized;
        }

        [Serializable]
        private sealed class CardJsonDto
        {
            public string name = null;
            public string front_image = null;
            public EffectJsonDto[] effects = null;
            public string[] effect_range = null;
            public string shape = null;
            public string id = null;
        }

        [Serializable]
        private sealed class EffectJsonDto
        {
            public string type = null;
            public int value = 0;
        }
    }
}
