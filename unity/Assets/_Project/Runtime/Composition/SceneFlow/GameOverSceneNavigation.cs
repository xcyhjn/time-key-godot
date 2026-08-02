using System;
using System.Threading;
using System.Threading.Tasks;
using TimeKey.Application.SceneFlow;
using TimeKey.Presentation.GameOver;
using UnityEngine;

namespace TimeKey.Composition.SceneFlow
{
    [DisallowMultipleComponent]
    public sealed class GameOverSceneNavigation : MonoBehaviour
    {
        [SerializeField] private BootstrapRoot bootstrap = null;
        [SerializeField] private SceneFlowStateStore stateStore = null;
        [SerializeField] private GameOverPresenter presenter = null;

        private CancellationTokenSource _lifetime;

        public SceneTransitionResult LastResult { get; private set; }

        private void OnEnable()
        {
            _lifetime = new CancellationTokenSource();
            presenter = presenter != null
                ? presenter
                : GetComponentInChildren<GameOverPresenter>(true);
            stateStore = stateStore != null
                ? stateStore
                : FindFirstObjectByType<SceneFlowStateStore>();
            if (presenter != null)
            {
                presenter.ReturnRequested += OnReturnRequested;
                if (stateStore?.LastOutcome != null)
                {
                    presenter.Apply(stateStore.LastOutcome);
                }
            }
        }

        private void OnDisable()
        {
            if (presenter != null)
            {
                presenter.ReturnRequested -= OnReturnRequested;
            }

            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
        }

        private async void OnReturnRequested()
        {
            try
            {
                await ReturnToMainMenuAsync(_lifetime.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                presenter?.SetBusy(false);
                Debug.LogException(exception, this);
            }
        }

        public async Task<SceneTransitionResult> ReturnToMainMenuAsync(
            CancellationToken cancellationToken = default)
        {
            bootstrap = bootstrap != null
                ? bootstrap
                : FindFirstObjectByType<BootstrapRoot>();
            if (bootstrap == null)
            {
                throw new InvalidOperationException("Game Over requires the persistent BootstrapRoot.");
            }

            await bootstrap.InitializationTask;
            cancellationToken.ThrowIfCancellationRequested();
            var sequence = bootstrap.ReserveTransitionSequence();
            LastResult = await bootstrap.TransitionAsync(
                new SceneTransitionRequest(
                    sequence,
                    "game-over-main-menu-" + sequence,
                    SceneId.GameOver,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)),
                CancellationToken.None);
            if (!LastResult.Succeeded)
            {
                presenter?.SetBusy(false);
                Debug.LogError("Game Over transition failed: " + LastResult.Message, this);
            }

            return LastResult;
        }
    }
}
