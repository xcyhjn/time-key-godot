using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using TimeKey.Application.EraClock;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition.SceneFlow;
using TimeKey.Domain.BattleFlow;
using TimeKey.Presentation;
using TimeKey.Presentation.BattleFlow;
using TimeKey.Presentation.EraClock;
using TimeKey.Presentation.MainMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TimeKey.Tests.PlayMode.SceneFlow
{
    public sealed class CombatShellGateEStabilityTests
    {
        private const float TimeoutSeconds = 30f;
        private int _previousWidth;
        private int _previousHeight;
        private bool _previousFullscreen;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousWidth = Screen.width;
            _previousHeight = Screen.height;
            _previousFullscreen = Screen.fullScreen;
            SceneInputLockState.SetLocked(false);
            yield return DestroyPersistentBootstraps();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneInputLockState.SetLocked(false);
            yield return DestroyPersistentBootstraps();
            if (_previousWidth > 0 && _previousHeight > 0)
            {
                Screen.SetResolution(_previousWidth, _previousHeight, _previousFullscreen);
            }

            yield return null;
        }

        private static IEnumerator DestroyPersistentBootstraps()
        {
            var bootstraps = Object.FindObjectsByType<BootstrapRoot>(
                FindObjectsInactive.Include);
            foreach (var bootstrap in bootstraps)
            {
                Object.Destroy(bootstrap.gameObject);
            }

            if (bootstraps.Length > 0)
            {
                yield return null;
            }
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator ThreeVictoryCycles_KeepIdentityTopologyMemoryAndResizeStable()
        {
            Screen.SetResolution(1280, 720, false);
            yield return null;
            var bootstrapTask = StartNewRun();
            yield return AwaitTask(bootstrapTask, "start run");
            var bootstrap = bootstrapTask.Result;
            var store = Object.FindAnyObjectByType<SceneFlowStateStore>();
            Assert.That(store, Is.Not.Null);
            yield return AwaitEraClock(
                store.OutOfBattleState.Era,
                store.OutOfBattleState.Phase);
            var runId = store.OutOfBattleState.RunId;
            var memory = new List<long>();

            for (var cycle = 1; cycle <= 3; cycle++)
            {
                var roomId = "combat-room-e-" + cycle;
                var launch = store.OutOfBattleState.CreateCombatLaunch(
                    "gate-e-launch-" + cycle,
                    roomId,
                    "combat-vertical-slice",
                    731 + cycle);
                var transition = Transition(
                    bootstrap,
                    SceneId.OutOfBattleShell,
                    SceneId.Combat,
                    launch,
                    "gate-e-enter-" + cycle);
                yield return AwaitTask(transition, "cycle " + cycle + " enter combat");
                Assert.That(transition.Result.Succeeded, Is.True, transition.Result.Message);
                yield return AwaitScene(bootstrap, SceneId.Combat);
                yield return AwaitEraClock(
                    store.OutOfBattleState.Era,
                    store.OutOfBattleState.Phase);

                Assert.That(store.ActiveLaunch, Is.Not.Null);
                Assert.That(store.ActiveLaunch.LaunchCorrelationId,
                    Is.EqualTo(launch.LaunchCorrelationId));
                var controller = Object.FindAnyObjectByType<VerticalSliceController>();
                Assert.That(controller, Is.Not.Null);
                var controllerId = controller.GetInstanceID();
                var resolution = controller.ResolveBattleOutcome(
                    BattleOutcome.VictorySettlement);
                Assert.That(resolution.Succeeded, Is.True);
                var reward = FindButton("RewardButton");
                reward.onClick.Invoke();
                reward.onClick.Invoke();

                yield return AwaitScene(bootstrap, SceneId.OutOfBattleShell);
                yield return AwaitEraClock(
                    store.OutOfBattleState.Era,
                    store.OutOfBattleState.Phase);
                Assert.That(store.ActiveLaunch, Is.Null);
                Assert.That(store.LastOutcome, Is.Not.Null);
                Assert.That(store.LastOutcome.RunId, Is.EqualTo(runId));
                Assert.That(store.LastOutcome.RoomId, Is.EqualTo(roomId));
                Assert.That(store.LastOutcome.LaunchCorrelationId,
                    Is.EqualTo(launch.LaunchCorrelationId));
                Assert.That(store.OutOfBattleState.SettledRoomIds,
                    Has.Count.EqualTo(cycle + 1));
                Assert.That(store.OutOfBattleState.SettledRoomIds.Distinct().Count(),
                    Is.EqualTo(cycle + 1));
                Assert.That(Object.FindObjectsByType<VerticalSliceController>(
                    FindObjectsInactive.Include).Any(item => item.GetInstanceID() == controllerId),
                    Is.False);
                AssertPersistentTopology(SceneId.OutOfBattleShell);

                yield return Resources.UnloadUnusedAssets();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                memory.Add(Profiler.GetTotalAllocatedMemoryLong());
            }

            Assert.That(memory[2] - memory[0], Is.LessThan(128L * 1024L * 1024L));
            const long materialGrowthThresholdBytes = 1024L * 1024L;
            Assert.That(
                memory[1] - memory[0] > materialGrowthThresholdBytes &&
                memory[2] - memory[1] > materialGrowthThresholdBytes,
                Is.False,
                "Post-GC memory must not materially grow in every completed cycle.");
            var firstGrowth = memory[1] - memory[0];
            var secondGrowth = memory[2] - memory[1];
            Assert.That(
                firstGrowth > 0L &&
                secondGrowth > 0L &&
                secondGrowth * 2L >= firstGrowth,
                Is.False,
                "Post-GC memory growth must converge rather than continue at a stable slope.");

            var roomButton = FindButton("CombatRoom");
            roomButton.Select();
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.EqualTo(roomButton.gameObject));
            Screen.SetResolution(2560, 1080, false);
            yield return null;
            yield return null;
            Assert.That(SceneInputLockState.IsLocked, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.EqualTo(roomButton.gameObject));
            var canvas = Object.FindAnyObjectByType<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.pixelRect.width, Is.GreaterThan(0f));
            Assert.That(canvas.pixelRect.height, Is.GreaterThan(0f));
            AssertPersistentTopology(SceneId.OutOfBattleShell);
        }

        private static async Task<BootstrapRoot> StartNewRun()
        {
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            BootstrapRoot bootstrap = null;
            for (var frame = 0; frame < 120 && bootstrap == null; frame++)
            {
                await Task.Yield();
                bootstrap = Object.FindAnyObjectByType<BootstrapRoot>();
            }

            if (bootstrap == null)
            {
                throw new InvalidOperationException("Bootstrap was not loaded.");
            }

            await bootstrap.InitializationTask;
            var sequence = bootstrap.ReserveTransitionSequence();
            var menu = await bootstrap.TransitionAsync(
                new SceneTransitionRequest(
                    sequence,
                    "gate-e-stability-menu-" + sequence,
                    SceneId.GameStart,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)));
            if (!menu.Succeeded)
            {
                throw new InvalidOperationException(menu.Message);
            }

            var presenter = Object.FindAnyObjectByType<MainMenuPresenter>();
            var newGame = presenter.GetComponentsInChildren<Button>(true)
                .First(button => button.name == "NewGame");
            newGame.onClick.Invoke();
            while (bootstrap.CurrentScene != SceneId.OutOfBattleShell ||
                   SceneInputLockState.IsLocked)
            {
                await Task.Yield();
            }

            return bootstrap;
        }

        private static Task<SceneTransitionResult> Transition(
            BootstrapRoot bootstrap,
            SceneId source,
            SceneId target,
            ISceneTransitionPayload payload,
            string correlation)
        {
            var sequence = bootstrap.ReserveTransitionSequence();
            return bootstrap.TransitionAsync(
                new SceneTransitionRequest(
                    sequence,
                    correlation + "-" + sequence,
                    source,
                    target,
                    payload));
        }

        private static Button FindButton(string name)
        {
            var button = Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                .FirstOrDefault(candidate =>
                    candidate.name == name && candidate.gameObject.activeInHierarchy);
            Assert.That(button, Is.Not.Null, "Missing active button: " + name);
            return button;
        }

        private static IEnumerator AwaitScene(BootstrapRoot bootstrap, SceneId sceneId)
        {
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (bootstrap.CurrentScene != sceneId || SceneInputLockState.IsLocked)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail("Timed out waiting for " + sceneId + ".");
                }

                yield return null;
            }

            yield return null;
        }

        private static IEnumerator AwaitTask(Task task, string operation)
        {
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!task.IsCompleted)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail(operation + " timed out.");
                }

                yield return null;
            }

            Assert.That(task.IsFaulted, Is.False, task.Exception?.ToString());
        }

        private static IEnumerator AwaitEraClock(int era, int phase)
        {
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                var clocks = Object.FindObjectsByType<EraClockPresenter>(
                    FindObjectsInactive.Include);
                if (clocks.Length == 1 &&
                    clocks[0].CurrentSnapshot != null &&
                    clocks[0].CurrentSnapshot.Era == era &&
                    clocks[0].CurrentSnapshot.Phase == phase &&
                    clocks[0].CurrentSnapshot.AnchorTarget == EraClockAnchorTarget.Hud &&
                    clocks[0].State == EraClockPresenterState.Settled)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Timed out waiting for the formal EraClock HUD state.");
        }

        private static void AssertPersistentTopology(SceneId expectedScene)
        {
            Assert.That(SceneManager.sceneCount, Is.EqualTo(2));
            Assert.That(Object.FindObjectsByType<BootstrapRoot>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<UnitySceneFlowEffects>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<PersistentInputGate>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<TransitionCanvasPresenter>(
                FindObjectsInactive.Include), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EraClockPresenter>(FindObjectsInactive.Include),
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
