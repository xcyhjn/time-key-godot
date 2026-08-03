using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TimeKey.Application.Overworld;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Overworld;
using TimeKey.Domain.OverworldMovement;

namespace TimeKey.Tests.EditMode.OverworldApplication
{
    public sealed class OverworldRunApplicationTests
    {
        [Test]
        public void Creation_IsDeterministicForNewAndSeedGames()
        {
            var config = Config(chapter: 1, width: 4);
            var newGame = new OverworldRunApplication(
                Start(RunStartKind.NewGame, 731, "run-new"),
                config,
                finalChapter: 3);
            var seedGame = new OverworldRunApplication(
                Start(RunStartKind.SeedGame, 731, "run-seed"),
                config,
                finalChapter: 3);
            var differentSeed = new OverworldRunApplication(
                Start(RunStartKind.SeedGame, 732, "run-other"),
                config,
                finalChapter: 3);

            Assert.That(newGame.Map.Fingerprint, Is.EqualTo(seedGame.Map.Fingerprint));
            Assert.That(newGame.Map.Fingerprint, Is.Not.EqualTo(differentSeed.Map.Fingerprint));
            Assert.That(newGame.Map.EntryNodeId, Is.EqualTo(seedGame.Map.EntryNodeId));
            Assert.That(newGame.CreatePersistenceSnapshot().RunStartKind,
                Is.EqualTo((int)RunStartKind.NewGame));
            Assert.That(seedGame.CreatePersistenceSnapshot().RunStartKind,
                Is.EqualTo((int)RunStartKind.SeedGame));
        }

        [Test]
        public void CombatLaunch_RequiresAdjacentCombatRoomAndPreservesExistingIdentity()
        {
            RunStartPayload start;
            MapNodeId target;
            var session = SessionWithAvailable(
                OverworldRoomType.Battle,
                out start,
                out target);
            var nonAdjacentBoss = session.Map.BossNodeId;

            var rejected = session.TryBeginCombat(
                new EnterOverworldRoomCommand(
                    1,
                    session.Revision,
                    session.Map.EntryNodeId,
                    nonAdjacentBoss),
                "launch-rejected",
                "battle-rejected",
                11);
            var acceptedCommand = new EnterOverworldRoomCommand(
                2,
                session.Revision,
                session.Map.EntryNodeId,
                target);
            var accepted = session.TryBeginCombat(
                acceptedCommand,
                "launch-accepted",
                "battle-accepted",
                22);
            var replay = session.TryBeginCombat(
                acceptedCommand,
                "launch-accepted",
                "battle-accepted",
                22);
            var conflict = session.TryBeginCombat(
                acceptedCommand,
                "changed-launch",
                "battle-accepted",
                22);

            Assert.That(rejected.Failure, Is.EqualTo(OverworldApplicationFailure.NotAdjacent));
            Assert.That(rejected.DomainFailure, Is.EqualTo(OverworldOperationFailure.NotAdjacent));
            Assert.That(accepted.Succeeded, Is.True);
            Assert.That(accepted.Launch, Is.TypeOf<CombatLaunchPayload>());
            Assert.That(accepted.Launch.RunId, Is.EqualTo(start.RunId));
            Assert.That(accepted.Launch.RunSeed, Is.EqualTo(start.RunSeed));
            Assert.That(accepted.Launch.Chapter, Is.EqualTo(start.Chapter));
            Assert.That(accepted.Launch.Era, Is.EqualTo(start.Era));
            Assert.That(accepted.Launch.Phase, Is.EqualTo(start.Phase));
            Assert.That(accepted.Launch.Timecoins, Is.EqualTo(start.Timecoins));
            Assert.That(accepted.Launch.CharacterId, Is.EqualTo(start.CharacterId));
            Assert.That(accepted.Launch.RoomId, Is.EqualTo(target.Value));
            Assert.That(accepted.Launch.DeckStableIds, Is.EqualTo(start.DeckStableIds));
            Assert.That(replay.Succeeded, Is.True);
            Assert.That(replay.WasAlreadyApplied, Is.True);
            Assert.That(conflict.Failure, Is.EqualTo(OverworldApplicationFailure.SequenceConflict));
            Assert.That(session.Revision, Is.EqualTo(1));
        }

