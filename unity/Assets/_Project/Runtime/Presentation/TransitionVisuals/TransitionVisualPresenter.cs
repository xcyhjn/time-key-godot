using System.Collections;
using TimeKey.Application.SceneFlow;
using UnityEngine;

namespace TimeKey.Presentation.TransitionVisuals
{
    [DisallowMultipleComponent]
    public sealed class TransitionVisualPresenter : MonoBehaviour, ISceneRevealPresentation
    {
        [SerializeField] private CanvasGroup coverGroup = null;
        [SerializeField] private GameObject loadingIndicator = null;
        [SerializeField, Min(0f)] private float duration = 0.4f;

        private Coroutine _routine;

        public bool IsComplete { get; private set; }

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
            if (coverGroup == null || !isActiveAndEnabled || duration <= 0f)
            {
                CompleteImmediately();
                return;
            }

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(Reveal());
        }

        public void PlayCover()
        {
            if (coverGroup == null || duration <= 0f)
            {
                CompleteCoverImmediately();
                return;
            }

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(Cover());
        }

        public void CompleteImmediately()
        {
            StopActiveRoutine();
            ApplyRevealedState();
        }

        public void CompleteCoverImmediately()
        {
            StopActiveRoutine();
            if (coverGroup != null)
            {
                coverGroup.alpha = 1f;
                coverGroup.blocksRaycasts = true;
                coverGroup.interactable = true;
            }

            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(true);
            }

            IsComplete = true;
        }

        private void ApplyRevealedState()
        {
            if (coverGroup != null)
            {
                coverGroup.alpha = 0f;
                coverGroup.blocksRaycasts = false;
                coverGroup.interactable = false;
            }

            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(false);
            }

            IsComplete = true;
        }

        private IEnumerator Reveal()
        {
            IsComplete = false;
            coverGroup.blocksRaycasts = true;
            coverGroup.interactable = true;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                coverGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            _routine = null;
            CompleteImmediately();
        }

        private IEnumerator Cover()
        {
            IsComplete = false;
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(true);
            }

            coverGroup.blocksRaycasts = true;
            coverGroup.interactable = true;
            var start = coverGroup.alpha;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                coverGroup.alpha = Mathf.Lerp(start, 1f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _routine = null;
            CompleteCoverImmediately();
        }

        private void StopActiveRoutine()
        {
            if (_routine == null)
            {
                return;
            }

            var routine = _routine;
            _routine = null;
            StopCoroutine(routine);
        }
    }
}
