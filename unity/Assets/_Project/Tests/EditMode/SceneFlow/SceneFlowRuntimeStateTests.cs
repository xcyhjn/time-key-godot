using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition.SceneFlow;
using TimeKey.Domain.BattleFlow;
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
    }
}