        [Test]
        public void CombatLaunch_RejectsAdjacentEventAndShopRooms()
        {
            foreach (var roomType in new[] { OverworldRoomType.Event, OverworldRoomType.Shop })
            {
                RunStartPayload start;
                MapNodeId target;
                var session = SessionWithAvailable(roomType, out start, out target);

                var result = session.TryBeginCombat(
                    new EnterOverworldRoomCommand(
                        1,
                        0,
                        session.Map.EntryNodeId,
                        target),
                    "launch-" + roomType,
                    "battle-" + roomType,
                    17);

                Assert.That(result.Failure,
                    Is.EqualTo(OverworldApplicationFailure.InvalidRoomType));
                Assert.That(session.Revision, Is.EqualTo(0));
            }
        }

        [Test]
        public void CombatOutcome_IsAppliedExactlyOnceAndChangedReplayConflicts()
        {
            RunStartPayload start;
            MapNodeId target;
            var session = SessionWithAvailable(
                OverworldRoomType.Battle,
                out start,
                out target);
            var launch = session.TryBeginCombat(
                new EnterOverworldRoomCommand(1, 0, session.Map.EntryNodeId, target),
                "launch-1",
                "battle-1",
                71).Launch;
            var outcome = VictoryOutcome(launch, "outcome-1", phaseDelta: 0);
            var changed = VictoryOutcome(launch, "outcome-1", phaseDelta: 1);

            var first = session.TryApplyCombatOutcome(2, 1, outcome);
            var replay = session.TryApplyCombatOutcome(3, 2, outcome);
            var conflict = session.TryApplyCombatOutcome(4, 2, changed);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.WasAlreadyApplied, Is.False);
            Assert.That(first.CommitPlan.Steps, Is.EqualTo(new[]
            {
                OverworldTransactionStepKind.AcceptReward,
                OverworldTransactionStepKind.ResolveRoom,
                OverworldTransactionStepKind.UnlockSuccessors,
                OverworldTransactionStepKind.CommitPersistence
            }));
            Assert.That(replay.Succeeded, Is.True);
            Assert.That(replay.WasAlreadyApplied, Is.True);
            Assert.That(conflict.Failure, Is.EqualTo(OverworldApplicationFailure.OutcomeConflict));
            Assert.That(session.Revision, Is.EqualTo(2));
            Assert.That(first.CommitPlan.PersistenceSnapshot.SettledNodeIds,
                Does.Contain(target.Value));
            Assert.That(first.CommitPlan.PersistenceSnapshot.ProcessedOutcomes.Count,
                Is.EqualTo(1));
        }

