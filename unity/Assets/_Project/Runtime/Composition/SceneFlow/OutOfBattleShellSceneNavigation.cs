using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using TimeKey.Application.EraClock;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.Overworld;
using TimeKey.Presentation.CombatShell;
using TimeKey.Presentation.EraClock;
using TimeKey.Presentation.OutOfBattleShell;
using TimeKey.Presentation.OverworldMovement;
using TimeKey.Presentation.SceneFlowFinale;
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
        [SerializeField] private OverworldMovementPresenter movementPresenter = null;
        [SerializeField] private EraClockPresenter eraClockPresenter = null;
        [SerializeField] private LayeredSceneRevealPresenter revealPresenter = null;

        private CancellationTokenSource _lifetime;
        private Coroutine _eraClockRoutine;
        private long _eraClockSequence;
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
            movementPresenter = movementPresenter != null
                ? movementPresenter
                : GetComponentInChildren<OverworldMovementPresenter>(true);
            eraClockPresenter = eraClockPresenter != null
                ? eraClockPresenter
                : GetComponentInChildren<EraClockPresenter>(true);
            revealPresenter = revealPresenter != null
                ? revealPresenter
                : GetComponentInChildren<LayeredSceneRevealPresenter>(true);
            if (presenter != null)
            {
                presenter.RoomConfirmationRequested += OnRoomConfirmationRequested;
                if (stateStore?.OutOfBattleState != null)
                {
                    if (movementPresenter != null)
                    {
                        movementPresenter.ApplySceneFlowState(
                            stateStore.OutOfBattleState.CurrentRoomId,
                            stateStore.OutOfBattleState.SettledRoomIds);
                    }

                    presenter.Apply(stateStore.OutOfBattleState);
                    BindFirstAvailableCombatRoom();
                    ApplySharedTopHud(stateStore.OutOfBattleState);
                    BeginEraClockProjection(stateStore.OutOfBattleState);
                }
            }

            if (movementPresenter != null)
            {
                movementPresenter.ArrivalCommitted += OnArrivalCommitted;
            }
        }

        private void OnDisable()
        {
            if (_eraClockRoutine != null)
            {
                StopCoroutine(_eraClockRoutine);
                _eraClockRoutine = null;
            }

            if (eraClockPresenter != null && eraClockPresenter.CurrentSnapshot != null)
            {
                eraClockPresenter.CancelAndSnap();
            }

            if (presenter != null)
            {
                presenter.RoomConfirmationRequested -= OnRoomConfirmationRequested;
            }

            if (movementPresenter != null)
            {
                movementPresenter.ArrivalCommitted -= OnArrivalCommitted;
            }

            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
        }

        private void OnArrivalCommitted(TimeKey.Domain.OverworldMovement.MapNodeId nodeId)
        {
            if (movementPresenter == null || presenter == null ||
                !movementPresenter.TryGetNodeType(nodeId, out var nodeType))
            {
                return;
            }

            presenter.SetCurrentRoomIdentity(nodeId, "时隙节点 " + nodeId.Value);
            presenter.SetRoomAvailable(nodeType != TimeKey.Domain.OverworldMovement.MapNodeType.Start);
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
            movementPresenter?.SetTransitionLocked(true);
            try
            {
                await bootstrap.InitializationTask;
                cancellationToken.ThrowIfCancellationRequested();
                var sequence = bootstrap.ReserveTransitionSequence();
                var state = stateStore.OutOfBattleState;
                var applicationResult = stateStore.PrepareCombatLaunch(
                    sequence,
                    request.RoomId,
                    state.RunId + "/" + request.RoomId + "/launch-" + sequence,
                    battleTag,
                    DeriveBattleSeed(state.RunSeed, request.RoomId));
                if (!applicationResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Overworld room selection failed: " +
                        applicationResult.Failure + ".");
                }

                PreparedLaunch = applicationResult.Launch;

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
                    PreparedLaunch = null;
                    _inFlight = false;
                    presenter?.SetTransitionLocked(false);
                    movementPresenter?.SetTransitionLocked(false);
                    presenter?.RejectPendingConfirmation();
                    Debug.LogError(
                        "Out-of-battle combat transition failed: " + LastResult.Message,
                        this);
                }

                return LastResult;
            }
            catch
            {
                PreparedLaunch = null;
                _inFlight = false;
                presenter?.SetTransitionLocked(false);
                movementPresenter?.SetTransitionLocked(false);
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

        private void BindFirstAvailableCombatRoom()
        {
            var application = stateStore?.OverworldRun;
            if (application == null || presenter == null)
            {
                return;
            }

            foreach (var nodeId in application.GetAvailableRoomIds())
            {
                var roomType = application.GetRoomType(nodeId);
                if (roomType != OverworldRoomType.Battle &&
                    roomType != OverworldRoomType.Elite &&
                    roomType != OverworldRoomType.Boss)
                {
                    continue;
                }

                presenter.SetCurrentRoomIdentity(
                    nodeId,
                    RoomTitle(roomType.Value, nodeId.Value));
                presenter.SetRoomAvailable(true);
                return;
            }

            presenter.SetRoomAvailable(false);
        }

        private static string RoomTitle(OverworldRoomType roomType, string roomId)
        {
            switch (roomType)
            {
                case OverworldRoomType.Elite:
                    return "精英战斗 · " + roomId;
                case OverworldRoomType.Boss:
                    return "章节首领 · " + roomId;
                default:
                    return "战斗房间 · " + roomId;
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

        private void BeginEraClockProjection(OutOfBattleShellState state)
        {
            if (eraClockPresenter == null || state == null)
            {
                return;
            }

            eraClockPresenter.ApplySnapshot(EraClockSnapshotAdapter.FromOutOfBattle(
                state,
                NextEraClockSequence(),
                EraClockAnchorTarget.Center));
            if (_eraClockRoutine != null)
            {
                StopCoroutine(_eraClockRoutine);
            }

            _eraClockRoutine = StartCoroutine(MoveEraClockToHudAfterReveal());
        }

        private IEnumerator MoveEraClockToHudAfterReveal()
        {
            yield return null;
            while (revealPresenter != null && !revealPresenter.IsComplete)
            {
                yield return null;
            }

            _eraClockRoutine = null;
            var state = stateStore?.OutOfBattleState;
            if (!isActiveAndEnabled || state == null || eraClockPresenter == null)
            {
                yield break;
            }

            eraClockPresenter.ApplySnapshot(EraClockSnapshotAdapter.FromOutOfBattle(
                state,
                NextEraClockSequence(),
                EraClockAnchorTarget.Hud));
        }

        private long NextEraClockSequence()
        {
            _eraClockSequence++;
            return _eraClockSequence;
        }
    }
}
