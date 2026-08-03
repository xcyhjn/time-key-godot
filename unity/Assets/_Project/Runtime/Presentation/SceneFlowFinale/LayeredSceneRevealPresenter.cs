using System;
using System.Collections;
using TimeKey.Application.SceneFlow;
using UnityEngine;

namespace TimeKey.Presentation.SceneFlowFinale
{
    [DisallowMultipleComponent]
    public sealed class LayeredSceneRevealPresenter : MonoBehaviour,
        ISceneRevealPresentation
    {
        [SerializeField] private CanvasGroup[] layers = Array.Empty<CanvasGroup>();
        [SerializeField, Min(0f)] private float layerDuration = 0.24f;
        [SerializeField, Min(0f)] private float layerInterval = 0.09f;

        private Coroutine _routine;

        public bool IsComplete { get; private set; }

        public int LayerCount => layers == null ? 0 : layers.Length;

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
            StopRoutine();
            IsComplete = false;
            if (!isActiveAndEnabled || layers == null || layers.Length == 0 ||
                layerDuration <= 0f)
            {
                CompleteImmediately();
                return;
            }

            ApplyFrame(0f, false);
            _routine = StartCoroutine(PlayRoutine());
        }

        public void CompleteImmediately()
        {
            StopRoutine();
            ApplyTerminalState();
            IsComplete = true;
        }

        private IEnumerator PlayRoutine()
        {
            yield return null;
            var elapsed = 0f;
            var totalDuration = layerDuration +
                (layerInterval * Math.Max(0, layers.Length - 1));
            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                ApplyFrame(elapsed, false);
                yield return null;
            }

            _routine = null;
            ApplyTerminalState();
            IsComplete = true;
        }

        private void ApplyFrame(float elapsed, bool terminal)
        {
            for (var index = 0; index < layers.Length; index++)
            {
                var layer = layers[index];
                if (layer == null)
                {
                    continue;
                }

                var localElapsed = elapsed - (layerInterval * index);
                var linear = terminal
                    ? 1f
                    : Mathf.Clamp01(localElapsed / layerDuration);
                layer.alpha = 1f - Mathf.Pow(1f - linear, 3f);
                layer.interactable = terminal;
                layer.blocksRaycasts = terminal;
            }
        }

        private void ApplyTerminalState()
        {
            if (layers == null)
            {
                return;
            }

            ApplyFrame(float.MaxValue, true);
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
