using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TimeKey.Application.EraClock;
using TimeKey.Presentation.EraClock;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.EraClock
{
    public sealed class EraClockVisualEvidenceTests
    {
        private const string EvidenceDirectoryPath =
            @"D:\godot\时之钥\时之钥\docs\migration\unity-3d\04-verification\evidence\era-clock-animation-gate-evidence";
        private const int EvidenceLayer = 31;

        private GameObject root;
        private Transform evidenceCanvas;
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
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                root = null;
                evidenceCanvas = null;
            }

            if (previousWidth > 0 && previousHeight > 0)
            {
                Screen.SetResolution(previousWidth, previousHeight, previousFullscreen);
            }

            yield return null;
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator CapturesThreeViewportsAndDynamicResize()
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

            for (var indexValue = 0; indexValue < viewports.Length; indexValue++)
            {
                Vector2Int viewport = viewports[indexValue];
                Screen.SetResolution(viewport.x, viewport.y, false);
                yield return null;
                yield return null;
                Rig rig = CreateRig(viewport);
                rig.Presenter.ApplySnapshot(Snapshot(2, 3, indexValue + 1));
                yield return null;
                CaptureAndRecord(
                    rig,
                    viewport,
                    "center-" + viewport.x + "x" + viewport.y + ".png",
                    "center",
                    index);
                AssertVisibleAndWithinViewport(rig, viewport);
                UnityEngine.Object.DestroyImmediate(root);
                root = null;
            }

            Screen.SetResolution(1600, 900, false);
            yield return null;
            yield return null;
            Rig resizedRig = CreateRig(new Vector2Int(1600, 900));
            resizedRig.Presenter.ApplySnapshot(
                Snapshot(2, 3, 10, EraClockAnchorTarget.Hud));
            yield return new WaitForSecondsRealtime(0.05f);
            resizedRig.HudAnchor.anchoredPosition = new Vector2(210f, -220f);
            resizedRig.Presenter.RefreshLayout();
            yield return null;
            CaptureAndRecord(
                resizedRig,
                new Vector2Int(1600, 900),
                "dynamic-resize-1600x900.png",
                "dynamic-resize",
                index);
            AssertVisibleAndWithinViewport(resizedRig, new Vector2Int(1600, 900));
            Assert.That(
                Vector3.Distance(resizedRig.ClockRoot.position, resizedRig.HudAnchor.position),
                Is.LessThan(1f));

            File.WriteAllText(
                Path.Combine(EvidenceDirectoryPath, "visual-evidence-index.json"),
                JsonUtility.ToJson(index, true));
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator CapturesRolloverTimelineWithResetPulse()
        {
            var timeline = new AnimationTimelineEvidence
            {
                unityVersion = UnityEngine.Application.unityVersion,
                renderer = SystemInfo.graphicsDeviceType.ToString(),
                viewport = "1920x1080",
                fromEra = 7,
                fromPhase = 8,
                fromSequence = 20,
                toEra = 8,
                toPhase = 1,
                toSequence = 21,
                transitionKind = EraClockPhaseTransition.Rollover.ToString(),
                owner = nameof(EraClockPresenter),
                fromAnchor = EraClockAnchorTarget.Center.ToString(),
                toAnchor = EraClockAnchorTarget.Center.ToString(),
                configuredDurationSeconds = 2f,
                completionReason = "Pending",
                cancellationReason = "None",
                inputLocked = false,
                events = new List<AnimationTimelineEvent>()
            };
            Screen.SetResolution(1920, 1080, false);
            yield return null;
            yield return null;
            Rig rig = CreateRig(new Vector2Int(1920, 1080), rolloverDuration: 2f);
            rig.Presenter.ApplySnapshot(Snapshot(7, 8, 20));
            yield return null;
            CaptureFrame(rig, "rollover-initial-1920x1080.png");
            timeline.events.Add(Event("initial", 0f, rig));

            float start = Time.realtimeSinceStartup;
            rig.Presenter.ApplySnapshot(Snapshot(8, 1, 21));
            timeline.events.Add(Event("accepted-rollover", 0f, rig));
            float pulseDeadline = Time.realtimeSinceStartup + 3f;
            while (rig.Pulse.alpha < 0.85f &&
                rig.Presenter.State == EraClockPresenterState.RolloverTransition &&
                Time.realtimeSinceStartup < pulseDeadline)
            {
                yield return null;
            }

            CaptureFrame(rig, "rollover-middle-1920x1080.png");
            timeline.events.Add(Event("reset-pulse", Time.realtimeSinceStartup - start, rig));
            Assert.That(rig.Pulse.alpha, Is.GreaterThan(0f));
            float terminalDeadline = Time.realtimeSinceStartup + 3f;
            while (rig.Presenter.State != EraClockPresenterState.Settled &&
                Time.realtimeSinceStartup < terminalDeadline)
            {
                yield return null;
            }

            CaptureFrame(rig, "rollover-complete-1920x1080.png");
            timeline.events.Add(Event("terminal-phase-one", Time.realtimeSinceStartup - start, rig));
            Assert.That(rig.Presenter.State, Is.EqualTo(EraClockPresenterState.Settled));
            Assert.That(rig.Progress.fillAmount, Is.EqualTo(0.125f).Within(0.01f));
            timeline.completionReason = rig.Presenter.LastCompletionReason.ToString();

            File.WriteAllText(
                Path.Combine(EvidenceDirectoryPath, "animation-timeline.json"),
                JsonUtility.ToJson(timeline, true));
        }

        private Rig CreateRig(
            Vector2Int viewport,
            float phaseDuration = 0f,
            float rolloverDuration = 0f,
            float anchorDuration = 0f)
        {
            root = new GameObject("EraClockEvidenceRoot");
            var cameraObject = new GameObject("EraClockEvidenceCamera", typeof(Camera));
            cameraObject.transform.SetParent(root.transform, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.055f, 0.1f, 1f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.cullingMask = 1 << EvidenceLayer;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            var canvasObject = new GameObject(
                "EraClockEvidenceCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);
            evidenceCanvas = canvasObject.transform;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform centerAnchor = CreateAnchor("CenterAnchor", Vector2.zero);
            centerAnchor.anchorMin = centerAnchor.anchorMax = new Vector2(0.5f, 0.5f);
            RectTransform hudAnchor = CreateAnchor("HudAnchor", new Vector2(0f, -110f));
            hudAnchor.anchorMin = hudAnchor.anchorMax = new Vector2(0.5f, 1f);

            var clockObject = new GameObject("EraClock", typeof(RectTransform), typeof(CanvasGroup));
            clockObject.transform.SetParent(evidenceCanvas, false);
            var clockRoot = clockObject.GetComponent<RectTransform>();
            clockRoot.sizeDelta = new Vector2(320f, 320f);
            var rootGroup = clockObject.GetComponent<CanvasGroup>();
            RawImage face = CreateRawImage("ClockFace", clockRoot, "Art/Shell/MainMenu/clock_noring");
            RawImage ring = CreateRawImage("ClockRing", clockRoot, "Art/Shell/MainMenu/ring");
            RawImage pointerImage = CreateRawImage("Pointer", clockRoot, "Art/Shell/MainMenu/point");
            RectTransform pointer = pointerImage.rectTransform;
            var progressObject = new GameObject("EraProgress", typeof(RectTransform), typeof(Image));
            progressObject.transform.SetParent(clockRoot, false);
            var progress = progressObject.GetComponent<Image>();
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Horizontal;
            progress.color = new Color(0.26f, 0.82f, 0.72f, 1f);
            progress.rectTransform.anchorMin = new Vector2(0.18f, 0.02f);
            progress.rectTransform.anchorMax = new Vector2(0.82f, 0.02f);
            progress.rectTransform.sizeDelta = new Vector2(0f, 16f);
            Font silver = Resources.Load<Font>("Fonts/Silver");
            Assert.That(silver, Is.Not.Null);
            Text eraLabel = CreateText("EraLabel", clockRoot, silver, new Vector2(0f, -190f));
            Text phaseLabel = CreateText("PhaseLabel", clockRoot, silver, new Vector2(0f, -228f));
            var pulseObject = new GameObject(
                "RolloverPulse",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(RawImage));
            pulseObject.transform.SetParent(clockRoot, false);
            var pulse = pulseObject.GetComponent<CanvasGroup>();
            var pulseImage = pulseObject.GetComponent<RawImage>();
            pulseImage.texture = Resources.Load<Texture>("Art/Shell/MainMenu/ring");
            pulseImage.color = new Color(1f, 0.72f, 0.24f, 0.8f);
            pulseImage.raycastTarget = false;
            pulseImage.rectTransform.anchorMin = Vector2.zero;
            pulseImage.rectTransform.anchorMax = Vector2.one;
            pulseImage.rectTransform.offsetMin = Vector2.zero;
            pulseImage.rectTransform.offsetMax = Vector2.zero;
            var presenter = clockObject.AddComponent<EraClockPresenter>();
            presenter.Rebind(
                new EraClockViewBindings(
                    clockRoot,
                    face,
                    ring,
                    pointer,
                    pointerImage,
                    progress,
                    eraLabel,
                    phaseLabel,
                    centerAnchor,
                    hudAnchor,
                    rootGroup,
                    pulse),
                new EraClockAnimationSettings(
                    phaseDuration,
                    rolloverDuration,
                    anchorDuration,
                    centerScale: 1f,
                    hudScale: 0.46f));
            SetLayerRecursively(canvasObject, EvidenceLayer);
            return new Rig(
                presenter,
                clockRoot,
                face,
                ring,
                pointer,
                pointerImage,
                progress,
                eraLabel,
                phaseLabel,
                centerAnchor,
                hudAnchor,
                rootGroup,
                pulse,
                camera);
        }

        private RectTransform CreateAnchor(string name, Vector2 position)
        {
            var anchorObject = new GameObject(name, typeof(RectTransform));
            anchorObject.transform.SetParent(evidenceCanvas, false);
            var anchor = anchorObject.GetComponent<RectTransform>();
            anchor.anchoredPosition = position;
            return anchor;
        }

        private static RawImage CreateRawImage(string name, RectTransform parent, string path)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<RawImage>();
            image.texture = Resources.Load<Texture>(path);
            Assert.That(image.texture, Is.Not.Null, path);
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            return image;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static Text CreateText(string name, RectTransform parent, Font font, Vector2 position)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 30;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.82f, 0.42f, 1f);
            text.rectTransform.sizeDelta = new Vector2(380f, 42f);
            text.rectTransform.anchoredPosition = position;
            return text;
        }

        private void CaptureAndRecord(
            Rig rig,
            Vector2Int viewport,
            string fileName,
            string state,
            VisualEvidenceIndex index)
        {
            Texture2D texture = Capture(rig.Camera, viewport.x, viewport.y);
            string path = Path.Combine(EvidenceDirectoryPath, fileName);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            RectInt contentBounds = FindContentBounds(texture);
            index.frames.Add(new VisualEvidenceFrame
            {
                file = fileName,
                state = state,
                viewport = viewport.x + "x" + viewport.y,
                width = texture.width,
                height = texture.height,
                distinctColorCount = DistinctColorCount(texture),
                checksum = Checksum(texture),
                rootBlocksRaycasts = rig.RootGroup.blocksRaycasts,
                withinViewport = IsWithinViewport(rig, viewport),
                contentMinX = contentBounds.xMin,
                contentMinY = contentBounds.yMin,
                contentMaxX = contentBounds.xMax,
                contentMaxY = contentBounds.yMax,
                rasterHasMargin = HasRasterMargin(contentBounds, viewport)
            });
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private void CaptureFrame(Rig rig, string fileName)
        {
            Texture2D texture = Capture(rig.Camera, Screen.width, Screen.height);
            File.WriteAllBytes(Path.Combine(EvidenceDirectoryPath, fileName), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static Texture2D Capture(Camera camera, int width, int height)
        {
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            camera.targetTexture = renderTexture;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = renderTexture;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            texture.Apply(false, false);
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
            return texture;
        }

        private static void AssertVisibleAndWithinViewport(Rig rig, Vector2Int viewport)
        {
            Assert.That(IsWithinViewport(rig, viewport), Is.True);
            Texture2D texture = Capture(rig.Camera, viewport.x, viewport.y);
            Assert.That(DistinctColorCount(texture), Is.GreaterThan(12));
            Assert.That(
                HasRasterMargin(FindContentBounds(texture), viewport),
                Is.True,
                "Rendered Era Clock pixels must not touch a viewport edge.");
            UnityEngine.Object.DestroyImmediate(texture);
            Assert.That(rig.RootGroup.blocksRaycasts, Is.False);
        }

        private static bool IsWithinViewport(Rig rig, Vector2Int viewport)
        {
            return RectWithinViewport(rig.ClockRoot, rig.Camera, viewport) &&
                RectWithinViewport(rig.EraLabel.rectTransform, rig.Camera, viewport) &&
                RectWithinViewport(rig.PhaseLabel.rectTransform, rig.Camera, viewport);
        }

        private static bool RectWithinViewport(
            RectTransform rect,
            Camera camera,
            Vector2Int viewport)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            for (var index = 0; index < corners.Length; index++)
            {
                Vector3 screen = RectTransformUtility.WorldToScreenPoint(camera, corners[index]);
                if (screen.x < 0f || screen.x > viewport.x || screen.y < 0f || screen.y > viewport.y)
                {
                    return false;
                }
            }

            return true;
        }

        private static int DistinctColorCount(Texture2D texture)
        {
            var colors = new HashSet<int>();
            Color32[] pixels = texture.GetPixels32();
            for (var index = 0; index < pixels.Length; index += Mathf.Max(1, pixels.Length / 32768))
            {
                colors.Add(
                    pixels[index].r << 24 |
                    pixels[index].g << 16 |
                    pixels[index].b << 8 |
                    pixels[index].a);
            }

            return colors.Count;
        }

        private static RectInt FindContentBounds(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            Color32 background = pixels[0];
            int minimumX = texture.width;
            int minimumY = texture.height;
            int maximumX = -1;
            int maximumY = -1;
            for (var y = 0; y < texture.height; y++)
            {
                int row = y * texture.width;
                for (var x = 0; x < texture.width; x++)
                {
                    if (pixels[row + x].Equals(background))
                    {
                        continue;
                    }

                    minimumX = Mathf.Min(minimumX, x);
                    minimumY = Mathf.Min(minimumY, y);
                    maximumX = Mathf.Max(maximumX, x);
                    maximumY = Mathf.Max(maximumY, y);
                }
            }

            return maximumX < 0
                ? new RectInt()
                : new RectInt(
                    minimumX,
                    minimumY,
                    maximumX - minimumX + 1,
                    maximumY - minimumY + 1);
        }

        private static bool HasRasterMargin(RectInt bounds, Vector2Int viewport)
        {
            return bounds.width > 0 &&
                bounds.height > 0 &&
                bounds.xMin > 0 &&
                bounds.yMin > 0 &&
                bounds.xMax < viewport.x &&
                bounds.yMax < viewport.y;
        }

        private static ulong Checksum(Texture2D texture)
        {
            unchecked
            {
                ulong hash = 1469598103934665603UL;
                Color32[] pixels = texture.GetPixels32();
                for (var index = 0; index < pixels.Length; index += Mathf.Max(1, pixels.Length / 4096))
                {
                    hash ^= pixels[index].r;
                    hash *= 1099511628211UL;
                    hash ^= pixels[index].g;
                    hash *= 1099511628211UL;
                    hash ^= pixels[index].b;
                    hash *= 1099511628211UL;
                }

                return hash;
            }
        }

        private static EraClockPresentationSnapshot Snapshot(
            int era,
            int phase,
            long sequence,
            EraClockAnchorTarget anchor = EraClockAnchorTarget.Center)
        {
            return new EraClockPresentationSnapshot(era, phase, sequence, anchor);
        }

        private static AnimationTimelineEvent Event(string name, float elapsed, Rig rig)
        {
            return new AnimationTimelineEvent
            {
                timeSeconds = elapsed,
                eventName = name,
                eraLabel = rig.EraLabel.text,
                phaseLabel = rig.PhaseLabel.text,
                progress = rig.Progress.fillAmount,
                pointerAngle = rig.Pointer.localEulerAngles.z,
                pulseAlpha = rig.Pulse.alpha,
                presenterState = rig.Presenter.State.ToString()
            };
        }

        [Serializable]
        private sealed class VisualEvidenceIndex
        {
            public string unityVersion;
            public string renderer;
            public List<VisualEvidenceFrame> frames;
        }

        [Serializable]
        private sealed class VisualEvidenceFrame
        {
            public string file;
            public string state;
            public string viewport;
            public int width;
            public int height;
            public int distinctColorCount;
            public ulong checksum;
            public bool rootBlocksRaycasts;
            public bool withinViewport;
            public int contentMinX;
            public int contentMinY;
            public int contentMaxX;
            public int contentMaxY;
            public bool rasterHasMargin;
        }

        [Serializable]
        private sealed class AnimationTimelineEvidence
        {
            public string unityVersion;
            public string renderer;
            public string viewport;
            public int fromEra;
            public int fromPhase;
            public long fromSequence;
            public int toEra;
            public int toPhase;
            public long toSequence;
            public string transitionKind;
            public string owner;
            public string fromAnchor;
            public string toAnchor;
            public float configuredDurationSeconds;
            public string completionReason;
            public string cancellationReason;
            public bool inputLocked;
            public List<AnimationTimelineEvent> events;
        }

        [Serializable]
        private sealed class AnimationTimelineEvent
        {
            public float timeSeconds;
            public string eventName;
            public string eraLabel;
            public string phaseLabel;
            public float progress;
            public float pointerAngle;
            public float pulseAlpha;
            public string presenterState;
        }

        private sealed class Rig
        {
            public Rig(
                EraClockPresenter presenter,
                RectTransform clockRoot,
                RawImage face,
                RawImage ring,
                RectTransform pointer,
                RawImage pointerImage,
                Image progress,
                Text eraLabel,
                Text phaseLabel,
                RectTransform centerAnchor,
                RectTransform hudAnchor,
                CanvasGroup rootGroup,
                CanvasGroup pulse,
                Camera camera)
            {
                Presenter = presenter;
                ClockRoot = clockRoot;
                Face = face;
                Ring = ring;
                Pointer = pointer;
                PointerImage = pointerImage;
                Progress = progress;
                EraLabel = eraLabel;
                PhaseLabel = phaseLabel;
                CenterAnchor = centerAnchor;
                HudAnchor = hudAnchor;
                RootGroup = rootGroup;
                Pulse = pulse;
                Camera = camera;
            }

            public EraClockPresenter Presenter { get; }
            public RectTransform ClockRoot { get; }
            public RawImage Face { get; }
            public RawImage Ring { get; }
            public RectTransform Pointer { get; }
            public RawImage PointerImage { get; }
            public Image Progress { get; }
            public Text EraLabel { get; }
            public Text PhaseLabel { get; }
            public RectTransform CenterAnchor { get; }
            public RectTransform HudAnchor { get; }
            public CanvasGroup RootGroup { get; }
            public CanvasGroup Pulse { get; }
            public Camera Camera { get; }
        }
    }
}
