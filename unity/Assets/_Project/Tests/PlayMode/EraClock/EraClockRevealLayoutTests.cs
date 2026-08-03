using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TimeKey.Application.EraClock;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition.SceneFlow;
using TimeKey.Domain.Deck;
using TimeKey.Presentation;
using TimeKey.Presentation.EraClock;
using TimeKey.Presentation.OutOfBattleShell;
using TimeKey.Presentation.SceneFlowFinale;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TimeKey.Tests.PlayMode.EraClock
{
    public sealed class EraClockRevealLayoutTests
    {
        private const string EvidenceDirectoryPath =
            @"D:\godot\时之钥\时之钥\docs\migration\unity-3d\04-verification\evidence\era-clock-closeout";
        private const float TimeoutSeconds = 30f;

        private int previousWidth;
        private int previousHeight;
        private bool previousFullscreen;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousWidth = Screen.width;
            previousHeight = Screen.height;
            previousFullscreen = Screen.fullScreen;
            Directory.CreateDirectory(EvidenceDirectoryPath);
            SceneInputLockState.SetLocked(false);
            Screen.SetResolution(1920, 1080, false);
            yield return DestroyPersistentBootstraps();
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneInputLockState.SetLocked(false);
            yield return DestroyPersistentBootstraps();
            if (previousWidth > 0 && previousHeight > 0)
            {
                Screen.SetResolution(previousWidth, previousHeight, previousFullscreen);
            }

            yield return null;
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator FormalRoute_MiddleRevealClockDoesNotOverlapContent()
        {
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            BootstrapRoot bootstrap = null;
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (bootstrap == null && Time.realtimeSinceStartup < deadline)
            {
                bootstrap = Object.FindAnyObjectByType<BootstrapRoot>();
                yield return null;
            }

            Assert.That(bootstrap, Is.Not.Null);
            while (!bootstrap.InitializationTask.IsCompleted &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(bootstrap.InitializationTask.IsFaulted, Is.False,
                bootstrap.InitializationTask.Exception?.ToString());
            var menuSequence = bootstrap.ReserveTransitionSequence();
            var menuTask = bootstrap.TransitionAsync(new SceneTransitionRequest(
                menuSequence,
                "era-clock-closeout-menu-" + menuSequence,
                SceneId.GameStart,
                SceneId.MainMenu,
                new EmptySceneTransitionPayload(SceneId.MainMenu)));
            while (!menuTask.IsCompleted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(menuTask.IsFaulted, Is.False, menuTask.Exception?.ToString());
            Assert.That(
                menuTask.Result.Succeeded || menuTask.Result.Message == "Busy",
                Is.True,
                menuTask.Result.Message);
            while ((bootstrap.CurrentScene != SceneId.MainMenu ||
                    SceneInputLockState.IsLocked) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(bootstrap.CurrentScene, Is.EqualTo(SceneId.MainMenu));
            Assert.That(SceneInputLockState.IsLocked, Is.False);
            FindActiveButton("NewGame").onClick.Invoke();

            EraClockPresenter clock = null;
            LayeredSceneRevealPresenter reveal = null;
            CanvasGroup background = null;
            CanvasGroup roomReveal = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                clock = Object.FindObjectsByType<EraClockPresenter>(
                        FindObjectsInactive.Include)
                    .FirstOrDefault(candidate =>
                        candidate.gameObject.scene.name == "OutOfBattleShell" &&
                        candidate.CurrentSnapshot?.AnchorTarget == EraClockAnchorTarget.Center);
                reveal = Object.FindObjectsByType<LayeredSceneRevealPresenter>(
                        FindObjectsInactive.Include)
                    .FirstOrDefault(candidate =>
                        candidate.gameObject.scene.name == "OutOfBattleShell");
                background = FindActiveGroup("BackgroundRevealLayer");
                roomReveal = FindActiveGroup("RoomRevealLayer");
                if (clock != null && reveal != null && !reveal.IsComplete &&
                    background != null && roomReveal != null &&
                    roomReveal.alpha >= 0.35f)
                {
                    break;
                }

                yield return null;
            }

            Assert.That(clock, Is.Not.Null, "Formal OutOfBattle Center clock was not observed.");
            Assert.That(reveal, Is.Not.Null);
            Assert.That(reveal.IsComplete, Is.False,
                "The regression must inspect the real reveal middle state.");
            Assert.That(clock.CurrentSnapshot.AnchorTarget,
                Is.EqualTo(EraClockAnchorTarget.Center));

            Camera camera = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude)
                .First(candidate =>
                    candidate.enabled &&
                    candidate.gameObject.scene.name == "OutOfBattleShell");
            PrepareCanvases(camera, "OutOfBattleShell");
            yield return null;
            Canvas.ForceUpdateCanvases();

            var clockRect = ScreenRect(clock.GetComponent<RectTransform>(), camera);
            var roomRect = ScreenRect(FindActiveRect("CombatRoom"), camera);
            var contextRect = ScreenRect(FindActiveRect("RunContext"), camera);
            var pauseRect = ScreenRect(FindActiveRect("PauseButton"), camera);
            var settingsRect = ScreenRect(FindActiveRect("SettingsButton"), camera);
            var overlaps = clockRect.Overlaps(roomRect) ||
                clockRect.Overlaps(contextRect) ||
                clockRect.Overlaps(pauseRect) ||
                clockRect.Overlaps(settingsRect);

            var captureName = overlaps
                ? "red-formal-middle-overlap-1920x1080.png"
                : "formal-middle-1920x1080.png";
            Capture(camera, Path.Combine(EvidenceDirectoryPath, captureName), 1920, 1080);
            File.WriteAllText(
                Path.Combine(EvidenceDirectoryPath, "formal-middle-layout-1920x1080.json"),
                JsonUtility.ToJson(new LayoutEvidence
                {
                    viewport = "1920x1080",
                    state = "CenterRevealMiddle",
                    clock = RectEvidence.From(clockRect),
                    room = RectEvidence.From(roomRect),
                    context = RectEvidence.From(contextRect),
                    pause = RectEvidence.From(pauseRect),
                    settings = RectEvidence.From(settingsRect),
                    overlaps = overlaps
                }, true));

            Assert.That(clockRect.Overlaps(roomRect), Is.False,
                "Center Era Clock overlaps the central CombatRoom during reveal.");
            Assert.That(clockRect.Overlaps(contextRect), Is.False,
                "Center Era Clock overlaps RunContext during reveal.");
            Assert.That(clockRect.Overlaps(pauseRect), Is.False);
            Assert.That(clockRect.Overlaps(settingsRect), Is.False);
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator FormalScene_CapturesThreeViewportsAndDynamicResize()
        {
            var index = new VisualEvidenceIndex
            {
                unityVersion = UnityEngine.Application.unityVersion,
                renderer = SystemInfo.graphicsDeviceType.ToString(),
                frames = new List<VisualEvidenceFrame>()
            };
            var viewports = new[]
            {
                new Vector2Int(1280, 720),
                new Vector2Int(1920, 1080),
                new Vector2Int(2560, 1080)
            };
            EraClockPresenter clock = null;
            Camera camera = null;
            long sequence = 100;

            foreach (var viewport in viewports)
            {
                Screen.SetResolution(viewport.x, viewport.y, false);
                yield return null;
                yield return LoadSingle("OutOfBattleShell");
                var entry = Object.FindAnyObjectByType<SceneContentEntry>(
                    FindObjectsInactive.Include);
                var shell = Object.FindAnyObjectByType<OutOfBattleShellPresenter>(
                    FindObjectsInactive.Include);
                var reveal = Object.FindAnyObjectByType<LayeredSceneRevealPresenter>(
                    FindObjectsInactive.Include);
                clock = Object.FindAnyObjectByType<EraClockPresenter>(
                    FindObjectsInactive.Include);
                camera = Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
                Assert.That(entry, Is.Not.Null);
                Assert.That(shell, Is.Not.Null);
                Assert.That(reveal, Is.Not.Null);
                Assert.That(clock, Is.Not.Null);
                Assert.That(camera, Is.Not.Null);

                var start = StartPayload();
                var state = new OutOfBattleShellState(start);
                entry.Bind(start);
                entry.SetCameraEnabled(true);
                shell.Apply(state);
                clock.ApplySnapshot(EraClockSnapshotAdapter.FromOutOfBattle(
                    state,
                    sequence++,
                    EraClockAnchorTarget.Center));
                PrepareCanvases(camera, "OutOfBattleShell");
                var groups = new[]
                {
                    FindActiveGroup("BackgroundRevealLayer"),
                    FindActiveGroup("ContextRevealLayer"),
                    FindActiveGroup("RoomRevealLayer")
                };
                Assert.That(groups, Has.All.Not.Null);

                reveal.PlayReveal();
                CaptureVisualFrame(
                    camera,
                    clock,
                    groups,
                    viewport,
                    "initial",
                    index);
                yield return null;
                reveal.PlayReveal();
                var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
                while (groups[2].alpha < 0.35f &&
                       Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(reveal.IsComplete, Is.False);
                AssertNoContentOverlap(clock, camera);
                CaptureVisualFrame(
                    camera,
                    clock,
                    groups,
                    viewport,
                    "middle",
                    index);
                while (!reveal.IsComplete && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(reveal.IsComplete, Is.True);
                AssertNoContentOverlap(clock, camera);
                CaptureVisualFrame(
                    camera,
                    clock,
                    groups,
                    viewport,
                    "complete",
                    index);

                clock.ApplySnapshot(EraClockSnapshotAdapter.FromOutOfBattle(
                    state,
                    sequence++,
                    EraClockAnchorTarget.Hud));
                yield return AwaitClockSettled(clock);
                CaptureVisualFrame(
                    camera,
                    clock,
                    groups,
                    viewport,
                    "hud",
                    index);
            }

            var resized = new Vector2Int(1600, 900);
            Screen.SetResolution(resized.x, resized.y, false);
            yield return null;
            yield return null;
            var current = clock.CurrentSnapshot;
            clock.ApplySnapshot(new EraClockPresentationSnapshot(
                current.Era,
                current.Phase,
                sequence++,
                EraClockAnchorTarget.Center));
            yield return AwaitClockSettled(clock);
            clock.RefreshLayout();
            AssertNoContentOverlap(clock, camera);
            CaptureVisualFrame(
                camera,
                clock,
                NamedRevealGroups(),
                resized,
                "resize-center",
                index);
            clock.ApplySnapshot(new EraClockPresentationSnapshot(
                current.Era,
                current.Phase,
                sequence,
                EraClockAnchorTarget.Hud));
            yield return AwaitClockSettled(clock);
            clock.RefreshLayout();
            CaptureVisualFrame(
                camera,
                clock,
                NamedRevealGroups(),
                resized,
                "resize-hud",
                index);

            File.WriteAllText(
                Path.Combine(EvidenceDirectoryPath, "visual-evidence-index.json"),
                JsonUtility.ToJson(index, true));
        }

        private static Button FindActiveButton(string name)
        {
            var button = Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                .FirstOrDefault(candidate =>
                    candidate.name == name && candidate.gameObject.activeInHierarchy);
            Assert.That(button, Is.Not.Null, "Missing active button: " + name);
            return button;
        }

        private static RectTransform FindActiveRect(string name)
        {
            var rect = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include)
                .FirstOrDefault(candidate =>
                    candidate.name == name && candidate.gameObject.activeInHierarchy);
            Assert.That(rect, Is.Not.Null, "Missing active RectTransform: " + name);
            return rect;
        }

        private static CanvasGroup FindActiveGroup(string name)
        {
            return Object.FindObjectsByType<CanvasGroup>(FindObjectsInactive.Include)
                .FirstOrDefault(candidate =>
                    candidate.name == name && candidate.gameObject.activeInHierarchy);
        }

        private static CanvasGroup[] NamedRevealGroups()
        {
            return new[]
            {
                FindActiveGroup("BackgroundRevealLayer"),
                FindActiveGroup("ContextRevealLayer"),
                FindActiveGroup("RoomRevealLayer")
            };
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

        private static IEnumerator AwaitClockSettled(EraClockPresenter clock)
        {
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (clock.State != EraClockPresenterState.Settled &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(clock.State, Is.EqualTo(EraClockPresenterState.Settled));
        }

        private static void AssertNoContentOverlap(EraClockPresenter clock, Camera camera)
        {
            Canvas.ForceUpdateCanvases();
            var clockRect = ScreenRect(clock.GetComponent<RectTransform>(), camera);
            Assert.That(clockRect.Overlaps(ScreenRect(FindActiveRect("CombatRoom"), camera)),
                Is.False);
            Assert.That(clockRect.Overlaps(ScreenRect(FindActiveRect("RunContext"), camera)),
                Is.False);
            Assert.That(clockRect.Overlaps(ScreenRect(FindActiveRect("PauseButton"), camera)),
                Is.False);
            Assert.That(clockRect.Overlaps(ScreenRect(FindActiveRect("SettingsButton"), camera)),
                Is.False);
        }

        private static void CaptureVisualFrame(
            Camera camera,
            EraClockPresenter clock,
            CanvasGroup[] groups,
            Vector2Int viewport,
            string state,
            VisualEvidenceIndex index)
        {
            var fileName = "out-of-battle-reveal-" + state + "-" +
                viewport.x + "x" + viewport.y + ".png";
            Capture(
                camera,
                Path.Combine(EvidenceDirectoryPath, fileName),
                viewport.x,
                viewport.y);
            index.frames.Add(new VisualEvidenceFrame
            {
                file = fileName,
                viewport = viewport.x + "x" + viewport.y,
                state = state,
                anchor = clock.CurrentSnapshot.AnchorTarget.ToString(),
                presenterState = clock.State.ToString(),
                backgroundAlpha = groups[0].alpha,
                contextAlpha = groups[1].alpha,
                roomAlpha = groups[2].alpha,
                inputLockedByClock = clock.CurrentSnapshot.IsInputLocked,
                clockBlocksRaycasts = clock.GetComponent<CanvasGroup>().blocksRaycasts
            });
        }

        private static RunStartPayload StartPayload()
        {
            return new RunStartPayload(
                RunStartKind.SeedGame,
                "era-clock-closeout-visual-run",
                731,
                "731",
                1,
                1,
                3,
                64,
                "silver-character",
                StarterDeck.OrderedStableIds);
        }

        private static void PrepareCanvases(Camera camera, string sceneName)
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (!canvas.isRootCanvas || canvas.gameObject.scene.name != sceneName)
                {
                    continue;
                }

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }

            Canvas.ForceUpdateCanvases();
        }

        private static Rect ScreenRect(RectTransform rect, Camera camera)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 minimum = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 maximum = minimum;
            for (var index = 1; index < corners.Length; index++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corners[index]);
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }

            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }

        private static void Capture(Camera camera, string path, int width, int height)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var priorTarget = camera.targetTexture;
            var priorActive = RenderTexture.active;
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.Destroy(texture);
            camera.targetTexture = priorTarget;
            RenderTexture.active = priorActive;
            target.Release();
            Object.Destroy(target);
        }

        private static IEnumerator DestroyPersistentBootstraps()
        {
            var bootstraps = Object.FindObjectsByType<BootstrapRoot>(
                FindObjectsInactive.Include);
            foreach (var bootstrap in bootstraps)
            {
                Object.Destroy(bootstrap.gameObject);
            }

            if (bootstraps.Length > 0)
            {
                yield return null;
            }
        }

        [System.Serializable]
        private sealed class LayoutEvidence
        {
            public string viewport;
            public string state;
            public RectEvidence clock;
            public RectEvidence room;
            public RectEvidence context;
            public RectEvidence pause;
            public RectEvidence settings;
            public bool overlaps;
        }

        [System.Serializable]
        private sealed class RectEvidence
        {
            public float xMin;
            public float yMin;
            public float xMax;
            public float yMax;

            public static RectEvidence From(Rect rect)
            {
                return new RectEvidence
                {
                    xMin = rect.xMin,
                    yMin = rect.yMin,
                    xMax = rect.xMax,
                    yMax = rect.yMax
                };
            }
        }

        [System.Serializable]
        private sealed class VisualEvidenceIndex
        {
            public string unityVersion;
            public string renderer;
            public List<VisualEvidenceFrame> frames;
        }

        [System.Serializable]
        private sealed class VisualEvidenceFrame
        {
            public string file;
            public string viewport;
            public string state;
            public string anchor;
            public string presenterState;
            public float backgroundAlpha;
            public float contextAlpha;
            public float roomAlpha;
            public bool inputLockedByClock;
            public bool clockBlocksRaycasts;
        }
    }
}
