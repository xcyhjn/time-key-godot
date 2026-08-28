using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeKey.Presentation.Feedback
{
    [DisallowMultipleComponent]
    public sealed class CombatFeedbackVfxPool : MonoBehaviour
    {
        [SerializeField] private CombatFeedbackOneShot oneShotPrefab = null;
        [SerializeField] private Transform oneShotRoot = null;
        [SerializeField] private int maxInstances = 8;

        private readonly List<CombatFeedbackOneShot> _instances =
            new List<CombatFeedbackOneShot>();
        private readonly HashSet<CombatFeedbackOneShot> _active =
            new HashSet<CombatFeedbackOneShot>();

        public event Action<CombatFeedbackEvent> PoolExhausted;

        public CombatFeedbackOneShot OneShotPrefab => oneShotPrefab;

        public Transform OneShotRoot => oneShotRoot;

        public int MaxInstances => Mathf.Max(0, maxInstances);

        public int CreatedCount => _instances.Count;

        public int ActiveCount => _active.Count;

        public int PeakActiveCount { get; private set; }

        public int RecycledCount { get; private set; }

        public int ExhaustedCount { get; private set; }

        public void Configure(
            CombatFeedbackOneShot prefab,
            Transform root,
            int instanceLimit)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (instanceLimit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(instanceLimit));
            }

            if (_active.Count > 0)
            {
                throw new InvalidOperationException(
                    "A feedback pool cannot be reconfigured while instances are active.");
            }

            if (oneShotPrefab == prefab && oneShotRoot == root && maxInstances == instanceLimit)
            {
                return;
            }

            oneShotPrefab = prefab;
            oneShotRoot = root;
            maxInstances = instanceLimit;
        }

        public CombatFeedbackOneShot Play(CombatFeedbackEvent feedbackEvent)
        {
            if (feedbackEvent == null)
            {
                throw new ArgumentNullException(nameof(feedbackEvent));
            }

            if (feedbackEvent.IsNoEffect ||
                feedbackEvent.Phase == CombatFeedbackPhase.Cancelled ||
                feedbackEvent.CancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var instance = FindAvailable();
            if (instance == null)
            {
                if (oneShotPrefab == null || _instances.Count >= MaxInstances)
                {
                    ExhaustedCount++;
                    PoolExhausted?.Invoke(feedbackEvent);
                    return null;
                }

                instance = Instantiate(
                    oneShotPrefab,
                    oneShotRoot == null ? transform : oneShotRoot,
                    false);
                instance.name = oneShotPrefab.name + "-pooled-" + _instances.Count;
                _instances.Add(instance);
            }

            _active.Add(instance);
            PeakActiveCount = Mathf.Max(PeakActiveCount, _active.Count);
            instance.gameObject.SetActive(true);
            instance.Play(feedbackEvent, Recycle);
            return instance;
        }

        public void CancelAll()
        {
            var active = new List<CombatFeedbackOneShot>(_active);
            for (var index = 0; index < active.Count; index++)
            {
                if (active[index] != null)
                {
                    active[index].Cancel();
                }
            }
        }

        private CombatFeedbackOneShot FindAvailable()
        {
            for (var index = 0; index < _instances.Count; index++)
            {
                var instance = _instances[index];
                if (instance != null && !_active.Contains(instance))
                {
                    return instance;
                }
            }

            return null;
        }

        private void Recycle(CombatFeedbackOneShot instance)
        {
            if (instance == null)
            {
                return;
            }

            _active.Remove(instance);
            RecycledCount++;
            instance.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            CancelAll();
        }

        private void OnDestroy()
        {
            _active.Clear();
            _instances.Clear();
        }
    }
}
