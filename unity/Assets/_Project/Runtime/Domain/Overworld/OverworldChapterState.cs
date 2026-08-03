using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TimeKey.Domain.OverworldMovement;

namespace TimeKey.Domain.Overworld
{
    public enum OverworldResolutionKind
    {
        Victory,
        Defeat,
        Completed,
        Cancelled
    }

    public enum OverworldOperationKind
    {
        EnterRoom,
        ResolveRoom
    }

    public enum OverworldOperationFailure
    {
        None,
        InvalidCommand,
        InvalidSequence,
        SequenceConflict,
        StaleRevision,
        SourceMismatch,
        MissingNode,
        NotAdjacent,
        RoomAlreadySettled,
        RoomInProgress,
        NoActiveRoom,
        ActiveRoomMismatch,
        InvalidResolution,
        ChapterAlreadyAdvanced
    }

    public sealed class EnterOverworldRoomCommand
    {
        public EnterOverworldRoomCommand(
            long sequence,
            int expectedRevision,
            MapNodeId sourceNodeId,
            MapNodeId targetNodeId)
        {
            Sequence = sequence;
            ExpectedRevision = expectedRevision;
            SourceNodeId = sourceNodeId;
            TargetNodeId = targetNodeId;
        }

        public long Sequence { get; }
        public int ExpectedRevision { get; }
        public MapNodeId SourceNodeId { get; }
        public MapNodeId TargetNodeId { get; }
    }

    public sealed class ResolveOverworldRoomCommand
    {
        public ResolveOverworldRoomCommand(
            long sequence,
            int expectedRevision,
            MapNodeId nodeId,
            OverworldResolutionKind resolution)
        {
            Sequence = sequence;
            ExpectedRevision = expectedRevision;
            NodeId = nodeId;
            Resolution = resolution;
        }

        public long Sequence { get; }
        public int ExpectedRevision { get; }
        public MapNodeId NodeId { get; }
        public OverworldResolutionKind Resolution { get; }
    }

    public sealed class OverworldOperationResult
    {
        internal OverworldOperationResult(
            long sequence,
            OverworldOperationKind kind,
            OverworldOperationFailure failure,
            int beforeRevision,
            int afterRevision,
            MapNodeId nodeId,
            bool chapterAdvanced)
        {
            Sequence = sequence;
            Kind = kind;
            Failure = failure;
            BeforeRevision = beforeRevision;
            AfterRevision = afterRevision;
            NodeId = nodeId;
            ChapterAdvanced = chapterAdvanced;
        }

        public bool Succeeded => Failure == OverworldOperationFailure.None;
        public long Sequence { get; }
        public OverworldOperationKind Kind { get; }
        public OverworldOperationFailure Failure { get; }
        public int BeforeRevision { get; }
        public int AfterRevision { get; }
        public MapNodeId NodeId { get; }
        public bool ChapterAdvanced { get; }
    }

    public sealed class OverworldNodeStateSnapshot
    {
        internal OverworldNodeStateSnapshot(
            OverworldMapNode node,
            bool visited,
            bool settled,
            bool available,
            bool active)
        {
            Id = node.Id;
            Chapter = node.Chapter;
            Layer = node.Layer;
            Slot = node.Slot;
            RoomType = node.RoomType;
            IsVisited = visited;
            IsSettled = settled;
            IsAvailable = available;
            IsActive = active;
        }

        public MapNodeId Id { get; }
        public int Chapter { get; }
        public int Layer { get; }
        public int Slot { get; }
        public OverworldRoomType RoomType { get; }
        public bool IsVisited { get; }
        public bool IsSettled { get; }
        public bool IsAvailable { get; }
        public bool IsActive { get; }
    }

    public sealed class OverworldChapterSnapshot
    {
        internal OverworldChapterSnapshot(
            int revision,
            MapNodeId currentNodeId,
            bool hasActiveRoom,
            MapNodeId activeRoomNodeId,
            bool chapterCompleted,
            int chapterAdvanceCount,
            IReadOnlyList<OverworldNodeStateSnapshot> nodes,
            IReadOnlyCollection<MapNodeId> visited,
            IReadOnlyCollection<MapNodeId> settled)
        {
            Revision = revision;
            CurrentNodeId = currentNodeId;
            HasActiveRoom = hasActiveRoom;
            ActiveRoomNodeId = activeRoomNodeId;
            ChapterCompleted = chapterCompleted;
            ChapterAdvanceCount = chapterAdvanceCount;
            Nodes = nodes;
            VisitedNodeIds = visited;
            SettledNodeIds = settled;
        }

        public int Revision { get; }
        public MapNodeId CurrentNodeId { get; }
        public bool HasActiveRoom { get; }
        public MapNodeId ActiveRoomNodeId { get; }
        public bool ChapterCompleted { get; }
        public int ChapterAdvanceCount { get; }
        public IReadOnlyList<OverworldNodeStateSnapshot> Nodes { get; }
        public IReadOnlyCollection<MapNodeId> VisitedNodeIds { get; }
        public IReadOnlyCollection<MapNodeId> SettledNodeIds { get; }
    }

