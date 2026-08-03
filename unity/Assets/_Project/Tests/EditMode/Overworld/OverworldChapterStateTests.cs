using System.Collections.Generic;
using NUnit.Framework;
using TimeKey.Domain.Overworld;
using TimeKey.Domain.OverworldMovement;
using ChapterMapNode = TimeKey.Domain.Overworld.OverworldMapNode;

namespace TimeKey.Tests.EditMode.Overworld
{
    public sealed class OverworldChapterStateTests
    {
        [Test]
        public void EntrySnapshot_ExposesOnlyAdjacentOutgoingRooms()
        {
            var map = CreateBranchingMap();
            var state = new OverworldChapterState(map);
            var snapshot = state.Snapshot();

            Assert.That(snapshot.CurrentNodeId, Is.EqualTo(map.EntryNodeId));
            Assert.That(Contains(snapshot.VisitedNodeIds, map.EntryNodeId), Is.True);
            Assert.That(Contains(snapshot.SettledNodeIds, map.EntryNodeId), Is.True);
            Assert.That(Find(snapshot, "battle").IsAvailable, Is.True);
            Assert.That(Find(snapshot, "event").IsAvailable, Is.True);
            Assert.That(Find(snapshot, "boss").IsAvailable, Is.False);
        }

        [Test]
        public void EnterRoom_RequiresSavedAdjacentEdgeAndHasNoPartialFailure()
        {
            var map = CreateBranchingMap();
            var state = new OverworldChapterState(map);

            var result = state.EnterRoom(new EnterOverworldRoomCommand(
                1,
                expectedRevision: 0,
                map.EntryNodeId,
                new MapNodeId("boss")));

            Assert.That(result.Failure, Is.EqualTo(OverworldOperationFailure.NotAdjacent));
            Assert.That(state.Revision, Is.EqualTo(0));
            Assert.That(state.CurrentNodeId, Is.EqualTo(map.EntryNodeId));
            Assert.That(state.Snapshot().HasActiveRoom, Is.False);
        }

        [Test]
        public void SameSequenceAndPayloadIsIdempotent_DifferentPayloadConflicts()
        {
            var map = CreateBranchingMap();
            var state = new OverworldChapterState(map);
            var command = new EnterOverworldRoomCommand(
                7,
                expectedRevision: 0,
                map.EntryNodeId,
                new MapNodeId("battle"));

            var first = state.EnterRoom(command);
            var replay = state.EnterRoom(command);
            var conflict = state.EnterRoom(new EnterOverworldRoomCommand(
                7,
                expectedRevision: 0,
                map.EntryNodeId,
                new MapNodeId("event")));

            Assert.That(first.Succeeded, Is.True);
            Assert.That(replay, Is.SameAs(first));
            Assert.That(conflict.Failure, Is.EqualTo(OverworldOperationFailure.SequenceConflict));
            Assert.That(state.Revision, Is.EqualTo(1));
            Assert.That(state.CurrentNodeId, Is.EqualTo(new MapNodeId("battle")));
        }

        [Test]
        public void StaleAndCrossKindSequenceConflictsAreExplicit()
        {
            var map = CreateBranchingMap();
            var state = new OverworldChapterState(map);
            state.EnterRoom(new EnterOverworldRoomCommand(
                1,
                0,
                map.EntryNodeId,
                new MapNodeId("battle")));

            var stale = state.ResolveRoom(new ResolveOverworldRoomCommand(
                2,
                expectedRevision: 0,
                new MapNodeId("battle"),
                OverworldResolutionKind.Victory));
            var conflict = state.ResolveRoom(new ResolveOverworldRoomCommand(
                1,
                expectedRevision: 1,
                new MapNodeId("battle"),
                OverworldResolutionKind.Victory));

            Assert.That(stale.Failure, Is.EqualTo(OverworldOperationFailure.StaleRevision));
            Assert.That(conflict.Failure, Is.EqualTo(OverworldOperationFailure.SequenceConflict));
            Assert.That(state.Snapshot().HasActiveRoom, Is.True);
            Assert.That(state.Revision, Is.EqualTo(1));
        }

        [Test]
        public void ActiveRoomBlocksSecondEntryAndResolutionKindMustMatchRoom()
        {
            var map = CreateBranchingMap();
            var state = new OverworldChapterState(map);
            state.EnterRoom(new EnterOverworldRoomCommand(
                1,
                0,
                map.EntryNodeId,
                new MapNodeId("battle")));

            var secondEntry = state.EnterRoom(new EnterOverworldRoomCommand(
                2,
                1,
                new MapNodeId("battle"),
                new MapNodeId("boss")));
            var wrongResolution = state.ResolveRoom(new ResolveOverworldRoomCommand(
                3,
                1,
                new MapNodeId("battle"),
                OverworldResolutionKind.Completed));

            Assert.That(secondEntry.Failure, Is.EqualTo(OverworldOperationFailure.RoomInProgress));
            Assert.That(wrongResolution.Failure, Is.EqualTo(OverworldOperationFailure.InvalidResolution));
            Assert.That(state.Snapshot().HasActiveRoom, Is.True);
        }

