using System;
using System.Collections.Generic;
using TimeKey.Domain.OverworldMovement;

namespace TimeKey.Domain.Overworld
{
    public sealed class OverworldMapGenerationConfig
    {
        public OverworldMapGenerationConfig(
            int chapter,
            int intermediateLayerCount = 4,
            int minimumNodesPerLayer = 2,
            int maximumNodesPerLayer = 3)
        {
            if (chapter < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(chapter));
            }

            if (intermediateLayerCount < 1 || intermediateLayerCount > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(intermediateLayerCount));
            }

            if (minimumNodesPerLayer < 1 || maximumNodesPerLayer < minimumNodesPerLayer ||
                maximumNodesPerLayer > 16)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumNodesPerLayer));
            }

            Chapter = chapter;
            IntermediateLayerCount = intermediateLayerCount;
            MinimumNodesPerLayer = minimumNodesPerLayer;
            MaximumNodesPerLayer = maximumNodesPerLayer;
        }

        public int Chapter { get; }
        public int IntermediateLayerCount { get; }
        public int MinimumNodesPerLayer { get; }
        public int MaximumNodesPerLayer { get; }
    }

    public sealed class DeterministicOverworldMapGenerator
    {
        public OverworldMapDefinition Generate(int seed, OverworldMapGenerationConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var random = new StableRandom(seed);
            var nodes = new List<OverworldMapNode>();
            var layers = new List<List<OverworldMapNode>>();
            layers.Add(CreateLayer(config.Chapter, 0, 1, OverworldRoomType.Entry, random, nodes));

            for (var layer = 1; layer <= config.IntermediateLayerCount; layer++)
            {
                var width = random.NextInclusive(
                    config.MinimumNodesPerLayer,
                    config.MaximumNodesPerLayer);
                layers.Add(CreateLayer(config.Chapter, layer, width, null, random, nodes));
            }

            layers.Add(CreateLayer(
                config.Chapter,
                config.IntermediateLayerCount + 1,
                1,
                OverworldRoomType.Boss,
                random,
                nodes));

            var edges = new List<OverworldMapEdge>();
            var unique = new HashSet<OverworldMapEdge>();
            for (var layer = 0; layer < layers.Count - 1; layer++)
            {
                ConnectAdjacentLayers(layers[layer], layers[layer + 1], random, edges, unique);
            }

            return new OverworldMapDefinition(seed, config.Chapter, nodes, edges);
        }

        private static List<OverworldMapNode> CreateLayer(
            int chapter,
            int layer,
            int width,
            OverworldRoomType? forcedType,
            StableRandom random,
            List<OverworldMapNode> allNodes)
        {
            var result = new List<OverworldMapNode>(width);
            for (var slot = 0; slot < width; slot++)
            {
                var type = forcedType ?? NextRoomType(random);
                var node = new OverworldMapNode(
                    new MapNodeId(
                        "chapter-" + chapter.ToString("D2") +
                        "-layer-" + layer.ToString("D2") +
                        "-node-" + slot.ToString("D2")),
                    chapter,
                    layer,
                    slot,
                    type);
                result.Add(node);
                allNodes.Add(node);
            }

            return result;
        }

        private static OverworldRoomType NextRoomType(StableRandom random)
        {
            var roll = random.Next(100);
            if (roll < 50)
            {
                return OverworldRoomType.Battle;
            }

            if (roll < 65)
            {
                return OverworldRoomType.Elite;
            }

            if (roll < 85)
            {
                return OverworldRoomType.Event;
            }

            return OverworldRoomType.Shop;
        }

        private static void ConnectAdjacentLayers(
            List<OverworldMapNode> from,
            List<OverworldMapNode> to,
            StableRandom random,
            List<OverworldMapEdge> edges,
            HashSet<OverworldMapEdge> unique)
        {
            for (var index = 0; index < from.Count; index++)
            {
                AddEdge(from[index], to[index % to.Count], edges, unique);
            }

            for (var index = 0; index < to.Count; index++)
            {
                AddEdge(from[index % from.Count], to[index], edges, unique);
            }

            for (var fromIndex = 0; fromIndex < from.Count; fromIndex++)
            {
                for (var toIndex = 0; toIndex < to.Count; toIndex++)
                {
                    if (random.Next(100) < 30)
                    {
                        AddEdge(from[fromIndex], to[toIndex], edges, unique);
                    }
                }
            }
        }

        private static void AddEdge(
            OverworldMapNode from,
            OverworldMapNode to,
            List<OverworldMapEdge> edges,
            HashSet<OverworldMapEdge> unique)
        {
            var edge = new OverworldMapEdge(from.Id, to.Id);
            if (unique.Add(edge))
            {
                edges.Add(edge);
            }
        }

        private sealed class StableRandom
        {
            private uint _state;

            public StableRandom(int seed)
            {
                _state = unchecked((uint)seed) ^ 0x9E3779B9u;
                if (_state == 0)
                {
                    _state = 0x6D2B79F5u;
                }
            }

            public int Next(int exclusiveMaximum)
            {
                _state ^= _state << 13;
                _state ^= _state >> 17;
                _state ^= _state << 5;
                return (int)(_state % (uint)exclusiveMaximum);
            }

            public int NextInclusive(int minimum, int maximum)
            {
                return minimum + Next(maximum - minimum + 1);
            }
        }
    }
}
