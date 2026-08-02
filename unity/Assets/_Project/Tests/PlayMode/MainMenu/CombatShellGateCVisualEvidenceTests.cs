using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition.SceneFlow;
using TimeKey.Presentation.GameStart;
using TimeKey.Presentation.MainMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.MainMenu
{
    public sealed class CombatShellGateCVisualEvidenceTests
    {
        private const float TimeoutSeconds = 15f;
        private static readonly Vector2Int[] Viewports =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1080)
        };
        private GameObject _eventSystem;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_eventSystem != null)
            {
                UnityEngine.Object.Destroy(_eventSystem);
            }

            SceneInputLockState.SetLocked(false);
            yield return null;
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator FormalScenes_RenderAnimationResponsiveMenuAndOverlays()
        {
            var evidence = EvidenceDirectory();
            Directory.CreateDirectory(evidence);

            yield return LoadSingle("GameStart");
            var startEntry = UnityEngine.Object.FindFirstObjectByType<SceneContentEntry>(
                FindObjectsInactive.Include);
            Assert.That(startEntry, Is.Not.Null);
            startEntry.Bind(new EmptySceneTransitionPayload(SceneId.GameStart));
            startEntry.SetCameraEnabled(true);
            var startCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            Assert.That(startCamera, Is.Not.Null);
            PrepareCanvas(startCamera);
            var start = UnityEngine.Object.FindFirstObjectByType<StartLogoPresenter>();
            Assert.That(start, Is.Not.Null);
            start.PlayReveal();
            yield return CaptureViewports(startCamera, evidence, "game-start", "initial");

            var middleDeadline = Time.realtimeSinceStartup + 1.25f;
            while (Time.realtimeSinceStartup < middleDeadline)
            {
                yield return null;
            }

            Assert.That(start.IsComplete, Is.False);
            yield return CaptureViewports(startCamera, evidence, "game-start", "middle");
            AssertFramesDiffer(
                evidence,
                "game-start-1280x720-initial.png",
                "game-start-1280x720-middle.png");
            var completionDeadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!start.IsComplete && Time.realtimeSinceStartup < completionDeadline)
            {
                yield return null;
            }

            Assert.That(start.IsComplete, Is.True);
            yield return CaptureViewports(
                startCamera,
                evidence,
                "game-start",
                "complete",
                false);
            AssertFramesDiffer(
                evidence,
                "game-start-1280x720-middle.png",
                "game-start-1280x720-complete.png");

            yield return LoadSingle("MainMenu");
            var menuEntry = UnityEngine.Object.FindFirstObjectByType<SceneContentEntry>(
                FindObjectsInactive.Include);
            Assert.That(menuEntry, Is.Not.Null);
            menuEntry.Bind(new EmptySceneTransitionPayload(SceneId.MainMenu));
            menuEntry.SetCameraEnabled(true);
            var menuCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            Assert.That(menuCamera, Is.Not.Null);
            PrepareCanvas(menuCamera);
            var menu = UnityEngine.Object.FindFirstObjectByType<MainMenuPresenter>();
            Assert.That(menu, Is.Not.Null);
            menu.PlayEntrance();
            yield return CaptureViewports(
                menuCamera,
                evidence,
                "main-menu",
                "entrance-initial",
                false);
            menu.PlayEntrance();
            var entranceMiddleDeadline = Time.realtimeSinceStartup + 0.3f;
            while (Time.realtimeSinceStartup < entranceMiddleDeadline)
            {
                yield return null;
            }

            Assert.That(menu.IsEntranceComplete, Is.False);
            yield return CaptureViewports(
                menuCamera,
                evidence,
                "main-menu",
                "entrance-middle");
            var entranceDeadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!menu.IsEntranceComplete && Time.realtimeSinceStartup < entranceDeadline)
            {
                yield return null;
            }

            Assert.That(menu.IsEntranceComplete, Is.True);
            yield return CaptureViewports(
                menuCamera,
                evidence,
                "main-menu",
                "entrance-complete");
            AssertFramesDiffer(
                evidence,
                "main-menu-1280x720-entrance-initial.png",
                "main-menu-1280x720-entrance-middle.png");
            AssertFramesDiffer(
                evidence,
                "main-menu-1280x720-entrance-middle.png",
                "main-menu-1280x720-entrance-complete.png");

            var silver = Resources.Load<Font>("Fonts/Silver");
            Assert.That(silver, Is.Not.Null);
            foreach (var text in menu.GetComponentsInChildren<Text>(true))
            {
                Assert.That(text.font, Is.EqualTo(silver), text.name);
            }

            Assert.That(Button(menu, "Continue").interactable, Is.False);
            yield return CaptureViewports(menuCamera, evidence, "main-menu", "idle");
            yield return CaptureViewports(menuCamera, evidence, "main-menu", "continue-disabled");

            _eventSystem = new GameObject(
                "EvidenceEventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            var eventSystem = _eventSystem.GetComponent<EventSystem>();
            var newGame = Button(menu, "NewGame");
            var settings = Button(menu, "Settings");
            var pointer = new PointerEventData(eventSystem) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(newGame.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            yield return CaptureViewports(menuCamera, evidence, "main-menu", "hover-left");
            ExecuteEvents.Execute(newGame.gameObject, pointer, ExecuteEvents.pointerExitHandler);
            ExecuteEvents.Execute(settings.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            yield return CaptureViewports(menuCamera, evidence, "main-menu", "hover-right");
            ExecuteEvents.Execute(settings.gameObject, pointer, ExecuteEvents.pointerExitHandler);

            eventSystem.SetSelectedGameObject(newGame.gameObject);
            yield return CaptureViewports(menuCamera, evidence, "main-menu", "focus-left");
            AssertFramesDiffer(
                evidence,
                "main-menu-1280x720-idle.png",
                "main-menu-1280x720-focus-left.png");
            eventSystem.SetSelectedGameObject(settings.gameObject);
            yield return CaptureViewports(menuCamera, evidence, "main-menu", "focus-right");

            eventSystem.SetSelectedGameObject(newGame.gameObject);
            yield return null;
            ExecuteEvents.Execute(newGame.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return CaptureViewports(menuCamera, evidence, "main-menu", "pressed");
            AssertFramesDiffer(
                evidence,
                "main-menu-1280x720-focus-left.png",
                "main-menu-1280x720-pressed.png");
            ExecuteEvents.Execute(newGame.gameObject, pointer, ExecuteEvents.pointerUpHandler);

            Button(menu, "SeedGame").onClick.Invoke();
            Assert.That(menu.IsSeedOpen, Is.True);
            Assert.That(SceneInputLockState.IsLocked, Is.True);
            yield return CaptureViewports(menuCamera, evidence, "main-menu", "seed");
            menu.CloseSeed();

            settings.onClick.Invoke();
            Assert.That(menu.IsSettingsOpen, Is.True);
            var settingsSliders = menu.GetComponentsInChildren<Slider>(true);
            Assert.That(settingsSliders, Has.Length.EqualTo(3));
            Assert.That(settingsSliders[0].interactable, Is.True);
            Assert.That(settingsSliders[1].interactable, Is.False);
            Assert.That(settingsSliders[2].interactable, Is.False);
            Assert.That(menu.GetComponentInChildren<Toggle>(true), Is.Not.Null);
            yield return CaptureViewports(menuCamera, evidence, "main-menu", "settings");
            menu.CloseSettings();

            Button(menu, "Database").onClick.Invoke();
            Assert.That(menu.IsModalOpen, Is.True);
            yield return CaptureViewports(menuCamera, evidence, "main-menu", "database");
            menu.CloseModal();

            Button(menu, "Exit").onClick.Invoke();
            Assert.That(menu.IsModalOpen, Is.True);
            yield return CaptureViewports(menuCamera, evidence, "main-menu", "exit");
            menu.CloseModal();
            Assert.That(SceneInputLockState.IsLocked, Is.False);
        }

        private static IEnumerator LoadSingle(string sceneName)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!operation.isDone && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(operation.isDone, Is.True, sceneName + " load timed out.");
            yield return null;
        }

        private static IEnumerator Capture(
            Camera camera,
            string directory,
            string fileName,
            int width,
            int height,
            bool requireVariation = true)
        {
            Screen.SetResolution(width, height, false);
            yield return null;
            yield return null;
            var path = Path.Combine(directory, fileName);
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();
            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Assert.That(texture.width, Is.EqualTo(width), path);
            Assert.That(texture.height, Is.EqualTo(height), path);
            if (requireVariation)
            {
                Assert.That(HasVisibleVariation(texture), Is.True, path);
            }
            UnityEngine.Object.Destroy(texture);
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            UnityEngine.Object.Destroy(renderTexture);
        }

        private static IEnumerator CaptureViewports(
            Camera camera,
            string directory,
            string scene,
            string state,
            bool requireVariation = true)
        {
            foreach (var viewport in Viewports)
            {
                yield return Capture(
                    camera,
                    directory,
                    scene + "-" + viewport.x + "x" + viewport.y + "-" + state + ".png",
                    viewport.x,
                    viewport.y,
                    requireVariation);
            }
        }

        private static void PrepareCanvas(Camera camera)
        {
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
        }

        private static bool HasVisibleVariation(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            var first = pixels[0];
            var step = Math.Max(1, pixels.Length / 2048);
            for (var index = step; index < pixels.Length; index += step)
            {
                if (!pixels[index].Equals(first))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertFramesDiffer(string directory, string first, string second)
        {
            var left = File.ReadAllBytes(Path.Combine(directory, first));
            var right = File.ReadAllBytes(Path.Combine(directory, second));
            var same = left.Length == right.Length;
            if (same)
            {
                for (var index = 0; index < left.Length; index++)
                {
                    if (left[index] != right[index])
                    {
                        same = false;
                        break;
                    }
                }
            }

            Assert.That(same, Is.False, first + " and " + second + " must differ.");
        }

        private static Button Button(MainMenuPresenter presenter, string name)
        {
            foreach (var button in presenter.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name)
                {
                    return button;
                }
            }

            Assert.Fail("Main menu button is missing: " + name + ".");
            return null;
        }

        private static string EvidenceDirectory()
        {
            var root = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            Assert.That(root, Is.Not.Null.And.Not.Empty);
            return Path.Combine(
                root,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "combat-shell-gate-c");
        }
    }
}
