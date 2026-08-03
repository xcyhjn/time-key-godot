using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition;
using TimeKey.Composition.SceneFlow;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Overworld;
using TimeKey.Presentation;
using TimeKey.Presentation.BattleFlow;
using TimeKey.Presentation.GameOver;
using TimeKey.Presentation.MainMenu;
using TimeKey.Presentation.OutOfBattleShell;
using TimeKey.Presentation.OverworldMovement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TimeKey.Tests.PlayMode.SceneFlow
{
    public sealed class CombatShellGateDRoundTripTests
    {
        private const float TimeoutSeconds = 25f;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneInputLockState.SetLocked(false);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneInputLockState.SetLocked(false);
            yield return null;
        }

        [UnityTest]
        public IEnumerator VictoryReward_ReturnsToSameRunAndSettlesRoomOnce()
        {
            BootstrapRoot bootstrap = null;
            yield return StartNewRun(value => bootstrap = value);
            var store = Object.FindAnyObjectByType<SceneFlowStateStore>();
            Assert.That(store, Is.Not.Null);
            var runId = store.OutOfBattleState.RunId;
            var runSeed = store.OutOfBattleState.RunSeed;

            yield return LaunchRoom(bootstrap);
            var launch = store.ActiveLaunch;
            Assert.That(launch, Is.Not.Null);
            Assert.That(launch.RunId, Is.EqualTo(runId));
            Assert.That(launch.RunSeed, Is.EqualTo(runSeed));
            Assert.That(launch.RoomId, Does.StartWith("chapter-01-layer-01-node-"));

            var composition = Object.FindAnyObjectByType<CombatCompositionRoot>();
            var controller = Object.FindAnyObjectByType<VerticalSliceController>();
            Assert.That(composition, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(composition.ActiveLaunch, Is.SameAs(launch));
            Assert.That(controller.BattleFlow.Era, Is.EqualTo(launch.Era));
            Assert.That(controller.BattleFlow.Phase, Is.EqualTo(launch.Phase));
            Assert.That(controller.BattleFlow.Timecoins, Is.EqualTo(launch.Timecoins));
            Assert.That(CaptureDeck(controller), Is.EquivalentTo(launch.DeckStableIds));

            var resolution = controller.ResolveBattleOutcome(BattleOutcome.VictorySettlement);
            Assert.That(resolution.Succeeded, Is.True);
            var settlement = Object.FindAnyObjectByType<BattleSettlementPresenter>();
            Assert.That(settlement, Is.Not.Null);
            Assert.That(settlement.IsRewardVisible, Is.True);
            var reward = FindButton("RewardButton");
            reward.onClick.Invoke();
            reward.onClick.Invoke();

            yield return AwaitScene(bootstrap, SceneId.OutOfBattleShell);
            Assert.That(store.ActiveLaunch, Is.Null);
            Assert.That(store.LastOutcome, Is.Not.Null);
            Assert.That(store.LastOutcome.RunId, Is.EqualTo(runId));
            Assert.That(store.LastOutcome.RoomId, Is.EqualTo(launch.RoomId));
            Assert.That(store.LastOutcome.LaunchCorrelationId,
                Is.EqualTo(launch.LaunchCorrelationId));
            Assert.That(store.LastOutcomeApplyResult.Succeeded, Is.True);
            Assert.That(store.LastOutcomeApplyResult.WasAlreadyApplied, Is.False);
            Assert.That(store.OutOfBattleState.RunId, Is.EqualTo(runId));
            Assert.That(store.OutOfBattleState.RunSeed, Is.EqualTo(runSeed));
            Assert.That(store.OutOfBattleState.SettledRoomIds, Does.Contain(launch.RoomId));
            Assert.That(store.OutOfBattleState.SettledRoomIds, Has.Count.EqualTo(2));

            var shell = Object.FindAnyObjectByType<OutOfBattleShellPresenter>();
            Assert.That(shell, Is.Not.Null);
            Assert.That(shell.RoomState, Is.EqualTo(OutOfBattleRoomState.Settled));
            AssertPersistentTopology(SceneId.OutOfBattleShell);
        }

        [UnityTest]
        public IEnumerator Defeat_GoesToGameOverThenClearsRunAtMainMenu()
        {
            BootstrapRoot bootstrap = null;
            yield return StartNewRun(value => bootstrap = value);
            var store = Object.FindAnyObjectByType<SceneFlowStateStore>();
            var runId = store.OutOfBattleState.RunId;
            yield return LaunchRoom(bootstrap);

            var controller = Object.FindAnyObjectByType<VerticalSliceController>();
            var resolution = controller.ResolveBattleOutcome(BattleOutcome.Defeat);
            Assert.That(resolution.Succeeded, Is.True);
            var settlement = Object.FindAnyObjectByType<BattleSettlementPresenter>();
            Assert.That(settlement.IsRewardVisible, Is.False);

            yield return AwaitScene(bootstrap, SceneId.GameOver);
            Assert.That(store.LastOutcome, Is.Not.Null);
            Assert.That(store.LastOutcome.RunId, Is.EqualTo(runId));
            Assert.That(store.LastOutcome.TargetScene, Is.EqualTo(SceneId.GameOver));
            Assert.That(store.ActiveLaunch, Is.Null);
            Assert.That(store.OutOfBattleState.SettledRoomIds, Has.Count.EqualTo(1));
            Assert.That(
                store.OutOfBattleState.SettledRoomIds,
                Does.Not.Contain(store.LastOutcome.RoomId));
            Assert.That(Object.FindAnyObjectByType<GameOverPresenter>(), Is.Not.Null);
            AssertPersistentTopology(SceneId.GameOver);

            FindButton("ReturnButton").onClick.Invoke();
            yield return AwaitScene(bootstrap, SceneId.MainMenu);
            Assert.That(store.OutOfBattleState, Is.Null);
            Assert.That(store.ActiveLaunch, Is.Null);
            Assert.That(store.LastOutcome, Is.Null);
            Assert.That(store.LastOutcomeApplyResult, Is.Null);
            Assert.That(Object.FindAnyObjectByType<MainMenuPresenter>(), Is.Not.Null);
            AssertPersistentTopology(SceneId.MainMenu);
        }

        private static IEnumerator StartNewRun(Action<BootstrapRoot> capture)
        {
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;
            var bootstrap = Object.FindAnyObjectByType<BootstrapRoot>();
            Assert.That(bootstrap, Is.Not.Null);
            yield return AwaitTask(bootstrap.InitializationTask, "Bootstrap initialization");
            yield return TransitionToMainMenu(bootstrap);

            var presenter = Object.FindAnyObjectByType<MainMenuPresenter>();
            Assert.That(presenter, Is.Not.Null);
            var newGame = Array.Find(
                presenter.GetComponentsInChildren<Button>(true),
                button => button.name == "NewGame");
            Assert.That(newGame, Is.Not.Null);
            newGame.onClick.Invoke();
            yield return AwaitScene(bootstrap, SceneId.OutOfBattleShell);
            capture(bootstrap);
        }

        private static IEnumerator TransitionToMainMenu(BootstrapRoot bootstrap)
        {
            var sequence = bootstrap.ReserveTransitionSequence();
            var task = bootstrap.TransitionAsync(
                new SceneTransitionRequest(
                    sequence,
                    "gate-d-main-menu-" + sequence,
                    SceneId.GameStart,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)));
            yield return AwaitTask(task, "GameStart to MainMenu");
            Assert.That(task.IsFaulted, Is.False, task.Exception?.ToString());
            Assert.That(task.Result.Succeeded, Is.True, task.Result.Message);
        }

        private static IEnumerator LaunchRoom(BootstrapRoot bootstrap)
        {
            var movement = Object.FindAnyObjectByType<OverworldMovementPresenter>();
            Assert.That(movement, Is.Not.Null);
            var target = movement.GetComponentsInChildren<OverworldNodeView>(true)
                .FirstOrDefault(view =>
                    view.State == OverworldNodeVisualState.Available &&
                    (view.RoomType == OverworldRoomType.Battle ||
                     view.RoomType == OverworldRoomType.Elite));
            Assert.That(target, Is.Not.Null, "The generated map has no available combat room.");
            target.GetComponent<Button>().onClick.Invoke();
            var selectionDeadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (movement.IsMoving)
            {
                if (Time.realtimeSinceStartup >= selectionDeadline)
                {
                    Assert.Fail("Timed out selecting generated combat room.");
                }

                yield return null;
            }

            var room = FindButton("CombatRoom");
            room.onClick.Invoke();
            yield return null;
            var confirm = FindButton("ConfirmButton");
            Assert.That(confirm.interactable, Is.True);
            confirm.onClick.Invoke();
            confirm.onClick.Invoke();
            yield return AwaitScene(bootstrap, SceneId.Combat);
        }

        private static Button FindButton(string name)
        {
            var button = Array.Find(
                Object.FindObjectsByType<Button>(FindObjectsInactive.Include),
                candidate => candidate.name == name && candidate.gameObject.activeInHierarchy);
            Assert.That(button, Is.Not.Null, "Missing active button: " + name);
            return button;
        }

        private static IReadOnlyList<string> CaptureDeck(VerticalSliceController controller)
        {
            return controller.BattleFlow.DrawPile
                .Concat(controller.BattleFlow.Hand)
                .Concat(controller.BattleFlow.DiscardPile)
                .Select(card => card.StableId)
                .ToArray();
        }

        private static IEnumerator AwaitScene(BootstrapRoot bootstrap, SceneId sceneId)
        {
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (bootstrap.CurrentScene != sceneId || SceneInputLockState.IsLocked)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail("Timed out waiting for scene " + sceneId + ".");
                }

                yield return null;
            }

            yield return null;
            AssertPersistentTopology(sceneId);
        }

        private static IEnumerator AwaitTask(Task task, string operation)
        {
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!task.IsCompleted)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail(operation + " exceeded " + TimeoutSeconds + " seconds.");
                }

                yield return null;
            }
        }

        private static void AssertPersistentTopology(SceneId expectedScene)
        {
            Assert.That(Object.FindObjectsByType<BootstrapRoot>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<PersistentInputGate>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
            var entries = Object.FindObjectsByType<SceneContentEntry>(
                FindObjectsInactive.Include);
            Assert.That(entries, Has.Length.EqualTo(1));
            Assert.That(entries[0].SceneId, Is.EqualTo(expectedScene));
            Assert.That(entries[0].IsBound, Is.True);
            Assert.That(entries[0].IsInteractive, Is.True);
        }
    }
}
