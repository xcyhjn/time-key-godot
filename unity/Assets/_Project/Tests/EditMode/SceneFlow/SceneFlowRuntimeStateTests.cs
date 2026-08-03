using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TimeKey.Application.Overworld;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition.SceneFlow;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Overworld;
using TimeKey.Domain.OverworldMovement;
using TimeKey.Infrastructure.Persistence;
using UnityEngine;

namespace TimeKey.Tests.EditMode.SceneFlow
{
    public sealed class SceneFlowRuntimeStateTests
    {
        [TearDown]
        public void TearDown()
        {
            SceneInputLockState.SetLocked(false);
        }

        [Test]
        public void StateStore_ConsumesMatchingOutcomeOnceAndRejectsAnotherLaunch()
        {
            var root = new GameObject("state-store-test");
            try
            {
                var store = root.AddComponent<SceneFlowStateStore>();
                var launch = Launch("launch-1", "room-1");
                var launchRequest = Request(
                    1,
                    SceneId.OutOfBattleShell,
                    SceneId.Combat,
                    launch);
                store.Record(launchRequest);
                store.Commit(launchRequest);
                var outcome = VictoryOutcome(launch, "outcome-1");

                var outcomeRequest = Request(
                    2,
                    SceneId.Combat,
                    SceneId.OutOfBattleShell,
                    outcome);
                store.Record(outcomeRequest);
                store.Commit(outcomeRequest);
                Assert.That(store.LastOutcomeApplyResult.Succeeded, Is.True);
                Assert.That(store.LastOutcomeApplyResult.WasAlreadyApplied, Is.False);
                Assert.That(store.OutOfBattleState.SettledRoomIds, Does.Contain("room-1"));
                Assert.That(store.ActiveLaunch, Is.Null);

                store.Record(outcomeRequest);
                Assert.That(store.LastOutcomeApplyResult.WasAlreadyApplied, Is.True);
                store.Commit(outcomeRequest);

                Assert.Throws<InvalidOperationException>(() =>
                    store.Record(Request(
                        3,
                        SceneId.Combat,
                        SceneId.GameOver,
                        DefeatOutcome(launch, "outcome-conflict"))));

                var otherLaunch = Launch("launch-2", "room-2");
                var otherOutcome = VictoryOutcome(otherLaunch, "outcome-2");
                Assert.Throws<InvalidOperationException>(() =>
                    store.Record(Request(
                        4,
                        SceneId.Combat,
                        SceneId.OutOfBattleShell,
                        otherOutcome)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StateStore_RollbackRestoresStateBeforeTypedRecord()
        {
            var root = new GameObject("state-store-rollback-test");
            try
            {
                var store = root.AddComponent<SceneFlowStateStore>();
                var launch = Launch("launch-rollback", "room-rollback");
                var request = Request(
                    1,
                    SceneId.OutOfBattleShell,
                    SceneId.Combat,
                    launch);

                store.Record(request);
                Assert.That(store.ActiveLaunch, Is.SameAs(launch));

                store.Rollback(request);
                Assert.That(store.LastRequest, Is.Null);
                Assert.That(store.LastPayload, Is.Null);
                Assert.That(store.ActiveLaunch, Is.Null);
                Assert.That(store.OutOfBattleState, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StateStore_GameOverToMainMenuClearsRunForNextLaunch()
        {
            var root = new GameObject("state-store-new-run-test");
            try
            {
                var store = root.AddComponent<SceneFlowStateStore>();
                var firstLaunch = Launch("first-launch", "first-room");
                var firstRequest = Request(
                    1,
                    SceneId.OutOfBattleShell,
                    SceneId.Combat,
                    firstLaunch);
                store.Record(firstRequest);
                store.Commit(firstRequest);

                var resetRequest = Request(
                    2,
                    SceneId.GameOver,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu));
                store.Record(resetRequest);
                store.Commit(resetRequest);

                Assert.That(store.OutOfBattleState, Is.Null);
                Assert.That(store.ActiveLaunch, Is.Null);
                Assert.That(store.LastOutcome, Is.Null);
                Assert.That(store.LastOutcomeApplyResult, Is.Null);

                var nextLaunch = Launch(
                    "next-launch",
                    "next-room",
                    "run-2");
                var nextRequest = Request(
                    3,
                    SceneId.OutOfBattleShell,
                    SceneId.Combat,
                    nextLaunch);
                store.Record(nextRequest);
                store.Commit(nextRequest);
                Assert.That(store.OutOfBattleState.RunId, Is.EqualTo("run-2"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StateStore_NewRunSaveAndContinueRestoreSameRunMapAndNode()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "timekey-gate-b-continue-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "overworld.json");
            var firstRoot = new GameObject("state-store-save-test");
            var secondRoot = new GameObject("state-store-continue-test");
            try
            {
                var first = firstRoot.AddComponent<SceneFlowStateStore>();
                first.ConfigurePersistencePath(path);
                var start = Start("run-continue", 731);
                var request = Request(
                    1,
                    SceneId.MainMenu,
                    SceneId.OutOfBattleShell,
                    start);
                first.Record(request);
                first.PrepareCommit(request);
                first.Commit(request);
                var expected = first.OverworldRun.CreatePersistenceSnapshot();

                var second = secondRoot.AddComponent<SceneFlowStateStore>();
                second.ConfigurePersistencePath(path);
                Assert.That(second.RefreshContinueAvailability(), Is.True);
                Assert.That(second.TryCreateContinuePayload(out var payload), Is.True);
                var continueRequest = Request(
                    2,
                    SceneId.MainMenu,
                    SceneId.OutOfBattleShell,
                    payload);
                second.Record(continueRequest);
                var actual = second.OverworldRun.CreatePersistenceSnapshot();

                Assert.That(actual.RunId, Is.EqualTo(expected.RunId));
                Assert.That(actual.MapFingerprint, Is.EqualTo(expected.MapFingerprint));
                Assert.That(actual.CurrentNodeId, Is.EqualTo(expected.CurrentNodeId));
                Assert.That(actual.DomainRevision, Is.EqualTo(expected.DomainRevision));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(firstRoot);
                UnityEngine.Object.DestroyImmediate(secondRoot);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void StateStore_FailedPersistencePrepareKeepsPriorSaveAndRollsBackRun()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "timekey-gate-b-rollback-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "overworld.json");
            var root = new GameObject("state-store-persistence-rollback-test");
            try
            {
                var repository = new OverworldSaveRepository(path);
                var store = root.AddComponent<SceneFlowStateStore>();
                store.ConfigurePersistenceRepository(repository);
                var firstRequest = Request(
                    1,
                    SceneId.MainMenu,
                    SceneId.OutOfBattleShell,
                    Start("run-prior", 731));
                store.Record(firstRequest);
                store.PrepareCommit(firstRequest);
                store.Commit(firstRequest);
                var priorFingerprint = store.OverworldRun.Map.Fingerprint;

                store.ConfigurePersistenceRepository(new OverworldSaveRepository(
                    path,
                    new ThrowBeforeReplace()));
                var replacement = Request(
                    2,
                    SceneId.MainMenu,
                    SceneId.OutOfBattleShell,
                    Start("run-replacement", 999));
                store.Record(replacement);

                Assert.Throws<IOException>(() => store.PrepareCommit(replacement));
                store.Rollback(replacement);

                Assert.That(store.OverworldRun.Map.Fingerprint, Is.EqualTo(priorFingerprint));
                var loaded = repository.Load();
                Assert.That(loaded.Succeeded, Is.True);
                Assert.That(loaded.Document.RunId, Is.EqualTo("run-prior"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void StateStore_LocalEventCommitPersistsBeforePromotingCandidate()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "timekey-gate-c-event-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "overworld.json");
            var root = new GameObject("state-store-event-test");
            try
            {
                var seed = SeedWithAvailable(OverworldRoomType.Event);
                var store = root.AddComponent<SceneFlowStateStore>();
                store.ConfigurePersistencePath(path);
                CommitStart(store, Start("run-event", seed), 1);
                var room = AvailableRoom(store.OverworldRun, OverworldRoomType.Event);

                var result = store.CompleteEventRoom(2, 3, room.Value);
                var loaded = new OverworldSaveRepository(path).Load();

                Assert.That(result.Succeeded, Is.True, result.Detail);
                Assert.That(
                    store.OverworldRun.ChapterSnapshot.SettledNodeIds.Contains(room),
                    Is.True);
                Assert.That(loaded.Succeeded, Is.True, loaded.Detail);
                Assert.That(loaded.Document.SettledNodeIds, Does.Contain(room.Value));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void StateStore_LocalShopPurchasePersistsBalanceAndDeckOnce()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "timekey-gate-c-shop-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "overworld.json");
            var root = new GameObject("state-store-shop-test");
            try
            {
                var seed = SeedWithAvailable(OverworldRoomType.Shop);
                var store = root.AddComponent<SceneFlowStateStore>();
                store.ConfigurePersistencePath(path);
                CommitStart(store, Start("run-shop", seed, timecoins: 100), 1);
                var room = AvailableRoom(store.OverworldRun, OverworldRoomType.Shop);
                var before = store.OverworldRun.CreatePersistenceSnapshot();

                var result = store.PurchaseShopRoom(2, 3, room.Value);
                var loaded = new OverworldSaveRepository(path).Load();

                Assert.That(result.Succeeded, Is.True, result.Detail);
                Assert.That(result.ShopOffer.TimecoinCost, Is.EqualTo(50));
                Assert.That(result.Snapshot.Timecoins, Is.EqualTo(50));
                Assert.That(result.Snapshot.DeckStableIds.Count,
                    Is.EqualTo(before.DeckStableIds.Count + 1));
                Assert.That(loaded.Document.Timecoins, Is.EqualTo(50));
                Assert.That(loaded.Document.DeckStableIds[^1],
                    Is.EqualTo(result.ShopOffer.CardStableId));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void StateStore_LocalRoomPersistenceFailureKeepsPriorRunAndSave()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "timekey-gate-c-local-rollback-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "overworld.json");
            var root = new GameObject("state-store-local-rollback-test");
            try
            {
                var seed = SeedWithAvailable(OverworldRoomType.Event);
                var repository = new OverworldSaveRepository(path);
                var store = root.AddComponent<SceneFlowStateStore>();
                store.ConfigurePersistenceRepository(repository);
                CommitStart(store, Start("run-local-prior", seed), 1);
                var prior = store.OverworldRun.CreatePersistenceSnapshot();
                var room = AvailableRoom(store.OverworldRun, OverworldRoomType.Event);
                store.ConfigurePersistenceRepository(new OverworldSaveRepository(
                    path,
                    new ThrowBeforeReplace()));

                var result = store.CompleteEventRoom(2, 3, room.Value);
                var loaded = repository.Load();

                Assert.That(result.Failure,
                    Is.EqualTo(OverworldLocalRoomCommitFailure.PersistenceFailed));
                Assert.That(store.OverworldRun.CreatePersistenceSnapshot().CurrentNodeId,
                    Is.EqualTo(prior.CurrentNodeId));
                Assert.That(
                    store.OverworldRun.ChapterSnapshot.SettledNodeIds.Contains(room),
                    Is.False);
                Assert.That(loaded.Document.CurrentNodeId, Is.EqualTo(prior.CurrentNodeId));
                Assert.That(loaded.Document.SettledNodeIds, Does.Not.Contain(room.Value));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void PersistentInputGate_PublishesAndClearsGlobalTransitionLock()
        {
            var root = new GameObject("input-gate-test");
            var gate = root.AddComponent<PersistentInputGate>();

            gate.SetLocked(true);
            Assert.That(SceneInputLockState.IsLocked, Is.True);

            gate.SetLocked(false);
            Assert.That(SceneInputLockState.IsLocked, Is.False);
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void SceneInputLockLease_DoesNotClearAnIndependentTransitionLock()
        {
            SceneInputLockState.SetLocked(true);
            var lease = SceneInputLockState.Acquire();
            SceneInputLockState.SetLocked(false);
            Assert.That(SceneInputLockState.IsLocked, Is.True);

            lease.Dispose();
            Assert.That(SceneInputLockState.IsLocked, Is.False);
        }

        [Test]
        public async Task InitialLoadFailure_RevealsAndUnlocksWithObservableFault()
        {
            var root = new GameObject("initial-load-failure-test");
            var routes = ScriptableObject.CreateInstance<SceneRouteCatalog>();
            try
            {
                var effects = root.AddComponent<UnitySceneFlowEffects>();
                var gate = root.AddComponent<PersistentInputGate>();
                var transition = root.AddComponent<TransitionCanvasPresenter>();
                SetField(effects, "routes", routes);
                SetField(effects, "inputGate", gate);
                SetField(effects, "transition", transition);

                try
                {
                    await effects.LoadInitialAsync(SceneId.GameStart, CancellationToken.None);
                    Assert.Fail("An unregistered initial route must fail.");
                }
                catch (InvalidOperationException exception)
                {
                    Assert.That(exception.Message, Does.Contain("No scene route"));
                }

                Assert.That(gate.IsLocked, Is.False);
                Assert.That(SceneInputLockState.IsLocked, Is.False);
                Assert.That(transition.IsCovered, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(routes);
            }
        }

        private static SceneTransitionRequest Request(
            long sequence,
            SceneId source,
            SceneId target,
            ISceneTransitionPayload payload)
        {
            return new SceneTransitionRequest(
                sequence,
                "request-" + sequence,
                source,
                target,
                payload);
        }

        private static CombatLaunchPayload Launch(
            string correlationId,
            string roomId,
            string runId = "run-1")
        {
            return new CombatLaunchPayload(
                correlationId,
                runId,
                731,
                1,
                1,
                1,
                roomId,
                "character-silver",
                0,
                "combat-vertical-slice",
                731,
                new[] { "lighting", "recover" });
        }

        private static RunStartPayload Start(string runId, int seed, int timecoins = 0)
        {
            return new RunStartPayload(
                RunStartKind.NewGame,
                runId,
                seed,
                seed.ToString(),
                1,
                1,
                1,
                timecoins,
                "character-silver",
                new[] { "lighting", "recover" });
        }

        private static void CommitStart(
            SceneFlowStateStore store,
            RunStartPayload start,
            long sequence)
        {
            var request = Request(
                sequence,
                SceneId.MainMenu,
                SceneId.OutOfBattleShell,
                start);
            store.Record(request);
            store.PrepareCommit(request);
            store.Commit(request);
        }

        private static int SeedWithAvailable(OverworldRoomType roomType)
        {
            for (var seed = 1; seed <= 1024; seed++)
            {
                var run = new OverworldRunApplication(
                    Start("seed-search", seed),
                    new OverworldMapGenerationConfig(1, 4, 2, 3),
                    finalChapter: 3);
                foreach (var roomId in run.GetAvailableRoomIds())
                {
                    if (run.GetRoomType(roomId) == roomType)
                    {
                        return seed;
                    }
                }
            }

            throw new AssertionException("No seed produced room type " + roomType + ".");
        }

        private static MapNodeId AvailableRoom(
            OverworldRunApplication run,
            OverworldRoomType roomType)
        {
            foreach (var roomId in run.GetAvailableRoomIds())
            {
                if (run.GetRoomType(roomId) == roomType)
                {
                    return roomId;
                }
            }

            throw new AssertionException("No available room of type " + roomType + ".");
        }

        private static CombatOutcome VictoryOutcome(
            CombatLaunchPayload launch,
            string correlationId)
        {
            var settlement = new BattleSettlementState(
                launch.BattleTag,
                launch.BattleSeed,
                new BattleRewardEntry("reward", BattleRewardKind.Acquire, "获得卡牌"));
            settlement.TryResolve(1, BattleOutcome.VictorySettlement);
            settlement.TryClaimReward(2);
            var boundary = settlement.TryCreateReturnBoundary(
                new BattleRoundLedger(1, 1, 0).Snapshot,
                launch.DeckStableIds);
            return CombatOutcome.TryCreate(
                correlationId,
                launch,
                settlement.Snapshot,
                boundary.Payload).Outcome;
        }

        private static CombatOutcome DefeatOutcome(
            CombatLaunchPayload launch,
            string correlationId)
        {
            var settlement = new BattleSettlementState(
                launch.BattleTag,
                launch.BattleSeed,
                new BattleRewardEntry("reward", BattleRewardKind.Acquire, "获得卡牌"));
            settlement.TryResolve(1, BattleOutcome.Defeat);
            var boundary = settlement.TryCreateReturnBoundary(
                new BattleRoundLedger(1, 1, 0).Snapshot,
                launch.DeckStableIds);
            return CombatOutcome.TryCreate(
                correlationId,
                launch,
                settlement.Snapshot,
                boundary.Payload).Outcome;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + fieldName);
            field.SetValue(target, value);
        }

        private sealed class ThrowBeforeReplace : IOverworldSaveWriteFaultInjector
        {
            public void OnStage(OverworldSaveWriteStage stage, string temporaryPath)
            {
                if (stage == OverworldSaveWriteStage.BeforeReplace)
                {
                    throw new IOException("Injected replace failure.");
                }
            }
        }
    }
}
