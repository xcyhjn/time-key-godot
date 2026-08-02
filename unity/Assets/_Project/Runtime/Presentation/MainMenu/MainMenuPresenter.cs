using System;
using System.Collections;
using TimeKey.Application.SceneFlow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimeKey.Presentation.MainMenu
{
    public enum MainMenuCommand
    {
        NewGame,
        SeedGame,
        Continue,
        Database,
        Settings,
        Exit
    }

    public sealed class MainMenuCommandRequest
    {
        public MainMenuCommandRequest(MainMenuCommand command, string seed = null)
        {
            Command = command;
            Seed = seed ?? string.Empty;
        }

        public MainMenuCommand Command { get; }

        public string Seed { get; }
    }

    public sealed class MainMenuSettingsSnapshot
    {
        public MainMenuSettingsSnapshot(
            float masterVolume,
            float musicVolume,
            float sfxVolume,
            bool fullscreen)
        {
            MasterVolume = Mathf.Clamp01(masterVolume);
            MusicVolume = Mathf.Clamp01(musicVolume);
            SfxVolume = Mathf.Clamp01(sfxVolume);
            Fullscreen = fullscreen;
        }

        public static MainMenuSettingsSnapshot Default { get; } =
            new MainMenuSettingsSnapshot(1f, 1f, 1f, false);

        public float MasterVolume { get; }

        public float MusicVolume { get; }

        public float SfxVolume { get; }

        public bool Fullscreen { get; }
    }

    public sealed class MainMenuSettingsRequest
    {
        public MainMenuSettingsRequest(MainMenuSettingsSnapshot snapshot)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public MainMenuSettingsSnapshot Snapshot { get; }
    }

    [DisallowMultipleComponent]
    public sealed class MainMenuPresenter : MonoBehaviour, ISceneRevealPresentation
    {
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private Text titleLabel = null;
        [SerializeField] private Text subtitleLabel = null;
        [SerializeField] private Text seedLabel = null;
        [SerializeField] private InputField seedInput = null;
        [SerializeField] private Font silverFont = null;
        [SerializeField] private Button newGameButton = null;
        [SerializeField] private Button seedGameButton = null;
        [SerializeField] private Button continueButton = null;
        [SerializeField] private Button databaseButton = null;
        [SerializeField] private Button settingsButton = null;
        [SerializeField] private Button exitButton = null;
        [SerializeField] private CanvasGroup seedGroup = null;
        [SerializeField] private Button seedConfirmButton = null;
        [SerializeField] private Button seedCancelButton = null;
        [SerializeField] private CanvasGroup modalGroup = null;
        [SerializeField] private Text modalTitle = null;
        [SerializeField] private Text modalBody = null;
        [SerializeField] private Button modalConfirmButton = null;
        [SerializeField] private Button modalCloseButton = null;
        [Header("Settings")]
        [SerializeField] private CanvasGroup settingsGroup = null;
        [SerializeField] private Slider masterVolumeSlider = null;
        [SerializeField] private Slider musicVolumeSlider = null;
        [SerializeField] private Slider sfxVolumeSlider = null;
        [SerializeField] private Toggle fullscreenToggle = null;
        [SerializeField] private Button settingsCloseButton = null;
        [Header("Entrance")]
        [SerializeField] private bool entranceEnabled = true;
        [SerializeField, Min(0f)] private float entranceLayerDuration = 0.35f;
        [SerializeField, Min(0f)] private float entranceLayerInterval = 0.1f;
        [SerializeField] private CanvasGroup backgroundEntranceGroup = null;
        [SerializeField] private CanvasGroup titleEntranceGroup = null;
        [SerializeField] private CanvasGroup clockEntranceGroup = null;
        [SerializeField] private CanvasGroup buttonsEntranceGroup = null;

        private IDisposable _overlayLock;
        private Coroutine _entranceRoutine;
        private GameObject _previousSelection;
        private bool _bound;
        private bool _continueAvailable;
        private bool _isBusy;
        private MainMenuSettingsSnapshot _settingsSnapshot =
            MainMenuSettingsSnapshot.Default;

        public event Action<MainMenuCommandRequest> CommandRequested;

        public event Action<MainMenuSettingsRequest> SettingsRequested;

        public event Action EntranceCompleted;

        public bool IsModalOpen => IsDialogOpen || IsSettingsOpen;

        public bool IsSeedOpen => seedGroup != null && seedGroup.blocksRaycasts;

        public bool IsSettingsOpen => settingsGroup != null && settingsGroup.blocksRaycasts;

        private bool IsDialogOpen => modalGroup != null && modalGroup.blocksRaycasts;

        public string Seed => seedInput == null ? string.Empty : seedInput.text;

        public bool IsEntranceComplete { get; private set; }

        bool ISceneRevealPresentation.IsComplete => IsEntranceComplete;

        void ISceneRevealPresentation.PlayReveal()
        {
            PlayEntrance();
        }

        void ISceneRevealPresentation.CompleteImmediately()
        {
            CompleteEntranceImmediately();
        }

        private void OnEnable()
        {
            if (!DependenciesAssigned())
            {
                return;
            }

            ApplySilverFont();
            Bind();
            CloseModal(false);
            CloseSeed(false);
            CloseSettings(false);
            _continueAvailable = false;
            _isBusy = false;
            ApplyButtonInteractivity();
            if (string.IsNullOrWhiteSpace(seedInput.text))
            {
                seedInput.text = "731";
            }

            ApplySettingsSnapshot(_settingsSnapshot);

            SelectInitialButton();
            PlayEntrance();
        }

        private void OnDisable()
        {
            StopEntrance(false);
            CloseModal(false);
            CloseSeed(false);
            CloseSettings(false);
            Unbind();
        }

        private void Update()
        {
            if (IsSeedOpen && Input.GetKeyDown(KeyCode.Return))
            {
                HandleSeedSubmitKey(KeyCode.Return);
                return;
            }

            if (IsSeedOpen && Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                HandleSeedSubmitKey(KeyCode.KeypadEnter);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscape();
            }
        }

        public bool HandleEscape()
        {
            if (IsDialogOpen)
            {
                CloseModal();
                return true;
            }

            if (IsSettingsOpen)
            {
                CloseSettings();
                return true;
            }

            if (IsSeedOpen)
            {
                CloseSeed();
                return true;
            }

            if (_isBusy)
            {
                return false;
            }

            OnSettings();
            return IsModalOpen;
        }

        public bool HandleSeedSubmitKey(KeyCode keyCode)
        {
            if (!IsSeedOpen ||
                (keyCode != KeyCode.Return && keyCode != KeyCode.KeypadEnter))
            {
                return false;
            }

            OnSeedConfirm();
            return true;
        }

        public void ApplySettingsSnapshot(MainMenuSettingsSnapshot snapshot)
        {
            _settingsSnapshot = snapshot ??
                throw new ArgumentNullException(nameof(snapshot));
            masterVolumeSlider?.SetValueWithoutNotify(snapshot.MasterVolume);
            musicVolumeSlider?.SetValueWithoutNotify(snapshot.MusicVolume);
            sfxVolumeSlider?.SetValueWithoutNotify(snapshot.SfxVolume);
            fullscreenToggle?.SetIsOnWithoutNotify(snapshot.Fullscreen);
        }

        public void PlayEntrance()
        {
            StopEntrance(false);
            IsEntranceComplete = false;
            if (!entranceEnabled || entranceLayerDuration <= 0f ||
                !EntranceLayersAssigned() || !isActiveAndEnabled)
            {
                CompleteEntranceImmediately();
                return;
            }

            ApplyEntranceFrame(0f);
            _entranceRoutine = StartCoroutine(PlayEntranceRoutine());
        }

        public void CompleteEntranceImmediately()
        {
            StopEntrance(true);
        }

        public void SetContinueAvailable(bool available)
        {
            _continueAvailable = available;
            ApplyButtonInteractivity();
        }

        public void SetBusy(bool busy)
        {
            _isBusy = busy;
            ApplyButtonInteractivity();
        }

        public void ShowModal(string title, string body, bool confirm)
        {
            if (IsSeedOpen || IsSettingsOpen || _isBusy)
            {
                return;
            }

            if (!IsDialogOpen)
            {
                _previousSelection = EventSystem.current == null
                    ? null
                    : EventSystem.current.currentSelectedGameObject;
                _overlayLock = SceneInputLockState.Acquire();
            }

            modalTitle.text = title;
            modalBody.text = body;
            modalConfirmButton.gameObject.SetActive(confirm);
            modalGroup.alpha = 1f;
            modalGroup.interactable = true;
            modalGroup.blocksRaycasts = true;
            ApplyButtonInteractivity();
            (confirm ? modalConfirmButton : modalCloseButton).Select();
        }

        public void CloseModal()
        {
            CloseModal(true);
        }

        private void Bind()
        {
            if (_bound)
            {
                return;
            }

            newGameButton.onClick.AddListener(OnNewGame);
            seedGameButton.onClick.AddListener(OpenSeed);
            continueButton.onClick.AddListener(OnContinue);
            databaseButton.onClick.AddListener(OnDatabase);
            settingsButton.onClick.AddListener(OnSettings);
            exitButton.onClick.AddListener(OnExit);
            seedConfirmButton.onClick.AddListener(OnSeedConfirm);
            seedCancelButton.onClick.AddListener(CloseSeed);
            modalConfirmButton.onClick.AddListener(OnModalConfirm);
            modalCloseButton.onClick.AddListener(CloseModal);
            if (settingsCloseButton != null)
            {
                settingsCloseButton.onClick.AddListener(CloseSettings);
            }
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnVolumeValueChanged);
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(OnVolumeValueChanged);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(OnVolumeValueChanged);
            }
            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenValueChanged);
            }
            _bound = true;
        }

        private void Unbind()
        {
            if (!_bound)
            {
                return;
            }

            newGameButton.onClick.RemoveListener(OnNewGame);
            seedGameButton.onClick.RemoveListener(OpenSeed);
            continueButton.onClick.RemoveListener(OnContinue);
            databaseButton.onClick.RemoveListener(OnDatabase);
            settingsButton.onClick.RemoveListener(OnSettings);
            exitButton.onClick.RemoveListener(OnExit);
            seedConfirmButton.onClick.RemoveListener(OnSeedConfirm);
            seedCancelButton.onClick.RemoveListener(CloseSeed);
            modalConfirmButton.onClick.RemoveListener(OnModalConfirm);
            modalCloseButton.onClick.RemoveListener(CloseModal);
            if (settingsCloseButton != null)
            {
                settingsCloseButton.onClick.RemoveListener(CloseSettings);
            }
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(OnVolumeValueChanged);
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveListener(OnVolumeValueChanged);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(OnVolumeValueChanged);
            }
            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenValueChanged);
            }
            _bound = false;
        }

        private void OnNewGame()
        {
            RequestNavigation(MainMenuCommand.NewGame);
        }

        private void OnContinue()
        {
            RequestNavigation(MainMenuCommand.Continue);
        }

        private void OpenSeed()
        {
            if (IsSeedOpen || IsModalOpen || IsSettingsOpen || _isBusy)
            {
                return;
            }

            _previousSelection = EventSystem.current == null
                ? null
                : EventSystem.current.currentSelectedGameObject;
            _overlayLock = SceneInputLockState.Acquire();
            seedGroup.alpha = 1f;
            seedGroup.interactable = true;
            seedGroup.blocksRaycasts = true;
            ApplyButtonInteractivity();
            seedInput.Select();
            seedInput.ActivateInputField();
        }

        private void OnSeedConfirm()
        {
            if (!IsSeedOpen)
            {
                return;
            }

            var seed = Seed;
            CloseSeed();
            RequestNavigation(MainMenuCommand.SeedGame, seed);
        }

        public void CloseSeed()
        {
            CloseSeed(true);
        }

        private void OnDatabase()
        {
            if (!CanOpenOverlay())
            {
                return;
            }

            ShowModal("数据库", "资料库将在完成首个房间后开放。", false);
            CommandRequested?.Invoke(new MainMenuCommandRequest(MainMenuCommand.Database));
        }

        private void OnSettings()
        {
            if (!CanOpenOverlay())
            {
                return;
            }

            if (!OpenSettings())
            {
                ShowModal("设置", "当前设置面板不可用。", false);
            }
            CommandRequested?.Invoke(new MainMenuCommandRequest(MainMenuCommand.Settings));
        }

        private void OnExit()
        {
            if (!CanOpenOverlay())
            {
                return;
            }

            ShowModal("退出游戏", "确定要离开时之钥吗？", true);
        }

        private void OnModalConfirm()
        {
            CloseModal();
            RequestNavigation(MainMenuCommand.Exit);
        }

        private void RequestNavigation(MainMenuCommand command, string seed = null)
        {
            if (_isBusy || IsModalOpen || IsSettingsOpen ||
                (command == MainMenuCommand.Continue && !_continueAvailable))
            {
                return;
            }

            _isBusy = true;
            ApplyButtonInteractivity();
            CommandRequested?.Invoke(new MainMenuCommandRequest(command, seed));
        }

        private void ApplyButtonInteractivity()
        {
            if (!DependenciesAssigned())
            {
                return;
            }

            var menuInteractable = !_isBusy && !IsModalOpen && !IsSeedOpen &&
                !IsSettingsOpen;
            canvasGroup.interactable = !_isBusy;
            canvasGroup.blocksRaycasts = !_isBusy;
            newGameButton.interactable = menuInteractable;
            seedGameButton.interactable = menuInteractable;
            continueButton.interactable = menuInteractable && _continueAvailable;
            databaseButton.interactable = menuInteractable;
            settingsButton.interactable = menuInteractable;
            exitButton.interactable = menuInteractable;
        }

        private void CloseModal(bool restoreFocus)
        {
            if (modalGroup == null)
            {
                return;
            }

            var wasOpen = IsDialogOpen;
            modalGroup.alpha = 0f;
            modalGroup.interactable = false;
            modalGroup.blocksRaycasts = false;
            ApplyButtonInteractivity();
            if (wasOpen)
            {
                ReleaseOverlayLock(restoreFocus);
            }
        }

        private void CloseSeed(bool restoreFocus)
        {
            if (seedGroup == null)
            {
                return;
            }

            var wasOpen = IsSeedOpen;
            seedGroup.alpha = 0f;
            seedGroup.interactable = false;
            seedGroup.blocksRaycasts = false;
            ApplyButtonInteractivity();
            if (wasOpen)
            {
                ReleaseOverlayLock(restoreFocus);
            }
        }

        public void CloseSettings()
        {
            CloseSettings(true);
        }

        public bool OpenSettings()
        {
            if (settingsGroup == null || !CanOpenOverlay())
            {
                return false;
            }

            _previousSelection = EventSystem.current == null
                ? null
                : EventSystem.current.currentSelectedGameObject;
            _overlayLock = SceneInputLockState.Acquire();
            settingsGroup.alpha = 1f;
            settingsGroup.interactable = true;
            settingsGroup.blocksRaycasts = true;
            ApplyButtonInteractivity();
            masterVolumeSlider?.Select();
            return IsSettingsOpen;
        }

        private void CloseSettings(bool restoreFocus)
        {
            if (settingsGroup == null)
            {
                return;
            }

            var wasOpen = IsSettingsOpen;
            settingsGroup.alpha = 0f;
            settingsGroup.interactable = false;
            settingsGroup.blocksRaycasts = false;
            ApplyButtonInteractivity();
            if (wasOpen)
            {
                ReleaseOverlayLock(restoreFocus);
            }
        }

        private void OnVolumeValueChanged(float value)
        {
            RequestSettingsChange();
        }

        private void OnFullscreenValueChanged(bool value)
        {
            RequestSettingsChange();
        }

        private void RequestSettingsChange()
        {
            _settingsSnapshot = new MainMenuSettingsSnapshot(
                masterVolumeSlider == null
                    ? _settingsSnapshot.MasterVolume
                    : masterVolumeSlider.value,
                musicVolumeSlider == null
                    ? _settingsSnapshot.MusicVolume
                    : musicVolumeSlider.value,
                sfxVolumeSlider == null
                    ? _settingsSnapshot.SfxVolume
                    : sfxVolumeSlider.value,
                fullscreenToggle == null
                    ? _settingsSnapshot.Fullscreen
                    : fullscreenToggle.isOn);
            SettingsRequested?.Invoke(new MainMenuSettingsRequest(_settingsSnapshot));
        }

        private bool CanOpenOverlay()
        {
            return !_isBusy && !IsModalOpen && !IsSeedOpen;
        }

        private void SelectInitialButton()
        {
            if (EventSystem.current != null && newGameButton.interactable)
            {
                EventSystem.current.SetSelectedGameObject(newGameButton.gameObject);
            }
        }

        private IEnumerator PlayEntranceRoutine()
        {
            var totalDuration = entranceLayerDuration + (entranceLayerInterval * 3f);
            var elapsed = 0f;
            while (elapsed < totalDuration)
            {
                ApplyEntranceFrame(elapsed);
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            _entranceRoutine = null;
            CompleteEntranceImmediately();
        }

        private void ApplyEntranceFrame(float elapsed)
        {
            ApplyLayerAlpha(backgroundEntranceGroup, elapsed);
            ApplyLayerAlpha(titleEntranceGroup, elapsed - entranceLayerInterval);
            ApplyLayerAlpha(clockEntranceGroup, elapsed - (entranceLayerInterval * 2f));
            ApplyLayerAlpha(buttonsEntranceGroup, elapsed - (entranceLayerInterval * 3f));
        }

        private void StopEntrance(bool notifyCompletion)
        {
            if (_entranceRoutine != null)
            {
                StopCoroutine(_entranceRoutine);
                _entranceRoutine = null;
            }

            ApplyEntranceTerminalState();
            var wasComplete = IsEntranceComplete;
            IsEntranceComplete = true;
            if (notifyCompletion && !wasComplete)
            {
                EntranceCompleted?.Invoke();
            }
        }

        private void ApplyEntranceTerminalState()
        {
            SetLayerAlpha(backgroundEntranceGroup, 1f);
            SetLayerAlpha(titleEntranceGroup, 1f);
            SetLayerAlpha(clockEntranceGroup, 1f);
            SetLayerAlpha(buttonsEntranceGroup, 1f);
        }

        private void ApplyLayerAlpha(CanvasGroup group, float elapsed)
        {
            SetLayerAlpha(group, Mathf.Clamp01(elapsed / entranceLayerDuration));
        }

        private static void SetLayerAlpha(CanvasGroup group, float alpha)
        {
            if (group != null)
            {
                group.alpha = alpha;
            }
        }

        private bool EntranceLayersAssigned()
        {
            return backgroundEntranceGroup != null && titleEntranceGroup != null &&
                clockEntranceGroup != null && buttonsEntranceGroup != null;
        }

        private void ReleaseOverlayLock(bool restoreFocus)
        {
            _overlayLock?.Dispose();
            _overlayLock = null;
            if (restoreFocus && EventSystem.current != null &&
                _previousSelection != null && _previousSelection.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(_previousSelection);
            }

            _previousSelection = null;
        }

        private void ApplySilverFont()
        {
            foreach (var text in GetComponentsInChildren<Text>(true))
            {
                text.font = silverFont;
            }
        }

        private bool DependenciesAssigned()
        {
            return canvasGroup != null && titleLabel != null && subtitleLabel != null &&
                seedLabel != null && seedInput != null && silverFont != null &&
                newGameButton != null && seedGameButton != null &&
                continueButton != null && databaseButton != null &&
                settingsButton != null && exitButton != null && seedGroup != null &&
                seedConfirmButton != null && seedCancelButton != null &&
                modalGroup != null && modalTitle != null && modalBody != null &&
                modalConfirmButton != null && modalCloseButton != null;
        }
    }
}
