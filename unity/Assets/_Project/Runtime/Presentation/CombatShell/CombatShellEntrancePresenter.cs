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
        [SerializeField, Min(0.1f)] private float duration = 0.45f;

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
            if (topHud == null || backgroundRoot == null)
            {
                return;
            }

            PlayReveal();
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            CompleteImmediately();
        }

        public void PlayReveal()
        {
            if (topHud == null || backgroundRoot == null)
            {
                CompleteImmediately();
                return;
            }

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            if (!isActiveAndEnabled)
            {
                CompleteImmediately();
                return;
            }

            _routine = StartCoroutine(Play());
        }

        public void CompleteImmediately()
        {
            if (topHud != null)
            {
                topHud.alpha = 1f;
            }

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
            backgroundRoot.localScale = _backgroundScale * 0.985f;
            yield return null;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                topHud.alpha = eased;
                backgroundRoot.localScale = Vector3.Lerp(
                    _backgroundScale * 0.985f,
                    _backgroundScale,
                    eased);
                yield return null;
            }

            topHud.alpha = 1f;
            backgroundRoot.localScale = _backgroundScale;
            IsComplete = true;
            _routine = null;
        }
    }
}
