using System;
using System.Threading;
using System.Threading.Tasks;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.BattleFlow;
using TimeKey.Presentation;
using UnityEngine;

namespace TimeKey.Composition.SceneFlow
{
    [DisallowMultipleComponent]
    public sealed class CombatSceneNavigation : MonoBehaviour
    {
        [SerializeField] private BootstrapRoot bootstrap = null;
        [SerializeField] private SceneFlowStateStore stateStore = null;
        [SerializeField] private VerticalSliceController controller = null;

        private CancellationTokenSource _lifetime;
        private bool _inFlight;

        public CombatOutcome PreparedOutcome { get; private set; }

        public SceneTransitionResult LastResult { get; private set; }

        private void OnEnable()
        {
            _lifetime = new CancellationTokenSource();
        }

        private void OnDisable()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
        }

        private void Update()
        {
            if (_inFlight || SceneInputLockState.IsLocked || controller == null ||
                controller.BattleFlow == null)
            {
                return;
            }

            var settlement = controller.BattleFlow.Settlement;
            var canSubmit = settlement.Outcome == BattleOutcome.Defeat ||
                (settlement.Outcome == BattleOutcome.VictorySettlement &&
                 settlement.IsRewardClaimed);
            if (canSubmit)
            {
                SubmitTerminalOutcome();
            }
        }

        private async void SubmitTerminalOutcome()
        {
            try
            {
                await SubmitTerminalOutcomeAsync(_lifetime.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        public async Task<SceneTransitionResult> SubmitTerminalOutcomeAsync(
            CancellationToken cancellationToken = default)
        {
            if (_inFlight)
            {
                return LastResult;
            }

            bootstrap = bootstrap != null
                ? bootstrap
                : FindFirstObjectByType<BootstrapRoot>();
            stateStore = stateStore != null
                ? stateStore
                : FindFirstObjectByType<SceneFlowStateStore>();
            if (bootstrap == null || stateStore == null || controller == null ||
                stateStore.ActiveLaunch == null)
            {
                return null;
            }

            var settlement = controller.BattleFlow?.Settlement;
            if (settlement == null || settlement.Outcome == BattleOutcome.Active ||
                (settlement.Outcome == BattleOutcome.VictorySettlement &&
                 !settlement.IsRewardClaimed))
            {
                return null;
            }

            _inFlight = true;
            try
            {
                var boundary = controller.CreateBattleReturnBoundary();
                if (!boundary.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Combat could not create a typed return boundary: " + boundary.Failure + ".");
                }

                var launch = stateStore.ActiveLaunch;
                if (PreparedOutcome == null)
                {
                    var outcome = CombatOutcome.TryCreate(
                        launch.LaunchCorrelationId + "-outcome",
                        launch,
                        settlement,
                        boundary.Payload);
                    if (!outcome.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Combat could not create a typed outcome: " + outcome.Failure + ".");
                    }

                    PreparedOutcome = outcome.Outcome;
                }

                await bootstrap.InitializationTask;
                cancellationToken.ThrowIfCancellationRequested();
                var sequence = bootstrap.ReserveTransitionSequence();
                LastResult = await bootstrap.TransitionAsync(
                    new SceneTransitionRequest(
                        sequence,
                        "combat-outcome-" + sequence,
                        SceneId.Combat,
                        PreparedOutcome.TargetScene,
                        PreparedOutcome),
                    CancellationToken.None);
                if (!LastResult.Succeeded)
                {
                    _inFlight = false;
                    Debug.LogError("Combat outcome transition failed: " + LastResult.Message, this);
                }

                return LastResult;
            }
            catch
            {
                _inFlight = false;
                throw;
            }
        }
    }
}
