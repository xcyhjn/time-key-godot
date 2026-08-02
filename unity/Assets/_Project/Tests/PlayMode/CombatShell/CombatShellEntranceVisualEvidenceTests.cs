using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition.SceneFlow;
using TimeKey.Presentation.CombatShell;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.CombatShell
{
    public sealed class CombatShellEntranceVisualEvidenceTests
    {
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 720;
        private const float TimeoutSeconds = 15f;

        private Scene _previousActiveScene;
        private Scene _loadedScene;
        private Camera _camera;
        private RenderTexture _renderTexture;
        private Transform _reusedSceneRoot;
        private bool _reusedSceneRootWasActive;
        private bool _ownsLoadedScene;
        private int _previousWidth;
        private int _previousHeight;
        private bool _previousFullscreen;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_camera != null)
            {
                _camera.targetTexture = null;
            }

            if (_previousActiveScene.IsValid() && _previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(_previousActiveScene);
            }

            if (_ownsLoadedScene && _loadedScene.IsValid() && _loadedScene.isLoaded)
            {
                var unload = SceneManager.UnloadSceneAsync(_loadedScene);
                if (unload != null)
                {
                    var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
                    while (!unload.isDone && Time.realtimeSinceStartup < deadline)
                    {
                        yield return null;
                    }

                    Assert.That(unload.isDone, Is.True, "Combat scene unload timed out.");
                }
            }

            if (_reusedSceneRoot != null)
            {
                _reusedSceneRoot.gameObject.SetActive(_reusedSceneRootWasActive);
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                UnityEngine.Object.Destroy(_renderTexture);
            }

            if (_previousWidth > 0 && _previousHeight > 0)
            {
                Screen.SetResolution(_previousWidth, _previousHeight, _previousFullscreen);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator SavedCombatScene_EntranceProgressesAndCapturesRenderedFrames()
        {
            _previousActiveScene = SceneManager.GetActiveScene();
            _previousWidth = Screen.width;
            _previousHeight = Screen.height;
            _previousFullscreen = Screen.fullScreen;
            Screen.SetResolution(CaptureWidth, CaptureHeight, false);
            yield return null;

            var existingScene = SceneManager.GetSceneByName("CombatVerticalSlice");
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                var unloadExisting = SceneManager.UnloadSceneAsync(existingScene);
                if (unloadExisting != null)
                {
                    var unloadDeadline = Time.realtimeSinceStartup + TimeoutSeconds;
                    while (!unloadExisting.isDone && Time.realtimeSinceStartup < unloadDeadline)
                    {
                        yield return null;
                    }

                    Assert.That(unloadExisting.isDone, Is.True,
                        "An existing combat scene could not be unloaded before evidence capture.");
                }
                else
                {
                    _loadedScene = existingScene;
                    _reusedSceneRoot = FindInScene<Transform>(_loadedScene, "VerticalSliceRoot");
                    Assert.That(_reusedSceneRoot, Is.Not.Null,
                        "The existing combat scene had no reusable vertical slice root.");
                    _reusedSceneRootWasActive = _reusedSceneRoot.gameObject.activeSelf;
                    _reusedSceneRoot.gameObject.SetActive(false);
                }
            }

            if (!_loadedScene.IsValid())
            {
                var load = SceneManager.LoadSceneAsync(
                    "CombatVerticalSlice",
                    LoadSceneMode.Additive);
                Assert.That(load, Is.Not.Null);
                var loadDeadline = Time.realtimeSinceStartup + TimeoutSeconds;
                while (!load.isDone && Time.realtimeSinceStartup < loadDeadline)
                {
                    yield return null;
                }

                Assert.That(load.isDone, Is.True, "Combat scene load timed out.");
                _loadedScene = SceneManager.GetSceneByName("CombatVerticalSlice");
                _ownsLoadedScene = true;
            }

            Assert.That(_loadedScene.IsValid() && _loadedScene.isLoaded, Is.True);
            SceneManager.SetActiveScene(_loadedScene);

            var root = FindInScene<Transform>(_loadedScene, "VerticalSliceRoot");
            var entry = FindInScene<SceneContentEntry>(_loadedScene);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.gameObject.activeSelf, Is.False);
            Assert.That(entry, Is.Not.Null);

            var presenter = root.GetComponent<CombatShellEntrancePresenter>();
            var topHudRoot = root.Find("SliceCanvas/HUD/CombatTopHUD");
            var background = root.Find("World/CombatShellBackground");
            var cameraRoot = root.Find("World/SliceCamera");
            Assert.That(presenter, Is.Not.Null);
            Assert.That(topHudRoot, Is.Not.Null);
            Assert.That(background, Is.Not.Null);
            Assert.That(cameraRoot, Is.Not.Null);
            var topHud = topHudRoot.GetComponent<CanvasGroup>();
            _camera = cameraRoot.GetComponent<Camera>();
            Assert.That(topHud, Is.Not.Null);
            Assert.That(_camera, Is.Not.Null);

            var restingScale = background.localScale;
            _renderTexture = new RenderTexture(
                CaptureWidth,
                CaptureHeight,
                24,
                RenderTextureFormat.ARGB32)
            {
                name = "CombatShellEntranceEvidence"
            };
            _renderTexture.Create();
            _camera.targetTexture = _renderTexture;

            entry.Bind(DirectLaunch());
            Assert.That(root.gameObject.activeSelf, Is.True);
            Assert.That(presenter.IsComplete, Is.False);
            Assert.That(topHud.alpha, Is.Zero.Within(0.001f));
            var initialScale = background.localScale;
            Assert.That(Vector3.Distance(initialScale, restingScale * 0.985f),
                Is.LessThan(0.0001f));

            Texture2D initial = null;
            Texture2D middle = null;
            Texture2D complete = null;
            try
            {
                initial = Capture();

                yield return null;
                presenter.PlayReveal();
                Assert.That(presenter.IsComplete, Is.False);
                Assert.That(topHud.alpha, Is.Zero.Within(0.001f));

                var middleDeadline = Time.realtimeSinceStartup + TimeoutSeconds;
                while (topHud.alpha <= 0.05f &&
                       !presenter.IsComplete &&
                       Time.realtimeSinceStartup < middleDeadline)
                {
                    yield return null;
                }

                Assert.That(presenter.IsComplete, Is.False,
                    "The saved entrance skipped its observable middle state.");
                Assert.That(topHud.alpha, Is.InRange(0.05f, 0.999f));
                var middleScale = background.localScale;
                Assert.That(Vector3.Distance(middleScale, restingScale),
                    Is.GreaterThan(0f));
                Assert.That(Vector3.Distance(middleScale, restingScale),
                    Is.LessThan(Vector3.Distance(initialScale, restingScale)));
                middle = Capture();

                var completeDeadline = Time.realtimeSinceStartup + TimeoutSeconds;
                while (!presenter.IsComplete &&
                       Time.realtimeSinceStartup < completeDeadline)
                {
                    yield return null;
                }

                Assert.That(presenter.IsComplete, Is.True,
                    "The saved entrance did not complete.");
                Assert.That(topHud.alpha, Is.EqualTo(1f).Within(0.001f));
                Assert.That(Vector3.Distance(background.localScale, restingScale),
                    Is.LessThan(0.0001f));
                complete = Capture();

                AssertRendered(initial, "initial");
                AssertRendered(middle, "middle");
                AssertRendered(complete, "complete");
                Assert.That(Checksum(initial), Is.Not.EqualTo(Checksum(middle)));
                Assert.That(Checksum(middle), Is.Not.EqualTo(Checksum(complete)));
                WriteEvidence(initial, "combat-shell-entrance-1280x720-initial.png");
                WriteEvidence(middle, "combat-shell-entrance-1280x720-middle.png");
                WriteEvidence(complete, "combat-shell-entrance-1280x720-complete.png");
            }
            finally
            {
                if (initial != null)
                {
                    UnityEngine.Object.Destroy(initial);
                }

                if (middle != null)
                {
                    UnityEngine.Object.Destroy(middle);
                }

                if (complete != null)
                {
                    UnityEngine.Object.Destroy(complete);
                }
            }
        }

        private Texture2D Capture()
        {
            Canvas.ForceUpdateCanvases();
            _camera.Render();
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _renderTexture;
                var image = new Texture2D(
                    CaptureWidth,
                    CaptureHeight,
                    TextureFormat.RGB24,
                    false);
                image.ReadPixels(
                    new Rect(0f, 0f, CaptureWidth, CaptureHeight),
                    0,
                    0,
                    false);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private static T FindInScene<T>(Scene scene, string objectName = null)
            where T : Component
        {
            foreach (var sceneRoot in scene.GetRootGameObjects())
            {
                var components = sceneRoot.GetComponentsInChildren<T>(true);
                for (var index = 0; index < components.Length; index++)
                {
                    if (objectName == null || components[index].name == objectName)
                    {
                        return components[index];
                    }
                }
            }

            return null;
        }

        private static CombatLaunchPayload DirectLaunch()
        {
            return new CombatLaunchPayload(
                "combat-shell-visual-launch",
                "combat-shell-visual-run",
                731,
                1,
                1,
                1,
                "combat-shell-visual-room",
                "silver-character",
                0,
                "combat-vertical-slice",
                731,
                new[]
                {
                    "lighting", "earthquake", "recover", "built", "poison",
                    "wind", "tornado", "lighting", "recover", "built", "poison", "wind"
                });
        }

        private static void AssertRendered(Texture2D image, string state)
        {
            Assert.That(image, Is.Not.Null, state);
            Assert.That(image.width, Is.EqualTo(CaptureWidth), state);
            Assert.That(image.height, Is.EqualTo(CaptureHeight), state);
            var colors = new HashSet<int>();
            var pixels = image.GetPixels32();
            for (var index = 0; index < pixels.Length; index += 37)
            {
                var pixel = pixels[index];
                colors.Add(pixel.r | (pixel.g << 8) | (pixel.b << 16));
            }

            Assert.That(colors.Count, Is.GreaterThan(64),
                state + " frame is blank or nearly uniform.");
        }

        private static ulong Checksum(Texture2D image)
        {
            var pixels = image.GetPixels32();
            ulong checksum = 1469598103934665603UL;
            for (var index = 0; index < pixels.Length; index += 37)
            {
                checksum ^= pixels[index].r;
                checksum *= 1099511628211UL;
                checksum ^= pixels[index].g;
                checksum *= 1099511628211UL;
                checksum ^= pixels[index].b;
                checksum *= 1099511628211UL;
            }

            return checksum;
        }

        private static void WriteEvidence(Texture2D image, string fileName)
        {
            var repositoryRoot = Environment.GetEnvironmentVariable(
                "TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                return;
            }

            var directory = Path.Combine(
                repositoryRoot,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "combat-shell-gate-b");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, fileName), image.EncodeToPNG());
        }
    }
}
