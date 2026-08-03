using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TimeKey.Application.EraClock;
using TimeKey.Application.Overworld;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Deck;
using TimeKey.Domain.Overworld;
using TimeKey.Domain.OverworldMovement;
using TimeKey.Infrastructure.Persistence;
using TimeKey.Presentation;
using TimeKey.Presentation.BattleFlow;
using TimeKey.Presentation.EraClock;
using TimeKey.Presentation.GameOver;
using TimeKey.Presentation.OutOfBattleShell;
using TimeKey.Presentation.OverworldMovement;
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
        public const string OverworldGateDCommandLineFlag =
            "-timekeyOverworldGateDSmoke";
        public const string OverworldGateDPrepareFlag =
            "-timekeyOverworldGateDPrepare";
        public const string OverworldGateDPreparePassMarker =
            "TIMEKEY_OVERWORLD_GATE_D_PLAYER_PREPARE_PASS";
        public const string OverworldGateDResumePassMarker =
            "TIMEKEY_OVERWORLD_GATE_D_PLAYER_RESUME_PASS";

        [SerializeField] private BootstrapRoot bootstrap = null;

        private int _quitCountdown;
        private int _exitCode;

        private async void Start()
        {
            var gateE = HasCommandLineFlag(GateECommandLineFlag);
            var overworldGateD = HasCommandLineFlag(OverworldGateDCommandLineFlag);
            if (!overworldGateD && !gateE && !HasCommandLineFlag(CommandLineFlag))
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

                if (overworldGateD)
                {
                    var prepare = HasCommandLineFlag(OverworldGateDPrepareFlag);
                    await RunOverworldGateDSmokeAsync(prepare);
                    Debug.Log(prepare
                        ? OverworldGateDPreparePassMarker
                        : OverworldGateDResumePassMarker);
                    RequestQuit(0, 120);
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

                if (overworldGateD)
                {
                    Debug.LogError("TIMEKEY_OVERWORLD_GATE_D_PLAYER_SMOKE_FAIL");
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
            AssertSingleEraClockOwner();
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
            await AwaitEraClockAsync(1, 1, EraClockAnchorTarget.Hud, 30f);
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
                    await AwaitEraClockAsync(
                        store.OutOfBattleState.Era,
                        store.OutOfBattleState.Phase,
                        EraClockAnchorTarget.Hud,
                        30f);
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
                    await AwaitEraClockAsync(
                        store.OutOfBattleState.Era,
                        store.OutOfBattleState.Phase,
                        EraClockAnchorTarget.Hud,
                        30f);
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
                            FindObjectsInactive.Include).Length,
                        eraClockPresenterCount = FindObjectsByType<EraClockPresenter>(
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
            summary.eraClockValidated = true;
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

        private async Task RunOverworldGateDSmokeAsync(bool prepare)
        {
            var evidence = OverworldGateDEvidenceDirectory();
            Directory.CreateDirectory(evidence);
            var savePath = Path.Combine(evidence, "player-overworld-save.json");
            if (prepare)
            {
                var cleanup = new OverworldSaveRepository(savePath).DeleteAll();
                if (!cleanup.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Gate D Player smoke could not reset its generated save: " +
                        cleanup.Failure + ": " + cleanup.Detail);
                }

                DeleteGateDOutputs(evidence);
            }

            var store = FindFirstObjectByType<SceneFlowStateStore>();
            if (store == null)
            {
                throw new InvalidOperationException(
                    "Gate D Player smoke requires the persistent state store.");
            }

            store.ConfigurePersistencePath(savePath);
            Screen.SetResolution(prepare ? 1280 : 1920, prepare ? 720 : 1080, false);
            await AwaitSceneAsync(SceneId.MainMenu, 30f);
            AssertTopology(SceneId.MainMenu);
            var plan = FindOverworldGateDRoutePlan();
            if (prepare)
            {
                await RunOverworldGateDPrepareAsync(evidence, savePath, store, plan);
            }
            else
            {
                await RunOverworldGateDResumeAsync(evidence, savePath, store, plan);
            }
        }

        private async Task RunOverworldGateDPrepareAsync(
            string evidence,
            string savePath,
            SceneFlowStateStore store,
            OverworldGateDRoutePlan plan)
        {
            await GateETransitionAsync(
                SceneId.MainMenu,
                SceneId.OutOfBattleShell,
                new RunStartPayload(
                    RunStartKind.NewGame,
                    "player-overworld-gate-d-run",
                    plan.Seed,
                    plan.Seed.ToString(),
                    1,
                    1,
                    1,
                    100,
                    "silver-character",
                    StarterDeck.OrderedStableIds),
                "player-overworld-gate-d-start");
            await AwaitSceneAsync(SceneId.OutOfBattleShell, 30f);
            await AwaitEraClockAsync(1, 1, EraClockAnchorTarget.Hud, 30f);
            AssertTopology(SceneId.OutOfBattleShell);

            var eventRoom = plan.ChapterOnePath[1];
            await CompleteLocalRoomPlayerAsync(
                store,
                eventRoom,
                Path.Combine(evidence, "player-event-selected-1280x720.png"),
                Path.Combine(evidence, "player-event-result-1280x720.png"));
            var snapshot = store.OverworldRun.CreatePersistenceSnapshot();
            var persisted = new OverworldSaveRepository(savePath).Load();
            if (!persisted.Succeeded ||
                persisted.Document.CurrentNodeId != eventRoom.Value ||
                snapshot.CurrentNodeId != eventRoom.Value ||
                snapshot.Timecoins != 100 ||
                snapshot.DeckStableIds.Count != StarterDeck.OrderedStableIds.Count)
            {
                throw new InvalidOperationException(
                    "Gate D Player prepare did not persist the exact Event boundary.");
            }

            var summary = new OverworldGateDPrepareSummary
            {
                status = "passed",
                seed = plan.Seed,
                runId = snapshot.RunId,
                eventRoomId = eventRoom.Value,
                mapFingerprint = snapshot.MapFingerprint,
                currentNodeId = snapshot.CurrentNodeId,
                timecoins = snapshot.Timecoins,
                deckCount = snapshot.DeckStableIds.Count,
                settledNodeIds = new List<string>(snapshot.SettledNodeIds),
                inputLocked = SceneInputLockState.IsLocked
            };
            if (summary.inputLocked)
            {
                throw new InvalidOperationException(
                    "Gate D Player prepare ended with input locked.");
            }

            File.WriteAllText(
                Path.Combine(evidence, "player-phase1-summary.json"),
                JsonUtility.ToJson(summary, true));
            await WaitFramesAsync(30);
        }

        private async Task RunOverworldGateDResumeAsync(
            string evidence,
            string savePath,
            SceneFlowStateStore store,
            OverworldGateDRoutePlan plan)
        {
            var phaseOnePath = Path.Combine(evidence, "player-phase1-summary.json");
            if (!File.Exists(phaseOnePath))
            {
                throw new InvalidOperationException(
                    "Gate D Player resume requires the prepare summary.");
            }

            var phaseOne = JsonUtility.FromJson<OverworldGateDPrepareSummary>(
                File.ReadAllText(phaseOnePath));
            await CapturePlayerScreenshotAsync(
                Path.Combine(evidence, "player-continue-1920x1080.png"));
            var continueButton = FindActiveButton("Continue");
            if (!continueButton.interactable)
            {
                throw new InvalidOperationException(
                    "Gate D Player resume found Continue disabled: " + store.ContinueDetail);
            }

            continueButton.onClick.Invoke();
            await AwaitSceneAsync(SceneId.OutOfBattleShell, 30f);
            await AwaitEraClockAsync(1, 1, EraClockAnchorTarget.Hud, 30f);
            AssertTopology(SceneId.OutOfBattleShell);
            var restored = store.OverworldRun.CreatePersistenceSnapshot();
            if (restored.RunId != phaseOne.runId ||
                restored.RunSeed != phaseOne.seed ||
                restored.MapFingerprint != phaseOne.mapFingerprint ||
                restored.CurrentNodeId != phaseOne.currentNodeId ||
                restored.Timecoins != phaseOne.timecoins ||
                restored.DeckStableIds.Count != phaseOne.deckCount)
            {
                throw new InvalidOperationException(
                    "Gate D Player Continue did not restore the exact Event boundary.");
            }

            var memoryBefore = Profiler.GetTotalAllocatedMemoryLong();
            var shopRoom = plan.ChapterOnePath[2];
            var offer = store.OverworldRun.GetShopOffer(shopRoom);
            var beforeShop = store.OverworldRun.CreatePersistenceSnapshot();
            await CompleteLocalRoomPlayerAsync(
                store,
                shopRoom,
                Path.Combine(evidence, "player-shop-selected-1920x1080.png"),
                Path.Combine(evidence, "player-shop-result-1920x1080.png"));
            var afterShop = store.OverworldRun.CreatePersistenceSnapshot();
            if (afterShop.Timecoins != beforeShop.Timecoins - offer.TimecoinCost ||
                afterShop.DeckStableIds.Count != beforeShop.DeckStableIds.Count + 1 ||
                afterShop.DeckStableIds[afterShop.DeckStableIds.Count - 1] !=
                offer.CardStableId)
            {
                throw new InvalidOperationException(
                    "Gate D Player Shop did not commit exactly once.");
            }

            var persistedShop = new OverworldSaveRepository(savePath).Load();
            if (!persistedShop.Succeeded ||
                persistedShop.Document.Timecoins != afterShop.Timecoins ||
                persistedShop.Document.DeckStableIds.Count != afterShop.DeckStableIds.Count)
            {
                throw new InvalidOperationException(
                    "Gate D Player Shop persistence does not match memory.");
            }

            CombatOutcome bossOutcome = null;
            var completedRooms = 2;
            for (var index = 3; index < plan.ChapterOnePath.Count; index++)
            {
                var roomId = plan.ChapterOnePath[index];
                var roomType = store.OverworldRun.GetRoomType(roomId);
                if (roomType == OverworldRoomType.Event ||
                    roomType == OverworldRoomType.Shop)
                {
                    await CompleteLocalRoomPlayerAsync(store, roomId, null, null);
                }
                else
                {
                    var isBoss = roomType == OverworldRoomType.Boss;
                    var outcome = await CompleteCombatRoomPlayerAsync(
                        store,
                        roomId,
                        BattleOutcome.VictorySettlement,
                        isBoss
                            ? Path.Combine(evidence, "player-boss-selected-1920x1080.png")
                            : null,
                        isBoss
                            ? Path.Combine(evidence, "player-boss-victory-1920x1080.png")
                            : null);
                    if (isBoss)
                    {
                        bossOutcome = outcome;
                    }
                }

                completedRooms++;
                AssertTopology(SceneId.OutOfBattleShell);
            }

            if (completedRooms < 3 || bossOutcome == null)
            {
                throw new InvalidOperationException(
                    "Gate D Player smoke did not complete the required Boss route.");
            }

            var chapterTwo = store.OverworldRun.CreatePersistenceSnapshot();
            if (chapterTwo.Chapter != 2)
            {
                throw new InvalidOperationException(
                    "Gate D Player smoke did not advance to chapter 2.");
            }

            var chapterFingerprint = chapterTwo.MapFingerprint;
            var replay = store.OverworldRun.TryApplyCombatOutcome(
                bootstrap.ReserveTransitionSequence(),
                store.OverworldRun.Revision,
                bossOutcome);
            if (!replay.Succeeded || !replay.WasAlreadyApplied ||
                store.OverworldRun.CreatePersistenceSnapshot().MapFingerprint !=
                chapterFingerprint)
            {
                throw new InvalidOperationException(
                    "Gate D Player smoke replayed the Boss outcome non-idempotently.");
            }

            Screen.SetResolution(2560, 1080, false);
            await WaitFramesAsync(3);
            await CapturePlayerScreenshotAsync(
                Path.Combine(evidence, "player-chapter-02-2560x1080.png"));

            var defeatRoom = FindAvailableCombatRoom(store.OverworldRun);
            await CompleteCombatRoomPlayerAsync(
                store,
                defeatRoom,
                BattleOutcome.Defeat,
                null,
                Path.Combine(evidence, "player-game-over-2560x1080.png"));
            if (FindFirstObjectByType<GameOverPresenter>() == null)
            {
                throw new InvalidOperationException(
                    "Gate D Player smoke did not bind GameOver.");
            }

            AssertTopology(SceneId.GameOver);
            FindActiveButton("ReturnButton").onClick.Invoke();
            await AwaitSceneAsync(SceneId.MainMenu, 30f);
            AssertTopology(SceneId.MainMenu);
            await CapturePlayerScreenshotAsync(
                Path.Combine(evidence, "player-returned-menu-2560x1080.png"));
            var deletedSave = new OverworldSaveRepository(savePath).Load().Status ==
                OverworldSaveLoadStatus.Missing;
            if (store.OverworldRun != null || store.OutOfBattleState != null ||
                store.ActiveLaunch != null || store.LastOutcome != null ||
                !deletedSave || SceneInputLockState.IsLocked)
            {
                throw new InvalidOperationException(
                    "Gate D Player smoke did not clean the defeated run exactly once.");
            }

            var memoryAfter = Profiler.GetTotalAllocatedMemoryLong();
            var summary = new OverworldGateDPlayerSummary
            {
                status = "passed",
                unityVersion = UnityEngine.Application.unityVersion,
                renderer = SystemInfo.graphicsDeviceType.ToString(),
                seed = plan.Seed,
                runId = phaseOne.runId,
                eventRoomId = phaseOne.eventRoomId,
                shopRoomId = shopRoom.Value,
                shopCardStableId = offer.CardStableId,
                shopCost = offer.TimecoinCost,
                timecoinsAfterShop = afterShop.Timecoins,
                deckCountAfterShop = afterShop.DeckStableIds.Count,
                completedRooms = completedRooms,
                bossRoomId = bossOutcome.RoomId,
                bossReplayIdempotent = true,
                chapter = chapterTwo.Chapter,
                chapterMapFingerprint = chapterFingerprint,
                defeatRoomId = defeatRoom.Value,
                saveDeletedAfterDefeat = deletedSave,
                finalInputLocked = SceneInputLockState.IsLocked,
                memoryGrowthBytes = memoryAfter - memoryBefore,
                routeNodeIds = plan.ChapterOnePath.Select(node => node.Value).ToList()
            };
            File.WriteAllText(
                Path.Combine(evidence, "player-smoke-summary.json"),
                JsonUtility.ToJson(summary, true));
            await WaitFramesAsync(30);
        }

        private static async Task CompleteLocalRoomPlayerAsync(
            SceneFlowStateStore store,
            MapNodeId roomId,
            string selectedScreenshot,
            string resultScreenshot)
        {
            await PrepareOverworldRoomSelectionAsync(roomId, selectedScreenshot);
            FindActiveButton("ConfirmButton").onClick.Invoke();
            var deadline = Time.realtimeSinceStartup + 30f;
            while (store.OverworldRun == null ||
                   !store.OverworldRun.CreatePersistenceSnapshot()
                       .SettledNodeIds.Contains(roomId.Value))
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    throw new TimeoutException(
                        "Timed out completing local room " + roomId + ".");
                }

                await Task.Yield();
            }

            await WaitFramesAsync(3);
            if (!string.IsNullOrWhiteSpace(resultScreenshot))
            {
                await CapturePlayerScreenshotAsync(resultScreenshot);
            }
        }

        private async Task<CombatOutcome> CompleteCombatRoomPlayerAsync(
            SceneFlowStateStore store,
            MapNodeId roomId,
            BattleOutcome outcome,
            string selectedScreenshot,
            string settlementScreenshot)
        {
            await PrepareOverworldRoomSelectionAsync(roomId, selectedScreenshot);
            FindActiveButton("ConfirmButton").onClick.Invoke();
            await AwaitSceneAsync(SceneId.Combat, 30f);
            AssertTopology(SceneId.Combat);
            var controller = FindFirstObjectByType<VerticalSliceController>();
            if (controller == null)
            {
                throw new InvalidOperationException(
                    "Gate D Player smoke could not find the combat controller.");
            }

            var resolution = controller.ResolveBattleOutcome(outcome);
            if (!resolution.Succeeded)
            {
                throw new InvalidOperationException(
                    "Gate D Player combat resolution failed: " + resolution.Failure + ".");
            }

            if (outcome == BattleOutcome.VictorySettlement)
            {
                await WaitFramesAsync(3);
                if (!string.IsNullOrWhiteSpace(settlementScreenshot))
                {
                    await CapturePlayerScreenshotAsync(settlementScreenshot);
                }

                FindActiveButton("RewardButton").onClick.Invoke();
                await AwaitSceneAsync(SceneId.OutOfBattleShell, 30f);
                await AwaitEraClockAsync(1, 1, EraClockAnchorTarget.Hud, 30f);
                AssertTopology(SceneId.OutOfBattleShell);
            }
            else
            {
                await AwaitSceneAsync(SceneId.GameOver, 30f);
                if (!string.IsNullOrWhiteSpace(settlementScreenshot))
                {
                    await CapturePlayerScreenshotAsync(settlementScreenshot);
                }
            }

            if (store.LastOutcome == null || store.LastOutcome.RoomId != roomId.Value)
            {
                throw new InvalidOperationException(
                    "Gate D Player smoke lost the combat outcome identity.");
            }

            return store.LastOutcome;
        }

        private static async Task PrepareOverworldRoomSelectionAsync(
            MapNodeId roomId,
            string screenshotPath)
        {
            var movement = FindFirstObjectByType<OverworldMovementPresenter>();
            if (movement == null)
            {
                throw new InvalidOperationException(
                    "Gate D Player smoke requires the dynamic map presenter.");
            }

            var view = movement.GetComponentsInChildren<OverworldNodeView>(true)
                .Single(candidate => candidate.Id == roomId);
            if (view.State != OverworldNodeVisualState.Available)
            {
                throw new InvalidOperationException(
                    "Gate D Player room is not available: " + roomId + ".");
            }

            view.GetComponent<Button>().onClick.Invoke();
            var deadline = Time.realtimeSinceStartup + 30f;
            while (movement.IsMoving)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    throw new TimeoutException(
                        "Timed out selecting Gate D room " + roomId + ".");
                }

                await Task.Yield();
            }

            if (!string.IsNullOrWhiteSpace(screenshotPath))
            {
                await CapturePlayerScreenshotAsync(screenshotPath);
            }

            FindActiveButton("CombatRoom").onClick.Invoke();
            await WaitFramesAsync(1);
            if (!FindActiveButton("ConfirmButton").interactable)
            {
                throw new InvalidOperationException(
                    "Gate D Player room confirmation is disabled: " + roomId + ".");
            }
        }

        private static MapNodeId FindAvailableCombatRoom(
            OverworldRunApplication application)
        {
            foreach (var nodeId in application.GetAvailableRoomIds())
            {
                var roomType = application.GetRoomType(nodeId);
                if (roomType.HasValue && IsOverworldCombatRoom(roomType.Value))
                {
                    return nodeId;
                }
            }

            throw new InvalidOperationException(
                "Gate D Player smoke found no available combat room.");
        }

        private static OverworldGateDRoutePlan FindOverworldGateDRoutePlan()
        {
            var generator = new DeterministicOverworldMapGenerator();
            for (var seed = 1; seed <= 10000; seed++)
            {
                var chapterOne = generator.Generate(
                    seed,
                    new OverworldMapGenerationConfig(1));
                var path = new List<MapNodeId> { chapterOne.EntryNodeId };
                IReadOnlyList<MapNodeId> route;
                if (!TryFindOverworldGateDPath(
                        chapterOne,
                        chapterOne.EntryNodeId,
                        path,
                        out route))
                {
                    continue;
                }

                var chapterTwo = generator.Generate(
                    seed,
                    new OverworldMapGenerationConfig(2));
                if (chapterTwo.GetOutgoing(chapterTwo.EntryNodeId)
                    .Any(nodeId => IsOverworldCombatRoom(
                        chapterTwo.GetNode(nodeId).RoomType)))
                {
                    return new OverworldGateDRoutePlan(seed, route);
                }
            }

            throw new InvalidOperationException(
                "Gate D Player smoke could not find a deterministic route.");
        }

        private static bool TryFindOverworldGateDPath(
            OverworldMapDefinition map,
            MapNodeId current,
            List<MapNodeId> path,
            out IReadOnlyList<MapNodeId> route)
        {
            if (current == map.BossNodeId)
            {
                var types = path.Skip(1)
                    .Select(nodeId => map.GetNode(nodeId).RoomType)
                    .ToArray();
                if (types.Length >= 4 &&
                    types[0] == OverworldRoomType.Event &&
                    types[1] == OverworldRoomType.Shop &&
                    types.Skip(2).All(type => type != OverworldRoomType.Shop) &&
                    types.Take(types.Length - 1).Any(IsOverworldCombatRoom))
                {
                    route = path.ToArray();
                    return true;
                }

                route = null;
                return false;
            }

            foreach (var next in map.GetOutgoing(current))
            {
                path.Add(next);
                if (TryFindOverworldGateDPath(map, next, path, out route))
                {
                    return true;
                }

                path.RemoveAt(path.Count - 1);
            }

            route = null;
            return false;
        }

        private static bool IsOverworldCombatRoom(OverworldRoomType roomType)
        {
            return roomType == OverworldRoomType.Battle ||
                roomType == OverworldRoomType.Elite ||
                roomType == OverworldRoomType.Boss;
        }

        private static void DeleteGateDOutputs(string directory)
        {
            var files = new[]
            {
                "player-phase1-summary.json",
                "player-smoke-summary.json",
                "player-event-selected-1280x720.png",
                "player-event-result-1280x720.png",
                "player-continue-1920x1080.png",
                "player-shop-selected-1920x1080.png",
                "player-shop-result-1920x1080.png",
                "player-boss-selected-1920x1080.png",
                "player-boss-victory-1920x1080.png",
                "player-chapter-02-2560x1080.png",
                "player-game-over-2560x1080.png",
                "player-returned-menu-2560x1080.png"
            };
            foreach (var file in files)
            {
                DeleteGateEOutput(directory, file);
            }
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
            var bootstrapCount = FindObjectsByType<BootstrapRoot>(
                FindObjectsInactive.Include).Length;
            var effectsCount = FindObjectsByType<UnitySceneFlowEffects>(
                FindObjectsInactive.Include).Length;
            var inputGateCount = FindObjectsByType<PersistentInputGate>(
                FindObjectsInactive.Include).Length;
            var transitionCount = FindObjectsByType<TransitionCanvasPresenter>(
                FindObjectsInactive.Include).Length;
            var eventSystemCount = FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include).Length;
            var audioSourceCount = FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include).Length;
            var eraClockCount = FindObjectsByType<EraClockPresenter>(
                FindObjectsInactive.Include).Length;
            var expectedEraClockCount = expectedScene == SceneId.GameOver ? 0 : 1;
            if (SceneManager.sceneCount != 2 ||
                bootstrapCount != 1 ||
                effectsCount != 1 ||
                inputGateCount != 1 ||
                transitionCount != 1 ||
                eventSystemCount != 1 ||
                audioSourceCount != 1 ||
                eraClockCount != expectedEraClockCount)
            {
                throw new InvalidOperationException(
                    "Player smoke found invalid topology for " + expectedScene +
                    ": scenes=" + SceneManager.sceneCount +
                    ", bootstrap=" + bootstrapCount +
                    ", effects=" + effectsCount +
                    ", inputGate=" + inputGateCount +
                    ", transition=" + transitionCount +
                    ", eventSystem=" + eventSystemCount +
                    ", audio=" + audioSourceCount +
                    ", eraClock=" + eraClockCount + ".");
            }

            var entries = FindObjectsByType<SceneContentEntry>(FindObjectsInactive.Include);
            if (entries.Length != 1 || entries[0].SceneId != expectedScene ||
                !entries[0].IsBound || !entries[0].IsInteractive)
            {
                throw new InvalidOperationException(
                    "Player smoke found invalid active content topology for " +
                    expectedScene + ".");
            }
        }

        private static void AssertSingleEraClockOwner()
        {
            var presenters = FindObjectsByType<EraClockPresenter>(
                FindObjectsInactive.Include);
            if (presenters.Length != 1)
            {
                throw new InvalidOperationException(
                    "Gate E Player smoke requires exactly one EraClock presenter.");
            }
        }

        private static async Task AwaitEraClockAsync(
            int era,
            int phase,
            EraClockAnchorTarget anchor,
            float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                var presenters = FindObjectsByType<EraClockPresenter>(
                    FindObjectsInactive.Include);
                if (presenters.Length == 1)
                {
                    EraClockPresentationSnapshot snapshot = presenters[0].CurrentSnapshot;
                    if (snapshot != null &&
                        snapshot.Era == era &&
                        snapshot.Phase == phase &&
                        snapshot.AnchorTarget == anchor &&
                        presenters[0].State == EraClockPresenterState.Settled)
                    {
                        return;
                    }
                }

                await Task.Yield();
            }

            throw new InvalidOperationException(
                "Gate E Player smoke timed out waiting for the formal EraClock state.");
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

        private static string OverworldGateDEvidenceDirectory()
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
                "overworld-map-gate-d");
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
            public bool eraClockValidated;
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
            public int eraClockPresenterCount;
        }

        [Serializable]
        private sealed class OverworldGateDPrepareSummary
        {
            public string status;
            public int seed;
            public string runId;
            public string eventRoomId;
            public string mapFingerprint;
            public string currentNodeId;
            public int timecoins;
            public int deckCount;
            public List<string> settledNodeIds;
            public bool inputLocked;
        }

        [Serializable]
        private sealed class OverworldGateDPlayerSummary
        {
            public string status;
            public string unityVersion;
            public string renderer;
            public int seed;
            public string runId;
            public string eventRoomId;
            public string shopRoomId;
            public string shopCardStableId;
            public int shopCost;
            public int timecoinsAfterShop;
            public int deckCountAfterShop;
            public int completedRooms;
            public string bossRoomId;
            public bool bossReplayIdempotent;
            public int chapter;
            public string chapterMapFingerprint;
            public string defeatRoomId;
            public bool saveDeletedAfterDefeat;
            public bool finalInputLocked;
            public long memoryGrowthBytes;
            public List<string> routeNodeIds;
        }

        private sealed class OverworldGateDRoutePlan
        {
            public OverworldGateDRoutePlan(
                int seed,
                IReadOnlyList<MapNodeId> chapterOnePath)
            {
                Seed = seed;
                ChapterOnePath = chapterOnePath;
            }

            public int Seed { get; }

            public IReadOnlyList<MapNodeId> ChapterOnePath { get; }
        }
    }
}
