using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TimeKey.Application.EraClock;
using UnityEngine;

namespace TimeKey.Presentation.EraClock
{
    public sealed class EraClockPlayerEvidenceDriver : MonoBehaviour
    {
        [SerializeField] private EraClockPresenter presenter;
        [SerializeField] private Camera evidenceCamera;
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private CanvasGroup rolloverPulse;

        private int errorCount;

        public void Configure(
            EraClockPresenter eraClockPresenter,
            Camera camera,
            CanvasGroup clockRootGroup,
            CanvasGroup pulse)
        {
            presenter = eraClockPresenter;
            evidenceCamera = camera;
            rootGroup = clockRootGroup;
            rolloverPulse = pulse;
        }

        private void OnEnable()
        {
            UnityEngine.Application.logMessageReceived += OnLogMessage;
        }

        private void Start()
        {
            StartCoroutine(RunEvidence());
        }

        private IEnumerator RunEvidence()
        {
            string directory = EvidenceDirectory();
            var summary = new PlayerEvidenceSummary
            {
                status = "failed",
                unityVersion = UnityEngine.Application.unityVersion,
                renderer = SystemInfo.graphicsDeviceType.ToString(),
                screenshots = new List<string>()
            };

            IEnumerator core = RunEvidenceCore(directory, summary);
            while (true)
            {
                bool hasNext = false;
                object current = null;
                Exception failure = null;
                try
                {
                    hasNext = core.MoveNext();
                    if (hasNext)
                    {
                        current = core.Current;
                    }
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                if (failure != null)
                {
                    summary.status = "failed";
                    summary.failure = failure.ToString();
                    summary.errorCount = errorCount;
                    WriteSummary(directory, summary);
                    Debug.LogException(failure);
                    yield return null;
                    UnityEngine.Application.Quit(1);
                    yield break;
                }

                if (!hasNext)
                {
                    break;
                }

                yield return current;
            }

            WriteSummary(directory, summary);
            Debug.Log("ERA_CLOCK_PLAYER_SMOKE_PASS");
            yield return null;
            UnityEngine.Application.Quit(0);
        }

        private IEnumerator RunEvidenceCore(string directory, PlayerEvidenceSummary summary)
        {
            Validate();
            Directory.CreateDirectory(directory);
            Screen.SetResolution(1280, 720, false);
            yield return null;
            yield return null;

            presenter.ApplySnapshot(Snapshot(7, 8, 1, EraClockAnchorTarget.Center));
            yield return null;
            Capture(directory, "player-rollover-initial-1280x720.png", summary);

            presenter.ApplySnapshot(Snapshot(8, 1, 2, EraClockAnchorTarget.Center));
            float pulseDeadline = Time.realtimeSinceStartup + 6f;
            while (rolloverPulse.alpha < 0.85f &&
                presenter.State == EraClockPresenterState.RolloverTransition &&
                Time.realtimeSinceStartup < pulseDeadline)
            {
                yield return null;
            }

            if (rolloverPulse.alpha < 0.85f)
            {
                throw new InvalidOperationException("Player did not observe the rollover pulse.");
            }

            Capture(directory, "player-rollover-pulse-1280x720.png", summary);
            float rolloverDeadline = Time.realtimeSinceStartup + 6f;
            while (presenter.State != EraClockPresenterState.Settled &&
                Time.realtimeSinceStartup < rolloverDeadline)
            {
                yield return null;
            }

            if (presenter.State != EraClockPresenterState.Settled)
            {
                throw new InvalidOperationException("Player rollover did not settle.");
            }

            Capture(directory, "player-rollover-complete-1280x720.png", summary);
            int materialCountBeforeRoutes = Resources.FindObjectsOfTypeAll<Material>().Length;
            for (var route = 0; route < 3; route++)
            {
                EraClockAnchorTarget target = route % 2 == 0
                    ? EraClockAnchorTarget.Hud
                    : EraClockAnchorTarget.Center;
                presenter.ApplySnapshot(Snapshot(8, 1, 3 + route, target));
                float anchorDeadline = Time.realtimeSinceStartup + 4f;
                while (presenter.State != EraClockPresenterState.Settled &&
                    Time.realtimeSinceStartup < anchorDeadline)
                {
                    yield return null;
                }

                if (presenter.State != EraClockPresenterState.Settled)
                {
                    throw new InvalidOperationException(
                        "Player anchor transition did not settle on route " + (route + 1) + ".");
                }
            }

            Capture(directory, "player-hud-terminal-1280x720.png", summary);
            int materialCountAfterRoutes = Resources.FindObjectsOfTypeAll<Material>().Length;
            summary.status = "passed";
            summary.width = Screen.width;
            summary.height = Screen.height;
            summary.finalEra = presenter.CurrentSnapshot.Era;
            summary.finalPhase = presenter.CurrentSnapshot.Phase;
            summary.finalState = presenter.State.ToString();
            summary.finalAnchor = presenter.CurrentSnapshot.AnchorTarget.ToString();
            summary.rootBlocksRaycasts = rootGroup.blocksRaycasts;
            summary.errorCount = errorCount;
            summary.isolatedRouteTransitions = 3;
            summary.presenterCount = UnityEngine.Object.FindObjectsByType<EraClockPresenter>(
                FindObjectsInactive.Include).Length;
            summary.materialCountBeforeRoutes = materialCountBeforeRoutes;
            summary.materialCountAfterRoutes = materialCountAfterRoutes;
            summary.materialGrowth = materialCountAfterRoutes - materialCountBeforeRoutes;
            if (summary.rootBlocksRaycasts ||
                errorCount != 0 ||
                summary.presenterCount != 1 ||
                summary.materialGrowth != 0)
            {
                throw new InvalidOperationException(
                    "Player terminal state retained input blocking, duplicate presenters, " +
                    "material growth, or logged an error.");
            }
        }

        private void Validate()
        {
            if (presenter == null ||
                evidenceCamera == null ||
                rootGroup == null ||
                rolloverPulse == null)
            {
                throw new InvalidOperationException(
                    "EraClock Player evidence requires Presenter, Camera, RootGroup, and RolloverPulse.");
            }
        }

        private void Capture(string directory, string fileName, PlayerEvidenceSummary summary)
        {
            int width = Screen.width;
            int height = Screen.height;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            RenderTexture previousTarget = evidenceCamera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            evidenceCamera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            evidenceCamera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            texture.Apply(false, false);
            File.WriteAllBytes(Path.Combine(directory, fileName), texture.EncodeToPNG());
            summary.screenshots.Add(fileName);
            UnityEngine.Object.Destroy(texture);
            evidenceCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            target.Release();
            UnityEngine.Object.Destroy(target);
        }

        private static EraClockPresentationSnapshot Snapshot(
            int era,
            int phase,
            long sequence,
            EraClockAnchorTarget anchor)
        {
            return new EraClockPresentationSnapshot(era, phase, sequence, anchor);
        }

        private static string EvidenceDirectory()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (arguments[index] == "-eraClockEvidenceDirectory")
                {
                    return arguments[index + 1];
                }
            }

            return Path.Combine(
                UnityEngine.Application.persistentDataPath,
                "era-clock-animation-gate-player");
        }

        private static void WriteSummary(string directory, PlayerEvidenceSummary summary)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(
                Path.Combine(directory, "player-smoke-summary.json"),
                JsonUtility.ToJson(summary, true));
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errorCount++;
            }
        }

        private void OnDisable()
        {
            UnityEngine.Application.logMessageReceived -= OnLogMessage;
        }

        [Serializable]
        private sealed class PlayerEvidenceSummary
        {
            public string status;
            public string unityVersion;
            public string renderer;
            public int width;
            public int height;
            public int finalEra;
            public int finalPhase;
            public string finalState;
            public string finalAnchor;
            public bool rootBlocksRaycasts;
            public int errorCount;
            public int isolatedRouteTransitions;
            public int presenterCount;
            public int materialCountBeforeRoutes;
            public int materialCountAfterRoutes;
            public int materialGrowth;
            public string failure;
            public List<string> screenshots;
        }
    }
}
