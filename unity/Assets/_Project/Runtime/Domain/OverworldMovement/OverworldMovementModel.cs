using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain.OverworldMovement
{
    public sealed class OverworldMovementModel
    {
        private readonly Dictionary<MapNodeId, OverworldMapNode> _nodes =
            new Dictionary<MapNodeId, OverworldMapNode>();
        private readonly HashSet<MapNodeId> _visited = new HashSet<MapNodeId>();
        private readonly HashSet<MapNodeId> _settled = new HashSet<MapNodeId>();
        private readonly Dictionary<string, MoveResult> _commandResults =
            new Dictionary<string, MoveResult>(StringComparer.Ordinal);
        private readonly HashSet<string> _committedTickets =
            new HashSet<string>(StringComparer.Ordinal);
        private MoveTicket _activeTicket;
        private int _ticketSequence;

        public OverworldMovementModel(
            IEnumerable<OverworldMapNode> nodes,
            MapNodeId currentNodeId)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            foreach (var node in nodes)
            {
                if (node == null)
                {
                    throw new ArgumentException("Map nodes cannot be null.", nameof(nodes));
                }

                if (_nodes.ContainsKey(node.Id))
                {
                    throw new ArgumentException("Map node identities must be unique.", nameof(nodes));
                }

                _nodes.Add(node.Id, node);
            }

            if (!_nodes.ContainsKey(currentNodeId))
            {
                throw new ArgumentException("The current node must exist in the map.", nameof(currentNodeId));
            }

            CurrentNodeId = currentNodeId;
            _visited.Add(currentNodeId);
        }

        public int Revision { get; private set; }

        public MapNodeId CurrentNodeId { get; private set; }

        public MovementPhase Phase { get; private set; } = MovementPhase.Idle;

        public MoveResult RequestMove(MoveCommand command)
        {
            if (command == null)
            {
                return MoveResult.FailureResult(MoveFailureReason.InvalidCommand);
            }

            MoveResult prior;
            if (_commandResults.TryGetValue(command.CommandId, out prior))
            {
                return prior.Succeeded
                    ? MoveResult.FailureResult(MoveFailureReason.DuplicateCommand)
                    : prior;
            }

            MoveFailureReason failure;
            if (command.ExpectedRevision != Revision)
            {
                failure = MoveFailureReason.StaleRevision;
            }
            else if (command.SourceNodeId != CurrentNodeId)
            {
                failure = MoveFailureReason.SourceMismatch;
            }
            else if (_activeTicket != null)
            {
                failure = MoveFailureReason.Reentrant;
            }
            else if (!_nodes.ContainsKey(command.TargetNodeId))
            {
                failure = MoveFailureReason.MissingNode;
            }
            else if (command.TargetNodeId == command.SourceNodeId)
            {
                failure = MoveFailureReason.SameNode;
            }
            else
            {
                failure = ValidateTarget(_nodes[command.TargetNodeId]);
                if (failure == MoveFailureReason.None &&
                    !_nodes[command.SourceNodeId].Coordinate.IsAdjacentTo(
                        _nodes[command.TargetNodeId].Coordinate))
                {
                    failure = MoveFailureReason.NonAdjacent;
                }
            }

            if (failure != MoveFailureReason.None)
            {
                var failed = MoveResult.FailureResult(failure);
                _commandResults[command.CommandId] = failed;
                return failed;
            }

            var ticket = new MoveTicket(
                "move-" + (++_ticketSequence),
                command,
                Revision);
            _activeTicket = ticket;
            Phase = MovementPhase.Moving;
            var success = MoveResult.Success(ticket);
            _commandResults[command.CommandId] = success;
            return success;
        }

        public MoveResult CommitArrival(MoveTicket ticket)
        {
            if (ticket == null || _committedTickets.Contains(ticket.TicketId))
            {
                return MoveResult.FailureResult(MoveFailureReason.AlreadyCommitted);
            }

            if (_activeTicket == null || !string.Equals(
                    _activeTicket.TicketId,
                    ticket.TicketId,
                    StringComparison.Ordinal) || ticket.Revision != Revision)
            {
                return MoveResult.FailureResult(MoveFailureReason.StaleTicket);
            }

            CurrentNodeId = ticket.Command.TargetNodeId;
            _visited.Add(CurrentNodeId);
            Revision++;
            _committedTickets.Add(ticket.TicketId);
            _activeTicket = null;
            Phase = MovementPhase.Arrived;
            return MoveResult.Success(ticket);
        }

        public MoveResult CancelOrFail(MoveTicket ticket)
        {
            if (ticket == null)
            {
                return MoveResult.FailureResult(MoveFailureReason.InvalidCommand);
            }

            if (_committedTickets.Contains(ticket.TicketId))
            {
                return MoveResult.FailureResult(MoveFailureReason.AlreadyCommitted);
            }

            if (_activeTicket == null || !string.Equals(
                    _activeTicket.TicketId,
                    ticket.TicketId,
                    StringComparison.Ordinal))
            {
                return MoveResult.FailureResult(MoveFailureReason.StaleTicket);
            }

            _activeTicket = null;
            Phase = MovementPhase.Idle;
            return MoveResult.Success(ticket);
        }

        public bool MarkSettled(MapNodeId nodeId)
        {
            if (!_nodes.ContainsKey(nodeId))
            {
                return false;
            }

            return _settled.Add(nodeId);
        }

        public void MarkRoomPending()
        {
            if (_activeTicket != null)
            {
                throw new InvalidOperationException("A move must be committed before room preparation.");
            }

            Phase = MovementPhase.RoomPending;
        }

        public OverworldMovementSnapshot Snapshot()
        {
            var nodes = new List<OverworldMapNodeSnapshot>(_nodes.Count);
            foreach (var pair in _nodes)
            {
                var node = pair.Value;
                var available = _activeTicket == null &&
                    node.Id != CurrentNodeId &&
                    !node.IsBlocked && !node.IsLocked && !_settled.Contains(node.Id) &&
                    _nodes[CurrentNodeId].Coordinate.IsAdjacentTo(node.Coordinate);
                nodes.Add(new OverworldMapNodeSnapshot(
                    node,
                    _visited.Contains(node.Id),
                    _settled.Contains(node.Id),
                    available));
            }

            return new OverworldMovementSnapshot(
                Revision,
                CurrentNodeId,
                Phase,
                new ReadOnlyCollection<OverworldMapNodeSnapshot>(nodes),
                new ReadOnlyCollection<MapNodeId>(new List<MapNodeId>(_visited)),
                new ReadOnlyCollection<MapNodeId>(new List<MapNodeId>(_settled)));
        }

        private MoveFailureReason ValidateTarget(OverworldMapNode node)
        {
            if (node.IsBlocked)
            {
                return MoveFailureReason.Blocked;
            }

            if (node.IsLocked)
            {
                return MoveFailureReason.Locked;
            }

            return _settled.Contains(node.Id)
                ? MoveFailureReason.Settled
                : MoveFailureReason.None;
        }
    }
}
