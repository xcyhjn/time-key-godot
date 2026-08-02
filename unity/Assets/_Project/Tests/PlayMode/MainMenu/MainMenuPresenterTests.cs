using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Presentation.MainMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace TimeKey.Tests.PlayMode.MainMenu
{
    public sealed class MainMenuPresenterTests
    {
        private GameObject _root;
        private GameObject _eventSystemRoot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneInputLockState.SetLocked(false);
            if (EventSystem.current == null)
            {
                _eventSystemRoot = new GameObject("EventSystem", typeof(EventSystem));
            }

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneInputLockState.SetLocked(false);
            if (_root != null)
            {
                Object.Destroy(_root);
            }

            if (_eventSystemRoot != null)
            {
                Object.Destroy(_eventSystemRoot);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Menu_DisablesContinueAndLocksOnlyItsModal()
        {
            var rig = CreateRig();
            var command = MainMenuCommand.NewGame;
            rig.Presenter.CommandRequested += value => command = value.Command;

            Assert.That(rig.Continue.interactable, Is.False);
            Assert.That(rig.Presenter.Seed, Is.EqualTo("731"));
            Assert.That(
                EventSystem.current.currentSelectedGameObject,
                Is.SameAs(rig.NewGame.gameObject));
            rig.Database.onClick.Invoke();
            Assert.That(command, Is.EqualTo(MainMenuCommand.Database));
            Assert.That(rig.Presenter.IsModalOpen, Is.True);
            Assert.That(SceneInputLockState.IsLocked, Is.True);
            Assert.That(rig.ModalTitle.text, Is.EqualTo("数据库"));

            rig.ModalClose.onClick.Invoke();
            Assert.That(rig.Presenter.IsModalOpen, Is.False);
            Assert.That(SceneInputLockState.IsLocked, Is.False);

            SceneInputLockState.SetLocked(true);
            rig.Settings.onClick.Invoke();
            Assert.That(rig.Presenter.IsSettingsOpen, Is.True);
            rig.SettingsClose.onClick.Invoke();
            Assert.That(SceneInputLockState.IsLocked, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Menu_SeedOverlayCancelsOrSubmitsTypedCommand()
        {
            var rig = CreateRig();
            var commands = new System.Collections.Generic.List<MainMenuCommandRequest>();
            rig.Presenter.CommandRequested += commands.Add;

            rig.SeedGame.onClick.Invoke();
            Assert.That(rig.Presenter.IsSeedOpen, Is.True);
            Assert.That(SceneInputLockState.IsLocked, Is.True);
            rig.SeedCancel.onClick.Invoke();
            Assert.That(rig.Presenter.IsSeedOpen, Is.False);
            Assert.That(SceneInputLockState.IsLocked, Is.False);
            Assert.That(commands, Is.Empty);

            rig.SeedGame.onClick.Invoke();
            rig.SeedInput.text = "2048-alpha";
            Assert.That(rig.Presenter.HandleSeedSubmitKey(KeyCode.Return), Is.True);
            Assert.That(rig.Presenter.HandleSeedSubmitKey(KeyCode.KeypadEnter), Is.False);
            Assert.That(rig.Presenter.IsSeedOpen, Is.False);
            Assert.That(commands, Has.Count.EqualTo(1));
            Assert.That(commands[0].Command, Is.EqualTo(MainMenuCommand.SeedGame));
            Assert.That(commands[0].Seed, Is.EqualTo("2048-alpha"));

            rig.Presenter.SetBusy(false);
            rig.SeedGame.onClick.Invoke();
            rig.SeedInput.text = "4096-beta";
            Assert.That(rig.Presenter.HandleSeedSubmitKey(KeyCode.KeypadEnter), Is.True);
            Assert.That(rig.Presenter.HandleSeedSubmitKey(KeyCode.Return), Is.False);
            Assert.That(commands, Has.Count.EqualTo(2));
            Assert.That(commands[1].Seed, Is.EqualTo("4096-beta"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Menu_EscapeUsesOverlayPriorityAndRestoresFocus()
        {
            var rig = CreateRig();

            rig.SeedGame.Select();
            rig.SeedGame.onClick.Invoke();
            Assert.That(rig.NewGame.interactable, Is.False);
            Assert.That(rig.Presenter.HandleEscape(), Is.True);
            Assert.That(rig.Presenter.IsSeedOpen, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(rig.SeedGame.gameObject));

            rig.Exit.Select();
            rig.Exit.onClick.Invoke();
            Assert.That(rig.Presenter.IsModalOpen, Is.True);
            Assert.That(rig.Presenter.HandleEscape(), Is.True);
            Assert.That(rig.Presenter.IsModalOpen, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(rig.Exit.gameObject));

            rig.NewGame.Select();
            Assert.That(rig.Presenter.HandleEscape(), Is.True);
            Assert.That(rig.Presenter.IsModalOpen, Is.True);
            Assert.That(rig.Presenter.IsSettingsOpen, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(rig.MasterVolume.gameObject));
            Assert.That(rig.Presenter.HandleEscape(), Is.True);
            Assert.That(rig.Presenter.IsModalOpen, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(rig.NewGame.gameObject));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Menu_SettingsUsesInjectedSnapshotAndEmitsTypedRequest()
        {
            var rig = CreateRig(false);
            rig.Presenter.ApplySettingsSnapshot(
                new MainMenuSettingsSnapshot(0.25f, 0.5f, 0.75f, true));
            var commands = new System.Collections.Generic.List<MainMenuCommandRequest>();
            var requests = new System.Collections.Generic.List<MainMenuSettingsRequest>();
            rig.Presenter.CommandRequested += commands.Add;
            rig.Presenter.SettingsRequested += requests.Add;
            rig.Root.SetActive(true);

            Assert.That(rig.MasterVolume.value, Is.EqualTo(0.25f));
            Assert.That(rig.MusicVolume.value, Is.EqualTo(0.5f));
            Assert.That(rig.SfxVolume.value, Is.EqualTo(0.75f));
            Assert.That(rig.Fullscreen.isOn, Is.True);
            Assert.That(rig.Presenter.OpenSettings(), Is.True);
            Assert.That(rig.Presenter.IsSettingsOpen, Is.True);
            Assert.That(rig.Presenter.IsModalOpen, Is.True);
            rig.SettingsClose.onClick.Invoke();

            rig.Settings.onClick.Invoke();
            Assert.That(commands, Has.Count.EqualTo(1));
            Assert.That(commands[0].Command, Is.EqualTo(MainMenuCommand.Settings));
            rig.MasterVolume.value = 0.4f;
            Assert.That(requests, Has.Count.EqualTo(1));
            Assert.That(requests[0].Snapshot.MasterVolume, Is.EqualTo(0.4f));
            Assert.That(requests[0].Snapshot.MusicVolume, Is.EqualTo(0.5f));
            Assert.That(requests[0].Snapshot.SfxVolume, Is.EqualTo(0.75f));
            Assert.That(requests[0].Snapshot.Fullscreen, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Menu_OverlayBlocksUnderlyingCommands()
        {
            var rig = CreateRig();
            var commands = new System.Collections.Generic.List<MainMenuCommandRequest>();
            rig.Presenter.CommandRequested += commands.Add;

            rig.SeedGame.onClick.Invoke();
            rig.Settings.onClick.Invoke();
            rig.Exit.onClick.Invoke();

            Assert.That(rig.Presenter.IsSeedOpen, Is.True);
            Assert.That(rig.Presenter.IsModalOpen, Is.False);
            Assert.That(commands, Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Menu_EntranceLayersAreStaggeredAndCompletionIsObservable()
        {
            var rig = CreateRig(false);
            SetField(rig.Presenter, "entranceLayerDuration", 1f);
            SetField(rig.Presenter, "entranceLayerInterval", 1f);
            var completions = 0;
            rig.Presenter.EntranceCompleted += () => completions++;

            rig.Root.SetActive(true);
            Assert.That(rig.Presenter.IsEntranceComplete, Is.False);
            Assert.That(rig.BackgroundEntrance.alpha, Is.Zero);
            Assert.That(rig.TitleEntrance.alpha, Is.Zero);
            Assert.That(rig.ClockEntrance.alpha, Is.Zero);
            Assert.That(rig.ButtonsEntrance.alpha, Is.Zero);

            InvokePrivate(rig.Presenter, "ApplyEntranceFrame", 1.5f);
            Assert.That(rig.BackgroundEntrance.alpha, Is.EqualTo(1f));
            Assert.That(rig.TitleEntrance.alpha, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(rig.ClockEntrance.alpha, Is.Zero);
            Assert.That(rig.ButtonsEntrance.alpha, Is.Zero);

            rig.Presenter.CompleteEntranceImmediately();
            Assert.That(rig.Presenter.IsEntranceComplete, Is.True);
            Assert.That(rig.BackgroundEntrance.alpha, Is.EqualTo(1f));
            Assert.That(rig.TitleEntrance.alpha, Is.EqualTo(1f));
            Assert.That(rig.ClockEntrance.alpha, Is.EqualTo(1f));
            Assert.That(rig.ButtonsEntrance.alpha, Is.EqualTo(1f));
            Assert.That(completions, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Menu_DisabledOrZeroDurationEntranceUsesTerminalState()
        {
            var disabledRig = CreateRig(false);
            SetField(disabledRig.Presenter, "entranceEnabled", false);
            disabledRig.Root.SetActive(true);
            AssertEntranceComplete(disabledRig);

            Object.Destroy(disabledRig.Root);
            yield return null;
            _root = null;

            var zeroDurationRig = CreateRig(false);
            SetField(zeroDurationRig.Presenter, "entranceLayerDuration", 0f);
            zeroDurationRig.Root.SetActive(true);
            AssertEntranceComplete(zeroDurationRig);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Menu_ConfirmsExitThroughCallbackAndSuppressesRepeatNavigation()
        {
            var rig = CreateRig();
            var commands = new System.Collections.Generic.List<MainMenuCommandRequest>();
            rig.Presenter.CommandRequested += commands.Add;

            rig.Exit.onClick.Invoke();
            Assert.That(commands, Is.Empty);
            Assert.That(rig.Presenter.IsModalOpen, Is.True);
            rig.ModalConfirm.onClick.Invoke();
            Assert.That(commands, Has.Count.EqualTo(1));
            Assert.That(commands[0].Command, Is.EqualTo(MainMenuCommand.Exit));

            rig.NewGame.onClick.Invoke();
            Assert.That(commands, Has.Count.EqualTo(1));
            rig.Presenter.SetBusy(false);
            rig.NewGame.onClick.Invoke();
            rig.NewGame.onClick.Invoke();
            Assert.That(commands, Has.Count.EqualTo(2));
            Assert.That(commands[0].Command, Is.EqualTo(MainMenuCommand.Exit));
            Assert.That(commands[1].Command, Is.EqualTo(MainMenuCommand.NewGame));
            yield return null;
        }

        private MainMenuRig CreateRig(bool activate = true)
        {
            _root = new GameObject("MainMenuRig");
            _root.SetActive(false);
            var presenter = _root.AddComponent<MainMenuPresenter>();
            var canvasGroup = _root.AddComponent<CanvasGroup>();
            var title = CreateText("Title");
            var subtitle = CreateText("Subtitle");
            var seedLabel = CreateText("SeedLabel");
            var backgroundEntrance = CreateLayer("BackgroundEntrance");
            var titleEntrance = title.gameObject.AddComponent<CanvasGroup>();
            var clockEntrance = CreateLayer("ClockEntrance");
            var buttonsEntrance = CreateLayer("ButtonsEntrance");
            var seedOverlay = new GameObject("SeedOverlay");
            seedOverlay.transform.SetParent(_root.transform, false);
            var seedGroup = seedOverlay.AddComponent<CanvasGroup>();
            var seedInputObject = new GameObject("SeedInput", typeof(RectTransform));
            seedInputObject.transform.SetParent(seedOverlay.transform, false);
            var seedInput = seedInputObject.AddComponent<InputField>();
            seedInput.textComponent = CreateText("SeedText", seedInputObject.transform);
            var buttons = new[]
            {
                CreateButton("NewGame", buttonsEntrance.transform),
                CreateButton("SeedGame", buttonsEntrance.transform),
                CreateButton("Continue", buttonsEntrance.transform),
                CreateButton("Database", buttonsEntrance.transform),
                CreateButton("Settings", buttonsEntrance.transform),
                CreateButton("Exit", buttonsEntrance.transform)
            };
            var seedConfirm = CreateButton("SeedConfirm", seedOverlay.transform);
            var seedCancel = CreateButton("SeedCancel", seedOverlay.transform);
            var modal = new GameObject("Modal");
            modal.transform.SetParent(_root.transform, false);
            var modalGroup = modal.AddComponent<CanvasGroup>();
            var modalTitle = CreateText("ModalTitle", modal.transform);
            var modalBody = CreateText("ModalBody", modal.transform);
            var modalConfirm = CreateButton("Confirm", modal.transform);
            var modalClose = CreateButton("Close", modal.transform);
            var settingsOverlay = new GameObject("SettingsOverlay");
            settingsOverlay.transform.SetParent(_root.transform, false);
            var settingsGroup = settingsOverlay.AddComponent<CanvasGroup>();
            var masterVolume = CreateSlider("MasterVolume", settingsOverlay.transform);
            var musicVolume = CreateSlider("MusicVolume", settingsOverlay.transform);
            var sfxVolume = CreateSlider("SfxVolume", settingsOverlay.transform);
            var fullscreen = CreateToggle("Fullscreen", settingsOverlay.transform);
            var settingsClose = CreateButton("SettingsClose", settingsOverlay.transform);
            SetField(presenter, "canvasGroup", canvasGroup);
            SetField(presenter, "titleLabel", title);
            SetField(presenter, "subtitleLabel", subtitle);
            SetField(presenter, "seedLabel", seedLabel);
            SetField(presenter, "seedInput", seedInput);
            SetField(presenter, "silverFont", Resources.Load<Font>("Fonts/Silver"));
            SetField(presenter, "newGameButton", buttons[0]);
            SetField(presenter, "seedGameButton", buttons[1]);
            SetField(presenter, "continueButton", buttons[2]);
            SetField(presenter, "databaseButton", buttons[3]);
            SetField(presenter, "settingsButton", buttons[4]);
            SetField(presenter, "exitButton", buttons[5]);
            SetField(presenter, "seedGroup", seedGroup);
            SetField(presenter, "seedConfirmButton", seedConfirm);
            SetField(presenter, "seedCancelButton", seedCancel);
            SetField(presenter, "modalGroup", modalGroup);
            SetField(presenter, "modalTitle", modalTitle);
            SetField(presenter, "modalBody", modalBody);
            SetField(presenter, "modalConfirmButton", modalConfirm);
            SetField(presenter, "modalCloseButton", modalClose);
            SetField(presenter, "settingsGroup", settingsGroup);
            SetField(presenter, "masterVolumeSlider", masterVolume);
            SetField(presenter, "musicVolumeSlider", musicVolume);
            SetField(presenter, "sfxVolumeSlider", sfxVolume);
            SetField(presenter, "fullscreenToggle", fullscreen);
            SetField(presenter, "settingsCloseButton", settingsClose);
            SetField(presenter, "backgroundEntranceGroup", backgroundEntrance);
            SetField(presenter, "titleEntranceGroup", titleEntrance);
            SetField(presenter, "clockEntranceGroup", clockEntrance);
            SetField(presenter, "buttonsEntranceGroup", buttonsEntrance);
            var rig = new MainMenuRig(
                _root,
                presenter,
                buttons[0],
                buttons[1],
                buttons[2],
                buttons[4],
                buttons[5],
                buttons[3],
                seedConfirm,
                seedCancel,
                seedInput,
                modalTitle,
                modalConfirm,
                modalClose,
                settingsGroup,
                masterVolume,
                musicVolume,
                sfxVolume,
                fullscreen,
                settingsClose,
                backgroundEntrance,
                titleEntrance,
                clockEntrance,
                buttonsEntrance);
            if (activate)
            {
                _root.SetActive(true);
            }

            return rig;
        }

        private CanvasGroup CreateLayer(string name)
        {
            var target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(_root.transform, false);
            return target.AddComponent<CanvasGroup>();
        }

        private Text CreateText(string name, Transform parent = null)
        {
            var target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent == null ? _root.transform : parent, false);
            return target.AddComponent<Text>();
        }

        private Button CreateButton(string name, Transform parent = null)
        {
            var target = new GameObject(name, typeof(RectTransform), typeof(Image));
            target.transform.SetParent(parent == null ? _root.transform : parent, false);
            var button = target.AddComponent<Button>();
            button.targetGraphic = target.GetComponent<Image>();
            return button;
        }

        private Slider CreateSlider(string name, Transform parent)
        {
            var target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            var slider = target.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        private Toggle CreateToggle(string name, Transform parent)
        {
            var target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target.AddComponent<Toggle>();
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static void InvokePrivate(object target, string name, params object[] arguments)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, arguments);
        }

        private static void AssertEntranceComplete(MainMenuRig rig)
        {
            Assert.That(rig.Presenter.IsEntranceComplete, Is.True);
            Assert.That(rig.BackgroundEntrance.alpha, Is.EqualTo(1f));
            Assert.That(rig.TitleEntrance.alpha, Is.EqualTo(1f));
            Assert.That(rig.ClockEntrance.alpha, Is.EqualTo(1f));
            Assert.That(rig.ButtonsEntrance.alpha, Is.EqualTo(1f));
        }

        private sealed class MainMenuRig
        {
            public MainMenuRig(
                GameObject root,
                MainMenuPresenter presenter,
                Button newGame,
                Button seedGame,
                Button continueButton,
                Button settings,
                Button exit,
                Button database,
                Button seedConfirm,
                Button seedCancel,
                InputField seedInput,
                Text modalTitle,
                Button modalConfirm,
                Button modalClose,
                CanvasGroup settingsGroup,
                Slider masterVolume,
                Slider musicVolume,
                Slider sfxVolume,
                Toggle fullscreen,
                Button settingsClose,
                CanvasGroup backgroundEntrance,
                CanvasGroup titleEntrance,
                CanvasGroup clockEntrance,
                CanvasGroup buttonsEntrance)
            {
                Root = root;
                Presenter = presenter;
                NewGame = newGame;
                SeedGame = seedGame;
                Continue = continueButton;
                Settings = settings;
                Exit = exit;
                Database = database;
                SeedConfirm = seedConfirm;
                SeedCancel = seedCancel;
                SeedInput = seedInput;
                ModalTitle = modalTitle;
                ModalConfirm = modalConfirm;
                ModalClose = modalClose;
                SettingsGroup = settingsGroup;
                MasterVolume = masterVolume;
                MusicVolume = musicVolume;
                SfxVolume = sfxVolume;
                Fullscreen = fullscreen;
                SettingsClose = settingsClose;
                BackgroundEntrance = backgroundEntrance;
                TitleEntrance = titleEntrance;
                ClockEntrance = clockEntrance;
                ButtonsEntrance = buttonsEntrance;
            }

            public GameObject Root { get; }
            public MainMenuPresenter Presenter { get; }
            public Button NewGame { get; }
            public Button SeedGame { get; }
            public Button Continue { get; }
            public Button Settings { get; }
            public Button Exit { get; }
            public Button Database { get; }
            public Button SeedConfirm { get; }
            public Button SeedCancel { get; }
            public InputField SeedInput { get; }
            public Text ModalTitle { get; }
            public Button ModalConfirm { get; }
            public Button ModalClose { get; }
            public CanvasGroup SettingsGroup { get; }
            public Slider MasterVolume { get; }
            public Slider MusicVolume { get; }
            public Slider SfxVolume { get; }
            public Toggle Fullscreen { get; }
            public Button SettingsClose { get; }
            public CanvasGroup BackgroundEntrance { get; }
            public CanvasGroup TitleEntrance { get; }
            public CanvasGroup ClockEntrance { get; }
            public CanvasGroup ButtonsEntrance { get; }
        }
    }
}
