using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using TimeKey.Application.EraClock;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.Deck;
using TimeKey.Infrastructure.Persistence;
using TimeKey.Presentation.EraClock;
using TimeKey.Presentation.MainMenu;
using UnityEngine;

namespace TimeKey.Composition.SceneFlow
{
    [DisallowMultipleComponent]
    public sealed class MainMenuSceneNavigation : MonoBehaviour
    {
        private const string MasterVolumeKey = "timekey.settings.master-volume";
        private const string FullscreenKey = "timekey.settings.fullscreen";

        [SerializeField] private BootstrapRoot bootstrap = null;
        [SerializeField] private MainMenuPresenter presenter = null;
        [SerializeField] private SceneFlowStateStore stateStore = null;
        [SerializeField] private EraClockPresenter eraClockPresenter = null;
        [SerializeField] private int defaultRunSeed = 731;

        private CancellationTokenSource _lifetime;
        private long _eraClockSequence;

        public SceneTransitionResult LastResult { get; private set; }

        public string LastContinueNotice { get; private set; } = string.Empty;

        private void OnEnable()
        {
            _lifetime = new CancellationTokenSource();
            if (presenter == null)
            {
                presenter = GetComponentInChildren<MainMenuPresenter>(true);
            }

            stateStore = stateStore != null
                ? stateStore
                : FindFirstObjectByType<SceneFlowStateStore>();
            eraClockPresenter = eraClockPresenter != null
                ? eraClockPresenter
                : GetComponentInChildren<EraClockPresenter>(true);

            if (presenter != null)
            {
                presenter.CommandRequested += OnCommandRequested;
                presenter.SettingsRequested += OnSettingsRequested;
                ApplySettings(LoadSettings(), false);
                var continueAvailable = stateStore != null &&
                    stateStore.RefreshContinueAvailability();
                presenter.SetContinueAvailable(continueAvailable);
                if (!continueAvailable && stateStore != null)
                {
                    LastContinueNotice = ContinueNotice(stateStore.ContinueStatus);
                    if (!string.IsNullOrEmpty(LastContinueNotice))
                    {
                        presenter.ShowModal("存档不可继续", LastContinueNotice, false);
                    }
                }
            }

            if (stateStore?.OutOfBattleState != null && eraClockPresenter != null)
            {
                eraClockPresenter.ApplySnapshot(EraClockSnapshotAdapter.FromOutOfBattle(
                    stateStore.OutOfBattleState,
                    NextEraClockSequence(),
                    EraClockAnchorTarget.Center));
            }
        }

        private void OnDisable()
        {
            if (presenter != null)
            {
                presenter.CommandRequested -= OnCommandRequested;
                presenter.SettingsRequested -= OnSettingsRequested;
            }

            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
        }

        public MainMenuCommandRequest LastCommandRequest { get; private set; }

        public MainMenuSettingsRequest LastSettingsRequest { get; private set; }

        private void OnSettingsRequested(MainMenuSettingsRequest request)
        {
            if (request == null)
            {
                return;
            }

            LastSettingsRequest = request;
            ApplySettings(request.Snapshot, true);
        }

        private MainMenuSettingsSnapshot LoadSettings()
        {
            return new MainMenuSettingsSnapshot(
                PlayerPrefs.GetFloat(MasterVolumeKey, 1f),
                1f,
                1f,
                PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0);
        }

