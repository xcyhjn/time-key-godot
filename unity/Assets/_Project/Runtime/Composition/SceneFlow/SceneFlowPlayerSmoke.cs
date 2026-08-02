 using System;
using System.Threading.Tasks;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Deck;
using UnityEngine;

namespace TimeKey.Composition.SceneFlow
{
    [DisallowMultipleComponent]
    public sealed class SceneFlowPlayerSmoke : MonoBehaviour
    {
        public const string CommandLineFlag = "-timekeyCombatShellSmoke";
        public const string PassMarker = "TIMEKEY_COMBAT_SHELL_GATE_A_PLAYER_SMOKE_PASS";

        [SerializeField] private BootstrapRoot bootstrap = null;

        private int _quitCountdown;
        private int _exitCode;

        private async void Start()
        {
            if (!HasCommandLineFlag())
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
                Debug.LogException(exception);
                RequestQuit(1);
            }
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

        private void RequestQuit(int exitCode)
        {
            _exitCode = exitCode;
            _quitCountdown = 2;
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

        private static bool HasCommandLineFlag()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length; index++)
            {
                if (string.Equals(
                        arguments[index],
                        CommandLineFlag,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
