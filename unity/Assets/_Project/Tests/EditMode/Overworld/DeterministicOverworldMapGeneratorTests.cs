using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TimeKey.Domain.Overworld;
using TimeKey.Domain.OverworldMovement;
using ChapterMapNode = TimeKey.Domain.Overworld.OverworldMapNode;

namespace TimeKey.Tests.EditMode.Overworld
{
    public sealed class DeterministicOverworldMapGeneratorTests
    {
        [TestCase(0)]
        [TestCase(17)]
        [TestCase(-17)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void Generate_SameSeedAndConfigProduceIdenticalMap(int seed)
        {
            var generator = new DeterministicOverworldMapGenerator();
            var config = new OverworldMapGenerationConfig(chapter: 2);

            var first = generator.Generate(seed, config);
            var second = generator.Generate(seed, config);

            Assert.That(Fingerprint(first), Is.EqualTo(Fingerprint(second)));
            Assert.That(first.Seed, Is.EqualTo(seed));
            Assert.That(first.Chapter, Is.EqualTo(2));
        }

        [Test]
        public void Generate_DifferentSeedsProduceObservableDifference()
        {
            var generator = new DeterministicOverworldMapGenerator();
            var config = new OverworldMapGenerationConfig(chapter: 1);

            var first = generator.Generate(101, config);
            var second = generator.Generate(202, config);

            Assert.That(Fingerprint(first), Is.Not.EqualTo(Fingerprint(second)));
        }

        [Test]
        public void Generate_ProducesValidLayeredGraphFromEntryToBoss()
        {
            var map = new DeterministicOverworldMapGenerator().Generate(
                seed: 8421,
                new OverworldMapGenerationConfig(
                    chapter: 3,
                    intermediateLayerCount: 6,
                    minimumNodesPerLayer: 2,
                    maximumNodesPerLayer: 4));
            var nodeIds = new HashSet<MapNodeId>();
            var edges = new HashSet<OverworldMapEdge>();
            var incoming = new Dictionary<MapNodeId, int>();
            foreach (var node in map.Nodes)
            {
                Assert.That(nodeIds.Add(node.Id), Is.True, "Duplicate node " + node.Id);
                incoming.Add(node.Id, 0);
            }

            foreach (var edge in map.Edges)
            {
                Assert.That(edge.From, Is.Not.EqualTo(edge.To));
                Assert.That(edges.Add(edge), Is.True, "Duplicate edge");
                Assert.That(map.GetNode(edge.To).Layer, Is.EqualTo(map.GetNode(edge.From).Layer + 1));
                incoming[edge.To]++;
            }

            foreach (var node in map.Nodes)
            {
                if (node.Id != map.EntryNodeId)
                {
                    Assert.That(incoming[node.Id], Is.GreaterThan(0), "Isolated predecessor " + node.Id);
                }

                if (node.Id != map.BossNodeId)
                {
                    Assert.That(map.GetOutgoing(node.Id).Count, Is.GreaterThan(0), "Isolated successor " + node.Id);
                }
            }

            Assert.That(IsReachable(map, map.EntryNodeId, map.BossNodeId), Is.True);
            Assert.That(map.GetNode(map.EntryNodeId).RoomType, Is.EqualTo(OverworldRoomType.Entry));
            Assert.That(map.GetNode(map.BossNodeId).RoomType, Is.EqualTo(OverworldRoomType.Boss));
        }

        [Test]
        public void Definition_RejectsDuplicateAndIllegalEdges()
        {
            var entry = Node("entry", 0, OverworldRoomType.Entry);
            var boss = Node("boss", 1, OverworldRoomType.Boss);
            var edge = new OverworldMapEdge(entry.Id, boss.Id);

            Assert.That(
                () => new OverworldMapDefinition(1, 1, new[] { entry, boss }, new[] { edge, edge }),
                Throws.ArgumentException);
            Assert.That(
                () => new OverworldMapDefinition(
                    1,
                    1,
                    new[] { entry, boss },
                    new[] { new OverworldMapEdge(entry.Id, entry.Id), edge }),
                Throws.ArgumentException);
        }

        [Test]
        public void SnapshotCollectionsCannotMutateGeneratedDefinition()
        {
            var map = new DeterministicOverworldMapGenerator().Generate(
                seed: 9,
                new OverworldMapGenerationConfig(chapter: 1));

            Assert.That(map.Nodes, Is.Not.InstanceOf<List<ChapterMapNode>>());
            Assert.That(map.Edges, Is.Not.InstanceOf<List<OverworldMapEdge>>());
            Assert.That(map.GetOutgoing(map.EntryNodeId), Is.Not.InstanceOf<List<MapNodeId>>());
        }

        [Test]
        public void Config_RejectsUnsafeBounds()
        {
            Assert.That(() => new OverworldMapGenerationConfig(0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new OverworldMapGenerationConfig(1, intermediateLayerCount: 33),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new OverworldMapGenerationConfig(1, minimumNodesPerLayer: 4, maximumNodesPerLayer: 3),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private static string Fingerprint(OverworldMapDefinition map)
        {
            var builder = new StringBuilder();
            foreach (var node in map.Nodes)
            {
                builder.Append(node.Id).Append(':').Append((int)node.RoomType).Append('|');
            }

            foreach (var edge in map.Edges)
            {
                builder.Append(edge.From).Append('>').Append(edge.To).Append('|');
            }

            return builder.ToString();
        }

        private static bool IsReachable(
            OverworldMapDefinition map,
            MapNodeId start,
            MapNodeId target)
        {
            var pending = new Queue<MapNodeId>();
            var visited = new HashSet<MapNodeId>();
            pending.Enqueue(start);
            visited.Add(start);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                if (current == target)
                {
                    return true;
                }

                foreach (var next in map.GetOutgoing(current))
                {
                    if (visited.Add(next))
                    {
                        pending.Enqueue(next);
                    }
                }
            }

            return false;
        }

        private static ChapterMapNode Node(string id, int layer, OverworldRoomType type)
        {
            return new ChapterMapNode(new MapNodeId(id), 1, layer, 0, type);
        }
    }
}
