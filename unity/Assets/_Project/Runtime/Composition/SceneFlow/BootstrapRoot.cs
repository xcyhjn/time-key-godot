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
        private Task _initializationTask;

        public bool IsReady { get; private set; }

        public SceneId CurrentScene =>
            _coordinator == null ? SceneId.None : _coordinator.CurrentScene;

        public Task InitializationTask => _initializationTask ?? Task.CompletedTask;

        public Exception InitializationException { get; private set; }

        private void Awake()
        {
            if (effects == null)
            {
                throw new InvalidOperationException("BootstrapRoot requires scene-flow effects.");
            }

            _initializationTask = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await effects.LoadInitialAsync(initialScene, CancellationToken.None);
                _coordinator = new SceneFlowCoordinator(initialScene, effects);
                IsReady = true;
            }
            catch (Exception exception)
            {
                InitializationException = exception;
                throw;
            }
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
