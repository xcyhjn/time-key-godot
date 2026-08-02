using System;
using System.Threading;
using System.Threading.Tasks;
using TimeKey.Application.SceneFlow;
using UnityEngine;

namespace TimeKey.Composition.SceneFlow
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class BootstrapRoot : MonoBehaviour
    {
        [SerializeField] private SceneId initialScene = SceneId.GameStart;
        [SerializeField] private UnitySceneFlowEffects effects = null;

        private SceneFlowCoordinator _coordinator;

        public bool IsReady { get; private set; }

        public SceneId CurrentScene =>
            _coordinator == null ? SceneId.None : _coordinator.CurrentScene;

        private async void Awake()
        {
            if (effects == null)
            {
                throw new InvalidOperationException("BootstrapRoot requires scene-flow effects.");
            }

            await effects.LoadInitialAsync(initialScene, CancellationToken.None);
            _coordinator = new SceneFlowCoordinator(initialScene, effects);
            IsReady = true;
        }

        public Task<SceneTransitionResult> TransitionAsync(
            SceneTransitionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!IsReady)
            {
                throw new InvalidOperationException("Bootstrap has not loaded its initial content scene.");
            }

            return _coordinator.TransitionAsync(request, cancellationToken);
        }
    }
}
