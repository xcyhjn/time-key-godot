using System;
using System.Threading;
using System.Threading.Tasks;
using TimeKey.Application.SceneFlow;
using TimeKey.Presentation.GameStart;
using UnityEngine;

namespace TimeKey.Composition.SceneFlow
{
    [DisallowMultipleComponent]
    public sealed class GameStartSceneNavigation : MonoBehaviour
    {
        [SerializeField] private BootstrapRoot bootstrap = null;
        [SerializeField] private StartLogoPresenter presenter = null;

        private CancellationTokenSource _lifetime;

        public SceneTransitionResult LastResult { get; private set; }

        private async void Start()
        {
            if (UnityEngine.Application.isBatchMode)
            {
                return;
            }

            _lifetime = new CancellationTokenSource();
            try
            {
                await NavigateToMainMenuAsync(_lifetime.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void OnDestroy()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
        }

        private async Task NavigateToMainMenuAsync(CancellationToken cancellationToken)
        {
            bootstrap = bootstrap != null
                ? bootstrap
                : FindFirstObjectByType<BootstrapRoot>();
            if (bootstrap == null)
            {
                throw new InvalidOperationException("GameStart requires the persistent BootstrapRoot.");
            }

            await bootstrap.InitializationTask;
            cancellationToken.ThrowIfCancellationRequested();
            if (bootstrap.CurrentScene != SceneId.GameStart)
            {
                return;
            }

            presenter = presenter != null
                ? presenter
                : GetComponentInChildren<StartLogoPresenter>(true);
            if (presenter != null)
            {
                await WaitForPresentationAsync(presenter, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var sequence = bootstrap.ReserveTransitionSequence();
            LastResult = await bootstrap.TransitionAsync(
                new SceneTransitionRequest(
                    sequence,
                    "game-start-complete-" + sequence,
                    SceneId.GameStart,
                    SceneId.MainMenu,
                    new EmptySceneTransitionPayload(SceneId.MainMenu)),
                CancellationToken.None);
            if (!LastResult.Succeeded)
            {
                Debug.LogError("GameStart transition failed: " + LastResult.Message, this);
            }
        }

        private static async Task WaitForPresentationAsync(
            StartLogoPresenter presenter,
            CancellationToken cancellationToken)
        {
            var cancellation = Task.Delay(Timeout.Infinite, cancellationToken);
            var completed = await Task.WhenAny(presenter.CompletionTask, cancellation);
            cancellationToken.ThrowIfCancellationRequested();
            await completed;
        }
    }
}
