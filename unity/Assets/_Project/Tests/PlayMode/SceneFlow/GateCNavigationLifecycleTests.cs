using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition.SceneFlow;
using TimeKey.Presentation.GameStart;
using TimeKey.Presentation.MainMenu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TimeKey.Tests.PlayMode.SceneFlow
{
    public sealed class GateCNavigationLifecycleTests
    {
        private const float TaskTimeoutSeconds = 15f;
        private const string MasterVolumeKey = "timekey.settings.master-volume";
        private const string FullscreenKey = "timekey.settings.fullscreen";
        private float _originalMasterVolume;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _originalMasterVolume = AudioListener.volume;
            PlayerPrefs.DeleteKey(MasterVolumeKey);
            PlayerPrefs.DeleteKey(FullscreenKey);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AudioListener.volume = _originalMasterVolume;
            PlayerPrefs.DeleteKey(MasterVolumeKey);
            PlayerPrefs.DeleteKey(FullscreenKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameStartAdapter_SourceUnloadDoesNotCancelCommittedTransition()
        {
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;

            var bootstrap = Object.FindAnyObjectByType<BootstrapRoot>();
            Assert.That(bootstrap, Is.Not.Null);
            yield return AwaitTask(bootstrap.InitializationTask, "Bootstrap initialization");

            var navigation = Object.FindAnyObjectByType<GameStartSceneNavigation>();
            var presenter = Object.FindAnyObjectByType<StartLogoPresenter>();
            Assert.That(navigation, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            presenter.CompleteImmediately();

            var lifetime = new CancellationTokenSource();
            SetField(navigation, "_lifetime", lifetime);
            var transition = InvokeTask(
                navigation,
                "NavigateToMainMenuAsync",
                lifetime.Token);
            yield return AwaitTask(transition, "GameStart adapter transition");

            Assert.That(transition.IsFaulted, Is.False, transition.Exception?.ToString());
            Assert.That(transition.IsCanceled, Is.False);
            Assert.That(lifetime.IsCancellationRequested, Is.True);
            Assert.That(navigation == null, Is.True, "The source adapter should be unloaded.");
            Assert.That(navigation.LastResult, Is.Not.Null);
            Assert.That(navigation.LastResult.Succeeded, Is.True, navigation.LastResult.Message);
            AssertTargetUnlocked(bootstrap, SceneId.MainMenu, "GameStart");
        }

        [UnityTest]
        public IEnumerator MainMenuAdapter_SourceUnloadDoesNotCancelCommittedTransition()
        {
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;

            var bootstrap = Object.FindAnyObjectByType<BootstrapRoot>();
            Assert.That(bootstrap, Is.Not.Null);
            yield return AwaitTask(bootstrap.InitializationTask, "Bootstrap initialization");
            yield return TransitionToMainMenu(bootstrap);

            var navigation = Object.FindAnyObjectByType<MainMenuSceneNavigation>();
            var presenter = Object.FindAnyObjectByType<MainMenuPresenter>();
            Assert.That(navigation, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            var newGameButton = Array.Find(
                presenter.GetComponentsInChildren<Button>(true),
                button => button.name == "NewGame");
            Assert.That(newGameButton, Is.Not.Null);

            newGameButton.onClick.Invoke();
            yield return AwaitCondition(
                () => navigation.LastResult != null,
                "MainMenu adapter transition");

            Assert.That(navigation == null, Is.True, "The source adapter should be unloaded.");
            Assert.That(navigation.LastResult.Succeeded, Is.True, navigation.LastResult.Message);
            AssertTargetUnlocked(bootstrap, SceneId.OutOfBattleShell, "MainMenu");
        }

        [UnityTest]
        public IEnumerator MainMenuAdapter_LoadsAndPersistsTypedSettingsRequests()
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, 0.63f);
            PlayerPrefs.SetInt(FullscreenKey, Screen.fullScreen ? 1 : 0);

            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            var entry = Object.FindAnyObjectByType<SceneContentEntry>();
            Assert.That(entry, Is.Not.Null);
            entry.Bind(new EmptySceneTransitionPayload(SceneId.MainMenu));
            yield return null;

            var navigation = Object.FindAnyObjectByType<MainMenuSceneNavigation>();
            var presenter = Object.FindAnyObjectByType<MainMenuPresenter>();
            Assert.That(navigation, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            var master = Array.Find(
                presenter.GetComponentsInChildren<Slider>(true),
                slider => slider.name == "MasterVolume");
            var fullscreen = Array.Find(
                presenter.GetComponentsInChildren<Toggle>(true),
                toggle => toggle.name == "Fullscreen");
            Assert.That(master, Is.Not.Null);
            Assert.That(fullscreen, Is.Not.Null);
            Assert.That(master.value, Is.EqualTo(0.63f).Within(0.001f));
            Assert.That(AudioListener.volume, Is.EqualTo(0.63f).Within(0.001f));
            Assert.That(fullscreen.isOn, Is.EqualTo(Screen.fullScreen));

            master.value = 0.37f;
            fullscreen.onValueChanged.Invoke(fullscreen.isOn);
            yield return null;

            Assert.That(navigation.LastSettingsRequest, Is.Not.Null);
            Assert.That(navigation.LastSettingsRequest.Snapshot.MasterVolume,
                Is.EqualTo(0.37f).Within(0.001f));
            Assert.That(AudioListener.volume, Is.EqualTo(0.37f).Within(0.001f));
            Assert.That(PlayerPrefs.GetFloat(MasterVolumeKey),
                Is.EqualTo(0.37f).Within(0.001f));
            Assert.That(PlayerPrefs.GetInt(FullscreenKey),
                Is.EqualTo(fullscreen.isOn ? 1 : 0));
        }

        private static IEnumerator TransitionToMainMenu(BootstrapRoot bootstrap)
        {
            var sequence = bootstrap.ReserveTransitionSequence();
            var transition = bootstrap.TransitionAsync(
                new SceneTransitionRequest(
                    sequence,
                    "gate-c-navigation-menu-" + sequence,
                    SceneId.GameStart,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)));
            yield return AwaitTask(transition, "Transition to MainMenu");
            Assert.That(transition.IsFaulted, Is.False, transition.Exception?.ToString());
            Assert.That(transition.Result.Succeeded, Is.True, transition.Result.Message);
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

        private static IEnumerator AwaitCondition(Func<bool> condition, string operation)
        {
            var deadline = Time.realtimeSinceStartup + TaskTimeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail(operation + " exceeded " + TaskTimeoutSeconds + " seconds.");
                }

                yield return null;
            }
        }

        private static void AssertTargetUnlocked(
            BootstrapRoot bootstrap,
            SceneId expectedTarget,
            string sourceSceneName)
        {
            Assert.That(SceneManager.GetSceneByName(sourceSceneName).isLoaded, Is.False);
            Assert.That(bootstrap.CurrentScene, Is.EqualTo(expectedTarget));
            Assert.That(Object.FindAnyObjectByType<PersistentInputGate>().IsLocked, Is.False);
            Assert.That(SceneInputLockState.IsLocked, Is.False);

            var entries = Object.FindObjectsByType<SceneContentEntry>(FindObjectsInactive.Include);
            Assert.That(entries, Has.Length.EqualTo(1));
            Assert.That(entries[0].SceneId, Is.EqualTo(expectedTarget));
            Assert.That(entries[0].IsBound, Is.True);
            Assert.That(entries[0].IsInteractive, Is.True);
        }

        private static Task InvokeTask(object target, string methodName, params object[] arguments)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Missing method: " + methodName);
            return (Task)method.Invoke(target, arguments);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + fieldName);
            field.SetValue(target, value);
        }
    }
}