        [Test]
        public void CombatDefeat_DoesNotSettleRoomOrMasqueradeAsCompletion()
        {
            RunStartPayload start;
            MapNodeId target;
            var session = SessionWithAvailable(
                OverworldRoomType.Battle,
                out start,
                out target);
            var launch = session.TryBeginCombat(
                new EnterOverworldRoomCommand(1, 0, session.Map.EntryNodeId, target),
                "launch-defeat",
                "battle-defeat",
                88).Launch;

            var result = session.TryApplyCombatOutcome(
                2,
                1,
                DefeatOutcome(launch, "outcome-defeat"));
            var snapshot = result.CommitPlan.PersistenceSnapshot;

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CommitPlan.Steps, Is.EqualTo(new[]
            {
                OverworldTransactionStepKind.RecordDefeat,
                OverworldTransactionStepKind.CommitPersistence
            }));
            Assert.That(snapshot.SettledNodeIds, Does.Not.Contain(target.Value));
            Assert.That(snapshot.ChapterCompleted, Is.False);
            Assert.That(snapshot.ActiveRoomId, Is.Empty);
            Assert.That(snapshot.ActiveLaunchCorrelationId, Is.Empty);
        }

        [Test]
        public void EventAndShopOutcomes_AreSeparateTypedContracts()
        {
            RunStartPayload eventStart;
            MapNodeId eventRoom;
            var eventSession = SessionWithAvailable(
                OverworldRoomType.Event,
                out eventStart,
                out eventRoom);
            eventSession.TryEnterEventRoom(new EnterOverworldRoomCommand(
                1,
                0,
                eventSession.Map.EntryNodeId,
                eventRoom));
            var wrongType = eventSession.TryApplyShopOutcome(
                2,
                1,
                new ShopRoomOutcome(
                    "shop-wrong",
                    eventStart.RunId,
                    eventRoom,
                    ShopRoomCompletion.Completed));
            var eventResult = eventSession.TryApplyEventOutcome(
                3,
                1,
                new EventRoomOutcome(
                    "event-complete",
                    eventStart.RunId,
                    eventRoom,
                    EventRoomCompletion.Completed));

            RunStartPayload shopStart;
            MapNodeId shopRoom;
            var shopSession = SessionWithAvailable(
                OverworldRoomType.Shop,
                out shopStart,
                out shopRoom);
            shopSession.TryEnterShopRoom(new EnterOverworldRoomCommand(
                1,
                0,
                shopSession.Map.EntryNodeId,
                shopRoom));
            var shopResult = shopSession.TryApplyShopOutcome(
                2,
                1,
                new ShopRoomOutcome(
                    "shop-cancel",
                    shopStart.RunId,
                    shopRoom,
                    ShopRoomCompletion.Cancelled));

            Assert.That(wrongType.Failure,
                Is.EqualTo(OverworldApplicationFailure.InvalidRoomType));
            Assert.That(eventResult.Succeeded, Is.True);
            Assert.That(eventResult.CommitPlan.PersistenceSnapshot.SettledNodeIds,
                Does.Contain(eventRoom.Value));
            Assert.That(shopResult.Succeeded, Is.True);
            Assert.That(shopResult.CommitPlan.PersistenceSnapshot.SettledNodeIds,
                Does.Not.Contain(shopRoom.Value));
            Assert.That(typeof(EventRoomOutcome), Is.Not.EqualTo(typeof(ShopRoomOutcome)));
            Assert.That(typeof(EventRoomOutcome).GetProperties(),
                Has.None.Property("PropertyType").EqualTo(typeof(CombatOutcome)));
            Assert.That(typeof(ShopRoomOutcome).GetProperties(),
                Has.None.Property("PropertyType").EqualTo(typeof(CombatOutcome)));
        }

        [Test]
        public void BossVictory_AdvancesOnceAndOrdersFullCommitPlan()
        {
            var session = new OverworldRunApplication(
                Start(RunStartKind.SeedGame, 411, "run-boss"),
                Config(chapter: 1, width: 1),
                finalChapter: 2);
            long sequence = 1;
            SettleOnlyIntermediateRoom(session, ref sequence);
            var bossLaunch = session.TryBeginCombat(
                new EnterOverworldRoomCommand(
                    sequence++,
                    session.Revision,
                    CurrentNodeId(session),
                    session.Map.BossNodeId),
                "launch-boss",
                "battle-boss",
                919).Launch;
            var bossOutcome = VictoryOutcome(bossLaunch, "outcome-boss", phaseDelta: 0);

            var first = session.TryApplyCombatOutcome(
                sequence++,
                session.Revision,
                bossOutcome);
            var replay = session.TryApplyCombatOutcome(
                sequence++,
                session.Revision,
                bossOutcome);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.CommitPlan.Steps, Is.EqualTo(new[]
            {
                OverworldTransactionStepKind.AcceptReward,
                OverworldTransactionStepKind.ResolveRoom,
                OverworldTransactionStepKind.UnlockSuccessors,
                OverworldTransactionStepKind.AdvanceChapter,
                OverworldTransactionStepKind.CommitPersistence
            }));
            Assert.That(first.CommitPlan.ChapterDecision.Kind,
                Is.EqualTo(OverworldChapterAdvanceKind.NextChapter));
            Assert.That(first.CommitPlan.ChapterDecision.NextChapter, Is.EqualTo(2));
            Assert.That(first.CommitPlan.PersistenceSnapshot.ChapterAdvanceCount, Is.EqualTo(1));
            Assert.That(replay.Succeeded, Is.True);
            Assert.That(replay.WasAlreadyApplied, Is.True);
            Assert.That(session.CreatePersistenceSnapshot().ChapterAdvanceCount, Is.EqualTo(1));
        }

        [Test]
        public void BossVictory_OnConfiguredLastChapterReturnsFinalDecision()
        {
            var session = new OverworldRunApplication(
                Start(RunStartKind.SeedGame, 512, "run-final", chapter: 2),
                Config(chapter: 2, width: 1),
                finalChapter: 2);
            long sequence = 1;
            SettleOnlyIntermediateRoom(session, ref sequence);
            var launch = session.TryBeginCombat(
                new EnterOverworldRoomCommand(
                    sequence++,
                    session.Revision,
                    CurrentNodeId(session),
                    session.Map.BossNodeId),
                "launch-final",
                "battle-final",
                1024).Launch;

            var result = session.TryApplyCombatOutcome(
                sequence,
                session.Revision,
                VictoryOutcome(launch, "outcome-final", phaseDelta: 0));

            Assert.That(result.CommitPlan.ChapterDecision.Kind,
                Is.EqualTo(OverworldChapterAdvanceKind.FinalChapter));
            Assert.That(result.CommitPlan.ChapterDecision.NextChapter, Is.EqualTo(0));
        }

        [Test]
        public void PersistenceSnapshot_IsVersionedOrderedAndDefensivelyCopied()
        {
            var deck = new List<string> { "lighting", "recover" };
            var start = new RunStartPayload(
                RunStartKind.SeedGame,
                "run-snapshot",
                731,
                "731",
                1,
                1,
                1,
                0,
                "silver-character",
                deck);
            var session = new OverworldRunApplication(
                start,
                Config(chapter: 1, width: 3),
                finalChapter: 3);
            deck[0] = "mutated-source";

            var first = session.CreatePersistenceSnapshot();
            var second = session.CreatePersistenceSnapshot();
            var mutableView = (IList<string>)first.DeckStableIds;

            Assert.That(first.SchemaVersion,
                Is.EqualTo(OverworldPersistenceSnapshot.CurrentSchemaVersion));
            Assert.That(first.MapConfigVersion,
                Is.EqualTo(OverworldPersistenceSnapshot.CurrentMapConfigVersion));
            Assert.That(first.DeckStableIds[0], Is.EqualTo("lighting"));
            Assert.That(first.MapFingerprint, Is.EqualTo(session.Map.Fingerprint));
            Assert.That(first.CurrentNodeId, Is.EqualTo(session.Map.EntryNodeId.Value));
            Assert.That(first.VisitedNodeIds,
                Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(first.SettledNodeIds,
                Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(first.DeckStableIds, Is.Not.SameAs(second.DeckStableIds));
            Assert.That(first.VisitedNodeIds, Is.Not.SameAs(second.VisitedNodeIds));
            Assert.That(() => mutableView.Add("illegal"), Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void PersistenceContract_ExposesNoDictionaryObjectOrUnityProperties()
        {
            foreach (var type in new[]
            {
                typeof(OverworldPersistenceSnapshot),
                typeof(OverworldProcessedOutcomeSnapshot),
                typeof(EventRoomOutcome),
                typeof(ShopRoomOutcome)
            })
            {
                foreach (var property in type.GetProperties(
                    BindingFlags.Instance | BindingFlags.Public))
                {
                    Assert.That(property.PropertyType, Is.Not.EqualTo(typeof(object)));
                    Assert.That(property.PropertyType.FullName,
                        Does.Not.StartWith("System.Collections.Generic.Dictionary"));
                    Assert.That(property.PropertyType.FullName,
                        Does.Not.StartWith("UnityEngine."));
                }
            }
        }

        private static OverworldRunApplication SessionWithAvailable(
            OverworldRoomType roomType,
            out RunStartPayload start,
            out MapNodeId target)
        {
            for (var seed = 1; seed <= 512; seed++)
            {
                start = Start(RunStartKind.SeedGame, seed, "run-" + seed);
                var session = new OverworldRunApplication(
                    start,
                    Config(chapter: 1, width: 4),
                    finalChapter: 3);
                foreach (var roomId in session.GetAvailableRoomIds())
                {
                    if (session.GetRoomType(roomId) == roomType)
                    {
                        target = roomId;
                        return session;
                    }
                }
            }

            throw new AssertionException("No deterministic seed produced " + roomType + ".");
        }

        private static void SettleOnlyIntermediateRoom(
            OverworldRunApplication session,
            ref long sequence)
        {
            var roomId = session.GetAvailableRoomIds()[0];
            var roomType = session.GetRoomType(roomId).Value;
            if (roomType == OverworldRoomType.Battle || roomType == OverworldRoomType.Elite)
            {
                var launch = session.TryBeginCombat(
                    new EnterOverworldRoomCommand(
                        sequence++,
                        session.Revision,
                        session.Map.EntryNodeId,
                        roomId),
                    "launch-intermediate",
                    "battle-intermediate",
                    404).Launch;
                var result = session.TryApplyCombatOutcome(
                    sequence++,
                    session.Revision,
                    VictoryOutcome(launch, "outcome-intermediate", phaseDelta: 0));
                Assert.That(result.Succeeded, Is.True);
            }
            else if (roomType == OverworldRoomType.Event)
            {
                session.TryEnterEventRoom(new EnterOverworldRoomCommand(
                    sequence++,
                    session.Revision,
                    session.Map.EntryNodeId,
                    roomId));
                var result = session.TryApplyEventOutcome(
                    sequence++,
                    session.Revision,
                    new EventRoomOutcome(
                        "event-intermediate",
                        session.CreatePersistenceSnapshot().RunId,
                        roomId,
                        EventRoomCompletion.Completed));
                Assert.That(result.Succeeded, Is.True);
            }
            else
            {
                Assert.That(roomType, Is.EqualTo(OverworldRoomType.Shop));
                session.TryEnterShopRoom(new EnterOverworldRoomCommand(
                    sequence++,
                    session.Revision,
                    session.Map.EntryNodeId,
                    roomId));
                var result = session.TryApplyShopOutcome(
                    sequence++,
                    session.Revision,
                    new ShopRoomOutcome(
                        "shop-intermediate",
                        session.CreatePersistenceSnapshot().RunId,
                        roomId,
                        ShopRoomCompletion.Completed));
                Assert.That(result.Succeeded, Is.True);
            }
        }

        private static MapNodeId CurrentNodeId(OverworldRunApplication session)
        {
            return new MapNodeId(session.CreatePersistenceSnapshot().CurrentNodeId);
        }

        private static RunStartPayload Start(
            RunStartKind kind,
            int seed,
            string runId,
            int chapter = 1)
        {
            return new RunStartPayload(
                kind,
                runId,
                seed,
                seed.ToString(),
                chapter,
                era: 1,
                phase: 1,
                timecoins: 0,
                characterId: "silver-character",
                deckStableIds: new[] { "lighting", "recover" });
        }

        private static OverworldMapGenerationConfig Config(int chapter, int width)
        {
            return new OverworldMapGenerationConfig(
                chapter,
                intermediateLayerCount: 1,
                minimumNodesPerLayer: width,
                maximumNodesPerLayer: width);
        }

        private static CombatOutcome VictoryOutcome(
            CombatLaunchPayload launch,
            string correlationId,
            int phaseDelta)
        {
            var settlement = new BattleSettlementState(
                launch.BattleTag,
                launch.BattleSeed,
                new BattleRewardEntry(
                    "reward-" + correlationId,
                    BattleRewardKind.Acquire,
                    "Reward"));
            settlement.TryResolve(1, BattleOutcome.VictorySettlement);
            settlement.TryClaimReward(2);
            var round = new BattleRoundLedger(
                launch.Era,
                launch.Phase + phaseDelta,
                launch.Timecoins).Snapshot;
            var boundary = settlement.TryCreateReturnBoundary(round, launch.DeckStableIds);
            var result = CombatOutcome.TryCreate(
                correlationId,
                launch,
                settlement.Snapshot,
                boundary.Payload);
            Assert.That(result.Succeeded, Is.True);
            return result.Outcome;
        }

        private static CombatOutcome DefeatOutcome(
            CombatLaunchPayload launch,
            string correlationId)
        {
            var settlement = new BattleSettlementState(
                launch.BattleTag,
                launch.BattleSeed,
                new BattleRewardEntry(
                    "reward-" + correlationId,
                    BattleRewardKind.Acquire,
                    "Reward"));
            settlement.TryResolve(1, BattleOutcome.Defeat);
            var round = new BattleRoundLedger(
                launch.Era,
                launch.Phase,
                launch.Timecoins).Snapshot;
            var boundary = settlement.TryCreateReturnBoundary(round, launch.DeckStableIds);
            var result = CombatOutcome.TryCreate(
                correlationId,
                launch,
                settlement.Snapshot,
                boundary.Payload);
            Assert.That(result.Succeeded, Is.True);
            return result.Outcome;
        }
    }
}
