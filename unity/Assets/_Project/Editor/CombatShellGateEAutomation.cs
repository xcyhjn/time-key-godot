using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TimeKey.Editor
{
    public static class CombatShellGateEAutomation
    {
        private static readonly string[] ExpectedScenes =
        {
            "Assets/_Project/Scenes/Shell/Bootstrap.unity",
            "Assets/_Project/Scenes/Shell/GameStart.unity",
            "Assets/_Project/Scenes/Shell/MainMenu.unity",
            "Assets/_Project/Scenes/Shell/OutOfBattleShell.unity",
            "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity",
            "Assets/_Project/Scenes/Shell/GameOver.unity"
        };

        [MenuItem("Time Key/Build Combat Shell Gate E")]
        public static void BuildGateE()
        {
            var repositoryRoot = Environment.GetEnvironmentVariable(
                "TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                throw new InvalidOperationException(
                    "TIMEKEY_REPOSITORY_ROOT is required for Gate E build evidence.");
            }

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (!scenes.SequenceEqual(ExpectedScenes, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Gate E Build Settings must contain the frozen six-scene order.");
            }

            var outputDirectory = Path.Combine(
                repositoryRoot,
                "unity",
                "Builds",
                "CombatShellGateE");
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, "TimeKey.exe");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Combat Shell Gate E build failed: " + report.summary.result + ".");
            }

            var attributionSource = Path.Combine(
                UnityEngine.Application.dataPath,
                "_Project",
                "Resources",
                "Fonts",
                "Silver-ATTRIBUTION.txt");
            var attributionDestination = Path.Combine(
                outputDirectory,
                "Silver-ATTRIBUTION.txt");
            File.Copy(attributionSource, attributionDestination, true);

            var evidenceDirectory = Path.Combine(
                repositoryRoot,
                "docs",
                "migration",
                "unity-3d",
                "04-verification",
                "evidence",
                "combat-shell-gate-e");
            Directory.CreateDirectory(evidenceDirectory);
            var summary = new GateEBuildSummary
            {
                result = report.summary.result.ToString(),
                unityVersion = UnityEngine.Application.unityVersion,
                target = report.summary.platform.ToString(),
                developmentBuild = true,
                sceneCount = scenes.Length,
                scenes = new List<string>(scenes),
                totalBytes = report.summary.totalSize,
                durationMilliseconds = report.summary.totalTime.TotalMilliseconds,
                executablePath = outputPath,
                executableSha256 = Sha256(outputPath),
                silverAttributionPath = attributionDestination,
                silverAttributionPresent = File.Exists(attributionDestination)
            };
            File.WriteAllText(
                Path.Combine(evidenceDirectory, "build-summary.json"),
                JsonUtility.ToJson(summary, true));
            Debug.Log(
                "TIMEKEY_COMBAT_SHELL_GATE_E_BUILD_PASS" +
                " scenes=" + scenes.Length +
                " bytes=" + report.summary.totalSize +
                " sha256=" + summary.executableSha256);
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var algorithm = SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(stream))
                .Replace("-", string.Empty);
        }

        [Serializable]
        private sealed class GateEBuildSummary
        {
            public string result;
            public string unityVersion;
            public string target;
            public bool developmentBuild;
            public int sceneCount;
            public List<string> scenes;
            public ulong totalBytes;
            public double durationMilliseconds;
            public string executablePath;
            public string executableSha256;
            public string silverAttributionPath;
            public bool silverAttributionPresent;
        }
    }
}
