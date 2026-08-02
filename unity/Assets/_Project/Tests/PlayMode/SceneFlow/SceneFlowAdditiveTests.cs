using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition.SceneFlow;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Deck;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using InvalidOperationException = System.InvalidOperationException;

namespace TimeKey.Tests.PlayMode.SceneFlow
{
    public sealed class SceneFlowAdditiveTests
    {
        private const float TaskTimeoutSeconds = 15f;

        [UnityTest]
        public IEnumerator Bootstrap_PerformsTypedRoundTripsWithoutPersistentDuplicates()
        {
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;
            var bootstrap = Object.FindAnyObjectByType<BootstrapRoot>();
            Assert.That(bootstrap, Is.Not.Null);
            var initialization = bootstrap.InitializationTask;
            yield return AwaitTask(initialization, "Bootstrap initialization");
            Assert.That(initialization.IsFaulted, Is.False, initialization.Exception?.ToString());
            Assert.That(bootstrap.IsReady, Is.True);

            AssertPersistentState(SceneId.GameStart);

            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    1,
                    "start-to-menu",
                    SceneId.GameStart,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)));
            AssertPersistentState(SceneId.MainMenu);
            var menu = Object.FindAnyObjectByType<TimeKey.Presentation.MainMenu.MainMenuPresenter>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.IsEntranceComplete, Is.True);

            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    2,
                    "menu-to-shell",
                    SceneId.MainMenu,
                    SceneId.OutOfBattleShell,
                    Start("gate-a-run")));
            AssertPersistentState(SceneId.OutOfBattleShell);

            var victoryLaunch = Launch("victory", "room-victory");
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    3,
                    victoryLaunch.LaunchCorrelationId,
                    SceneId.OutOfBattleShell,
                    SceneId.Combat,
                    victoryLaunch));
            AssertPersistentState(SceneId.Combat);

            var victory = Outcome(victoryLaunch, BattleOutcome.VictorySettlement);
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    4,
                    victory.OutcomeCorrelationId,
                    SceneId.Combat,
                    SceneId.OutOfBattleShell,
                    victory));
            AssertPersistentState(SceneId.OutOfBattleShell);
            var stateStore = Object.FindAnyObjectByType<SceneFlowStateStore>();
            Assert.That(stateStore, Is.Not.Null);
            Assert.That(stateStore.LastOutcome, Is.SameAs(victory));
            Assert.That(stateStore.LastOutcomeApplyResult.Succeeded, Is.True);
            Assert.That(stateStore.LastOutcomeApplyResult.WasAlreadyApplied, Is.False);
            Assert.That(stateStore.OutOfBattleState.SettledRoomIds,
                Does.Contain(victoryLaunch.RoomId));

            var rejectedLaunch = Launch("settled-retry", victoryLaunch.RoomId);
            yield return TransitionFailure(
                bootstrap,
                new SceneTransitionRequest(
                    5,
                    rejectedLaunch.LaunchCorrelationId,
                    SceneId.OutOfBattleShell,
                    SceneId.Combat,
                    rejectedLaunch),
                SceneTransitionPhase.BindingPayload);
            AssertPersistentState(SceneId.OutOfBattleShell);
            Assert.That(stateStore.LastOutcome, Is.SameAs(victory));
            Assert.That(stateStore.LastPayload, Is.SameAs(victory));

            var defeatLaunch = Launch("defeat", "room-defeat");
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    6,
                    defeatLaunch.LaunchCorrelationId,
                    SceneId.OutOfBattleShell,
                    SceneId.Combat,
                    defeatLaunch));
            var defeat = Outcome(defeatLaunch, BattleOutcome.Defeat);
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    7,
                    defeat.OutcomeCorrelationId,
                    SceneId.Combat,
                    SceneId.GameOver,
                    defeat));
            AssertPersistentState(SceneId.GameOver);

            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    8,
                    "game-over-to-menu",
                    SceneId.GameOver,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)));
            AssertPersistentState(SceneId.MainMenu);
            Assert.That(stateStore.OutOfBattleState, Is.Null);
            Assert.That(stateStore.ActiveLaunch, Is.Null);

            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    9,
                    "new-run-shell",
                    SceneId.MainMenu,
                    SceneId.OutOfBattleShell,
                    Start("gate-a-run-2")));
            var newRunLaunch = Launch("new-run", "room-new", "gate-a-run-2");
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    10,
                    newRunLaunch.LaunchCorrelationId,
                    SceneId.OutOfBattleShell,
                    SceneId.Combat,
                    newRunLaunch));
            AssertPersistentState(SceneId.Combat);
            Assert.That(stateStore.OutOfBattleState.RunId, Is.EqualTo("gate-a-run-2"));
        }

        [UnityTest]
        public IEnumerator Bootstrap_InitialFailureIsObservableAndRestoresCoverAndInput()
        {
            var root = new GameObject("BootstrapFailureFixture");
            root.SetActive(false);
            var routes = ScriptableObject.CreateInstance<SceneRouteCatalog>();
            var gate = root.AddComponent<PersistentInputGate>();
            var transition = root.AddComponent<TransitionCanvasPresenter>();
            var stateStore = root.AddComponent<SceneFlowStateStore>();
            var effects = root.AddComponent<UnitySceneFlowEffects>();
            var bootstrap = root.AddComponent<BootstrapRoot>();
            SetField(effects, "routes", routes);
            SetField(effects, "inputGate", gate);
            SetField(effects, "transition", transition);
            SetField(effects, "stateStore", stateStore);
            SetField(bootstrap, "effects", effects);

            root.SetActive(true);
            var initialization = bootstrap.InitializationTask;
            yield return AwaitTask(initialization, "Expected Bootstrap initialization fault");

            Assert.That(initialization.IsFaulted, Is.True);
            Assert.That(bootstrap.InitializationException, Is.TypeOf<InvalidOperationException>());
            Assert.That(bootstrap.IsReady, Is.False);
            Assert.That(gate.IsLocked, Is.False);
            Assert.That(SceneInputLockState.IsLocked, Is.False);
            Assert.That(transition.IsCovered, Is.False);

            Object.Destroy(root);
            Object.Destroy(routes);
            yield return null;
        }

        private static IEnumerator Transition(
            BootstrapRoot bootstrap,
            SceneTransitionRequest request)
        {
            Task<SceneTransitionResult> task = bootstrap.TransitionAsync(request);
            yield return AwaitTask(task, "Scene transition " + request.CorrelationId);

            Assert.That(task.IsFaulted, Is.False, task.Exception?.ToString());
            Assert.That(task.IsCanceled, Is.False);
            Assert.That(task.Result.Succeeded, Is.True, task.Result.Message);
            Assert.That(task.Result.IsInputLocked, Is.False);
            Assert.That(task.Result.ActiveScene, Is.EqualTo(request.Target));
        }

        private static IEnumerator TransitionFailure(
            BootstrapRoot bootstrap,
            SceneTransitionRequest request,
            SceneTransitionPhase expectedPhase)
        {
            Task<SceneTransitionResult> task = bootstrap.TransitionAsync(request);
            yield return AwaitTask(task, "Expected scene transition failure " + request.CorrelationId);

            Assert.That(task.IsFaulted, Is.False, task.Exception?.ToString());
            Assert.That(task.IsCanceled, Is.False);
            Assert.That(task.Result.Succeeded, Is.False);
            Assert.That(task.Result.Failure, Is.EqualTo(SceneTransitionFailure.EffectFailed));
            Assert.That(task.Result.FailedPhase, Is.EqualTo(expectedPhase));
            Assert.That(task.Result.ActiveScene, Is.EqualTo(request.Source));
            Assert.That(task.Result.IsInputLocked, Is.False);
        }

        private static IEnumerator AwaitTask(Task task, string operation)
        {
            var deadline = Time.realtimeSinceStartup + TaskTimeoutSeconds;
            while (!task.IsCompleted)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail(operation + " exceeded " + TaskTimeoutSeconds + " seconds.");
                }

                yield return null;
            }
        }

        private static void AssertPersistentState(SceneId currentScene)
        {
            Assert.That(Object.FindObjectsByType<BootstrapRoot>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<UnitySceneFlowEffects>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<TransitionCanvasPresenter>(
                    FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));

            var entries = Object.FindObjectsByType<SceneContentEntry>(FindObjectsInactive.Include);
            Assert.That(entries, Has.Length.EqualTo(1));
            Assert.That(entries[0].SceneId, Is.EqualTo(currentScene));
            Assert.That(entries[0].IsBound, Is.True);
            Assert.That(entries[0].IsInteractive, Is.True);

            var loadedScenes = 0;
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                if (SceneManager.GetSceneAt(index).isLoaded)
                {
                    loadedScenes++;
                }
            }

            Assert.That(loadedScenes, Is.EqualTo(2));
        }

        private static CombatLaunchPayload Launch(
            string identity,
            string roomId,
            string runId = "gate-a-run")
        {
            return new CombatLaunchPayload(
                identity + "-launch",
                runId,
                731,
                1,
                1,
                1,
                roomId,
                "silver-character",
                0,
                "combat-vertical-slice",
                731,
                StarterDeck.OrderedStableIds);
        }

        private static RunStartPayload Start(string runId)
        {
            return new RunStartPayload(
                RunStartKind.NewGame,
                runId,
                731,
                "731",
                1,
                1,
                1,
                0,
                StarterDeck.OrderedStableIds);
        }

        private static CombatOutcome Outcome(
            CombatLaunchPayload launch,
            BattleOutcome battleOutcome)
        {
            var settlement = new BattleSettlementState(
                launch.BattleTag,
                launch.BattleSeed,
                new BattleRewardEntry(
                    "gate-a/acquire-card",
                    BattleRewardKind.Acquire,
                    "获得卡牌"));
            settlement.TryResolve(1, battleOutcome);
            if (battleOutcome == BattleOutcome.VictorySettlement)
            {
                settlement.TryClaimReward(2);
            }

            var boundary = settlement.TryCreateReturnBoundary(
                new BattleRoundLedger(1, 1, 0).Snapshot,
                launch.DeckStableIds);
            var result = CombatOutcome.TryCreate(
                launch.LaunchCorrelationId + "-outcome",
                launch,
                settlement.Snapshot,
                boundary.Payload);
            Assert.That(result.Succeeded, Is.True);
            return result.Outcome;
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
