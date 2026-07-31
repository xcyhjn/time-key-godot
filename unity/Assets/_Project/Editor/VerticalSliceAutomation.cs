using System;
using System.Globalization;
using System.IO;
using System.Text;
using TimeKey.Presentation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeKey.Editor
{
    public static class VerticalSliceAutomation
    {
        private const string ScenePath = "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity";

        [MenuItem("Time Key/Build, Validate and Capture Slice 01")]
        public static void BuildValidateAndCapture()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException("The vertical slice scene could not be opened.");
            }

            var root = GameObject.Find("VerticalSliceRoot");
            if (root == null)
            {
                throw new InvalidOperationException("VerticalSliceRoot is missing.");
            }

            var controller = root.GetComponent<VerticalSliceController>();
            if (controller == null)
            {
                throw new InvalidOperationException("VerticalSliceController is missing.");
            }

            controller.BuildSceneGraph();
            ValidateScene(controller);

            var evidenceDirectory = GetEvidenceDirectory();
            Directory.CreateDirectory(evidenceDirectory);
            var initial = Capture(controller, evidenceDirectory, "initial-1920x1080.png", 1920, 1080);

            if (!controller.SelectCard(VerticalSliceController.LightingCardId) ||
                !controller.SelectTarget(VerticalSliceController.TargetId) ||
                !controller.TryPlaceSelected(0, 0))
            {
                throw new InvalidOperationException("The frozen interaction path could not be arranged.");
            }

            var snapshot = controller.ResolveTimeline();
            if (snapshot.TargetHpBefore != 10 || snapshot.TargetHpAfter != 0 || !snapshot.EnemyIntentResolved)
            {
                throw new InvalidOperationException("The frozen resolution snapshot does not match the contract.");
            }

            var resolvedLarge = Capture(controller, evidenceDirectory, "resolved-1920x1080.png", 1920, 1080);
            var resolvedSmall = Capture(controller, evidenceDirectory, "resolved-1280x720.png", 1280, 720);

            var buildDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Windows"));
            Directory.CreateDirectory(buildDirectory);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(buildDirectory, "TimeKeySlice.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Windows Player build failed: " + report.summary.result);
            }

            WriteSummary(evidenceDirectory, initial, resolvedLarge, resolvedSmall, report);
            Debug.Log("TIMEKEY_SLICE_01_HARNESS_PASS");
        }

        private static void ValidateScene(VerticalSliceController controller)
        {
            if (controller.SceneCamera == null || !controller.SceneCamera.orthographic)
            {
                throw new InvalidOperationException("An orthographic slice camera is required.");
            }

            if (controller.SceneCanvas == null ||
                controller.SceneCanvas.renderMode != RenderMode.ScreenSpaceCamera)
            {
                throw new InvalidOperationException("A screen-space camera Canvas is required.");
            }

            if (controller.TimelineSlotCount != 36)
            {
                throw new InvalidOperationException("The timeline must contain exactly 36 slots.");
            }

            if (UnityEngine.Object.FindObjectsByType<MeshFilter>().Length < 9)
            {
                throw new InvalidOperationException("The scene must contain at least nine hex meshes.");
            }

            var scaler = controller.SceneCanvas.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.referenceResolution != new Vector2(1920f, 1080f))
            {
                throw new InvalidOperationException("The CanvasScaler reference resolution is not frozen.");
            }
        }

        private static CaptureStats Capture(
            VerticalSliceController controller,
            string directory,
            string fileName,
            int width,
            int height)
        {
            var camera = controller.SceneCamera;
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();

                var stats = Measure(texture, width, height);
                if (stats.LuminanceRange < 0.10f || stats.SampledOpaquePixels == 0)
                {
                    throw new InvalidOperationException(fileName + " appears blank or single-tone.");
                }

                File.WriteAllBytes(Path.Combine(directory, fileName), texture.EncodeToPNG());
                return stats;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture);
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static CaptureStats Measure(Texture2D texture, int width, int height)
        {
            var minimum = 1f;
            var maximum = 0f;
            var opaque = 0;
            const int stride = 16;

            for (var y = stride / 2; y < height; y += stride)
            {
                for (var x = stride / 2; x < width; x += stride)
                {
                    var color = texture.GetPixel(x, y);
                    var luminance = (0.2126f * color.r) + (0.7152f * color.g) + (0.0722f * color.b);
                    minimum = Mathf.Min(minimum, luminance);
                    maximum = Mathf.Max(maximum, luminance);
                    if (color.a > 0.5f)
                    {
                        opaque++;
                    }
                }
            }

            return new CaptureStats(width, height, maximum - minimum, opaque);
        }

        private static string GetEvidenceDirectory()
        {
            var repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            }

            if (!Directory.Exists(Path.Combine(repositoryRoot, "docs", "migration", "unity-3d")))
            {
                throw new DirectoryNotFoundException("TIMEKEY_REPOSITORY_ROOT does not contain migration docs.");
            }

            return Path.Combine(
                repositoryRoot,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "unity-slice-01");
        }

        private static void WriteSummary(
            string directory,
            CaptureStats initial,
            CaptureStats resolvedLarge,
            CaptureStats resolvedSmall,
            BuildReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"passed\",");
            builder.AppendLine("  \"scene\": \"CombatVerticalSlice\",");
            builder.AppendLine("  \"seed\": 731,");
            builder.AppendLine("  \"targetHpAfter\": 0,");
            builder.AppendLine("  \"enemyIntentResolved\": true,");
            builder.AppendLine("  \"screenshots\": [");
            AppendCapture(builder, initial, true);
            AppendCapture(builder, resolvedLarge, true);
            AppendCapture(builder, resolvedSmall, false);
            builder.AppendLine("  ],");
            builder.AppendLine("  \"buildResult\": \"" + report.summary.result + "\",");
            builder.AppendLine("  \"buildBytes\": " + report.summary.totalSize);
            builder.AppendLine("}");
            File.WriteAllText(Path.Combine(directory, "harness-summary.json"), builder.ToString());
        }

        private static void AppendCapture(StringBuilder builder, CaptureStats stats, bool trailingComma)
        {
            builder.Append("    {\"width\": ");
            builder.Append(stats.Width);
            builder.Append(", \"height\": ");
            builder.Append(stats.Height);
            builder.Append(", \"luminanceRange\": ");
            builder.Append(stats.LuminanceRange.ToString("0.000", CultureInfo.InvariantCulture));
            builder.Append(", \"sampledOpaquePixels\": ");
            builder.Append(stats.SampledOpaquePixels);
            builder.AppendLine(trailingComma ? "}," : "}");
        }

        private readonly struct CaptureStats
        {
            public CaptureStats(int width, int height, float luminanceRange, int sampledOpaquePixels)
            {
                Width = width;
                Height = height;
                LuminanceRange = luminanceRange;
                SampledOpaquePixels = sampledOpaquePixels;
            }

            public int Width { get; }

            public int Height { get; }

            public float LuminanceRange { get; }

            public int SampledOpaquePixels { get; }
        }
    }
}
