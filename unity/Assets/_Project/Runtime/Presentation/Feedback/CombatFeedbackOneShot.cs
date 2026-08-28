using System;
using UnityEngine;

namespace TimeKey.Presentation.Feedback
{
    [DisallowMultipleComponent]
    public sealed class CombatFeedbackOneShot : MonoBehaviour
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        [SerializeField] private ParticleSystem particles = null;
        [SerializeField] private Renderer[] renderers = Array.Empty<Renderer>();
        [SerializeField] private float fallbackDuration = 0.35f;

        private MaterialPropertyBlock _propertyBlock;
        private Action<CombatFeedbackOneShot> _recycle;
        private CombatFeedbackEvent _event;
        private float _remaining;
        private float _totalDuration;

        public bool IsPlaying { get; private set; }

        public CombatFeedbackEvent CurrentEvent => _event;

        private void Awake()
        {
            EnsurePropertyBlock();
        }

        public void Play(
            CombatFeedbackEvent feedbackEvent,
            Action<CombatFeedbackOneShot> recycle)
        {
            if (feedbackEvent == null)
            {
                throw new ArgumentNullException(nameof(feedbackEvent));
            }

            if (recycle == null)
            {
                throw new ArgumentNullException(nameof(recycle));
            }

            EnsureReferences();
            EnsurePropertyBlock();
            _event = feedbackEvent;
            _recycle = recycle;
            _totalDuration = Mathf.Max(fallbackDuration, feedbackEvent.Duration);
            _remaining = _totalDuration;
            transform.position = feedbackEvent.WorldAnchor.WorldPosition;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one * 0.18f;
            IsPlaying = true;
            ApplyVisualState(feedbackEvent.Kind);

            if (particles != null)
            {
                particles.Clear(true);
                particles.Play(true);
            }
        }

        public void Cancel()
        {
            if (IsPlaying)
            {
                Finish();
            }
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (_event.CancellationToken.IsCancellationRequested)
            {
                Finish();
                return;
            }

            _remaining -= Time.unscaledDeltaTime;
            var normalized = _totalDuration <= 0f
                ? 1f
                : Mathf.Clamp01(1f - (_remaining / _totalDuration));
            transform.localScale = Vector3.one * Mathf.Lerp(
                0.18f,
                1.05f,
                Mathf.Sin(normalized * Mathf.PI));
            if (_remaining <= 0f)
            {
                Finish();
            }
        }

        private void OnDisable()
        {
            if (IsPlaying)
            {
                Finish();
            }
        }

        private void EnsureReferences()
        {
            if (fallbackDuration < 0f || float.IsNaN(fallbackDuration) ||
                float.IsInfinity(fallbackDuration))
            {
                fallbackDuration = 0.35f;
            }

            if (particles == null)
            {
                particles = GetComponentInChildren<ParticleSystem>(true);
            }

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        private void EnsurePropertyBlock()
        {
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }
        }

        private void ApplyVisualState(CombatFeedbackKind kind)
        {
            var color = ColorFor(kind);
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                _propertyBlock.Clear();
                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorProperty, color);
                _propertyBlock.SetColor(ColorProperty, color);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void Finish()
        {
            IsPlaying = false;
            _event = null;
            if (particles != null)
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            var recycle = _recycle;
            _recycle = null;
            recycle?.Invoke(this);
        }

        private static Color ColorFor(CombatFeedbackKind kind)
        {
            switch (kind)
            {
                case CombatFeedbackKind.Damage:
                case CombatFeedbackKind.PoisonApply:
                case CombatFeedbackKind.PoisonTick:
                case CombatFeedbackKind.TowerDecay:
                    return new Color(0.96f, 0.24f, 0.2f, 1f);
                case CombatFeedbackKind.Recover:
                    return new Color(0.24f, 0.9f, 0.56f, 1f);
                case CombatFeedbackKind.ElevationPulse:
                    return new Color(0.28f, 0.78f, 1f, 1f);
                case CombatFeedbackKind.Victory:
                    return new Color(1f, 0.8f, 0.26f, 1f);
                case CombatFeedbackKind.Defeat:
                    return new Color(0.65f, 0.2f, 0.3f, 1f);
                default:
                    return new Color(0.78f, 0.86f, 0.95f, 1f);
            }
        }
    }
}
