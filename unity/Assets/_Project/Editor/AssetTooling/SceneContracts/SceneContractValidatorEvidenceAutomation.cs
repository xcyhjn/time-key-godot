using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TimeKey.Editor.AssetTooling.SceneContracts
{
    public static class SceneContractValidatorEvidenceAutomation
    {
        private static readonly Vector2Int[] WindowSizes =
        {
            new Vector2Int(640, 420),
            new Vector2Int(960, 640),
            new Vector2Int(1440, 900)
        };

        private static SceneContractValidatorWindow _window;
        private static SceneContractReport _report;
        private static string _evidenceDirectory;
        private static int _sizeIndex;
        private static int _repaintFrames;

        public static void Run()
        {
            try
            {
                RunCore();
            }
            catch (Exception exception)
            {
                FailAndExit(exception);
            }
        }

        private static void RunCore()
        {
            var repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                Debug.LogError("TIMEKEY_REPOSITORY_ROOT is required for evidence capture.");
                EditorApplication.Exit(2);
                return;
            }

            _evidenceDirectory = Path.Combine(
                repositoryRoot,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "pluginization-and-asset-tooling");
            Directory.CreateDirectory(_evidenceDirectory);
            _report = SceneContractValidator.ValidateCombatScene();
            if (!_report.IsValid)
            {
                File.WriteAllText(
                    Path.Combine(_evidenceDirectory, "scene-contract-report.json"),
                    _report.ToJson());
                Debug.LogError("Scene contract validation failed before visual capture.");
                EditorApplication.Exit(3);
                return;
            }

            _window = SceneContractValidatorWindow.OpenWindow();
            _window.DisplayReport(_report);
            _window.Focus();
            _sizeIndex = 0;
            ApplyWindowSize();
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                TickCore();
            }
            catch (Exception exception)
            {
                FailAndExit(exception);
            }
        }

        private static void TickCore()
        {
            _window.Repaint();
            _repaintFrames++;
            if (_repaintFrames < 8)
            {
                return;
            }

            CaptureCurrentWindow();
            _sizeIndex++;
            if (_sizeIndex < WindowSizes.Length)
            {
                ApplyWindowSize();
                return;
            }

            EditorApplication.update -= Tick;
            File.WriteAllText(
                Path.Combine(_evidenceDirectory, "scene-contract-report.json"),
                _report.ToJson());
            File.WriteAllText(
                Path.Combine(_evidenceDirectory, "editor-window-visual-summary.json"),
                BuildVisualSummary());
            CloseWindow();
            Debug.Log("[SceneContractValidatorEvidence] PASS");
            EditorApplication.Exit(0);
        }

        private static void FailAndExit(Exception exception)
        {
            EditorApplication.update -= Tick;
            CloseWindow();
            Debug.LogException(exception);
            EditorApplication.Exit(4);
        }

        private static void CloseWindow()
        {
            if (_window != null)
            {
                _window.Close();
                _window = null;
            }
        }

        private static void ApplyWindowSize()
        {
            var size = WindowSizes[_sizeIndex];
            _window.position = new Rect(32f, 32f, size.x, size.y);
            _window.Focus();
            _window.Repaint();
            _repaintFrames = 0;
        }

        private static void CaptureCurrentWindow()
        {
            var size = WindowSizes[_sizeIndex];
            var rect = _window.position;
            var pixels = InternalEditorUtility.ReadScreenPixel(
                new Vector2(rect.x, rect.y),
                size.x,
                size.y);
            if (pixels == null || pixels.Length != size.x * size.y)
            {
                throw new InvalidOperationException(
                    "Editor window capture returned an unexpected pixel count.");
            }

            var texture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            try
            {
                texture.SetPixels(pixels);
                texture.Apply(false, false);
                var path = Path.Combine(
                    _evidenceDirectory,
                    "scene-contract-validator-" + size.x + "x" + size.y + ".png");
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static string BuildVisualSummary()
        {
            var sizes = new List<string>();
            for (var index = 0; index < WindowSizes.Length; index++)
            {
                sizes.Add("\"" + WindowSizes[index].x + "x" + WindowSizes[index].y + "\"");
            }

            return "{\n" +
                   "  \"status\": \"captured\",\n" +
                   "  \"contractValid\": true,\n" +
                   "  \"errorCount\": 0,\n" +
                   "  \"fontAsset\": \"Assets/_Project/Resources/Fonts/Silver.ttf\",\n" +
                   "  \"windowSizes\": [" + string.Join(", ", sizes) + "]\n" +
                   "}\n";
        }
    }
}
