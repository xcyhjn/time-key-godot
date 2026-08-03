using System.Collections.Generic;
using NUnit.Framework;
using TimeKey.Domain.OverworldMovement;

namespace TimeKey.Tests.EditMode.OverworldMovement
{
    public sealed class OverworldMovementModelTests
    {
        private static readonly MapNodeId Start = new MapNodeId("start");
        private static readonly MapNodeId East = new MapNodeId("east");
        private static readonly MapNodeId SouthWest = new MapNodeId("south-west");
        private static readonly MapNodeId Far = new MapNodeId("far");

        [Test]
        public void AxialHexCoord_UsesSixUniqueDirections()
        {
            var origin = new AxialHexCoord(0, 0);
            var directions = new[]
            {
                new AxialHexCoord(-1, 1), new AxialHexCoord(-1, 0),
                new AxialHexCoord(0, -1), new AxialHexCoord(1, -1),
                new AxialHexCoord(1, 0), new AxialHexCoord(0, 1)
            };

            Assert.That(new HashSet<AxialHexCoord>(directions).Count, Is.EqualTo(6));
            foreach (var direction in directions)
            {
                Assert.That(origin.IsAdjacentTo(direction), Is.True);
            }
        }

        [Test]
        public void RequestMove_DoesNotCommitBeforeArrival()
        {
            var model = CreateModel();

            var result = model.RequestMove(Command("move-east", 0, East));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(model.CurrentNodeId, Is.EqualTo(Start));
            Assert.That(model.Revision, Is.EqualTo(0));
            Assert.That(model.Phase, Is.EqualTo(MovementPhase.Moving));
        }

        [Test]
        public void CommitArrival_ChangesPositionAndRevisionOnce()
        {
            var model = CreateModel();
            var ticket = model.RequestMove(Command("move-east", 0, East)).Ticket;

            var committed = model.CommitArrival(ticket);
            var replay = model.CommitArrival(ticket);

            Assert.That(committed.Succeeded, Is.True);
            Assert.That(replay.Failure, Is.EqualTo(MoveFailureReason.AlreadyCommitted));
            Assert.That(model.CurrentNodeId, Is.EqualTo(East));
            Assert.That(model.Revision, Is.EqualTo(1));
            Assert.That(model.Snapshot().VisitedNodeIds, Does.Contain(East));
        }

        [TestCase(MoveFailureReason.SameNode)]
        [TestCase(MoveFailureReason.NonAdjacent)]
        [TestCase(MoveFailureReason.MissingNode)]
        [TestCase(MoveFailureReason.Blocked)]
        [TestCase(MoveFailureReason.Locked)]
        public void RequestMove_RejectsInvalidTargetWithoutSideEffects(MoveFailureReason expected)
        {
            var model = CreateModel();
            MoveCommand command;
            switch (expected)
            {
                case MoveFailureReason.SameNode:
                    command = Command("same", 0, Start);
                    break;
                case MoveFailureReason.NonAdjacent:
                    command = Command("far", 0, Far);
                    break;
                case MoveFailureReason.MissingNode:
                    command = Command("missing", 0, new MapNodeId("missing"));
                    break;
                case MoveFailureReason.Blocked:
                    command = Command("blocked", 0, new MapNodeId("blocked"));
                    break;
                default:
                    command = Command("locked", 0, new MapNodeId("locked"));
                    break;
            }

            var result = model.RequestMove(command);

            Assert.That(result.Failure, Is.EqualTo(expected));
            Assert.That(model.CurrentNodeId, Is.EqualTo(Start));
            Assert.That(model.Revision, Is.EqualTo(0));
        }

        [Test]
        public void RequestMove_RejectsStaleAndReentrantCommands()
        {
            var model = CreateModel();
            var first = model.RequestMove(Command("first", 0, East));
            var reentrant = model.RequestMove(Command("reentrant", 0, SouthWest));
            model.CommitArrival(first.Ticket);
            var stale = model.RequestMove(
                new MoveCommand("stale", 0, East, SouthWest));

            Assert.That(first.Succeeded, Is.True);
            Assert.That(reentrant.Failure, Is.EqualTo(MoveFailureReason.Reentrant));
            Assert.That(stale.Failure, Is.EqualTo(MoveFailureReason.StaleRevision));
        }

        [Test]
        public void CancelOrFail_RestoresIdleWithoutPartialCommit()
        {
            var model = CreateModel();
            var ticket = model.RequestMove(Command("cancel", 0, East)).Ticket;

            var cancelled = model.CancelOrFail(ticket);
            var replay = model.CancelOrFail(ticket);

            Assert.That(cancelled.Succeeded, Is.True);
            Assert.That(replay.Failure, Is.EqualTo(MoveFailureReason.StaleTicket));
            Assert.That(model.CurrentNodeId, Is.EqualTo(Start));
            Assert.That(model.Revision, Is.EqualTo(0));
            Assert.That(model.Phase, Is.EqualTo(MovementPhase.Idle));
        }

        [Test]
        public void SettledNode_IsRejectedAndNotAvailable()
        {
            var model = CreateModel();
            Assert.That(model.MarkSettled(East), Is.True);

            var result = model.RequestMove(Command("settled", 0, East));

            Assert.That(result.Failure, Is.EqualTo(MoveFailureReason.Settled));
            var east = Find(model.Snapshot(), East);
            Assert.That(east.IsSettled, Is.True);
            Assert.That(east.IsAvailable, Is.False);
        }

        [Test]
        public void Snapshot_CollectionsAreDefensiveCopies()
        {
            var model = CreateModel();
            var snapshot = model.Snapshot();

            Assert.That(snapshot.VisitedNodeIds, Is.Not.SameAs(model.Snapshot().VisitedNodeIds));
            Assert.That(snapshot.Nodes, Is.Not.SameAs(model.Snapshot().Nodes));
        }

        private static OverworldMovementModel CreateModel()
        {
            return new OverworldMovementModel(
                new[]
                {
                    new OverworldMapNode(Start, new AxialHexCoord(0, 0), MapNodeType.Start),
                    new OverworldMapNode(East, new AxialHexCoord(1, 0), MapNodeType.Normal),
                    new OverworldMapNode(SouthWest, new AxialHexCoord(-1, 1), MapNodeType.Event),
                    new OverworldMapNode(Far, new AxialHexCoord(2, 0), MapNodeType.Normal),
                    new OverworldMapNode(new MapNodeId("blocked"), new AxialHexCoord(0, 1), MapNodeType.Blocked),
                    new OverworldMapNode(new MapNodeId("locked"), new AxialHexCoord(-1, 0), MapNodeType.Normal, false, true)
                },
                Start);
        }

        private static MoveCommand Command(string id, int revision, MapNodeId target)
        {
            return new MoveCommand(id, revision, Start, target);
        }

        private static OverworldMapNodeSnapshot Find(
            OverworldMovementSnapshot snapshot,
            MapNodeId id)
        {
            foreach (var node in snapshot.Nodes)
            {
                if (node.Id == id)
                {
                    return node;
                }
            }

            Assert.Fail("Missing node " + id);
            return null;
        }
    }
}