    public sealed class OverworldChapterState
    {
        private readonly OverworldMapDefinition _map;
        private readonly HashSet<MapNodeId> _visited = new HashSet<MapNodeId>();
        private readonly HashSet<MapNodeId> _settled = new HashSet<MapNodeId>();
        private readonly Dictionary<long, JournalEntry> _journal =
            new Dictionary<long, JournalEntry>();
        private bool _hasActiveRoom;
        private MapNodeId _activeRoomNodeId;

        public OverworldChapterState(OverworldMapDefinition map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            CurrentNodeId = map.EntryNodeId;
            _visited.Add(CurrentNodeId);
            _settled.Add(CurrentNodeId);
        }

        public int Revision { get; private set; }
        public MapNodeId CurrentNodeId { get; private set; }
        public bool ChapterCompleted { get; private set; }
        public int ChapterAdvanceCount { get; private set; }

        public OverworldOperationResult EnterRoom(EnterOverworldRoomCommand command)
        {
            if (command == null)
            {
                return Failed(0, OverworldOperationKind.EnterRoom, OverworldOperationFailure.InvalidCommand);
            }

            JournalEntry existing;
            if (_journal.TryGetValue(command.Sequence, out existing))
            {
                return existing.Matches(command)
                    ? existing.Result
                    : Failed(command.Sequence, OverworldOperationKind.EnterRoom, OverworldOperationFailure.SequenceConflict);
            }

            OverworldOperationFailure failure;
            if (command.Sequence <= 0)
            {
                failure = OverworldOperationFailure.InvalidSequence;
            }
            else if (command.ExpectedRevision != Revision)
            {
                failure = OverworldOperationFailure.StaleRevision;
            }
            else if (command.SourceNodeId != CurrentNodeId)
            {
                failure = OverworldOperationFailure.SourceMismatch;
            }
            else if (_hasActiveRoom)
            {
                failure = OverworldOperationFailure.RoomInProgress;
            }
            else if (_map.GetNode(command.TargetNodeId) == null)
            {
                failure = OverworldOperationFailure.MissingNode;
            }
            else if (_settled.Contains(command.TargetNodeId))
            {
                failure = OverworldOperationFailure.RoomAlreadySettled;
            }
            else if (!_map.HasEdge(command.SourceNodeId, command.TargetNodeId))
            {
                failure = OverworldOperationFailure.NotAdjacent;
            }
            else
            {
                failure = OverworldOperationFailure.None;
            }

            var before = Revision;
            if (failure == OverworldOperationFailure.None)
            {
                CurrentNodeId = command.TargetNodeId;
                _visited.Add(CurrentNodeId);
                _activeRoomNodeId = CurrentNodeId;
                _hasActiveRoom = true;
                Revision++;
            }

            var result = new OverworldOperationResult(
                command.Sequence,
                OverworldOperationKind.EnterRoom,
                failure,
                before,
                Revision,
                command.TargetNodeId,
                chapterAdvanced: false);
            _journal.Add(command.Sequence, JournalEntry.ForEnter(command, result));
            return result;
        }

        public OverworldOperationResult ResolveRoom(ResolveOverworldRoomCommand command)
        {
            if (command == null)
            {
                return Failed(0, OverworldOperationKind.ResolveRoom, OverworldOperationFailure.InvalidCommand);
            }

            JournalEntry existing;
            if (_journal.TryGetValue(command.Sequence, out existing))
            {
                return existing.Matches(command)
                    ? existing.Result
                    : Failed(command.Sequence, OverworldOperationKind.ResolveRoom, OverworldOperationFailure.SequenceConflict);
            }

            var node = _map.GetNode(command.NodeId);
            OverworldOperationFailure failure;
            if (command.Sequence <= 0)
            {
                failure = OverworldOperationFailure.InvalidSequence;
            }
            else if (command.ExpectedRevision != Revision)
            {
                failure = OverworldOperationFailure.StaleRevision;
            }
            else if (!_hasActiveRoom)
            {
                failure = OverworldOperationFailure.NoActiveRoom;
            }
            else if (command.NodeId != _activeRoomNodeId)
            {
                failure = OverworldOperationFailure.ActiveRoomMismatch;
            }
            else if (!IsResolutionValid(node.RoomType, command.Resolution))
            {
                failure = OverworldOperationFailure.InvalidResolution;
            }
            else if (node.RoomType == OverworldRoomType.Boss &&
                     command.Resolution == OverworldResolutionKind.Victory &&
                     ChapterCompleted)
            {
                failure = OverworldOperationFailure.ChapterAlreadyAdvanced;
            }
            else
            {
                failure = OverworldOperationFailure.None;
            }

            var before = Revision;
            var chapterAdvanced = false;
            if (failure == OverworldOperationFailure.None)
            {
                if (IsSuccessfulResolution(command.Resolution))
                {
                    _settled.Add(command.NodeId);
                    if (node.RoomType == OverworldRoomType.Boss)
                    {
                        ChapterCompleted = true;
                        ChapterAdvanceCount++;
                        chapterAdvanced = true;
                    }
                }

                _hasActiveRoom = false;
                _activeRoomNodeId = default(MapNodeId);
                Revision++;
            }

            var result = new OverworldOperationResult(
                command.Sequence,
                OverworldOperationKind.ResolveRoom,
                failure,
                before,
                Revision,
                command.NodeId,
                chapterAdvanced);
            _journal.Add(command.Sequence, JournalEntry.ForResolve(command, result));
            return result;
        }

