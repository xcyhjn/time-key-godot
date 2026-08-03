using System;
using System.Collections;
using TimeKey.Application.SceneFlow;
using UnityEngine;

namespace TimeKey.Presentation.CombatShell
{
    [DisallowMultipleComponent]
    public sealed class CombatShellEntrancePresenter : MonoBehaviour, ISceneRevealPresentation
    {
        [SerializeField] private CanvasGroup topHud = null;
        [SerializeField] private Transform backgroundRoot = null;
        [SerializeField] private CanvasGroup[] stagedUiGroups = Array.Empty<CanvasGroup>();
        [SerializeField, Min(0f)] private float duration = 0.45f;
        [SerializeField, Min(0f)] private float layerInterval = 0.10f;

        private Vector3 _backgroundScale;
        private Coroutine _routine;

        public bool IsComplete { get; private set; }

        private void Awake()
        {
            if (backgroundRoot != null)
            {
                _backgroundScale = backgroundRoot.localScale;
            }
        }

        private void OnEnable()
        {
            PlayReveal();
        }

        private void OnDisable()
        {
            CompleteImmediately();
        }

        public void PlayReveal()
        {
            if (topHud == null || backgroundRoot == null || duration <= 0f)
            {
                CompleteImmediately();
                return;
            }

            StopRoutine();

            if (!isActiveAndEnabled)
            {
                CompleteImmediately();
                return;
            }

            _routine = StartCoroutine(Play());
        }

        public void CompleteImmediately()
        {
            StopRoutine();
            if (topHud != null)
            {
                topHud.alpha = 1f;
                topHud.interactable = true;
                topHud.blocksRaycasts = true;
            }

            SetStagedUiTerminalState();

            if (backgroundRoot != null && _backgroundScale != Vector3.zero)
            {
                backgroundRoot.localScale = _backgroundScale;
            }

            IsComplete = true;
        }

        private IEnumerator Play()
        {
            IsComplete = false;
            topHud.alpha = 0f;
            topHud.interactable = false;
            topHud.blocksRaycasts = false;
            ApplyStagedUiFrame(0f, false);
            backgroundRoot.localScale = _backgroundScale * 0.985f;
            yield return null;

            var elapsed = 0f;
            var stagedCount = stagedUiGroups == null ? 0 : stagedUiGroups.Length;
            var totalDuration = duration + (layerInterval * stagedCount);
            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                topHud.alpha = eased;
                ApplyStagedUiFrame(elapsed, false);
                backgroundRoot.localScale = Vector3.Lerp(
                    _backgroundScale * 0.985f,
                    _backgroundScale,
                    eased);
                yield return null;
            }

            topHud.alpha = 1f;
            topHud.interactable = true;
            topHud.blocksRaycasts = true;
            SetStagedUiTerminalState();
            backgroundRoot.localScale = _backgroundScale;
            IsComplete = true;
            _routine = null;
        }

        private void ApplyStagedUiFrame(float elapsed, bool terminal)
        {
            if (stagedUiGroups == null)
            {
                return;
            }

            for (var index = 0; index < stagedUiGroups.Length; index++)
            {
                var group = stagedUiGroups[index];
                if (group == null)
                {
                    continue;
                }

                var localElapsed = elapsed - (layerInterval * (index + 1));
                var linear = terminal
                    ? 1f
                    : Mathf.Clamp01(localElapsed / duration);
                group.alpha = 1f - Mathf.Pow(1f - linear, 3f);
                group.interactable = terminal;
                group.blocksRaycasts = terminal;
            }
        }

        private void SetStagedUiTerminalState()
        {
            ApplyStagedUiFrame(float.MaxValue, true);
        }

        private void StopRoutine()
        {
            if (_routine == null)
            {
                return;
            }

            StopCoroutine(_routine);
            _routine = null;
        }
    }
}
