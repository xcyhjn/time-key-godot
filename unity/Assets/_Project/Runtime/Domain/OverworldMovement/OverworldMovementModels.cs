using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain.OverworldMovement
{
    public enum MapNodeType
    {
        Start,
        Normal,
        Elite,
        Event,
        Boss,
        CharacterChoice,
        Blocked
    }

    public enum MovementPhase
    {
        Idle,
        Moving,
        Arrived,
        RoomPending
    }

    public enum MoveFailureReason
    {
        None,
        InvalidCommand,
        DuplicateCommand,
        StaleRevision,
        SourceMismatch,
        SameNode,
        MissingNode,
        NonAdjacent,
        Blocked,
        Locked,
        Settled,
        Reentrant,
        StaleTicket,
        AlreadyCommitted
    }

    public sealed class OverworldMapNode
    {
        public OverworldMapNode(
            MapNodeId id,
            AxialHexCoord coordinate,
            MapNodeType type,
            bool isBlocked = false,
            bool isLocked = false)
        {
            Id = id;
            Coordinate = coordinate;
            Type = type;
            IsBlocked = isBlocked || type == MapNodeType.Blocked;
            IsLocked = isLocked;
        }

        public MapNodeId Id { get; }

        public AxialHexCoord Coordinate { get; }

        public MapNodeType Type { get; }

        public bool IsBlocked { get; }

        public bool IsLocked { get; }
    }

    public sealed class OverworldMapNodeSnapshot
    {
        internal OverworldMapNodeSnapshot(
            OverworldMapNode node,
            bool isVisited,
            bool isSettled,
            bool isAvailable)
        {
            Id = node.Id;
            Coordinate = node.Coordinate;
            Type = node.Type;
            IsBlocked = node.IsBlocked;
            IsLocked = node.IsLocked;
            IsVisited = isVisited;
            IsSettled = isSettled;
            IsAvailable = isAvailable;
        }

        public MapNodeId Id { get; }
        public AxialHexCoord Coordinate { get; }
        public MapNodeType Type { get; }
        public bool IsBlocked { get; }
        public bool IsLocked { get; }
        public bool IsVisited { get; }
        public bool IsSettled { get; }
        public bool IsAvailable { get; }
    }

    public sealed class MoveCommand
    {
        public MoveCommand(
            string commandId,
            int expectedRevision,
            MapNodeId sourceNodeId,
            MapNodeId targetNodeId)
        {
            if (string.IsNullOrWhiteSpace(commandId))
            {
                throw new ArgumentException("A move command identity is required.", nameof(commandId));
            }

            CommandId = commandId.Trim();
            ExpectedRevision = expectedRevision;
            SourceNodeId = sourceNodeId;
            TargetNodeId = targetNodeId;
        }

        public string CommandId { get; }
        public int ExpectedRevision { get; }
        public MapNodeId SourceNodeId { get; }
        public MapNodeId TargetNodeId { get; }
    }

    public sealed class MoveTicket
    {
        internal MoveTicket(string ticketId, MoveCommand command, int revision)
        {
            TicketId = ticketId;
            Command = command;
            Revision = revision;
        }

        public string TicketId { get; }
        public MoveCommand Command { get; }
        public int Revision { get; }
    }

    public sealed class MoveResult
    {
        private MoveResult(bool succeeded, MoveFailureReason failure, MoveTicket ticket)
        {
            Succeeded = succeeded;
            Failure = failure;
            Ticket = ticket;
        }

        public bool Succeeded { get; }
        public MoveFailureReason Failure { get; }
        public MoveTicket Ticket { get; }

        internal static MoveResult Success(MoveTicket ticket)
        {
            return new MoveResult(true, MoveFailureReason.None, ticket);
        }

        internal static MoveResult FailureResult(MoveFailureReason reason)
        {
            return new MoveResult(false, reason, null);
        }
    }

    public sealed class OverworldMovementSnapshot
    {
        internal OverworldMovementSnapshot(
            int revision,
            MapNodeId currentNodeId,
            MovementPhase phase,
            IReadOnlyList<OverworldMapNodeSnapshot> nodes,
            IReadOnlyCollection<MapNodeId> visitedNodeIds,
            IReadOnlyCollection<MapNodeId> settledNodeIds)
        {
            Revision = revision;
            CurrentNodeId = currentNodeId;
            Phase = phase;
            Nodes = nodes;
            VisitedNodeIds = visitedNodeIds;
            SettledNodeIds = settledNodeIds;
        }

        public int Revision { get; }
        public MapNodeId CurrentNodeId { get; }
        public MovementPhase Phase { get; }
        public IReadOnlyList<OverworldMapNodeSnapshot> Nodes { get; }
        public IReadOnlyCollection<MapNodeId> VisitedNodeIds { get; }
        public IReadOnlyCollection<MapNodeId> SettledNodeIds { get; }
    }
}
