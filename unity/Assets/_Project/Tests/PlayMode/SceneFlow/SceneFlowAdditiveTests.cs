using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Application.Overworld;
using TimeKey.Composition;
using TimeKey.Composition.SceneFlow;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Deck;
using TimeKey.Domain.Overworld;
using TimeKey.Domain.OverworldMovement;
using TimeKey.Infrastructure.Persistence;
using TimeKey.Presentation;
using TimeKey.Presentation.BattleFlow;
using TimeKey.Presentation.GameOver;
using TimeKey.Presentation.MainMenu;
using TimeKey.Presentation.OutOfBattleShell;
using TimeKey.Presentation.OverworldMovement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using InvalidOperationException = System.InvalidOperationException;
using Object = UnityEngine.Object;

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
            var focusBeforeFailure = EventSystem.current.currentSelectedGameObject;
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
            Assert.That(
                EventSystem.current.currentSelectedGameObject,
                Is.SameAs(focusBeforeFailure));

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

        [UnityTest]
        public IEnumerator Bootstrap_GateBGeneratedCombatRoundTripPersistsAndContinuesExactly()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "timekey-gate-b-additive-" + System.Guid.NewGuid().ToString("N"));
            var savePath = Path.Combine(directory, "overworld.json");
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;
            var bootstrap = Object.FindAnyObjectByType<BootstrapRoot>();
            yield return AwaitTask(bootstrap.InitializationTask, "Bootstrap initialization");
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    1,
                    "gate-b-start-to-menu",
                    SceneId.GameStart,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)));

            var store = Object.FindAnyObjectByType<SceneFlowStateStore>();
            store.ConfigurePersistencePath(savePath);
            var seed = FindSeedWithAvailableCombatRoom();
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    2,
                    "gate-b-menu-to-shell",
                    SceneId.MainMenu,
                    SceneId.OutOfBattleShell,
                    Start("gate-b-generated-run", seed)));

            var roomId = FindAvailableCombatRoom(store.OverworldRun);
            var launchResult = store.PrepareCombatLaunch(
                3,
                roomId.Value,
                "gate-b-generated-launch",
                "combat-vertical-slice",
                seed);
            Assert.That(launchResult.Succeeded, Is.True, launchResult.Failure.ToString());
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    3,
                    launchResult.Launch.LaunchCorrelationId,
                    SceneId.OutOfBattleShell,
                    SceneId.Combat,
                    launchResult.Launch));

            var outcome = Outcome(
                launchResult.Launch,
                BattleOutcome.VictorySettlement);
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    4,
                    outcome.OutcomeCorrelationId,
                    SceneId.Combat,
                    SceneId.OutOfBattleShell,
                    outcome));

            var expected = store.OverworldRun.CreatePersistenceSnapshot();
            Assert.That(expected.SettledNodeIds, Does.Contain(roomId.Value));
            Assert.That(SceneInputLockState.IsLocked, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Not.Null);
            var loaded = new OverworldSaveRepository(savePath).Load();
            Assert.That(loaded.Succeeded, Is.True, loaded.Detail);
            Assert.That(loaded.Document.CurrentNodeId, Is.EqualTo(roomId.Value));

            var restoreRoot = new GameObject("GateBContinueRestore");
            var restore = restoreRoot.AddComponent<SceneFlowStateStore>();
            restore.ConfigurePersistencePath(savePath);
            Assert.That(restore.RefreshContinueAvailability(), Is.True, restore.ContinueDetail);
            Assert.That(restore.TryCreateContinuePayload(out var payload), Is.True);
            var continueRequest = new SceneTransitionRequest(
                5,
                "gate-b-continue",
                SceneId.MainMenu,
                SceneId.OutOfBattleShell,
                payload);
            restore.Record(continueRequest);
            var actual = restore.OverworldRun.CreatePersistenceSnapshot();
            Assert.That(actual.RunId, Is.EqualTo(expected.RunId));
            Assert.That(actual.MapFingerprint, Is.EqualTo(expected.MapFingerprint));
            Assert.That(actual.CurrentNodeId, Is.EqualTo(expected.CurrentNodeId));
            Assert.That(actual.SettledNodeIds, Is.EqualTo(expected.SettledNodeIds));
            var replay = restore.OverworldRun.TryApplyCombatOutcome(
                5,
                restore.OverworldRun.Revision,
                outcome);
            var conflict = restore.OverworldRun.TryApplyCombatOutcome(
                6,
                restore.OverworldRun.Revision,
                Outcome(launchResult.Launch, BattleOutcome.Defeat));
            Assert.That(replay.Succeeded, Is.True);
            Assert.That(replay.WasAlreadyApplied, Is.True);
            Assert.That(
                conflict.Failure,
                Is.EqualTo(TimeKey.Application.Overworld.OverworldApplicationFailure.OutcomeConflict));

            Object.Destroy(restoreRoot);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [UnityTest]
        public IEnumerator Bootstrap_GateBPersistenceFailureRestoresCombatFocusAndPriorSave()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "timekey-gate-b-failure-" + System.Guid.NewGuid().ToString("N"));
            var savePath = Path.Combine(directory, "overworld.json");
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;
            var bootstrap = Object.FindAnyObjectByType<BootstrapRoot>();
            yield return AwaitTask(bootstrap.InitializationTask, "Bootstrap initialization");
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    1,
                    "gate-b-failure-start-to-menu",
                    SceneId.GameStart,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)));

            var store = Object.FindAnyObjectByType<SceneFlowStateStore>();
            store.ConfigurePersistencePath(savePath);
            var seed = FindSeedWithAvailableCombatRoom();
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    2,
                    "gate-b-failure-menu-to-shell",
                    SceneId.MainMenu,
                    SceneId.OutOfBattleShell,
                    Start("gate-b-failure-run", seed)));
            var roomId = FindAvailableCombatRoom(store.OverworldRun);
            var launchResult = store.PrepareCombatLaunch(
                3,
                roomId.Value,
                "gate-b-failure-launch",
                "combat-vertical-slice",
                seed);
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    3,
                    launchResult.Launch.LaunchCorrelationId,
                    SceneId.OutOfBattleShell,
                    SceneId.Combat,
                    launchResult.Launch));

            store.ConfigurePersistenceRepository(new OverworldSaveRepository(
                savePath,
                new ThrowBeforeReplace()));
            var focusBeforeFailure = EventSystem.current.currentSelectedGameObject;
            var outcome = Outcome(
                launchResult.Launch,
                BattleOutcome.VictorySettlement);
            yield return TransitionFailure(
                bootstrap,
                new SceneTransitionRequest(
                    4,
                    outcome.OutcomeCorrelationId,
                    SceneId.Combat,
                    SceneId.OutOfBattleShell,
                    outcome),
                SceneTransitionPhase.UnloadingSource);

            Assert.That(store.ActiveLaunch, Is.SameAs(launchResult.Launch));
            Assert.That(store.OverworldRun.ChapterSnapshot.HasActiveRoom, Is.True);
            Assert.That(SceneInputLockState.IsLocked, Is.False);
            Assert.That(
                EventSystem.current.currentSelectedGameObject,
                Is.SameAs(focusBeforeFailure));
            var persisted = new OverworldSaveRepository(savePath).Load();
            Assert.That(persisted.Succeeded, Is.True);
            Assert.That(persisted.Document.SettledNodeIds, Does.Not.Contain(roomId.Value));

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [UnityTest]
        public IEnumerator Bootstrap_MainMenuContinueLoadsValidatedSavedRun()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "timekey-gate-b-menu-continue-" + System.Guid.NewGuid().ToString("N"));
            var savePath = Path.Combine(directory, "overworld.json");
            var savedRun = new OverworldRunApplication(
                Start("gate-b-menu-continue", 731),
                new OverworldMapGenerationConfig(1),
                finalChapter: 3);
            var saved = new OverworldSaveRepository(savePath).Save(
                OverworldSaveMapper.ToDocument(savedRun.CreatePersistenceSnapshot()));
            Assert.That(saved.Succeeded, Is.True, saved.Detail);

            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;
            var bootstrap = Object.FindAnyObjectByType<BootstrapRoot>();
            yield return AwaitTask(bootstrap.InitializationTask, "Bootstrap initialization");
            var store = Object.FindAnyObjectByType<SceneFlowStateStore>();
            store.ConfigurePersistencePath(savePath);
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    1,
                    "gate-b-continue-menu",
                    SceneId.GameStart,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)));

            var continueButton = FindButton("Continue");
            Assert.That(continueButton.interactable, Is.True);
            continueButton.onClick.Invoke();
            var deadline = Time.realtimeSinceStartup + TaskTimeoutSeconds;
            while (bootstrap.CurrentScene != SceneId.OutOfBattleShell ||
                   SceneInputLockState.IsLocked ||
                   !IsOnlyEntryInteractive(SceneId.OutOfBattleShell))
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail("Continue navigation timed out.");
                }

                yield return null;
            }

            Assert.That(store.OverworldRun, Is.Not.Null);
            Assert.That(
                store.OverworldRun.CreatePersistenceSnapshot().RunId,
                Is.EqualTo("gate-b-menu-continue"));
            AssertPersistentState(SceneId.OutOfBattleShell);

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [UnityTest]
        public IEnumerator Bootstrap_GateCEventRoomSkipsMissingContentAndPersists()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "timekey-gate-c-event-" + System.Guid.NewGuid().ToString("N"));
            var savePath = Path.Combine(directory, "overworld.json");
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;
            var bootstrap = Object.FindAnyObjectByType<BootstrapRoot>();
            yield return AwaitTask(bootstrap.InitializationTask, "Bootstrap initialization");
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    1,
                    "gate-c-event-start-to-menu",
                    SceneId.GameStart,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)));

            var store = Object.FindAnyObjectByType<SceneFlowStateStore>();
            store.ConfigurePersistencePath(savePath);
            var seed = FindSeedWithAvailableRoomType(OverworldRoomType.Event);
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    2,
                    "gate-c-event-menu-to-shell",
                    SceneId.MainMenu,
                    SceneId.OutOfBattleShell,
                    Start("gate-c-event-run", seed, 100)));

            var roomId = FindAvailableRoom(store.OverworldRun, OverworldRoomType.Event);
            var before = store.OverworldRun.CreatePersistenceSnapshot();
            yield return SelectAndConfirmLocalRoom(store, roomId, "安全跳过");
            var after = store.OverworldRun.CreatePersistenceSnapshot();

            Assert.That(after.CurrentNodeId, Is.EqualTo(roomId.Value));
            Assert.That(after.SettledNodeIds, Does.Contain(roomId.Value));
            Assert.That(after.Timecoins, Is.EqualTo(before.Timecoins));
            Assert.That(after.DeckStableIds, Is.EqualTo(before.DeckStableIds));
            Assert.That(FindText("RoomDetail").text, Does.Contain("后继路线已解锁"));
            var persisted = new OverworldSaveRepository(savePath).Load();
            Assert.That(persisted.Succeeded, Is.True, persisted.Detail);
            Assert.That(persisted.Document.CurrentNodeId, Is.EqualTo(roomId.Value));

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [UnityTest]
        public IEnumerator Bootstrap_GateCShopPurchasePersistsBalanceAndCardExactlyOnce()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "timekey-gate-c-shop-" + System.Guid.NewGuid().ToString("N"));
            var savePath = Path.Combine(directory, "overworld.json");
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;
            var bootstrap = Object.FindAnyObjectByType<BootstrapRoot>();
            yield return AwaitTask(bootstrap.InitializationTask, "Bootstrap initialization");
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    1,
                    "gate-c-shop-start-to-menu",
                    SceneId.GameStart,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)));

            var store = Object.FindAnyObjectByType<SceneFlowStateStore>();
            store.ConfigurePersistencePath(savePath);
            var seed = FindSeedWithAvailableRoomType(OverworldRoomType.Shop);
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    2,
                    "gate-c-shop-menu-to-shell",
                    SceneId.MainMenu,
                    SceneId.OutOfBattleShell,
                    Start("gate-c-shop-run", seed, 100)));

            var roomId = FindAvailableRoom(store.OverworldRun, OverworldRoomType.Shop);
            var offer = store.OverworldRun.GetShopOffer(roomId);
            var before = store.OverworldRun.CreatePersistenceSnapshot();
            yield return SelectAndConfirmLocalRoom(store, roomId, "价格");
            var after = store.OverworldRun.CreatePersistenceSnapshot();

            Assert.That(after.Timecoins, Is.EqualTo(before.Timecoins - offer.TimecoinCost));
            Assert.That(after.DeckStableIds.Count, Is.EqualTo(before.DeckStableIds.Count + 1));
            Assert.That(after.DeckStableIds.Last(), Is.EqualTo(offer.CardStableId));
            var replay = store.PurchaseShopRoom(20, 21, roomId.Value);
            Assert.That(replay.Succeeded, Is.False);
            Assert.That(store.OverworldRun.CreatePersistenceSnapshot().Timecoins,
                Is.EqualTo(after.Timecoins));
            var persisted = new OverworldSaveRepository(savePath).Load();
            Assert.That(persisted.Succeeded, Is.True, persisted.Detail);
            Assert.That(persisted.Document.Timecoins, Is.EqualTo(after.Timecoins));
            Assert.That(persisted.Document.DeckStableIds.Last(), Is.EqualTo(offer.CardStableId));

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [UnityTest]
        public IEnumerator Bootstrap_GateCCorruptAndFutureSavesShowRecoverableChineseNotice()
        {
            yield return AssertInvalidSaveNotice(false);
            yield return AssertInvalidSaveNotice(true);
        }

        [UnityTest]
        public IEnumerator Bootstrap_GateDEventAndShopSurviveRestartWithoutDuplicateSpend()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "timekey-gate-d-restart-" + Guid.NewGuid().ToString("N"));
            var savePath = Path.Combine(directory, "overworld.json");
            var plan = FindGateDRoutePlan();

            BootstrapRoot bootstrap = null;
            SceneFlowStateStore store = null;
            yield return StartGateDRun(
                savePath,
                plan.Seed,
                "gate-d-restart-run",
                100,
                value => bootstrap = value,
                value => store = value);

            var eventRoom = plan.ChapterOnePath[1];
            var shopRoom = plan.ChapterOnePath[2];
            yield return SelectAndConfirmLocalRoom(store, eventRoom, "安全跳过");
            var afterEvent = store.OverworldRun.CreatePersistenceSnapshot();
            Assert.That(afterEvent.CurrentNodeId, Is.EqualTo(eventRoom.Value));
            Assert.That(afterEvent.Timecoins, Is.EqualTo(100));
            Assert.That(afterEvent.DeckStableIds, Is.EqualTo(StarterDeck.OrderedStableIds));

            yield return RestartAndContinue(
                savePath,
                value => bootstrap = value,
                value => store = value);
            var restoredEvent = store.OverworldRun.CreatePersistenceSnapshot();
            AssertEquivalentRunBoundary(afterEvent, restoredEvent);

            var offer = store.OverworldRun.GetShopOffer(shopRoom);
            yield return SelectAndConfirmLocalRoom(store, shopRoom, "价格");
            var afterShop = store.OverworldRun.CreatePersistenceSnapshot();
            Assert.That(afterShop.Timecoins, Is.EqualTo(100 - offer.TimecoinCost));
            Assert.That(afterShop.DeckStableIds.Count,
                Is.EqualTo(afterEvent.DeckStableIds.Count + 1));
            Assert.That(afterShop.DeckStableIds.Last(), Is.EqualTo(offer.CardStableId));

            yield return RestartAndContinue(
                savePath,
                value => bootstrap = value,
                value => store = value);
            var restoredShop = store.OverworldRun.CreatePersistenceSnapshot();
            AssertEquivalentRunBoundary(afterShop, restoredShop);
            var beforeReplay = store.OverworldRun.CreatePersistenceSnapshot();
            var replay = store.PurchaseShopRoom(
                bootstrap.ReserveTransitionSequence(),
                bootstrap.ReserveTransitionSequence(),
                shopRoom.Value);
            Assert.That(replay.Succeeded, Is.False);
            var afterReplay = store.OverworldRun.CreatePersistenceSnapshot();
            Assert.That(afterReplay.Timecoins, Is.EqualTo(beforeReplay.Timecoins));
            Assert.That(afterReplay.DeckStableIds, Is.EqualTo(beforeReplay.DeckStableIds));
            Assert.That(afterReplay.SettledNodeIds, Is.EqualTo(beforeReplay.SettledNodeIds));
            AssertPersistentState(SceneId.OutOfBattleShell);

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [UnityTest]
        public IEnumerator Bootstrap_GateDThreeRoomBossAdvanceAndDefeatRouteStayExact()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "timekey-gate-d-route-" + Guid.NewGuid().ToString("N"));
            var savePath = Path.Combine(directory, "overworld.json");
            var plan = FindGateDRoutePlan();

            BootstrapRoot bootstrap = null;
            SceneFlowStateStore store = null;
            yield return StartGateDRun(
                savePath,
                plan.Seed,
                "gate-d-route-run",
                100,
                value => bootstrap = value,
                value => store = value);

            CombatOutcome bossOutcome = null;
            var completedRooms = 0;
            for (var index = 1; index < plan.ChapterOnePath.Count; index++)
            {
                var roomId = plan.ChapterOnePath[index];
                var roomType = store.OverworldRun.GetRoomType(roomId);
                if (roomType == OverworldRoomType.Event)
                {
                    yield return SelectAndConfirmLocalRoom(store, roomId, "安全跳过");
                }
                else if (roomType == OverworldRoomType.Shop)
                {
                    yield return SelectAndConfirmLocalRoom(store, roomId, "价格");
                }
                else
                {
                    yield return SelectAndResolveCombatRoom(
                        bootstrap,
                        store,
                        roomId,
                        BattleOutcome.VictorySettlement,
                        value =>
                        {
                            if (roomType == OverworldRoomType.Boss)
                            {
                                bossOutcome = value;
                            }
                        });
                }

                completedRooms++;
                Assert.That(SceneInputLockState.IsLocked, Is.False);
                AssertPersistentState(SceneId.OutOfBattleShell);
            }

            Assert.That(completedRooms, Is.GreaterThanOrEqualTo(3));
            Assert.That(bossOutcome, Is.Not.Null);
            var chapterTwo = store.OverworldRun.CreatePersistenceSnapshot();
            var expectedChapterTwo = new OverworldRunApplication(
                new RunStartPayload(
                    RunStartKind.NewGame,
                    "gate-d-expected-chapter-two",
                    plan.Seed,
                    plan.Seed.ToString(),
                    2,
                    chapterTwo.Era,
                    chapterTwo.Phase,
                    chapterTwo.Timecoins,
                    chapterTwo.CharacterId,
                    chapterTwo.DeckStableIds),
                new OverworldMapGenerationConfig(2),
                finalChapter: 3);
            Assert.That(chapterTwo.Chapter, Is.EqualTo(2));
            Assert.That(chapterTwo.MapFingerprint,
                Is.EqualTo(expectedChapterTwo.Map.Fingerprint));
            Assert.That(chapterTwo.CurrentNodeId,
                Is.EqualTo(expectedChapterTwo.Map.EntryNodeId.Value));

            var replay = store.OverworldRun.TryApplyCombatOutcome(
                bootstrap.ReserveTransitionSequence(),
                store.OverworldRun.Revision,
                bossOutcome);
            Assert.That(replay.Succeeded, Is.True);
            Assert.That(replay.WasAlreadyApplied, Is.True);
            var afterReplay = store.OverworldRun.CreatePersistenceSnapshot();
            Assert.That(afterReplay.Chapter, Is.EqualTo(2));
            Assert.That(afterReplay.MapFingerprint, Is.EqualTo(chapterTwo.MapFingerprint));
            Assert.That(afterReplay.CurrentNodeId, Is.EqualTo(chapterTwo.CurrentNodeId));

            var defeatRoom = FindAvailableCombatRoom(store.OverworldRun);
            yield return SelectAndResolveCombatRoom(
                bootstrap,
                store,
                defeatRoom,
                BattleOutcome.Defeat,
                value => { });
            Assert.That(bootstrap.CurrentScene, Is.EqualTo(SceneId.GameOver));
            Assert.That(Object.FindAnyObjectByType<GameOverPresenter>(), Is.Not.Null);
            AssertPersistentState(SceneId.GameOver);

            FindButton("ReturnButton").onClick.Invoke();
            yield return AwaitInteractiveScene(bootstrap, SceneId.MainMenu);
            Assert.That(store.OverworldRun, Is.Null);
            Assert.That(store.OutOfBattleState, Is.Null);
            Assert.That(store.ActiveLaunch, Is.Null);
            Assert.That(store.LastOutcome, Is.Null);
            AssertPersistentState(SceneId.MainMenu);

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
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
            return Start(runId, 731);
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
                StarterDeck.OrderedStableIds);
        }

        private static int FindSeedWithAvailableRoomType(OverworldRoomType roomType)
        {
            var generator = new DeterministicOverworldMapGenerator();
            for (var seed = 1; seed <= 10000; seed++)
            {
                var map = generator.Generate(seed, new OverworldMapGenerationConfig(1));
                foreach (var nodeId in map.GetOutgoing(map.EntryNodeId))
                {
                    if (map.GetNode(nodeId).RoomType == roomType)
                    {
                        return seed;
                    }
                }
            }

            Assert.Fail("No deterministic seed exposed an adjacent " + roomType + " room.");
            return 0;
        }

        private static MapNodeId FindAvailableRoom(
            OverworldRunApplication application,
            OverworldRoomType roomType)
        {
            foreach (var nodeId in application.GetAvailableRoomIds())
            {
                if (application.GetRoomType(nodeId) == roomType)
                {
                    return nodeId;
                }
            }

            Assert.Fail("The generated map has no available " + roomType + " room.");
            return default(MapNodeId);
        }

        private static IEnumerator SelectAndConfirmLocalRoom(
            SceneFlowStateStore store,
            MapNodeId roomId,
            string expectedDetail)
        {
            var movement = Object.FindAnyObjectByType<OverworldMovementPresenter>();
            Assert.That(movement, Is.Not.Null);
            var view = movement.GetComponentsInChildren<OverworldNodeView>(true)
                .Single(candidate => candidate.Id == roomId);
            view.GetComponent<Button>().onClick.Invoke();
            var deadline = Time.realtimeSinceStartup + TaskTimeoutSeconds;
            while (movement.IsMoving)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail("Generated room selection timed out.");
                }

                yield return null;
            }

            Assert.That(movement.SelectedNodeId, Is.EqualTo(roomId.Value));
            var presenter = Object.FindAnyObjectByType<OutOfBattleShellPresenter>();
            Assert.That(presenter.RoomId, Is.EqualTo(roomId.Value));
            Assert.That(FindText("RoomDetail").text, Does.Contain(expectedDetail));
            FindButton("CombatRoom").onClick.Invoke();
            yield return null;
            Assert.That(FindButton("ConfirmButton").interactable, Is.True);
            FindButton("ConfirmButton").onClick.Invoke();
            while (!store.OverworldRun.CreatePersistenceSnapshot()
                       .SettledNodeIds.Contains(roomId.Value))
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail("Local room completion timed out.");
                }

                yield return null;
            }

            yield return null;
        }

        private static IEnumerator StartGateDRun(
            string savePath,
            int seed,
            string runId,
            int timecoins,
            Action<BootstrapRoot> captureBootstrap,
            Action<SceneFlowStateStore> captureStore)
        {
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;
            var bootstrap = Object.FindAnyObjectByType<BootstrapRoot>();
            yield return AwaitTask(bootstrap.InitializationTask, "Bootstrap initialization");
            var store = Object.FindAnyObjectByType<SceneFlowStateStore>();
            store.ConfigurePersistencePath(savePath);
            var menuSequence = bootstrap.ReserveTransitionSequence();
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    menuSequence,
                    "gate-d-start-menu-" + menuSequence,
                    SceneId.GameStart,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)));
            var runSequence = bootstrap.ReserveTransitionSequence();
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    runSequence,
                    "gate-d-start-run-" + runSequence,
                    SceneId.MainMenu,
                    SceneId.OutOfBattleShell,
                    Start(runId, seed, timecoins)));
            captureBootstrap(bootstrap);
            captureStore(store);
        }

        private static IEnumerator RestartAndContinue(
            string savePath,
            Action<BootstrapRoot> captureBootstrap,
            Action<SceneFlowStateStore> captureStore)
        {
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;
            var bootstrap = Object.FindAnyObjectByType<BootstrapRoot>();
            yield return AwaitTask(
                bootstrap.InitializationTask,
                "Restarted Bootstrap initialization");
            var store = Object.FindAnyObjectByType<SceneFlowStateStore>();
            store.ConfigurePersistencePath(savePath);
            var sequence = bootstrap.ReserveTransitionSequence();
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    sequence,
                    "gate-d-restart-menu-" + sequence,
                    SceneId.GameStart,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)));
            var continueButton = FindButton("Continue");
            Assert.That(continueButton.interactable, Is.True, store.ContinueDetail);
            continueButton.onClick.Invoke();
            yield return AwaitInteractiveScene(bootstrap, SceneId.OutOfBattleShell);
            captureBootstrap(bootstrap);
            captureStore(store);
        }

        private static IEnumerator SelectAndResolveCombatRoom(
            BootstrapRoot bootstrap,
            SceneFlowStateStore store,
            MapNodeId roomId,
            BattleOutcome outcome,
            Action<CombatOutcome> captureOutcome)
        {
            var movement = Object.FindAnyObjectByType<OverworldMovementPresenter>();
            Assert.That(movement, Is.Not.Null);
            var view = movement.GetComponentsInChildren<OverworldNodeView>(true)
                .Single(candidate => candidate.Id == roomId);
            Assert.That(view.State, Is.EqualTo(OverworldNodeVisualState.Available));
            view.GetComponent<Button>().onClick.Invoke();
            var deadline = Time.realtimeSinceStartup + TaskTimeoutSeconds;
            while (movement.IsMoving)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail("Combat room selection timed out.");
                }

                yield return null;
            }

            FindButton("CombatRoom").onClick.Invoke();
            yield return null;
            var confirm = FindButton("ConfirmButton");
            Assert.That(confirm.interactable, Is.True);
            confirm.onClick.Invoke();
            yield return AwaitInteractiveScene(bootstrap, SceneId.Combat);

            var controller = Object.FindAnyObjectByType<VerticalSliceController>();
            Assert.That(controller, Is.Not.Null);
            var resolution = controller.ResolveBattleOutcome(outcome);
            Assert.That(resolution.Succeeded, Is.True, resolution.Failure.ToString());
            var settlement = Object.FindAnyObjectByType<BattleSettlementPresenter>();
            Assert.That(settlement, Is.Not.Null);
            if (outcome == BattleOutcome.VictorySettlement)
            {
                Assert.That(settlement.IsRewardVisible, Is.True);
                FindButton("RewardButton").onClick.Invoke();
                yield return AwaitInteractiveScene(bootstrap, SceneId.OutOfBattleShell);
            }
            else
            {
                Assert.That(settlement.IsRewardVisible, Is.False);
                yield return AwaitInteractiveScene(bootstrap, SceneId.GameOver);
            }

            captureOutcome(store.LastOutcome);
        }

        private static IEnumerator AwaitInteractiveScene(
            BootstrapRoot bootstrap,
            SceneId sceneId)
        {
            var deadline = Time.realtimeSinceStartup + TaskTimeoutSeconds;
            while (bootstrap.CurrentScene != sceneId ||
                   SceneInputLockState.IsLocked ||
                   !IsOnlyEntryInteractive(sceneId))
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail("Timed out waiting for interactive " + sceneId + ".");
                }

                yield return null;
            }

            yield return null;
        }

        private static void AssertEquivalentRunBoundary(
            OverworldPersistenceSnapshot expected,
            OverworldPersistenceSnapshot actual)
        {
            Assert.That(actual.RunId, Is.EqualTo(expected.RunId));
            Assert.That(actual.Chapter, Is.EqualTo(expected.Chapter));
            Assert.That(actual.MapFingerprint, Is.EqualTo(expected.MapFingerprint));
            Assert.That(actual.CurrentNodeId, Is.EqualTo(expected.CurrentNodeId));
            Assert.That(actual.Timecoins, Is.EqualTo(expected.Timecoins));
            Assert.That(actual.DeckStableIds, Is.EqualTo(expected.DeckStableIds));
            Assert.That(actual.SettledNodeIds, Is.EqualTo(expected.SettledNodeIds));
            Assert.That(actual.VisitedNodeIds, Is.EqualTo(expected.VisitedNodeIds));
        }

        private static GateDRoutePlan FindGateDRoutePlan()
        {
            var generator = new DeterministicOverworldMapGenerator();
            for (var seed = 1; seed <= 10000; seed++)
            {
                var chapterOne = generator.Generate(
                    seed,
                    new OverworldMapGenerationConfig(1));
                var path = new List<MapNodeId> { chapterOne.EntryNodeId };
                IReadOnlyList<MapNodeId> route;
                if (!TryFindGateDPath(chapterOne, chapterOne.EntryNodeId, path, out route))
                {
                    continue;
                }

                var chapterTwo = generator.Generate(
                    seed,
                    new OverworldMapGenerationConfig(2));
                var hasChapterTwoCombat = chapterTwo.GetOutgoing(chapterTwo.EntryNodeId)
                    .Any(nodeId => IsCombatRoom(chapterTwo.GetNode(nodeId).RoomType));
                if (hasChapterTwoCombat)
                {
                    return new GateDRoutePlan(seed, route);
                }
            }

            Assert.Fail("No deterministic Gate D route was found.");
            return null;
        }

        private static bool TryFindGateDPath(
            OverworldMapDefinition map,
            MapNodeId current,
            List<MapNodeId> path,
            out IReadOnlyList<MapNodeId> route)
        {
            if (current == map.BossNodeId)
            {
                var roomTypes = path.Skip(1)
                    .Select(nodeId => map.GetNode(nodeId).RoomType)
                    .ToArray();
                if (roomTypes.Length >= 4 &&
                    roomTypes[0] == OverworldRoomType.Event &&
                    roomTypes[1] == OverworldRoomType.Shop &&
                    roomTypes.Skip(2).All(type => type != OverworldRoomType.Shop) &&
                    roomTypes.Take(roomTypes.Length - 1).Any(IsCombatRoom))
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
                if (TryFindGateDPath(map, next, path, out route))
                {
                    return true;
                }

                path.RemoveAt(path.Count - 1);
            }

            route = null;
            return false;
        }

        private static bool IsCombatRoom(OverworldRoomType roomType)
        {
            return roomType == OverworldRoomType.Battle ||
                roomType == OverworldRoomType.Elite ||
                roomType == OverworldRoomType.Boss;
        }

        private static IEnumerator AssertInvalidSaveNotice(bool futureVersion)
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "timekey-gate-c-invalid-" + System.Guid.NewGuid().ToString("N"));
            var savePath = Path.Combine(directory, "overworld.json");
            Directory.CreateDirectory(directory);
            if (futureVersion)
            {
                var run = new OverworldRunApplication(
                    Start("gate-c-future-run", 731),
                    new OverworldMapGenerationConfig(1),
                    finalChapter: 3);
                var repository = new OverworldSaveRepository(savePath);
                Assert.That(repository.Save(
                    OverworldSaveMapper.ToDocument(run.CreatePersistenceSnapshot())).Succeeded,
                    Is.True);
                var json = File.ReadAllText(savePath)
                    .Replace("\"schemaVersion\": 2", "\"schemaVersion\": 999");
                File.WriteAllText(savePath, json);
            }
            else
            {
                File.WriteAllText(savePath, "{not-json");
            }

            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;
            var bootstrap = Object.FindAnyObjectByType<BootstrapRoot>();
            yield return AwaitTask(bootstrap.InitializationTask, "Bootstrap initialization");
            var store = Object.FindAnyObjectByType<SceneFlowStateStore>();
            store.ConfigurePersistencePath(savePath);
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    1,
                    futureVersion ? "gate-c-future-menu" : "gate-c-corrupt-menu",
                    SceneId.GameStart,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)));

            var navigation = Object.FindAnyObjectByType<MainMenuSceneNavigation>();
            var presenter = Object.FindAnyObjectByType<MainMenuPresenter>();
            Assert.That(FindButton("Continue").interactable, Is.False);
            Assert.That(presenter.IsModalOpen, Is.True);
            Assert.That(navigation.LastContinueNotice,
                Does.Contain(futureVersion ? "更新版本" : "损坏"));
            Assert.That(FindText("ModalBody").text,
                Does.Contain(futureVersion ? "更新版本" : "损坏"));

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        private static int FindSeedWithAvailableCombatRoom()
        {
            var generator = new DeterministicOverworldMapGenerator();
            for (var seed = 1; seed <= 1000; seed++)
            {
                var map = generator.Generate(seed, new OverworldMapGenerationConfig(1));
                foreach (var nodeId in map.GetOutgoing(map.EntryNodeId))
                {
                    var type = map.GetNode(nodeId).RoomType;
                    if (type == OverworldRoomType.Battle ||
                        type == OverworldRoomType.Elite ||
                        type == OverworldRoomType.Boss)
                    {
                        return seed;
                    }
                }
            }

            Assert.Fail("No deterministic seed exposed an adjacent combat room.");
            return 0;
        }

        private static MapNodeId FindAvailableCombatRoom(
            OverworldRunApplication application)
        {
            foreach (var nodeId in application.GetAvailableRoomIds())
            {
                var type = application.GetRoomType(nodeId);
                if (type == OverworldRoomType.Battle ||
                    type == OverworldRoomType.Elite ||
                    type == OverworldRoomType.Boss)
                {
                    return nodeId;
                }
            }

            Assert.Fail("The generated map has no available combat room.");
            return default(MapNodeId);
        }

        private static Button FindButton(string name)
        {
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                if (button.gameObject.name == name)
                {
                    return button;
                }
            }

            Assert.Fail("Missing button " + name + ".");
            return null;
        }

        private static Text FindText(string name)
        {
            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include))
            {
                if (text.gameObject.name == name)
                {
                    return text;
                }
            }

            Assert.Fail("Missing text " + name + ".");
            return null;
        }

        private static bool IsOnlyEntryInteractive(SceneId sceneId)
        {
            var entries = Object.FindObjectsByType<SceneContentEntry>(
                FindObjectsInactive.Include);
            return entries.Length == 1 && entries[0].SceneId == sceneId &&
                entries[0].IsInteractive;
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

        private sealed class GateDRoutePlan
        {
            public GateDRoutePlan(int seed, IReadOnlyList<MapNodeId> chapterOnePath)
            {
                Seed = seed;
                ChapterOnePath = chapterOnePath;
            }

            public int Seed { get; }
            public IReadOnlyList<MapNodeId> ChapterOnePath { get; }
        }

        private sealed class ThrowBeforeReplace : IOverworldSaveWriteFaultInjector
        {
            public void OnStage(OverworldSaveWriteStage stage, string temporaryPath)
            {
                if (stage == OverworldSaveWriteStage.BeforeReplace)
                {
                    throw new IOException("Injected Gate B persistence failure.");
                }
            }
        }
    }
}