        [TestCase(OverworldRoomType.Event)]
        [TestCase(OverworldRoomType.Shop)]
        public void NonCombatRoomCompletesAndUnlocksSuccessor(OverworldRoomType roomType)
        {
            var map = CreateLinearMap(roomType);
            var state = new OverworldChapterState(map);
            var room = new MapNodeId("room");
            state.EnterRoom(new EnterOverworldRoomCommand(1, 0, map.EntryNodeId, room));

            var result = state.ResolveRoom(new ResolveOverworldRoomCommand(
                2,
                1,
                room,
                OverworldResolutionKind.Completed));
            var snapshot = state.Snapshot();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(Contains(snapshot.VisitedNodeIds, room), Is.True);
            Assert.That(Contains(snapshot.SettledNodeIds, room), Is.True);
            Assert.That(Find(snapshot, "boss").IsAvailable, Is.True);
        }

        [Test]
        public void BossVictoryAdvancesChapterExactlyOnce()
        {
            var map = CreateLinearMap(OverworldRoomType.Battle);
            var state = AdvanceToBoss(map);
            var boss = map.BossNodeId;
            var command = new ResolveOverworldRoomCommand(
                4,
                state.Revision,
                boss,
                OverworldResolutionKind.Victory);

            var first = state.ResolveRoom(command);
            var replay = state.ResolveRoom(command);
            var second = state.ResolveRoom(new ResolveOverworldRoomCommand(
                5,
                state.Revision,
                boss,
                OverworldResolutionKind.Victory));

            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.ChapterAdvanced, Is.True);
            Assert.That(replay, Is.SameAs(first));
            Assert.That(second.Failure, Is.EqualTo(OverworldOperationFailure.NoActiveRoom));
            Assert.That(state.ChapterCompleted, Is.True);
            Assert.That(state.ChapterAdvanceCount, Is.EqualTo(1));
        }

        [Test]
        public void BossDefeatDoesNotSettleOrAdvanceChapter()
        {
            var map = CreateLinearMap(OverworldRoomType.Battle);
            var state = AdvanceToBoss(map);

            var result = state.ResolveRoom(new ResolveOverworldRoomCommand(
                4,
                state.Revision,
                map.BossNodeId,
                OverworldResolutionKind.Defeat));
            var snapshot = state.Snapshot();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChapterAdvanced, Is.False);
            Assert.That(state.ChapterCompleted, Is.False);
            Assert.That(state.ChapterAdvanceCount, Is.EqualTo(0));
            Assert.That(Contains(snapshot.SettledNodeIds, map.BossNodeId), Is.False);
        }

        [Test]
        public void SnapshotCollectionsAreDefensive()
        {
            var state = new OverworldChapterState(CreateBranchingMap());
            var first = state.Snapshot();
            var second = state.Snapshot();

            Assert.That(first.Nodes, Is.Not.SameAs(second.Nodes));
            Assert.That(first.VisitedNodeIds, Is.Not.SameAs(second.VisitedNodeIds));
            Assert.That(first.SettledNodeIds, Is.Not.SameAs(second.SettledNodeIds));
            Assert.That(first.Nodes, Is.Not.InstanceOf<List<OverworldNodeStateSnapshot>>());
        }

        private static OverworldChapterState AdvanceToBoss(OverworldMapDefinition map)
        {
            var state = new OverworldChapterState(map);
            var room = new MapNodeId("room");
            state.EnterRoom(new EnterOverworldRoomCommand(1, 0, map.EntryNodeId, room));
            state.ResolveRoom(new ResolveOverworldRoomCommand(
                2,
                1,
                room,
                OverworldResolutionKind.Victory));
            state.EnterRoom(new EnterOverworldRoomCommand(
                3,
                2,
                room,
                map.BossNodeId));
            return state;
        }

        private static OverworldMapDefinition CreateLinearMap(OverworldRoomType roomType)
        {
            var entry = Node("entry", 0, OverworldRoomType.Entry);
            var room = Node("room", 1, roomType);
            var boss = Node("boss", 2, OverworldRoomType.Boss);
            return new OverworldMapDefinition(
                seed: 17,
                chapter: 1,
                new[] { entry, room, boss },
                new[]
                {
                    new OverworldMapEdge(entry.Id, room.Id),
                    new OverworldMapEdge(room.Id, boss.Id)
                });
        }

        private static OverworldMapDefinition CreateBranchingMap()
        {
            var entry = Node("entry", 0, OverworldRoomType.Entry, 0);
            var battle = Node("battle", 1, OverworldRoomType.Battle, 0);
            var eventNode = Node("event", 1, OverworldRoomType.Event, 1);
            var boss = Node("boss", 2, OverworldRoomType.Boss, 0);
            return new OverworldMapDefinition(
                seed: 22,
                chapter: 1,
                new[] { entry, battle, eventNode, boss },
                new[]
                {
                    new OverworldMapEdge(entry.Id, battle.Id),
                    new OverworldMapEdge(entry.Id, eventNode.Id),
                    new OverworldMapEdge(battle.Id, boss.Id),
                    new OverworldMapEdge(eventNode.Id, boss.Id)
                });
        }

        private static ChapterMapNode Node(
            string id,
            int layer,
            OverworldRoomType type,
            int slot = 0)
        {
            return new ChapterMapNode(new MapNodeId(id), 1, layer, slot, type);
        }

        private static OverworldNodeStateSnapshot Find(
            OverworldChapterSnapshot snapshot,
            string id)
        {
            var nodeId = new MapNodeId(id);
            foreach (var node in snapshot.Nodes)
            {
                if (node.Id == nodeId)
                {
                    return node;
                }
            }

            Assert.Fail("Missing node " + id);
            return null;
        }

        private static bool Contains(
            IEnumerable<MapNodeId> nodes,
            MapNodeId target)
        {
            foreach (var node in nodes)
            {
                if (node == target)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
