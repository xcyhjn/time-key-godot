using System;
using System.Collections;
using System.Threading.Tasks;
using TimeKey.Application.SceneFlow;
using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Presentation.GameStart
{
    [DisallowMultipleComponent]
    public sealed class StartLogoPresenter : MonoBehaviour, ISceneRevealPresentation
    {
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private Graphic background = null;
        [SerializeField] private RawImage keyImage = null;
        [SerializeField] private Text[] characterLabels = Array.Empty<Text>();
        [SerializeField] private Font silverFont = null;
        [SerializeField, Min(0.1f)] private float duration = 3f;
        [SerializeField, Min(0f)] private float characterTravel = 50f;
        [SerializeField] private Color blackBackground = Color.black;
        [SerializeField] private Color goldBackground = new Color(0.99f, 0.93f, 0.67f, 1f);
        [SerializeField] private bool allowSkip;

        private Coroutine _routine;
        private Vector2[] _restingPositions = Array.Empty<Vector2>();
        private TaskCompletionSource<bool> _completionSource = NewCompletionSource();
        private bool _completionRaised;

        public event Action Completed;

        public bool IsComplete { get; private set; }

        public Task CompletionTask => _completionSource.Task;

        public bool AllowSkip
        {
            get => allowSkip;
            set => allowSkip = value;
        }

        private void Awake()
        {
            CaptureRestingPositions();
        }

        private void OnEnable()
        {
            if (DependenciesAssigned())
            {
                PlayReveal();
            }
            else
            {
                CompleteImmediately();
            }
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            ApplyTerminalState(false);
        }

        private void Update()
        {
            if (allowSkip && !IsComplete && Input.anyKeyDown)
            {
                CompleteImmediately();
            }
        }

        public void PlayReveal()
        {
            if (!DependenciesAssigned() || !isActiveAndEnabled)
            {
                CompleteImmediately();
                return;
            }

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _completionRaised = false;
            _completionSource = NewCompletionSource();
            foreach (var label in characterLabels)
            {
                label.font = silverFont;
            }

            _routine = StartCoroutine(Play());
        }

        public void CompleteImmediately()
        {
            if (_routine != null)
            {
                var routine = _routine;
                _routine = null;
                StopCoroutine(routine);
            }

            ApplyTerminalState(true);
        }

        private void ApplyTerminalState(bool notify)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (DependenciesAssigned())
            {
                SetVisualState(1f);
            }
            IsComplete = true;
            _completionSource.TrySetResult(true);
            if (notify && !_completionRaised)
            {
                _completionRaised = true;
                Completed?.Invoke();
            }
        }

        private IEnumerator Play()
        {
            IsComplete = false;
            canvasGroup.alpha = 1f;
            SetVisualState(0f);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (allowSkip && Input.anyKeyDown)
                {
                    CompleteImmediately();
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                SetVisualState(t);
                yield return null;
            }

            _routine = null;
            CompleteImmediately();
        }

        private bool DependenciesAssigned()
        {
            return canvasGroup != null && background != null && keyImage != null &&
                characterLabels != null && characterLabels.Length == 3 &&
                characterLabels[0] != null && characterLabels[1] != null &&
                characterLabels[2] != null && silverFont != null;
        }

        private void CaptureRestingPositions()
        {
            if (characterLabels == null || characterLabels.Length != 3)
            {
                _restingPositions = Array.Empty<Vector2>();
                return;
            }

            _restingPositions = new Vector2[characterLabels.Length];
            for (var index = 0; index < characterLabels.Length; index++)
            {
                _restingPositions[index] = characterLabels[index].rectTransform.anchoredPosition;
            }
        }

        private void SetVisualState(float progress)
        {
            progress = Mathf.Clamp01(progress);
            var first = Mathf.Clamp01(progress * 3f);
            var middle = Mathf.Clamp01((progress - (1f / 3f)) * 3f);
            var last = Mathf.Clamp01((progress - (2f / 3f)) * 3f);
            var fade = progress < (1f / 3f)
                ? first
                : progress < (2f / 3f) ? 1f : 1f - last;
            background.color = progress < (1f / 3f)
                ? Color.Lerp(blackBackground, goldBackground, first)
                : progress < (2f / 3f)
                    ? goldBackground
                    : Color.Lerp(goldBackground, blackBackground, last);
            SetAlpha(keyImage, fade);
            for (var index = 0; index < characterLabels.Length; index++)
            {
                SetAlpha(characterLabels[index], fade);
                var direction = index == 1 ? 1f : -1f;
                var offset = progress < (1f / 3f)
                    ? Mathf.Lerp(direction * characterTravel, 0f, first)
                    : progress < (2f / 3f)
                        ? Mathf.Lerp(0f, -direction * characterTravel, middle)
                        : Mathf.Lerp(-direction * characterTravel, 0f, last);
                characterLabels[index].rectTransform.anchoredPosition =
                    _restingPositions[index] + Vector2.up * offset;
            }
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private static TaskCompletionSource<bool> NewCompletionSource()
        {
            return new TaskCompletionSource<bool>();
        }
    }
}
