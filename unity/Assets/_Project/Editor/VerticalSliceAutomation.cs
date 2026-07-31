using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;
using TimeKey.Presentation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeKey.Editor
{
    public static class VerticalSliceAutomation
    {
        private const string ScenePath = "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity";

        [MenuItem("Time Key/Build, Validate and Capture Combat Board")]
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
            var captures = new List<CaptureStats>();

            controller.SetBoardView(32f);
            captures.Add(Capture(controller, evidenceDirectory, "card-idle-1920x1080.png", 1920, 1080));

            var hoverEvent = new PointerEventData(EventSystem.current);
            controller.CardHand.OnPointerEnter(hoverEvent);
            controller.CardHand.ApplyVisualStateImmediate();
            captures.Add(Capture(controller, evidenceDirectory, "card-hover-1920x1080.png", 1920, 1080));
            controller.CardHand.OnPointerExit(new PointerEventData(EventSystem.current));
            controller.CardHand.ApplyVisualStateImmediate();

            if (!controller.SelectCard(VerticalSliceController.LightingCardId))
            {
                throw new InvalidOperationException("The original LIGHTING card could not be selected.");
            }

            controller.CardHand.ApplyVisualStateImmediate();
            captures.Add(Capture(controller, evidenceDirectory, "card-selected-1920x1080.png", 1920, 1080));
            captures.Add(Capture(controller, evidenceDirectory, "card-selected-2560x1080.png", 2560, 1080));

            if (!controller.SelectTarget(VerticalSliceController.TargetId))
            {
                throw new InvalidOperationException("The frozen target could not be selected.");
            }

            foreach (var yaw in new[] { 0, 90, 180, 270 })
            {
                controller.SetBoardView(yaw);
                captures.Add(Capture(
                    controller,
                    evidenceDirectory,
                    string.Format("target-range-yaw-{0:000}-1920x1080.png", yaw),
                    1920,
                    1080));
            }

            controller.SetBoardView(32f);
            if (controller.PreviewTimelineSelected(2, 1))
            {
                throw new InvalidOperationException("The occupied enemy-intent cell was reported as legal.");
            }

            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "timeline-invalid-1920x1080.png",
                1920,
                1080));

            if (!controller.PreviewTimelineSelected(0, 0))
            {
                throw new InvalidOperationException("The frozen legal timeline cell was reported as invalid.");
            }

            captures.Add(Capture(
                controller,
                evidenceDirectory,
                "timeline-valid-1920x1080.png",
                1920,
                1080));

            if (!controller.TryPlaceSelected(0, 0))
            {
                throw new InvalidOperationException("The frozen interaction path could not be arranged.");
            }

            captures.Add(Capture(controller, evidenceDirectory, "placed-1280x720.png", 1280, 720));

            var snapshot = controller.ResolveTimeline();
            if (snapshot.TargetHpBefore != 10 || snapshot.TargetHpAfter != 0 || !snapshot.EnemyIntentResolved)
            {
                throw new InvalidOperationException("The frozen resolution snapshot does not match the contract.");
            }

            controller.SetBoardView(32f);
            captures.Add(Capture(controller, evidenceDirectory, "resolved-1920x1080.png", 1920, 1080));

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

            WriteSummary(evidenceDirectory, captures, report);
            Debug.Log("TIMEKEY_COMBAT_BOARD_HARNESS_PASS");
        }

        private static void ValidateScene(VerticalSliceController controller)
        {
            if (controller.SceneCamera == null || controller.SceneCamera.orthographic)
            {
                throw new InvalidOperationException("A perspective combat-board camera is required.");
            }

            if (controller.BoardCamera == null)
            {
                throw new InvalidOperationException("The orbit camera controller is missing.");
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

            if (controller.BoardTileCount != 19 || UnityEngine.Object.FindObjectsByType<MeshFilter>().Length < 19)
            {
                throw new InvalidOperationException("The scene must contain the frozen 19-hex combat board.");
            }

            if (GameObject.Find("OriginalArt-center_altar") == null)
            {
                throw new InvalidOperationException("The original center altar art is missing.");
            }

            var scaler = controller.SceneCanvas.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.referenceResolution != new Vector2(1920f, 1080f))
            {
                throw new InvalidOperationException("The CanvasScaler reference resolution is not frozen.");
            }

            if (controller.CardHand == null || controller.CardHand.Artwork == null)
            {
                throw new InvalidOperationException("The original card hand view is missing.");
            }

            if (controller.BoardRangePreview == null || controller.BoardRangePreview.RegisteredCount != 19)
            {
                throw new InvalidOperationException("The board range preview is not registered to all 19 tiles.");
            }

            if (controller.TimelinePreview == null || controller.TimelinePreview.RegisteredCount != 36)
            {
                throw new InvalidOperationException("The timeline preview is not registered to all 36 cells.");
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
                return new CaptureStats(
                    fileName,
                    stats.Width,
                    stats.Height,
                    stats.LuminanceRange,
                    stats.SampledOpaquePixels);
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

            return new CaptureStats(string.Empty, width, height, maximum - minimum, opaque);
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
                "unity-slice-02b1");
        }

        private static void WriteSummary(
            string directory,
            IReadOnlyList<CaptureStats> captures,
            BuildReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"passed\",");
            builder.AppendLine("  \"scene\": \"CombatCardInteractionSlice\",");
            builder.AppendLine("  \"seed\": 731,");
            builder.AppendLine("  \"boardTiles\": 19,");
            builder.AppendLine("  \"cameraYawEvidence\": [0, 90, 180, 270],");
            builder.AppendLine("  \"cardArt\": \"lighting\",");
            builder.AppendLine("  \"cardInteraction\": \"hover-select-target-preview-commit\",");
            builder.AppendLine("  \"targetHpAfter\": 0,");
            builder.AppendLine("  \"enemyIntentResolved\": true,");
            builder.AppendLine("  \"screenshots\": [");
            for (var index = 0; index < captures.Count; index++)
            {
                AppendCapture(builder, captures[index], index < captures.Count - 1);
            }
            builder.AppendLine("  ],");
            builder.AppendLine("  \"buildResult\": \"" + report.summary.result + "\",");
            builder.AppendLine("  \"buildBytes\": " + report.summary.totalSize);
            builder.AppendLine("}");
            File.WriteAllText(Path.Combine(directory, "harness-summary.json"), builder.ToString());
        }

        private static void AppendCapture(StringBuilder builder, CaptureStats stats, bool trailingComma)
        {
            builder.Append("    {\"file\": \"");
            builder.Append(stats.FileName);
            builder.Append("\", \"width\": ");
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
            public CaptureStats(string fileName, int width, int height, float luminanceRange, int sampledOpaquePixels)
            {
                FileName = fileName;
                Width = width;
                Height = height;
                LuminanceRange = luminanceRange;
                SampledOpaquePixels = sampledOpaquePixels;
            }

            public string FileName { get; }

            public int Width { get; }

            public int Height { get; }

            public float LuminanceRange { get; }

            public int SampledOpaquePixels { get; }
        }
    }
}
