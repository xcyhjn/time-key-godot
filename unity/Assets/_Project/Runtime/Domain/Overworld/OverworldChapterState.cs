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

    public sealed class OverworldOperationJournalSnapshot
    {
        public OverworldOperationJournalSnapshot(
            long sequence,
            OverworldOperationKind kind,
            int expectedRevision,
            MapNodeId sourceNodeId,
            MapNodeId nodeId,
            OverworldResolutionKind resolution,
            OverworldOperationFailure failure,
            int beforeRevision,
            int afterRevision,
            bool chapterAdvanced)
        {
            if (sequence <= 0 || expectedRevision < 0 || beforeRevision < 0 ||
                afterRevision < beforeRevision)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }

            Sequence = sequence;
            Kind = kind;
            ExpectedRevision = expectedRevision;
            SourceNodeId = sourceNodeId;
            NodeId = nodeId;
            Resolution = resolution;
            Failure = failure;
            BeforeRevision = beforeRevision;
            AfterRevision = afterRevision;
            ChapterAdvanced = chapterAdvanced;
        }

        public long Sequence { get; }
        public OverworldOperationKind Kind { get; }
        public int ExpectedRevision { get; }
        public MapNodeId SourceNodeId { get; }
        public MapNodeId NodeId { get; }
        public OverworldResolutionKind Resolution { get; }
        public OverworldOperationFailure Failure { get; }
        public int BeforeRevision { get; }
        public int AfterRevision { get; }
        public bool ChapterAdvanced { get; }
    }

    public sealed class OverworldChapterRestoreState
    {
        private readonly ReadOnlyCollection<MapNodeId> _visitedNodeIds;
        private readonly ReadOnlyCollection<MapNodeId> _settledNodeIds;
        private readonly ReadOnlyCollection<OverworldOperationJournalSnapshot> _journal;

        public OverworldChapterRestoreState(
            int revision,
            MapNodeId currentNodeId,
            bool hasActiveRoom,
            MapNodeId activeRoomNodeId,
            bool chapterCompleted,
            int chapterAdvanceCount,
            IReadOnlyCollection<MapNodeId> visitedNodeIds,
            IReadOnlyCollection<MapNodeId> settledNodeIds,
            IReadOnlyList<OverworldOperationJournalSnapshot> journal)
        {
            if (revision < 0 || chapterAdvanceCount < 0 || chapterAdvanceCount > 1 ||
                chapterCompleted != (chapterAdvanceCount == 1))
            {
                throw new ArgumentOutOfRangeException(nameof(revision));
            }

            Revision = revision;
            CurrentNodeId = currentNodeId;
            HasActiveRoom = hasActiveRoom;
            ActiveRoomNodeId = activeRoomNodeId;
            ChapterCompleted = chapterCompleted;
            ChapterAdvanceCount = chapterAdvanceCount;
            _visitedNodeIds = Copy(visitedNodeIds, nameof(visitedNodeIds));
            _settledNodeIds = Copy(settledNodeIds, nameof(settledNodeIds));
            _journal = new ReadOnlyCollection<OverworldOperationJournalSnapshot>(
                new List<OverworldOperationJournalSnapshot>(
                    journal ?? throw new ArgumentNullException(nameof(journal))));
        }

        public int Revision { get; }
        public MapNodeId CurrentNodeId { get; }
        public bool HasActiveRoom { get; }
        public MapNodeId ActiveRoomNodeId { get; }
        public bool ChapterCompleted { get; }
        public int ChapterAdvanceCount { get; }
        public IReadOnlyCollection<MapNodeId> VisitedNodeIds => _visitedNodeIds;
        public IReadOnlyCollection<MapNodeId> SettledNodeIds => _settledNodeIds;
        public IReadOnlyList<OverworldOperationJournalSnapshot> Journal => _journal;

        private static ReadOnlyCollection<MapNodeId> Copy(
            IReadOnlyCollection<MapNodeId> source,
            string parameterName)
        {
            return new ReadOnlyCollection<MapNodeId>(
                new List<MapNodeId>(
                    source ?? throw new ArgumentNullException(parameterName)));
        }
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

        private OverworldChapterState(
            OverworldMapDefinition map,
            OverworldChapterRestoreState restore)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            ValidateRestore(map, restore);
            Revision = restore.Revision;
            CurrentNodeId = restore.CurrentNodeId;
            _hasActiveRoom = restore.HasActiveRoom;
            _activeRoomNodeId = restore.ActiveRoomNodeId;
            ChapterCompleted = restore.ChapterCompleted;
            ChapterAdvanceCount = restore.ChapterAdvanceCount;
            foreach (var nodeId in restore.VisitedNodeIds)
            {
                _visited.Add(nodeId);
            }

            foreach (var nodeId in restore.SettledNodeIds)
            {
                _settled.Add(nodeId);
            }

            foreach (var entry in restore.Journal)
            {
                _journal.Add(entry.Sequence, JournalEntry.Restore(entry));
            }
        }

        public int Revision { get; private set; }
        public MapNodeId CurrentNodeId { get; private set; }
        public bool ChapterCompleted { get; private set; }
        public int ChapterAdvanceCount { get; private set; }

        public static OverworldChapterState Restore(
            OverworldMapDefinition map,
            OverworldChapterRestoreState restore)
        {
            return new OverworldChapterState(map, restore);
        }

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

        public OverworldChapterRestoreState ExportState()
        {
            var journal = new List<OverworldOperationJournalSnapshot>(_journal.Count);
            var sequences = new List<long>(_journal.Keys);
            sequences.Sort();
            foreach (var sequence in sequences)
            {
                journal.Add(_journal[sequence].Snapshot(sequence));
            }

            return new OverworldChapterRestoreState(
                Revision,
                CurrentNodeId,
                _hasActiveRoom,
                _activeRoomNodeId,
                ChapterCompleted,
                ChapterAdvanceCount,
                new ReadOnlyCollection<MapNodeId>(new List<MapNodeId>(_visited)),
                new ReadOnlyCollection<MapNodeId>(new List<MapNodeId>(_settled)),
                new ReadOnlyCollection<OverworldOperationJournalSnapshot>(journal));
        }

        private static void ValidateRestore(
            OverworldMapDefinition map,
            OverworldChapterRestoreState restore)
        {
            if (restore == null)
            {
                throw new ArgumentNullException(nameof(restore));
            }

            var visited = new HashSet<MapNodeId>();
            foreach (var nodeId in restore.VisitedNodeIds)
            {
                if (map.GetNode(nodeId) == null || !visited.Add(nodeId))
                {
                    throw new ArgumentException("Restore contains an invalid visited node.", nameof(restore));
                }
            }

            var settled = new HashSet<MapNodeId>();
            foreach (var nodeId in restore.SettledNodeIds)
            {
                if (!visited.Contains(nodeId) || !settled.Add(nodeId))
                {
                    throw new ArgumentException("Restore contains an invalid settled node.", nameof(restore));
                }
            }

            if (map.GetNode(restore.CurrentNodeId) == null ||
                !visited.Contains(restore.CurrentNodeId) ||
                !settled.Contains(map.EntryNodeId))
            {
                throw new ArgumentException("Restore current/entry state is invalid.", nameof(restore));
            }

            if (restore.HasActiveRoom &&
                (restore.ActiveRoomNodeId != restore.CurrentNodeId ||
                 settled.Contains(restore.ActiveRoomNodeId)))
            {
                throw new ArgumentException("Restore active room state is invalid.", nameof(restore));
            }

            var sequences = new HashSet<long>();
            foreach (var entry in restore.Journal)
            {
                if (entry == null || !sequences.Add(entry.Sequence) ||
                    entry.AfterRevision > restore.Revision ||
                    (entry.Failure == OverworldOperationFailure.None &&
                     map.GetNode(entry.NodeId) == null) ||
                    (entry.Failure == OverworldOperationFailure.None &&
                     entry.Kind == OverworldOperationKind.EnterRoom &&
                     map.GetNode(entry.SourceNodeId) == null))
                {
                    throw new ArgumentException("Restore operation journal is invalid.", nameof(restore));
                }
            }
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

            public OverworldOperationJournalSnapshot Snapshot(long sequence)
            {
                return new OverworldOperationJournalSnapshot(
                    sequence,
                    Kind,
                    ExpectedRevision,
                    SourceNodeId,
                    NodeId,
                    Resolution,
                    Result.Failure,
                    Result.BeforeRevision,
                    Result.AfterRevision,
                    Result.ChapterAdvanced);
            }

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

            public static JournalEntry Restore(OverworldOperationJournalSnapshot snapshot)
            {
                return new JournalEntry(
                    snapshot.Kind,
                    snapshot.ExpectedRevision,
                    snapshot.SourceNodeId,
                    snapshot.NodeId,
                    snapshot.Resolution,
                    new OverworldOperationResult(
                        snapshot.Sequence,
                        snapshot.Kind,
                        snapshot.Failure,
                        snapshot.BeforeRevision,
                        snapshot.AfterRevision,
                        snapshot.NodeId,
                        snapshot.ChapterAdvanced));
            }
        }
    }
}
