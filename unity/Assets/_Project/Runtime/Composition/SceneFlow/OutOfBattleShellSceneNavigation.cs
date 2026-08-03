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
                presenter.RoomInteractionStateChanged += OnRoomInteractionStateChanged;
                if (stateStore?.OutOfBattleState != null)
                {
                    if (movementPresenter != null)
                    {
                        if (stateStore.OverworldRun != null)
                        {
                            movementPresenter.BindAuthoritativeMap(
                                stateStore.OverworldRun.Map,
                                stateStore.OverworldRun.ChapterSnapshot);
                        }
                        else
                        {
                            movementPresenter.ApplySceneFlowState(
                                stateStore.OutOfBattleState.CurrentRoomId,
                                stateStore.OutOfBattleState.SettledRoomIds);
                        }
                    }

                    presenter.Apply(stateStore.OutOfBattleState);
                    BindCurrentMapPrompt();
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
                presenter.RoomInteractionStateChanged -= OnRoomInteractionStateChanged;
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
                !movementPresenter.TryGetRoomType(nodeId, out var roomType))
            {
                return;
            }

            BindRoomPresentation(nodeId, roomType);
        }

        private void OnRoomInteractionStateChanged(OutOfBattleRoomInteractionState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.RoomId) ||
                movementPresenter == null)
            {
                return;
            }

            movementPresenter.SetRoomInteractionState(
                new TimeKey.Domain.OverworldMovement.MapNodeId(state.RoomId),
                state.Selected,
                state.Confirming);
        }

        private async void OnRoomConfirmationRequested(
            OutOfBattleRoomConfirmationRequest request)
        {
            try
            {
                var roomType = stateStore?.OverworldRun?.GetRoomType(
                    new TimeKey.Domain.OverworldMovement.MapNodeId(request.RoomId));
                if (roomType == OverworldRoomType.Event || roomType == OverworldRoomType.Shop)
                {
                    await CompleteLocalRoomAsync(request, roomType.Value, _lifetime.Token);
                }
                else
                {
                    await LaunchCombatAsync(request, _lifetime.Token);
                }
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
            movementPresenter?.RequestRoomPreparation(
                new TimeKey.Domain.OverworldMovement.MapNodeId(request.RoomId));
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

        private async Task CompleteLocalRoomAsync(
            OutOfBattleRoomConfirmationRequest request,
            OverworldRoomType roomType,
            CancellationToken cancellationToken)
        {
            if (_inFlight)
            {
                return;
            }

            bootstrap = bootstrap != null
                ? bootstrap
                : FindFirstObjectByType<BootstrapRoot>();
            stateStore = stateStore != null
                ? stateStore
                : FindFirstObjectByType<SceneFlowStateStore>();
            if (bootstrap == null || stateStore?.OverworldRun == null)
            {
                throw new InvalidOperationException(
                    "Local overworld rooms require Bootstrap and an authoritative run.");
            }

            _inFlight = true;
            movementPresenter?.RequestRoomPreparation(
                new TimeKey.Domain.OverworldMovement.MapNodeId(request.RoomId));
            presenter?.SetTransitionLocked(true);
            movementPresenter?.SetTransitionLocked(true);
            try
            {
                await bootstrap.InitializationTask;
                cancellationToken.ThrowIfCancellationRequested();
                var entrySequence = bootstrap.ReserveTransitionSequence();
                var outcomeSequence = bootstrap.ReserveTransitionSequence();
                var result = roomType == OverworldRoomType.Event
                    ? stateStore.CompleteEventRoom(
                        entrySequence,
                        outcomeSequence,
                        request.RoomId)
                    : stateStore.PurchaseShopRoom(
                        entrySequence,
                        outcomeSequence,
                        request.RoomId);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Local overworld room failed: " + result.Failure + ": " +
                        result.ApplicationFailure + ": " + result.Detail);
                }

                movementPresenter?.ApplyAuthoritativeSnapshot(
                    stateStore.OverworldRun.ChapterSnapshot);
                presenter?.Apply(stateStore.OutOfBattleState);
                presenter?.SetCurrentRoomPresentation(
                    new TimeKey.Domain.OverworldMovement.MapNodeId(request.RoomId),
                    RoomTitle(roomType, request.RoomId),
                    roomType == OverworldRoomType.Event
                        ? "事件资源尚未迁移，本节点已安全跳过并保存。"
                        : "已购买 " + CardDisplayName(result.ShopOffer.CardStableId) +
                          "，消费 " + result.ShopOffer.TimecoinCost + " 时间币。",
                    "已完成",
                    confirmAvailable: false);
                presenter?.ShowResolvedRoom(
                    roomType == OverworldRoomType.Event
                        ? "事件节点已结算，后继路线已解锁。"
                        : "购买已写入牌组与存档，后继路线已解锁。");
                ApplySharedTopHud(stateStore.OutOfBattleState);
                movementPresenter?.FocusFirstAvailable();
                _inFlight = false;
                presenter?.SetTransitionLocked(false);
                movementPresenter?.SetTransitionLocked(false);
            }
            catch
            {
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

        private void BindCurrentMapPrompt()
        {
            var application = stateStore?.OverworldRun;
            if (application == null || presenter == null)
            {
                return;
            }

            var current = application.ChapterSnapshot.CurrentNodeId;
            presenter.SetCurrentRoomPresentation(
                current,
                "章节地图",
                "使用鼠标、方向键或手柄选择发光的相邻节点。",
                "进入",
                confirmAvailable: false);
            presenter.SetRoomAvailable(false);
        }

        private void BindRoomPresentation(
            TimeKey.Domain.OverworldMovement.MapNodeId nodeId,
            OverworldRoomType roomType)
        {
            var detail = "确认后进入该房间。";
            var confirmText = "进入战斗";
            var confirmAvailable = true;
            if (roomType == OverworldRoomType.Event)
            {
                detail = "Godot 的事件场景资源缺失。可安全跳过此节点，不会生成剧情或奖励。";
                confirmText = "跳过事件";
            }
            else if (roomType == OverworldRoomType.Shop)
            {
                var application = stateStore?.OverworldRun;
                var offer = application?.GetShopOffer(nodeId);
                var balance = application?.CreatePersistenceSnapshot().Timecoins ?? 0;
                if (offer == null)
                {
                    detail = "商店数据当前不可用。";
                    confirmAvailable = false;
                }
                else
                {
                    detail = "商品：" + CardDisplayName(offer.CardStableId) +
                        "\n价格：" + offer.TimecoinCost + " 时间币" +
                        "\n余额：" + balance;
                    confirmText = "购买并离开";
                    confirmAvailable = balance >= offer.TimecoinCost;
                    if (!confirmAvailable)
                    {
                        detail += "\n余额不足，当前不可购买。";
                    }
                }
            }

            presenter.SetCurrentRoomPresentation(
                nodeId,
                RoomTitle(roomType, nodeId.Value),
                detail,
                confirmText,
                confirmAvailable);
        }

        private static string RoomTitle(OverworldRoomType roomType, string roomId)
        {
            switch (roomType)
            {
                case OverworldRoomType.Elite:
                    return "精英战斗 · " + roomId;
                case OverworldRoomType.Boss:
                    return "章节首领 · " + roomId;
                case OverworldRoomType.Event:
                    return "时序事件 · " + roomId;
                case OverworldRoomType.Shop:
                    return "时钥商店 · " + roomId;
                default:
                    return "战斗房间 · " + roomId;
            }
        }

        private static string CardDisplayName(string stableId)
        {
            switch (stableId)
            {
                case "lighting":
                    return "雷击";
                case "earthquake":
                    return "地震";
                case "wind":
                    return "风";
                case "recover":
                    return "恢复";
                case "tower":
                    return "塔";
                case "poison":
                    return "毒";
                case "tornado":
                    return "龙卷风";
                default:
                    return stableId;
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
            movementPresenter?.FocusFirstAvailable();
        }

        private long NextEraClockSequence()
        {
            _eraClockSequence++;
            return _eraClockSequence;
        }
    }
}
