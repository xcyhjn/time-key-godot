using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition.SceneFlow;
using TimeKey.Domain.BattleFlow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TimeKey.Tests.PlayMode.SceneFlow
{
    public sealed class SceneFlowAdditiveTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_PerformsTypedRoundTripsWithoutPersistentDuplicates()
        {
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;
            var bootstrap = Object.FindAnyObjectByType<BootstrapRoot>();
            Assert.That(bootstrap, Is.Not.Null);
            while (!bootstrap.IsReady)
            {
                yield return null;
            }

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

            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    2,
                    "menu-to-shell",
                    SceneId.MainMenu,
                    SceneId.OutOfBattleShell,
                    new EmptySceneTransitionPayload(SceneId.OutOfBattleShell)));
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

            var defeatLaunch = Launch("defeat", "room-defeat");
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    5,
                    defeatLaunch.LaunchCorrelationId,
                    SceneId.OutOfBattleShell,
                    SceneId.Combat,
                    defeatLaunch));
            var defeat = Outcome(defeatLaunch, BattleOutcome.Defeat);
            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    6,
                    defeat.OutcomeCorrelationId,
                    SceneId.Combat,
                    SceneId.GameOver,
                    defeat));
            AssertPersistentState(SceneId.GameOver);

            yield return Transition(
                bootstrap,
                new SceneTransitionRequest(
                    7,
                    "game-over-to-menu",
                    SceneId.GameOver,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)));
            AssertPersistentState(SceneId.MainMenu);
        }

        private static IEnumerator Transition(
            BootstrapRoot bootstrap,
            SceneTransitionRequest request)
        {
            Task<SceneTransitionResult> task = bootstrap.TransitionAsync(request);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            Assert.That(task.IsFaulted, Is.False, task.Exception?.ToString());
            Assert.That(task.IsCanceled, Is.False);
            Assert.That(task.Result.Succeeded, Is.True, task.Result.Message);
            Assert.That(task.Result.IsInputLocked, Is.False);
            Assert.That(task.Result.ActiveScene, Is.EqualTo(request.Target));
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

        private static CombatLaunchPayload Launch(string identity, string roomId)
        {
            return new CombatLaunchPayload(
                identity + "-launch",
                "gate-a-run",
                731,
                1,
                1,
                1,
                roomId,
                "silver-character",
                0,
                "combat-vertical-slice",
                731,
                new[]
                {
                    "lighting", "earthquake", "recover", "built", "poison",
                    "wind", "tornado", "lighting", "recover", "built", "poison", "wind"
                });
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
    }
}