        private void ApplySettings(MainMenuSettingsSnapshot snapshot, bool persist)
        {
            presenter.ApplySettingsSnapshot(snapshot);
            AudioListener.volume = snapshot.MasterVolume;
            Screen.fullScreen = snapshot.Fullscreen;
            if (!persist)
            {
                return;
            }

            PlayerPrefs.SetFloat(MasterVolumeKey, snapshot.MasterVolume);
            PlayerPrefs.SetInt(FullscreenKey, snapshot.Fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        private async void OnCommandRequested(MainMenuCommandRequest request)
        {
            if (request == null)
            {
                return;
            }

            LastCommandRequest = request;
            var command = request.Command;
            if (command == MainMenuCommand.Exit)
            {
                UnityEngine.Application.Quit();
                return;
            }

            if (command != MainMenuCommand.NewGame &&
                command != MainMenuCommand.SeedGame &&
                command != MainMenuCommand.Continue)
            {
                return;
            }

            try
            {
                await NavigateToShellAsync(request, _lifetime.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                if (presenter != null)
                {
                    presenter.SetBusy(false);
                }

                Debug.LogException(exception, this);
            }
        }

        private async Task NavigateToShellAsync(
            MainMenuCommandRequest request,
            CancellationToken cancellationToken)
        {
            bootstrap = bootstrap != null
                ? bootstrap
                : FindFirstObjectByType<BootstrapRoot>();
            if (bootstrap == null)
            {
                throw new InvalidOperationException("MainMenu requires the persistent BootstrapRoot.");
            }

            await bootstrap.InitializationTask;
            cancellationToken.ThrowIfCancellationRequested();
            var sequence = bootstrap.ReserveTransitionSequence();
            var runStart = CreateRunStartPayload(request, sequence);
            if (eraClockPresenter != null)
            {
                eraClockPresenter.ApplySnapshot(EraClockSnapshotAdapter.FromRunStart(
                    runStart,
                    NextEraClockSequence(),
                    EraClockAnchorTarget.Center));
            }

            LastResult = await bootstrap.TransitionAsync(
                new SceneTransitionRequest(
                    sequence,
                    "main-menu-" + request.Command + "-" + sequence,
                    SceneId.MainMenu,
                    SceneId.OutOfBattleShell,
                    runStart),
                CancellationToken.None);
            if (!LastResult.Succeeded)
            {
                if (presenter != null)
                {
                    presenter.SetBusy(false);
                }

                Debug.LogError("MainMenu transition failed: " + LastResult.Message, this);
            }
        }

        private RunStartPayload CreateRunStartPayload(
            MainMenuCommandRequest request,
            long sequence)
        {
            if (request.Command == MainMenuCommand.Continue)
            {
                RunStartPayload continued;
                if (stateStore == null || !stateStore.TryCreateContinuePayload(out continued))
                {
                    throw new InvalidOperationException(
                        "当前存档不可继续：" + (stateStore?.ContinueDetail ?? "未找到存档"));
                }

                return continued;
            }

            var defaultSeedText = defaultRunSeed.ToString(CultureInfo.InvariantCulture);
            var suppliedSeed = request.Seed.Trim();
            var seedText = request.Command == MainMenuCommand.SeedGame &&
                !string.IsNullOrWhiteSpace(suppliedSeed)
                    ? suppliedSeed
                    : defaultSeedText;
            var runSeed = request.Command == MainMenuCommand.SeedGame
                ? ParseSeed(seedText)
                : defaultRunSeed;
            return new RunStartPayload(
                request.Command == MainMenuCommand.SeedGame
                    ? RunStartKind.SeedGame
                    : RunStartKind.NewGame,
                "run-" + runSeed + "-" + sequence,
                runSeed,
                seedText,
                1,
                1,
                1,
                0,
                "silver-character",
                StarterDeck.OrderedStableIds);
        }

        private static int ParseSeed(string seedText)
        {
            if (int.TryParse(
                    seedText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var numeric))
            {
                return numeric;
            }

            unchecked
            {
                var hash = 2166136261u;
                for (var index = 0; index < seedText.Length; index++)
                {
                    hash ^= seedText[index];
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7fffffff);
            }
        }

        private long NextEraClockSequence()
        {
            _eraClockSequence++;
            return _eraClockSequence;
        }

        private static string ContinueNotice(OverworldSaveLoadStatus status)
        {
            switch (status)
            {
                case OverworldSaveLoadStatus.Corrupt:
                    return "存档内容已损坏。旧文件仍保留，请开始新游戏或恢复备份。";
                case OverworldSaveLoadStatus.UnsupportedFutureVersion:
                    return "该存档来自更新版本，当前版本不会覆盖它。请升级游戏后重试。";
                case OverworldSaveLoadStatus.IoFailure:
                    return "暂时无法读取存档。请检查文件占用或磁盘权限后重试。";
                default:
                    return string.Empty;
            }
        }
    }
}
