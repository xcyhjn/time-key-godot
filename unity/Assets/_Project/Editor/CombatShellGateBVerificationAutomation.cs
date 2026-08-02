using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TimeKey.Composition;
using TimeKey.Presentation;
using TimeKey.Presentation.CombatShell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Editor
{
    public static class CombatShellGateBVerificationAutomation
    {
        [MenuItem("Time Key/Capture Combat Shell Gate B")]
        public static void CaptureGateB()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var scene = EditorSceneManager.OpenScene(
                CombatShellGateBAutomation.CombatScenePath,
                OpenSceneMode.Single);
            var root = scene.GetRootGameObjects()
                .SingleOrDefault(value => value.name == "VerticalSliceRoot");
            if (root == null)
            {
                throw new InvalidOperationException("VerticalSliceRoot is missing.");
            }

            var entrance = root.GetComponent<CombatShellEntrancePresenter>();
            if (entrance != null)
            {
                entrance.enabled = false;
            }

            root.SetActive(true);
            var composition = root.GetComponent<CombatCompositionRoot>();
            var controller = root.GetComponent<VerticalSliceController>();
            if (composition == null || controller == null)
            {
                throw new InvalidOperationException("The combat composition is incomplete.");
            }

            composition.Initialize();
            controller.BuildSceneGraph();
            var topHud = root.transform.Find("SliceCanvas/HUD/CombatTopHUD");
            var background = root.transform.Find("World/CombatShellBackground");
            if (topHud == null || background == null)
            {
                throw new InvalidOperationException("Gate B presentation roots are missing.");
            }

            topHud.GetComponent<CanvasGroup>().alpha = 1f;
            topHud.GetComponent<CombatTopHudPresenter>().Bind();
            background.localScale = Vector3.one;
            controller.CardHandHost.ApplyVisualStateImmediate();

            var directory = GetEvidenceDirectory();
            Directory.CreateDirectory(directory);
            var captures = new List<CaptureStats>();

            controller.SetBoardView(0f);
            captures.Add(Capture(controller, directory, "combat-final-1280x720.png", 1280, 720));
            captures.Add(Capture(controller, directory, "combat-final-1920x1080.png", 1920, 1080));
            captures.Add(Capture(controller, directory, "combat-final-2560x1080.png", 2560, 1080));

            foreach (var yaw in new[] { 0f, 90f, 180f, 270f })
            {
                controller.SetBoardView(yaw);
                captures.Add(Capture(
                    controller,
                    directory,
                    string.Format(CultureInfo.InvariantCulture, "background-yaw-{0:000}.png", yaw),
                    1920,
                    1080));
            }

            controller.SetBoardView(0f, BoardOrbitCameraController.MinimumPitch, BoardOrbitCameraController.MinimumDistance);
            captures.Add(Capture(controller, directory, "background-pitch-min-zoom-min.png", 1920, 1080));
            controller.SetBoardView(0f, BoardOrbitCameraController.MaximumPitch, BoardOrbitCameraController.MaximumDistance);
            captures.Add(Capture(controller, directory, "background-pitch-max-zoom-max.png", 1920, 1080));

            controller.SetBoardView(0f);
            var coexistCard = controller.CardHandHost.Cards
                .FirstOrDefault(card => card.StableId == "poison");
            if (coexistCard == null || !controller.SelectCard(coexistCard.ViewId) ||
                !controller.SelectTarget(VerticalSliceController.TargetId) ||
                !controller.PreviewTimelineSelected(0, 0))
            {
                throw new InvalidOperationException(
                    "The Gate B coexistence interaction could not be arranged.");
            }

            controller.CardHandHost.ApplyVisualStateImmediate();
            captures.Add(Capture(controller, directory, "interaction-coexist-1280x720.png", 1280, 720));
            captures.Add(Capture(controller, directory, "interaction-coexist-1920x1080.png", 1920, 1080));

            var pause = topHud.Find("TopBar/Commands/PauseButton")?.GetComponent<Button>();
            if (pause == null)
            {
                throw new InvalidOperationException("The saved pause button is missing.");
            }

            pause.onClick.Invoke();
            var modal = topHud.Find("Modal")?.GetComponent<CanvasGroup>();
            if (modal == null || modal.alpha < 0.99f || !modal.blocksRaycasts)
            {
                throw new InvalidOperationException("The pause modal did not become interactive.");
            }

            captures.Add(Capture(controller, directory, "pause-modal-1280x720.png", 1280, 720));
            WriteSummary(directory, captures);
            Debug.Log("TIMEKEY_COMBAT_SHELL_GATE_B_CAPTURE_PASS");
        }

        private static CaptureStats Capture(
            VerticalSliceController controller,
            string directory,
            string fileName,
            int width,
            int height)
        {
            var camera = controller.SceneCamera;
            var renderTexture = RenderTexture.GetTemporary(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                texture.Apply();

                var luminanceRange = MeasureLuminanceRange(texture, width, height);
                if (luminanceRange < 0.1f)
                {
                    throw new InvalidOperationException(fileName + " appears blank or single-tone.");
                }

                File.WriteAllBytes(Path.Combine(directory, fileName), texture.EncodeToPNG());
                return new CaptureStats(fileName, width, height, luminanceRange);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture);
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static float MeasureLuminanceRange(Texture2D texture, int width, int height)
        {
            var minimum = 1f;
            var maximum = 0f;
            const int stride = 16;
            for (var y = stride / 2; y < height; y += stride)
            {
                for (var x = stride / 2; x < width; x += stride)
                {
                    var color = texture.GetPixel(x, y);
                    var luminance = (0.2126f * color.r) + (0.7152f * color.g) + (0.0722f * color.b);
                    minimum = Mathf.Min(minimum, luminance);
                    maximum = Mathf.Max(maximum, luminance);
                }
            }

            return maximum - minimum;
        }

        private static string GetEvidenceDirectory()
        {
            var repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                repositoryRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ".."));
            }

            return Path.Combine(
                repositoryRoot,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "combat-shell-gate-b");
        }

        private static void WriteSummary(string directory, IReadOnlyList<CaptureStats> captures)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"status\": \"captured\",");
            builder.AppendLine("  \"manualReviewRequired\": true,");
            builder.AppendLine("  \"font\": \"Silver\",");
            builder.AppendLine("  \"responsiveViewports\": [\"1280x720\", \"1920x1080\", \"2560x1080\"],");
            builder.AppendLine("  \"cameraYawEvidence\": [0, 90, 180, 270],");
            builder.AppendLine("  \"cameraBounds\": [\"minimum-pitch/minimum-zoom\", \"maximum-pitch/maximum-zoom\"],");
            builder.AppendLine("  \"animationEvidence\": \"entrance-frame-summary.json\",");
            builder.AppendLine("  \"pauseModalBlocksRaycasts\": true,");
            builder.AppendLine("  \"screenshots\": [");
            for (var index = 0; index < captures.Count; index++)
            {
                var capture = captures[index];
                builder.Append("    {\"file\": \"");
                builder.Append(capture.FileName);
                builder.Append("\", \"width\": ");
                builder.Append(capture.Width);
                builder.Append(", \"height\": ");
                builder.Append(capture.Height);
                builder.Append(", \"luminanceRange\": ");
                builder.Append(capture.LuminanceRange.ToString("0.000", CultureInfo.InvariantCulture));
                builder.AppendLine(index < captures.Count - 1 ? "}," : "}");
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            File.WriteAllText(Path.Combine(directory, "visual-summary.json"), builder.ToString());
        }

        private readonly struct CaptureStats
        {
            public CaptureStats(string fileName, int width, int height, float luminanceRange)
            {
                FileName = fileName;
                Width = width;
                Height = height;
                LuminanceRange = luminanceRange;
            }

            public string FileName { get; }

            public int Width { get; }

            public int Height { get; }

            public float LuminanceRange { get; }
        }
    }
}
