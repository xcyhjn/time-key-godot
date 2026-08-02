using System;
using System.Threading;
using System.Threading.Tasks;
using TimeKey.Application.SceneFlow;
using TimeKey.Presentation.CombatShell;
using TimeKey.Presentation.OutOfBattleShell;
using UnityEngine;

namespace TimeKey.Composition.SceneFlow
{
    [DisallowMultipleComponent]
    public sealed class OutOfBattleShellSceneNavigation : MonoBehaviour
    {
        [SerializeField] private BootstrapRoot bootstrap = null;
        [SerializeField] private SceneFlowStateStore stateStore = null;
        [SerializeField] private OutOfBattleShellPresenter presenter = null;
        [SerializeField] private CombatTopHudPresenter sharedTopHud = null;
        [SerializeField] private string battleTag = "combat-vertical-slice";

        private CancellationTokenSource _lifetime;
        private bool _inFlight;

        public CombatLaunchPayload PreparedLaunch { get; private set; }

        public SceneTransitionResult LastResult { get; private set; }

        private void OnEnable()
        {
            _lifetime = new CancellationTokenSource();
            presenter = presenter != null
                ? presenter
                : GetComponentInChildren<OutOfBattleShellPresenter>(true);
            stateStore = stateStore != null
                ? stateStore
                : FindFirstObjectByType<SceneFlowStateStore>();
            if (presenter != null)
            {
                presenter.RoomConfirmationRequested += OnRoomConfirmationRequested;
                if (stateStore?.OutOfBattleState != null)
                {
                    presenter.Apply(stateStore.OutOfBattleState);
                    ApplySharedTopHud(stateStore.OutOfBattleState);
                }
            }
        }

        private void OnDisable()
        {
            if (presenter != null)
            {
                presenter.RoomConfirmationRequested -= OnRoomConfirmationRequested;
            }

            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
        }

        private async void OnRoomConfirmationRequested(
            OutOfBattleRoomConfirmationRequest request)
        {
            try
            {
                await LaunchCombatAsync(request, _lifetime.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _inFlight = false;
                presenter?.SetTransitionLocked(false);
                presenter?.RejectPendingConfirmation();
                Debug.LogException(exception, this);
            }
        }

        public async Task<SceneTransitionResult> LaunchCombatAsync(
            OutOfBattleRoomConfirmationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

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
            if (bootstrap == null || stateStore?.OutOfBattleState == null)
            {
                throw new InvalidOperationException(
                    "Out-of-battle navigation requires Bootstrap and a typed run state.");
            }

            _inFlight = true;
            presenter?.SetTransitionLocked(true);
            try
            {
                await bootstrap.InitializationTask;
                cancellationToken.ThrowIfCancellationRequested();
                var sequence = bootstrap.ReserveTransitionSequence();
                if (PreparedLaunch == null)
                {
                    var state = stateStore.OutOfBattleState;
                    PreparedLaunch = state.CreateCombatLaunch(
                        state.RunId + "/" + request.RoomId + "/launch-" + sequence,
                        request.RoomId,
                        battleTag,
                        DeriveBattleSeed(state.RunSeed, request.RoomId));
                }

                LastResult = await bootstrap.TransitionAsync(
                    new SceneTransitionRequest(
                        sequence,
                        "out-of-battle-combat-" + sequence,
                        SceneId.OutOfBattleShell,
                        SceneId.Combat,
                        PreparedLaunch),
                    CancellationToken.None);
                if (!LastResult.Succeeded)
                {
                    _inFlight = false;
                    presenter?.SetTransitionLocked(false);
                    presenter?.RejectPendingConfirmation();
                    Debug.LogError(
                        "Out-of-battle combat transition failed: " + LastResult.Message,
                        this);
                }

                return LastResult;
            }
            catch
            {
                _inFlight = false;
                presenter?.SetTransitionLocked(false);
                presenter?.RejectPendingConfirmation();
                throw;
            }
        }

        private static int DeriveBattleSeed(int runSeed, string roomId)
        {
            unchecked
            {
                var hash = (uint)runSeed;
                for (var index = 0; index < roomId.Length; index++)
                {
                    hash ^= roomId[index];
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7fffffff);
            }
        }

        private void ApplySharedTopHud(OutOfBattleShellState state)
        {
            if (sharedTopHud == null || state == null)
            {
                return;
            }

            sharedTopHud.Apply(new CombatTopHudDisplay(
                state.Era,
                state.Phase,
                state.Timecoins,
                state.DeckStableIds.Count,
                0,
                0,
                0,
                0,
                state.CharacterId == "silver-character"
                    ? "银 · 时钥行者"
                    : state.CharacterId,
                false));
        }
    }
}
