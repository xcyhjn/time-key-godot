using System;
using System.IO;
using System.Security.Cryptography;
using TimeKey.Presentation.EraClock;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeKey.Editor.EraClock
{
    public static class EraClockEvidenceAutomation
    {
        private const int EvidenceLayer = 31;
        private const string EvidenceScenePath =
            "Assets/_Project/Editor/EraClock/EraClockPlayerEvidence.unity";
        private const string FacePath =
            "Assets/_Project/Resources/Art/Shell/MainMenu/clock_noring.png";
        private const string RingPath =
            "Assets/_Project/Resources/Art/Shell/MainMenu/ring.png";
        private const string PointerPath =
            "Assets/_Project/Resources/Art/Shell/MainMenu/point.png";
        private const string SilverPath =
            "Assets/_Project/Resources/Fonts/Silver.ttf";

        [MenuItem("TimeKey/Era Clock/Build Player Evidence")]
        public static void BuildPlayerEvidence()
        {
            string repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                throw new InvalidOperationException("TIMEKEY_REPOSITORY_ROOT is required.");
            }

            string evidenceDirectory = Path.Combine(
                repositoryRoot,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "era-clock-animation-gate-player");
            Directory.CreateDirectory(evidenceDirectory);
            AuthorEvidenceScene();

            string buildDirectory = Path.Combine(
                Path.GetTempPath(),
                "TimeKeyEraClockEvidence",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(buildDirectory);
            string executablePath = Path.Combine(buildDirectory, "TimeKeyEraClockEvidence.exe");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { EvidenceScenePath },
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });

            var summary = new PlayerBuildSummary
            {
                result = report.summary.result.ToString(),
                unityVersion = UnityEngine.Application.unityVersion,
                target = report.summary.platform.ToString(),
                developmentBuild = true,
                scene = EvidenceScenePath,
                totalBytes = report.summary.totalSize,
                durationMilliseconds = report.summary.totalTime.TotalMilliseconds,
                executablePath = executablePath,
                executableSha256 = File.Exists(executablePath)
                    ? Sha256(executablePath)
                    : string.Empty,
                buildSettingsUnchanged = true,
                isolatedEvidenceScene = true
            };
            File.WriteAllText(
                Path.Combine(evidenceDirectory, "build-summary.json"),
                JsonUtility.ToJson(summary, true));
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "EraClock evidence Player build failed: " + report.summary.result);
            }
        }

        private static void AuthorEvidenceScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var sceneRoot = new GameObject("EraClockPlayerEvidence");
            var cameraObject = new GameObject("EvidenceCamera", typeof(Camera));
            cameraObject.transform.SetParent(sceneRoot.transform, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.055f, 0.1f, 1f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.cullingMask = 1 << EvidenceLayer;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var canvasObject = new GameObject(
                "EvidenceCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(sceneRoot.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform centerAnchor = Anchor(
                "CenterAnchor",
                canvasObject.transform,
                new Vector2(0.5f, 0.5f),
                Vector2.zero);
            RectTransform hudAnchor = Anchor(
                "HudAnchor",
                canvasObject.transform,
                new Vector2(0.5f, 1f),
                new Vector2(210f, -220f));
            var clockObject = new GameObject("EraClock", typeof(RectTransform), typeof(CanvasGroup));
            clockObject.transform.SetParent(canvasObject.transform, false);
            var clockRoot = clockObject.GetComponent<RectTransform>();
            clockRoot.sizeDelta = new Vector2(320f, 320f);
            var rootGroup = clockObject.GetComponent<CanvasGroup>();
            RawImage face = Raw("ClockFace", clockRoot, FacePath);
            RawImage ring = Raw("ClockRing", clockRoot, RingPath);
            RawImage pointer = Raw("Pointer", clockRoot, PointerPath);
            var progressObject = new GameObject("EraProgress", typeof(RectTransform), typeof(Image));
            progressObject.transform.SetParent(clockRoot, false);
            var progress = progressObject.GetComponent<Image>();
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Horizontal;
            progress.color = new Color(0.26f, 0.82f, 0.72f, 1f);
            progress.rectTransform.anchorMin = new Vector2(0.18f, 0.02f);
            progress.rectTransform.anchorMax = new Vector2(0.82f, 0.02f);
            progress.rectTransform.sizeDelta = new Vector2(0f, 16f);
            Font silver = LoadRequired<Font>(SilverPath);
            Text eraLabel = Label("EraLabel", clockRoot, silver, new Vector2(0f, -190f));
            Text phaseLabel = Label("PhaseLabel", clockRoot, silver, new Vector2(0f, -228f));
            var pulseObject = new GameObject(
                "RolloverPulse",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(RawImage));
            pulseObject.transform.SetParent(clockRoot, false);
            var pulse = pulseObject.GetComponent<CanvasGroup>();
            var pulseImage = pulseObject.GetComponent<RawImage>();
            pulseImage.texture = LoadRequired<Texture>(RingPath);
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
                    pointer.rectTransform,
                    pointer,
                    progress,
                    eraLabel,
                    phaseLabel,
                    centerAnchor,
                    hudAnchor,
                    rootGroup,
                    pulse),
                new EraClockAnimationSettings(
                    phaseDuration: 0.24f,
                    rolloverDuration: 2f,
                    anchorDuration: 0.65f,
                    centerScale: 1f,
                    hudScale: 0.46f));
            var driver = sceneRoot.AddComponent<EraClockPlayerEvidenceDriver>();
            driver.Configure(presenter, camera, rootGroup, pulse);
            SetLayerRecursively(canvasObject, EvidenceLayer);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, EvidenceScenePath))
            {
                throw new InvalidOperationException("Failed to save EraClock evidence scene.");
            }
        }

        private static RectTransform Anchor(
            string name,
            Transform parent,
            Vector2 anchor,
            Vector2 position)
        {
            var target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            var rect = target.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            return rect;
        }

        private static RawImage Raw(string name, RectTransform parent, string path)
        {
            var target = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            target.transform.SetParent(parent, false);
            var image = target.GetComponent<RawImage>();
            image.texture = LoadRequired<Texture>(path);
            image.raycastTarget = false;
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            return image;
        }

        private static Text Label(
            string name,
            RectTransform parent,
            Font font,
            Vector2 position)
        {
            var target = new GameObject(name, typeof(RectTransform), typeof(Text));
            target.transform.SetParent(parent, false);
            var label = target.GetComponent<Text>();
            label.font = font;
            label.fontSize = 30;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 0.82f, 0.42f, 1f);
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = new Vector2(380f, 42f);
            label.rectTransform.anchoredPosition = position;
            return label;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Required EraClock asset is missing: " + path);
            }

            return asset;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var algorithm = SHA256.Create())
            {
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        [Serializable]
        private sealed class PlayerBuildSummary
        {
            public string result;
            public string unityVersion;
            public string target;
            public bool developmentBuild;
            public string scene;
            public ulong totalBytes;
            public double durationMilliseconds;
            public string executablePath;
            public string executableSha256;
            public bool buildSettingsUnchanged;
            public bool isolatedEvidenceScene;
        }
    }
}