        public OverworldChapterSnapshot Snapshot()
        {
            var nodes = new List<OverworldNodeStateSnapshot>(_map.Nodes.Count);
            var available = new HashSet<MapNodeId>();
            if (!_hasActiveRoom && _settled.Contains(CurrentNodeId) && !ChapterCompleted)
            {
                foreach (var nodeId in _map.GetOutgoing(CurrentNodeId))
                {
                    if (!_settled.Contains(nodeId))
                    {
                        available.Add(nodeId);
                    }
                }
            }

            foreach (var node in _map.Nodes)
            {
                nodes.Add(new OverworldNodeStateSnapshot(
                    node,
                    _visited.Contains(node.Id),
                    _settled.Contains(node.Id),
                    available.Contains(node.Id),
                    _hasActiveRoom && _activeRoomNodeId == node.Id));
            }

            return new OverworldChapterSnapshot(
                Revision,
                CurrentNodeId,
                _hasActiveRoom,
                _activeRoomNodeId,
                ChapterCompleted,
                ChapterAdvanceCount,
                new ReadOnlyCollection<OverworldNodeStateSnapshot>(nodes),
                new ReadOnlyCollection<MapNodeId>(new List<MapNodeId>(_visited)),
                new ReadOnlyCollection<MapNodeId>(new List<MapNodeId>(_settled)));
        }

        private OverworldOperationResult Failed(
            long sequence,
            OverworldOperationKind kind,
            OverworldOperationFailure failure)
        {
            return new OverworldOperationResult(
                sequence,
                kind,
                failure,
                Revision,
                Revision,
                default(MapNodeId),
                chapterAdvanced: false);
        }

        private static bool IsResolutionValid(
            OverworldRoomType roomType,
            OverworldResolutionKind resolution)
        {
            if (roomType == OverworldRoomType.Battle ||
                roomType == OverworldRoomType.Elite ||
                roomType == OverworldRoomType.Boss)
            {
                return resolution == OverworldResolutionKind.Victory ||
                       resolution == OverworldResolutionKind.Defeat;
            }

            return (roomType == OverworldRoomType.Event || roomType == OverworldRoomType.Shop) &&
                   (resolution == OverworldResolutionKind.Completed ||
                    resolution == OverworldResolutionKind.Cancelled);
        }

        private static bool IsSuccessfulResolution(OverworldResolutionKind resolution)
        {
            return resolution == OverworldResolutionKind.Victory ||
                   resolution == OverworldResolutionKind.Completed;
        }

        private sealed class JournalEntry
        {
            private JournalEntry(
                OverworldOperationKind kind,
                int expectedRevision,
                MapNodeId sourceNodeId,
                MapNodeId nodeId,
                OverworldResolutionKind resolution,
                OverworldOperationResult result)
            {
                Kind = kind;
                ExpectedRevision = expectedRevision;
                SourceNodeId = sourceNodeId;
                NodeId = nodeId;
                Resolution = resolution;
                Result = result;
            }

            public OverworldOperationKind Kind { get; }
            public int ExpectedRevision { get; }
            public MapNodeId SourceNodeId { get; }
            public MapNodeId NodeId { get; }
            public OverworldResolutionKind Resolution { get; }
            public OverworldOperationResult Result { get; }

            public bool Matches(EnterOverworldRoomCommand command)
            {
                return Kind == OverworldOperationKind.EnterRoom &&
                       ExpectedRevision == command.ExpectedRevision &&
                       SourceNodeId == command.SourceNodeId &&
                       NodeId == command.TargetNodeId;
            }

            public bool Matches(ResolveOverworldRoomCommand command)
            {
                return Kind == OverworldOperationKind.ResolveRoom &&
                       ExpectedRevision == command.ExpectedRevision &&
                       NodeId == command.NodeId &&
                       Resolution == command.Resolution;
            }

            public static JournalEntry ForEnter(
                EnterOverworldRoomCommand command,
                OverworldOperationResult result)
            {
                return new JournalEntry(
                    OverworldOperationKind.EnterRoom,
                    command.ExpectedRevision,
                    command.SourceNodeId,
                    command.TargetNodeId,
                    default(OverworldResolutionKind),
                    result);
            }

            public static JournalEntry ForResolve(
                ResolveOverworldRoomCommand command,
                OverworldOperationResult result)
            {
                return new JournalEntry(
                    OverworldOperationKind.ResolveRoom,
                    command.ExpectedRevision,
                    default(MapNodeId),
                    command.NodeId,
                    command.Resolution,
                    result);
            }
        }
    }
}
