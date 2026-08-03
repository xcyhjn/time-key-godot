using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Deck;
using TimeKey.Presentation;
using TimeKey.Presentation.BattleFlow;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace TimeKey.Composition.SceneFlow
{
    [DisallowMultipleComponent]
    public sealed class SceneFlowPlayerSmoke : MonoBehaviour
    {
        public const string CommandLineFlag = "-timekeyCombatShellSmoke";
        public const string PassMarker = "TIMEKEY_COMBAT_SHELL_GATE_A_PLAYER_SMOKE_PASS";
        public const string GateECommandLineFlag = "-timekeyCombatShellGateESmoke";
        public const string GateEPassMarker =
            "TIMEKEY_COMBAT_SHELL_GATE_E_PLAYER_SMOKE_PASS";

        [SerializeField] private BootstrapRoot bootstrap = null;

        private int _quitCountdown;
        private int _exitCode;

        private async void Start()
        {
            var gateE = HasCommandLineFlag(GateECommandLineFlag);
            if (!gateE && !HasCommandLineFlag(CommandLineFlag))
            {
                return;
            }

            try
            {
                await bootstrap.InitializationTask;
                if (!bootstrap.IsReady)
                {
                    throw new InvalidOperationException(
                        "Bootstrap initialization completed without a ready scene.");
                }

                if (gateE)
                {
                    await RunGateESmokeAsync();
                    Debug.Log(GateEPassMarker);
                    RequestQuit(0, 30);
                    return;
                }

                await Transition(
                    1,
                    "smoke-start-menu",
                    SceneId.GameStart,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu));
                await Transition(
                    2,
                    "smoke-menu-shell",
                    SceneId.MainMenu,
                    SceneId.OutOfBattleShell,
                    new RunStartPayload(
                        RunStartKind.NewGame,
                        "player-smoke-run",
                        731,
                        "731",
                        1,
                        1,
                        1,
                        0,
                        StarterDeck.OrderedStableIds));

                var launch = Launch();
                await Transition(
                    3,
                    launch.LaunchCorrelationId,
                    SceneId.OutOfBattleShell,
                    SceneId.Combat,
                    launch);
                var outcome = VictoryOutcome(launch);
                await Transition(
                    4,
                    outcome.OutcomeCorrelationId,
                    SceneId.Combat,
                    SceneId.OutOfBattleShell,
                    outcome);

                Debug.Log(PassMarker);
                RequestQuit(0);
            }
            catch (Exception exception)
            {
                if (gateE)
                {
                    Debug.LogError("TIMEKEY_COMBAT_SHELL_GATE_E_PLAYER_SMOKE_FAIL");
                }

                Debug.LogException(exception);
                RequestQuit(1);
            }
        }

        private async Task RunGateESmokeAsync()
        {
            await AwaitSceneAsync(SceneId.MainMenu, 30f);
            Screen.SetResolution(1280, 720, false);
            await WaitFramesAsync(3);
            var evidence = EvidenceDirectory();
            Directory.CreateDirectory(evidence);
            DeleteGateEOutput(evidence, "player-smoke-summary.json");
            DeleteGateEOutput(evidence, "player-main-menu-1280x720.png");
            DeleteGateEOutput(evidence, "player-out-of-battle-1280x720.png");
            DeleteGateEOutput(evidence, "player-combat-1280x720.png");
            DeleteGateEOutput(evidence, "player-victory-1280x720.png");
            DeleteGateEOutput(evidence, "player-returned-shell-2560x1080.png");
            await CapturePlayerScreenshotAsync(
                Path.Combine(evidence, "player-main-menu-1280x720.png"));

            await GateETransitionAsync(
                SceneId.MainMenu,
                SceneId.OutOfBattleShell,
                new RunStartPayload(
                    RunStartKind.NewGame,
                    "player-gate-e-run",
                    731,
                    "731",
                    1,
                    1,
                    1,
                    0,
                    "silver-character",
                    StarterDeck.OrderedStableIds),
                "player-gate-e-start");
            await AwaitSceneAsync(SceneId.OutOfBattleShell, 30f);
            await CapturePlayerScreenshotAsync(
                Path.Combine(evidence, "player-out-of-battle-1280x720.png"));

            var store = FindFirstObjectByType<SceneFlowStateStore>();
            if (store?.OutOfBattleState == null)
            {
                throw new InvalidOperationException(
                    "Gate E Player smoke requires the typed out-of-battle state.");
            }

            var summary = new GateEPlayerSummary
            {
                status = "passed",
                unityVersion = UnityEngine.Application.unityVersion,
                renderer = SystemInfo.graphicsDeviceType.ToString(),
                runId = store.OutOfBattleState.RunId,
                cycles = new List<GateECycleSummary>()
            };
            using (var renderCounterRecorder = StartRenderCounter(out var renderCounter))
            {
                if (!renderCounterRecorder.Valid)
                {
                    throw new InvalidOperationException(
                        "Gate E Player smoke could not attach a render counter.");
                }

                summary.renderCounter = renderCounter;
                for (var cycle = 1; cycle <= 3; cycle++)
                {
                    var roomId = "player-gate-e-room-" + cycle;
                    var launch = store.OutOfBattleState.CreateCombatLaunch(
                        "player-gate-e-launch-" + cycle,
                        roomId,
                        "combat-vertical-slice",
                        731 + cycle);
                    var stopwatch = Stopwatch.StartNew();
                    await GateETransitionAsync(
                        SceneId.OutOfBattleShell,
                        SceneId.Combat,
                        launch,
                        "player-gate-e-enter-" + cycle);
                    await AwaitSceneAsync(SceneId.Combat, 30f);
                    AssertTopology(SceneId.Combat);
                    if (cycle == 1)
                    {
                        await CapturePlayerScreenshotAsync(
                            Path.Combine(evidence, "player-combat-1280x720.png"));
                    }

                    var controller = FindFirstObjectByType<VerticalSliceController>();
                    if (controller == null || store.ActiveLaunch == null ||
                        store.ActiveLaunch.LaunchCorrelationId != launch.LaunchCorrelationId)
                    {
                        throw new InvalidOperationException(
                            "Gate E Player smoke lost the active launch identity.");
                    }

                    var resolution = controller.ResolveBattleOutcome(
                        BattleOutcome.VictorySettlement);
                    if (!resolution.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Gate E Player smoke could not resolve Victory: " +
                            resolution.Failure + ".");
                    }

                    if (cycle == 1)
                    {
                        await CapturePlayerScreenshotAsync(
                            Path.Combine(evidence, "player-victory-1280x720.png"));
                    }

                    var reward = FindActiveButton("RewardButton");
                    reward.onClick.Invoke();
                    reward.onClick.Invoke();
                    await AwaitSceneAsync(SceneId.OutOfBattleShell, 30f);
                    stopwatch.Stop();
                    AssertTopology(SceneId.OutOfBattleShell);
                    if (store.ActiveLaunch != null || store.LastOutcome == null ||
                        store.LastOutcome.RoomId != roomId ||
                        store.OutOfBattleState.SettledRoomIds.Count != cycle)
                    {
                        throw new InvalidOperationException(
                            "Gate E Player smoke did not settle exactly one room in cycle " +
                            cycle + ".");
                    }

                    await UnloadUnusedAssetsAsync();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    FrameTimingManager.CaptureFrameTimings();
                    await WaitFramesAsync(3);
                    var timings = new FrameTiming[1];
                    var timingCount = FrameTimingManager.GetLatestTimings(1, timings);
                    var cycleSummary = new GateECycleSummary
                    {
                        cycle = cycle,
                        roomId = roomId,
                        launchCorrelationId = launch.LaunchCorrelationId,
                        outcomeCorrelationId = store.LastOutcome.OutcomeCorrelationId,
                        transitionMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                        cpuFrameMilliseconds = timingCount > 0 ? timings[0].cpuFrameTime : 0d,
                        gpuFrameMilliseconds = timingCount > 0 ? timings[0].gpuFrameTime : 0d,
                        renderCounterValue = renderCounterRecorder.LastValue,
                        totalAllocatedMemoryBytes = Profiler.GetTotalAllocatedMemoryLong(),
                        loadedSceneCount = SceneManager.sceneCount,
                        bootstrapCount = FindObjectsByType<BootstrapRoot>(
                            FindObjectsInactive.Include).Length,
                        contentEntryCount = FindObjectsByType<SceneContentEntry>(
                            FindObjectsInactive.Include).Length
                    };
                    summary.cycles.Add(cycleSummary);
                    Debug.Log(
                        "TIMEKEY_COMBAT_SHELL_GATE_E_CYCLE" +
                        "|cycle=" + cycleSummary.cycle +
                        "|room=" + cycleSummary.roomId +
                        "|launch=" + cycleSummary.launchCorrelationId +
                        "|outcome=" + cycleSummary.outcomeCorrelationId);
                    Debug.Log(
                        "TIMEKEY_COMBAT_SHELL_GATE_E_PERF" +
                        "|cycle=" + cycleSummary.cycle +
                        "|transitionMs=" +
                        cycleSummary.transitionMilliseconds.ToString("F2") +
                        "|cpuMs=" + cycleSummary.cpuFrameMilliseconds.ToString("F3") +
                        "|gpuMs=" + cycleSummary.gpuFrameMilliseconds.ToString("F3") +
                        "|renderCounterValue=" + cycleSummary.renderCounterValue +
                        "|renderCounter=" + renderCounter +
                        "|memoryBytes=" + cycleSummary.totalAllocatedMemoryBytes);
                    if (cycleSummary.renderCounterValue <= 0L)
                    {
                        throw new InvalidOperationException(
                            "Gate E Player smoke could not capture a positive render counter.");
                    }
                }
            }

            summary.memoryGrowthBytes =
                summary.cycles[2].totalAllocatedMemoryBytes -
                summary.cycles[0].totalAllocatedMemoryBytes;
            summary.strictlyMonotonicMemoryGrowth =
                summary.cycles[0].totalAllocatedMemoryBytes <
                summary.cycles[1].totalAllocatedMemoryBytes &&
                summary.cycles[1].totalAllocatedMemoryBytes <
                summary.cycles[2].totalAllocatedMemoryBytes;
            const long materialGrowthThresholdBytes = 1024L * 1024L;
            summary.materiallyMonotonicMemoryGrowth =
                summary.cycles[1].totalAllocatedMemoryBytes -
                summary.cycles[0].totalAllocatedMemoryBytes >
                materialGrowthThresholdBytes &&
                summary.cycles[2].totalAllocatedMemoryBytes -
                summary.cycles[1].totalAllocatedMemoryBytes >
                materialGrowthThresholdBytes;
            var firstGrowth =
                summary.cycles[1].totalAllocatedMemoryBytes -
                summary.cycles[0].totalAllocatedMemoryBytes;
            var secondGrowth =
                summary.cycles[2].totalAllocatedMemoryBytes -
                summary.cycles[1].totalAllocatedMemoryBytes;
            summary.sustainedMonotonicMemoryGrowth =
                firstGrowth > 0L &&
                secondGrowth > 0L &&
                secondGrowth * 2L >= firstGrowth;
            if (summary.materiallyMonotonicMemoryGrowth ||
                summary.sustainedMonotonicMemoryGrowth)
            {
                throw new InvalidOperationException(
                    "Gate E Player smoke detected sustained memory growth.");
            }

            if (summary.memoryGrowthBytes > 128L * 1024L * 1024L)
            {
                throw new InvalidOperationException(
                    "Gate E Player smoke exceeded the three-cycle memory budget.");
            }

            Screen.SetResolution(2560, 1080, false);
            await WaitFramesAsync(3);
            await CapturePlayerScreenshotAsync(
                Path.Combine(evidence, "player-returned-shell-2560x1080.png"));
            summary.finalInputLocked = SceneInputLockState.IsLocked;
            if (summary.finalInputLocked)
            {
                throw new InvalidOperationException(
                    "Gate E Player smoke ended with input locked.");
            }

            File.WriteAllText(
                Path.Combine(evidence, "player-smoke-summary.json"),
                JsonUtility.ToJson(summary, true));
            await WaitFramesAsync(30);
        }

        private static ProfilerRecorder StartRenderCounter(out string counterName)
        {
            var candidates = new[]
            {
                "Draw Calls Count",
                "Batches Count",
                "SetPass Calls Count"
            };
            foreach (var candidate in candidates)
            {
                var recorder = ProfilerRecorder.StartNew(
                    ProfilerCategory.Render,
                    candidate,
                    15);
                if (recorder.Valid)
                {
                    counterName = candidate;
                    return recorder;
                }

                recorder.Dispose();
            }

            counterName = "unavailable";
            return default;
        }

        private void LateUpdate()
        {
            if (_quitCountdown <= 0)
            {
                return;
            }

            _quitCountdown--;
            if (_quitCountdown == 0)
            {
                UnityEngine.Application.Quit(_exitCode);
            }
        }

        private void RequestQuit(int exitCode, int frameDelay = 2)
        {
            _exitCode = exitCode;
            _quitCountdown = Math.Max(2, frameDelay);
        }

        private async Task Transition(
            long sequence,
            string correlationId,
            SceneId source,
            SceneId target,
            ISceneTransitionPayload payload)
        {
            var result = await bootstrap.TransitionAsync(
                new SceneTransitionRequest(
                    sequence,
                    correlationId,
                    source,
                    target,
                    payload));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Player smoke transition failed at " + result.FailedPhase + ": " +
                    result.Failure + " / " + result.Message);
            }
        }

        private static CombatLaunchPayload Launch()
        {
            return new CombatLaunchPayload(
                "player-smoke-launch",
                "player-smoke-run",
                731,
                1,
                1,
                1,
                "player-smoke-room",
                "silver-character",
                0,
                "combat-vertical-slice",
                731,
                StarterDeck.OrderedStableIds);
        }

        private static CombatOutcome VictoryOutcome(CombatLaunchPayload launch)
        {
            var settlement = new BattleSettlementState(
                launch.BattleTag,
                launch.BattleSeed,
                new BattleRewardEntry(
                    "player-smoke/acquire-card",
                    BattleRewardKind.Acquire,
                    "获得卡牌"));
            settlement.TryResolve(1, BattleOutcome.VictorySettlement);
            settlement.TryClaimReward(2);
            var boundary = settlement.TryCreateReturnBoundary(
                new BattleRoundLedger(1, 1, 0).Snapshot,
                launch.DeckStableIds);
            var result = CombatOutcome.TryCreate(
                "player-smoke-outcome",
                launch,
                settlement.Snapshot,
                boundary.Payload);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Player smoke could not create the typed combat outcome: " + result.Failure + ".");
            }

            return result.Outcome;
        }

        private async Task GateETransitionAsync(
            SceneId source,
            SceneId target,
            ISceneTransitionPayload payload,
            string correlation)
        {
            var sequence = bootstrap.ReserveTransitionSequence();
            var result = await bootstrap.TransitionAsync(
                new SceneTransitionRequest(
                    sequence,
                    correlation + "-" + sequence,
                    source,
                    target,
                    payload));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Gate E Player transition failed at " + result.FailedPhase + ": " +
                    result.Failure + " / " + result.Message);
            }
        }

        private async Task AwaitSceneAsync(SceneId sceneId, float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (bootstrap.CurrentScene != sceneId || SceneInputLockState.IsLocked)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    throw new TimeoutException("Timed out waiting for " + sceneId + ".");
                }

                await Task.Yield();
            }

            await Task.Yield();
        }

        private static async Task WaitFramesAsync(int count)
        {
            for (var index = 0; index < count; index++)
            {
                await Task.Yield();
            }
        }

        private static async Task CapturePlayerScreenshotAsync(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            ScreenCapture.CaptureScreenshot(path);
            for (var frame = 0; frame < 120; frame++)
            {
                await Task.Yield();
                if (File.Exists(path) && new FileInfo(path).Length > 0L)
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                "Player screenshot was not written: " + path);
        }

        private static void DeleteGateEOutput(string directory, string fileName)
        {
            var path = Path.Combine(directory, fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static async Task UnloadUnusedAssetsAsync()
        {
            var operation = Resources.UnloadUnusedAssets();
            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }

        private static Button FindActiveButton(string name)
        {
            var button = FindObjectsByType<Button>(FindObjectsInactive.Include)
                .FirstOrDefault(candidate =>
                    candidate.name == name && candidate.gameObject.activeInHierarchy);
            if (button == null)
            {
                throw new InvalidOperationException("Missing active button: " + name + ".");
            }

            return button;
        }

        private static void AssertTopology(SceneId expectedScene)
        {
            if (SceneManager.sceneCount != 2 ||
                FindObjectsByType<BootstrapRoot>(FindObjectsInactive.Include).Length != 1 ||
                FindObjectsByType<UnitySceneFlowEffects>(FindObjectsInactive.Include).Length != 1 ||
                FindObjectsByType<PersistentInputGate>(FindObjectsInactive.Include).Length != 1 ||
                FindObjectsByType<TransitionCanvasPresenter>(FindObjectsInactive.Include).Length != 1 ||
                FindObjectsByType<EventSystem>(FindObjectsInactive.Include).Length != 1 ||
                FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length != 1)
            {
                throw new InvalidOperationException(
                    "Gate E Player smoke found duplicate persistent topology.");
            }

            var entries = FindObjectsByType<SceneContentEntry>(FindObjectsInactive.Include);
            if (entries.Length != 1 || entries[0].SceneId != expectedScene ||
                !entries[0].IsBound || !entries[0].IsInteractive)
            {
                throw new InvalidOperationException(
                    "Gate E Player smoke found invalid active content topology.");
            }
        }

        private static string EvidenceDirectory()
        {
            var root = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new InvalidOperationException(
                    "TIMEKEY_REPOSITORY_ROOT is required for Player evidence.");
            }

            return Path.Combine(
                root,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "combat-shell-gate-e");
        }

        private static bool HasCommandLineFlag(string flag)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length; index++)
            {
                if (string.Equals(
                        arguments[index],
                        flag,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        [Serializable]
        private sealed class GateEPlayerSummary
        {
            public string status;
            public string unityVersion;
            public string renderer;
            public string runId;
            public string renderCounter;
            public List<GateECycleSummary> cycles;
            public long memoryGrowthBytes;
            public bool strictlyMonotonicMemoryGrowth;
            public bool materiallyMonotonicMemoryGrowth;
            public bool sustainedMonotonicMemoryGrowth;
            public bool finalInputLocked;
        }

        [Serializable]
        private sealed class GateECycleSummary
        {
            public int cycle;
            public string roomId;
            public string launchCorrelationId;
            public string outcomeCorrelationId;
            public double transitionMilliseconds;
            public double cpuFrameMilliseconds;
            public double gpuFrameMilliseconds;
            public long renderCounterValue;
            public long totalAllocatedMemoryBytes;
            public int loadedSceneCount;
            public int bootstrapCount;
            public int contentEntryCount;
        }
    }
}
