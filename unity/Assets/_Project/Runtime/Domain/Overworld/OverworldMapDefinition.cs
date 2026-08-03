using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TimeKey.Domain.OverworldMovement;

namespace TimeKey.Domain.Overworld
{
    public enum OverworldRoomType
    {
        Entry,
        Battle,
        Elite,
        Event,
        Shop,
        Boss
    }

    public sealed class OverworldMapNode
    {
        public OverworldMapNode(
            MapNodeId id,
            int chapter,
            int layer,
            int slot,
            OverworldRoomType roomType)
        {
            if (string.IsNullOrEmpty(id.Value))
            {
                throw new ArgumentException("A node identity is required.", nameof(id));
            }

            if (chapter < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(chapter));
            }

            if (layer < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(layer));
            }

            if (slot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            Id = id;
            Chapter = chapter;
            Layer = layer;
            Slot = slot;
            RoomType = roomType;
        }

        public MapNodeId Id { get; }
        public int Chapter { get; }
        public int Layer { get; }
        public int Slot { get; }
        public OverworldRoomType RoomType { get; }
    }

    public readonly struct OverworldMapEdge : IEquatable<OverworldMapEdge>
    {
        public OverworldMapEdge(MapNodeId from, MapNodeId to)
        {
            From = from;
            To = to;
        }

        public MapNodeId From { get; }
        public MapNodeId To { get; }

        public bool Equals(OverworldMapEdge other)
        {
            return From == other.From && To == other.To;
        }

        public override bool Equals(object obj)
        {
            return obj is OverworldMapEdge && Equals((OverworldMapEdge)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (From.GetHashCode() * 397) ^ To.GetHashCode();
            }
        }
    }

    public sealed class OverworldMapDefinition
    {
        private readonly Dictionary<MapNodeId, OverworldMapNode> _nodesById;
        private readonly Dictionary<MapNodeId, IReadOnlyList<MapNodeId>> _outgoing;

        public OverworldMapDefinition(
            int seed,
            int chapter,
            IEnumerable<OverworldMapNode> nodes,
            IEnumerable<OverworldMapEdge> edges)
        {
            if (chapter < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(chapter));
            }

            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (edges == null)
            {
                throw new ArgumentNullException(nameof(edges));
            }

            Seed = seed;
            Chapter = chapter;
            _nodesById = new Dictionary<MapNodeId, OverworldMapNode>();
            var nodeCopy = new List<OverworldMapNode>();
            foreach (var node in nodes)
            {
                if (node == null || node.Chapter != chapter || _nodesById.ContainsKey(node.Id))
                {
                    throw new ArgumentException("Nodes must be non-null, unique, and belong to the map chapter.", nameof(nodes));
                }

                _nodesById.Add(node.Id, node);
                nodeCopy.Add(node);
            }

            if (nodeCopy.Count < 2)
            {
                throw new ArgumentException("A map requires an entry and a boss.", nameof(nodes));
            }

            var edgeCopy = new List<OverworldMapEdge>();
            var uniqueEdges = new HashSet<OverworldMapEdge>();
            var mutableOutgoing = new Dictionary<MapNodeId, List<MapNodeId>>();
            var incomingCounts = new Dictionary<MapNodeId, int>();
            foreach (var node in nodeCopy)
            {
                mutableOutgoing.Add(node.Id, new List<MapNodeId>());
                incomingCounts.Add(node.Id, 0);
            }

            foreach (var edge in edges)
            {
                if (!_nodesById.ContainsKey(edge.From) || !_nodesById.ContainsKey(edge.To))
                {
                    throw new ArgumentException("Every edge endpoint must exist.", nameof(edges));
                }

                if (edge.From == edge.To || !uniqueEdges.Add(edge))
                {
                    throw new ArgumentException("Self-loops and duplicate edges are not allowed.", nameof(edges));
                }

                var from = _nodesById[edge.From];
                var to = _nodesById[edge.To];
                if (to.Layer != from.Layer + 1)
                {
                    throw new ArgumentException("Edges may only connect consecutive layers.", nameof(edges));
                }

                edgeCopy.Add(edge);
                mutableOutgoing[edge.From].Add(edge.To);
                incomingCounts[edge.To]++;
            }

            OverworldMapNode entry = null;
            OverworldMapNode boss = null;
            foreach (var node in nodeCopy)
            {
                if (node.RoomType == OverworldRoomType.Entry)
                {
                    if (entry != null)
                    {
                        throw new ArgumentException("A map must contain exactly one entry.", nameof(nodes));
                    }

                    entry = node;
                }
                else if (node.RoomType == OverworldRoomType.Boss)
                {
                    if (boss != null)
                    {
                        throw new ArgumentException("A map must contain exactly one boss.", nameof(nodes));
                    }

                    boss = node;
                }
            }

            if (entry == null || boss == null || entry.Layer != 0 || boss.Layer <= 0)
            {
                throw new ArgumentException("Entry and boss layers are invalid.", nameof(nodes));
            }

            foreach (var node in nodeCopy)
            {
                if (node.Id != entry.Id && incomingCounts[node.Id] == 0)
                {
                    throw new ArgumentException("Non-entry nodes require a predecessor.", nameof(edges));
                }

                if (node.Id != boss.Id && mutableOutgoing[node.Id].Count == 0)
                {
                    throw new ArgumentException("Non-boss nodes require a successor.", nameof(edges));
                }
            }

            if (!IsReachable(entry.Id, boss.Id, mutableOutgoing))
            {
                throw new ArgumentException("The boss must be reachable from the entry.", nameof(edges));
            }

            EntryNodeId = entry.Id;
            BossNodeId = boss.Id;
            Nodes = new ReadOnlyCollection<OverworldMapNode>(nodeCopy);
            Edges = new ReadOnlyCollection<OverworldMapEdge>(edgeCopy);
            _outgoing = new Dictionary<MapNodeId, IReadOnlyList<MapNodeId>>();
            foreach (var pair in mutableOutgoing)
            {
                _outgoing.Add(
                    pair.Key,
                    new ReadOnlyCollection<MapNodeId>(new List<MapNodeId>(pair.Value)));
            }
        }

        public int Seed { get; }
        public int Chapter { get; }
        public MapNodeId EntryNodeId { get; }
        public MapNodeId BossNodeId { get; }
        public IReadOnlyList<OverworldMapNode> Nodes { get; }
        public IReadOnlyList<OverworldMapEdge> Edges { get; }

        public OverworldMapNode GetNode(MapNodeId nodeId)
        {
            OverworldMapNode node;
            return _nodesById.TryGetValue(nodeId, out node) ? node : null;
        }

        public IReadOnlyList<MapNodeId> GetOutgoing(MapNodeId nodeId)
        {
            IReadOnlyList<MapNodeId> outgoing;
            return _outgoing.TryGetValue(nodeId, out outgoing)
                ? outgoing
                : new ReadOnlyCollection<MapNodeId>(new List<MapNodeId>());
        }

        public bool HasEdge(MapNodeId from, MapNodeId to)
        {
            var outgoing = GetOutgoing(from);
            for (var index = 0; index < outgoing.Count; index++)
            {
                if (outgoing[index] == to)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsReachable(
            MapNodeId start,
            MapNodeId target,
            Dictionary<MapNodeId, List<MapNodeId>> outgoing)
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

                foreach (var next in outgoing[current])
                {
                    if (visited.Add(next))
                    {
                        pending.Enqueue(next);
                    }
                }
            }

            return false;
        }
    }
}
