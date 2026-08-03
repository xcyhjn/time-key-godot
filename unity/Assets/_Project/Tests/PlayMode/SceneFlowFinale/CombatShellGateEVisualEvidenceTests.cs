using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition.SceneFlow;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Deck;
using TimeKey.Presentation;
using TimeKey.Presentation.CombatShell;
using TimeKey.Presentation.GameOver;
using TimeKey.Presentation.OutOfBattleShell;
using TimeKey.Presentation.SceneFlowFinale;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TimeKey.Tests.PlayMode.SceneFlowFinale
{
    public sealed class CombatShellGateEVisualEvidenceTests
    {
        private const int Width = 1280;
        private const int Height = 720;
        private const float TimeoutSeconds = 15f;

        private int _previousWidth;
        private int _previousHeight;
        private bool _previousFullscreen;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousWidth = Screen.width;
            _previousHeight = Screen.height;
            _previousFullscreen = Screen.fullScreen;
            Screen.SetResolution(Width, Height, false);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_previousWidth > 0 && _previousHeight > 0)
            {
                Screen.SetResolution(_previousWidth, _previousHeight, _previousFullscreen);
            }

            SceneInputLockState.SetLocked(false);
            yield return null;
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator FormalScenes_CaptureLayeredRevealTimelines()
        {
            var evidence = EvidenceDirectory();
            Directory.CreateDirectory(evidence);
            var timeline = new AnimationTimelineEvidence
            {
                unityVersion = UnityEngine.Application.unityVersion,
                renderer = SystemInfo.graphicsDeviceType.ToString(),
                viewport = Width + "x" + Height,
                scenes = new List<SceneTimelineEvidence>()
            };

            yield return CaptureOutOfBattle(evidence, timeline);
            yield return CaptureCombat(evidence, timeline);
            yield return CaptureGameOver(evidence, timeline);

            File.WriteAllText(
                Path.Combine(evidence, "animation-timeline.json"),
                JsonUtility.ToJson(timeline, true));
        }

        private static IEnumerator CaptureOutOfBattle(
            string evidence,
            AnimationTimelineEvidence timeline)
        {
            yield return LoadSingle("OutOfBattleShell");
            var entry = Object.FindFirstObjectByType<SceneContentEntry>(
                FindObjectsInactive.Include);
            var presenter = Object.FindFirstObjectByType<OutOfBattleShellPresenter>(
                FindObjectsInactive.Include);
            var reveal = Object.FindFirstObjectByType<LayeredSceneRevealPresenter>(
                FindObjectsInactive.Include);
            Assert.That(entry, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(reveal, Is.Not.Null);

            var start = StartPayload();
            entry.Bind(start);
            entry.SetCameraEnabled(true);
            presenter.Apply(new OutOfBattleShellState(start));
            var camera = Object.FindFirstObjectByType<Camera>();
            PrepareCanvases(camera);
            var oceanImage = NamedRawImage("MapBackground");
            var ocean = oceanImage.GetComponent<OutOfBattleOceanBackground>();
            Assert.That(ocean, Is.Not.Null);
            Assert.That(oceanImage.texture.name, Is.EqualTo("out-bg_sea"));
            var groups = NamedGroups(
                "BackgroundRevealLayer",
                "ContextRevealLayer",
                "RoomRevealLayer");
            yield return CaptureRevealSequence(
                reveal,
                camera,
                groups,
                evidence,
                "out-of-battle",
                timeline);
            yield return CaptureOceanViewport(
                camera,
                ocean,
                oceanImage,
                evidence,
                1280,
                720);
            yield return CaptureOceanViewport(
                camera,
                ocean,
                oceanImage,
                evidence,
                1920,
                1080);
            yield return CaptureOceanViewport(
                camera,
                ocean,
                oceanImage,
                evidence,
                2560,
                1080);
            Screen.SetResolution(Width, Height, false);
            yield return null;
            yield return null;
        }

        private static IEnumerator CaptureCombat(
            string evidence,
            AnimationTimelineEvidence timeline)
        {
            yield return LoadSingle("CombatVerticalSlice");
            var entry = Object.FindFirstObjectByType<SceneContentEntry>(
                FindObjectsInactive.Include);
            Assert.That(entry, Is.Not.Null);
            entry.Bind(DirectLaunch());
            entry.SetCameraEnabled(true);
            var reveal = Object.FindFirstObjectByType<CombatShellEntrancePresenter>(
                FindObjectsInactive.Include);
            var camera = Object.FindFirstObjectByType<Camera>();
            Assert.That(reveal, Is.Not.Null);
            PrepareCanvases(camera);
            var groups = NamedGroups(
                "Status",
                "Timeline",
                "DetailPanel",
                "EffectFrameHost",
                "CardHandHost");

            reveal.PlayReveal();
            var frames = new List<FrameEvidence>
            {
                Frame("initial", 0f, groups)
            };
            Assert.That(groups[0].alpha, Is.Zero.Within(0.001f));
            Assert.That(groups[1].alpha, Is.Zero.Within(0.001f));
            Assert.That(groups[2].alpha, Is.Zero.Within(0.001f));
            Assert.That(groups[3].alpha, Is.Zero.Within(0.001f));
            Assert.That(groups[4].alpha, Is.Zero.Within(0.001f));
            yield return Capture(camera, evidence, "combat-reveal-initial.png", false);
            yield return null;
            reveal.PlayReveal();
            var started = Time.realtimeSinceStartup;

            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (groups[0].alpha < 0.35f && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(groups[0].alpha, Is.GreaterThan(groups[1].alpha));
            Assert.That(groups[1].alpha, Is.GreaterThanOrEqualTo(groups[2].alpha));
            Assert.That(groups[2].alpha, Is.GreaterThanOrEqualTo(groups[3].alpha));
            Assert.That(groups[3].alpha, Is.GreaterThanOrEqualTo(groups[4].alpha));
            frames.Add(Frame("middle", Time.realtimeSinceStartup - started, groups));
            yield return Capture(camera, evidence, "combat-reveal-middle.png", true);

            while (!reveal.IsComplete && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(reveal.IsComplete, Is.True);
            AssertTerminal(groups);
            frames.Add(Frame("complete", Time.realtimeSinceStartup - started, groups));
            yield return Capture(camera, evidence, "combat-reveal-complete.png", true);
            AssertFramesDiffer(evidence, "combat-reveal-initial.png", "combat-reveal-middle.png");
            AssertFramesDiffer(evidence, "combat-reveal-middle.png", "combat-reveal-complete.png");
            timeline.scenes.Add(new SceneTimelineEvidence
            {
                scene = "CombatVerticalSlice",
                layerOrder = new[]
                {
                    "Status", "Timeline", "DetailPanel", "EffectFrameHost",
                    "CardHandHost"
                },
                frames = frames
            });
        }

        private static IEnumerator CaptureGameOver(
            string evidence,
            AnimationTimelineEvidence timeline)
        {
            yield return LoadSingle("GameOver");
            var outcome = DefeatOutcome();
            var entry = Object.FindFirstObjectByType<SceneContentEntry>(
                FindObjectsInactive.Include);
            var presenter = Object.FindFirstObjectByType<GameOverPresenter>(
                FindObjectsInactive.Include);
            var reveal = Object.FindFirstObjectByType<LayeredSceneRevealPresenter>(
                FindObjectsInactive.Include);
            Assert.That(entry, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(reveal, Is.Not.Null);
            entry.Bind(outcome);
            entry.SetCameraEnabled(true);
            presenter.Apply(outcome);
            var camera = Object.FindFirstObjectByType<Camera>();
            PrepareCanvases(camera);
            var groups = NamedGroups("BackgroundRevealLayer", "PanelRevealLayer");
            yield return CaptureRevealSequence(
                reveal,
                camera,
                groups,
                evidence,
                "game-over",
                timeline);
        }

        private static IEnumerator CaptureRevealSequence(
            LayeredSceneRevealPresenter reveal,
            Camera camera,
            CanvasGroup[] groups,
            string evidence,
            string filePrefix,
            AnimationTimelineEvidence timeline)
        {
            reveal.PlayReveal();
            var frames = new List<FrameEvidence>
            {
                Frame("initial", 0f, groups)
            };
            foreach (var group in groups)
            {
                Assert.That(group.alpha, Is.Zero.Within(0.001f));
                Assert.That(group.blocksRaycasts, Is.False);
            }
            yield return Capture(camera, evidence, filePrefix + "-reveal-initial.png", false);
            yield return null;
            reveal.PlayReveal();
            var started = Time.realtimeSinceStartup;

            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (groups[0].alpha < 0.35f && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(groups[0].alpha, Is.GreaterThan(groups[groups.Length - 1].alpha));
            frames.Add(Frame("middle", Time.realtimeSinceStartup - started, groups));
            yield return Capture(camera, evidence, filePrefix + "-reveal-middle.png", true);
            while (!reveal.IsComplete && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(reveal.IsComplete, Is.True);
            AssertTerminal(groups);
            frames.Add(Frame("complete", Time.realtimeSinceStartup - started, groups));
            yield return Capture(camera, evidence, filePrefix + "-reveal-complete.png", true);
            AssertFramesDiffer(
                evidence,
                filePrefix + "-reveal-initial.png",
                filePrefix + "-reveal-middle.png");
            AssertFramesDiffer(
                evidence,
                filePrefix + "-reveal-middle.png",
                filePrefix + "-reveal-complete.png");
            timeline.scenes.Add(new SceneTimelineEvidence
            {
                scene = filePrefix,
                layerOrder = LayerNames(groups),
                frames = frames
            });
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
            bool requireVariation,
            int width = Width,
            int height = Height)
        {
            Assert.That(camera, Is.Not.Null);
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var priorTarget = camera.targetTexture;
            var priorActive = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();
            var path = Path.Combine(directory, fileName);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            if (requireVariation)
            {
                Assert.That(HasVisibleVariation(texture), Is.True, path);
            }

            Object.Destroy(texture);
            camera.targetTexture = priorTarget;
            RenderTexture.active = priorActive;
            target.Release();
            Object.Destroy(target);
            yield break;
        }

        private static IEnumerator CaptureOceanViewport(
            Camera camera,
            OutOfBattleOceanBackground ocean,
            RawImage image,
            string directory,
            int width,
            int height)
        {
            Screen.SetResolution(width, height, false);
            yield return null;
            yield return null;
            ocean.RefreshUv();
            var rect = image.rectTransform.rect;
            Assert.That(
                image.uvRect.width / image.uvRect.height,
                Is.EqualTo(rect.width / rect.height).Within(0.01f));
            yield return Capture(
                camera,
                directory,
                "out-of-battle-ocean-" + width + "x" + height + ".png",
                true,
                width,
                height);
        }

        private static void PrepareCanvases(Camera camera)
        {
            Assert.That(camera, Is.Not.Null);
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Assert.That(canvases, Is.Not.Empty);
            foreach (var canvas in canvases)
            {
                if (!canvas.isRootCanvas)
                {
                    continue;
                }

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }

            Canvas.ForceUpdateCanvases();
        }

        private static CanvasGroup[] NamedGroups(params string[] names)
        {
            var groups = new CanvasGroup[names.Length];
            var candidates = Object.FindObjectsByType<CanvasGroup>(FindObjectsInactive.Include);
            for (var index = 0; index < names.Length; index++)
            {
                foreach (var candidate in candidates)
                {
                    if (candidate.name == names[index])
                    {
                        groups[index] = candidate;
                        break;
                    }
                }

                Assert.That(groups[index], Is.Not.Null, "Missing CanvasGroup: " + names[index]);
            }

            return groups;
        }

        private static RawImage NamedRawImage(string name)
        {
            var images = Object.FindObjectsByType<RawImage>(FindObjectsInactive.Include);
            foreach (var image in images)
            {
                if (image.name == name)
                {
                    return image;
                }
            }

            Assert.Fail("Missing RawImage: " + name);
            return null;
        }

        private static void AssertTerminal(IEnumerable<CanvasGroup> groups)
        {
            foreach (var group in groups)
            {
                Assert.That(group.alpha, Is.EqualTo(1f).Within(0.001f), group.name);
                Assert.That(group.interactable, Is.True, group.name);
                Assert.That(group.blocksRaycasts, Is.True, group.name);
            }
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
                    if (left[index] == right[index])
                    {
                        continue;
                    }

                    same = false;
                    break;
                }
            }

            Assert.That(same, Is.False, first + " and " + second);
        }

        private static FrameEvidence Frame(
            string name,
            float elapsedSeconds,
            CanvasGroup[] groups)
        {
            var alphas = new float[groups.Length];
            for (var index = 0; index < groups.Length; index++)
            {
                alphas[index] = groups[index].alpha;
            }

            return new FrameEvidence
            {
                frame = name,
                elapsedSeconds = elapsedSeconds,
                layerAlphas = alphas
            };
        }

        private static string[] LayerNames(CanvasGroup[] groups)
        {
            var names = new string[groups.Length];
            for (var index = 0; index < groups.Length; index++)
            {
                names[index] = groups[index].name;
            }

            return names;
        }

        private static RunStartPayload StartPayload()
        {
            return new RunStartPayload(
                RunStartKind.SeedGame,
                "gate-e-visual-run",
                731,
                "731",
                1,
                2,
                3,
                64,
                "silver-character",
                StarterDeck.OrderedStableIds);
        }

        private static CombatLaunchPayload DirectLaunch()
        {
            return new OutOfBattleShellState(StartPayload()).CreateCombatLaunch(
                "gate-e-visual-launch",
                "gate-e-visual-room",
                "combat-vertical-slice",
                731);
        }

        private static CombatOutcome DefeatOutcome()
        {
            var launch = DirectLaunch();
            var settlement = new BattleSettlementState(
                launch.BattleTag,
                launch.BattleSeed,
                new BattleRewardEntry(
                    "gate-e-visual-reward",
                    BattleRewardKind.Acquire,
                    "获得卡牌"));
            Assert.That(settlement.TryResolve(1, BattleOutcome.Defeat).Succeeded, Is.True);
            var boundary = settlement.TryCreateReturnBoundary(
                new BattleRoundLedger(2, 3, 64).Snapshot,
                launch.DeckStableIds);
            Assert.That(boundary.Succeeded, Is.True);
            var result = CombatOutcome.TryCreate(
                "gate-e-visual-defeat",
                launch,
                settlement.Snapshot,
                boundary.Payload);
            Assert.That(result.Succeeded, Is.True);
            return result.Outcome;
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
                "combat-shell-gate-e");
        }

        [Serializable]
        private sealed class AnimationTimelineEvidence
        {
            public string unityVersion;
            public string renderer;
            public string viewport;
            public List<SceneTimelineEvidence> scenes;
        }

        [Serializable]
        private sealed class SceneTimelineEvidence
        {
            public string scene;
            public string[] layerOrder;
            public List<FrameEvidence> frames;
        }

        [Serializable]
        private sealed class FrameEvidence
        {
            public string frame;
            public float elapsedSeconds;
            public float[] layerAlphas;
        }
    }
}
