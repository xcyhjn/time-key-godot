using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TimeKey.Application.SceneFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeKey.Composition.SceneFlow
{
    [DisallowMultipleComponent]
    public sealed class UnitySceneFlowEffects : MonoBehaviour, ISceneFlowEffects
    {
        [SerializeField] private SceneRouteCatalog routes = null;
        [SerializeField] private PersistentInputGate inputGate = null;
        [SerializeField] private TransitionCanvasPresenter transition = null;
        [SerializeField] private SceneFlowStateStore stateStore = null;

        private AsyncOperation _pendingLoad;
        private SceneContentEntry _activeEntry;
        private SceneContentEntry _targetEntry;

        public async Task LoadInitialAsync(
            SceneId initialScene,
            CancellationToken cancellationToken)
        {
            string sceneName = null;
            try
            {
                inputGate.SetLocked(true);
                transition.SetCovered(true);
                sceneName = routes.GetSceneName(initialScene);
                var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (operation == null)
                {
                    throw new InvalidOperationException(
                        "The initial content scene could not be loaded.");
                }

                await AwaitOperation(operation, cancellationToken);
                var scene = SceneManager.GetSceneByName(sceneName);
                var entry = FindEntry(scene, initialScene);
                entry.Bind(new EmptySceneTransitionPayload(initialScene));
                entry.SetCameraEnabled(true);
                SceneManager.SetActiveScene(scene);
                entry.EnsureRenderable();
                await YieldFrameAsync(cancellationToken);
                _activeEntry = entry;
                transition.SetCovered(false);
                entry.PlayRevealPresentation();
                await AwaitRevealPresentation(entry, transition, cancellationToken);
                inputGate.SetLocked(false);
                entry.SetInteractive(true);
            }
            catch
            {
                var failedScene = string.IsNullOrEmpty(sceneName)
                    ? default(Scene)
                    : SceneManager.GetSceneByName(sceneName);
                if (failedScene.IsValid() && failedScene.isLoaded)
                {
                    var unload = SceneManager.UnloadSceneAsync(failedScene);
                    if (unload != null)
                    {
                        await AwaitOperation(unload, CancellationToken.None);
                    }
                }

                transition.SetCovered(false);
                inputGate.SetLocked(false);
                throw;
            }
        }

        public async Task<SceneFlowEffectResult> ExecuteAsync(
            SceneTransitionPhase phase,
            SceneTransitionRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                switch (phase)
                {
                    case SceneTransitionPhase.InputLocked:
                        inputGate.SetLocked(true);
                        _activeEntry?.SetInteractive(false);
                        break;
                    case SceneTransitionPhase.Covering:
                        transition.SetCovered(true);
                        await AwaitTransitionPresentation(transition, cancellationToken);
                        _activeEntry?.SetCameraEnabled(false);
                        break;
                    case SceneTransitionPhase.LoadingTarget:
                        await LoadTargetAsync(request.Target, cancellationToken);
                        break;
                    case SceneTransitionPhase.ActivatingTarget:
                        await ActivateTargetAsync(request.Target, cancellationToken);
                        break;
                    case SceneTransitionPhase.BindingPayload:
                        stateStore.Record(request);
                        _targetEntry.Bind(request.Payload);
                        break;
                    case SceneTransitionPhase.WaitingForFirstRenderableFrame:
                        _targetEntry.SetCameraEnabled(true);
                        _targetEntry.EnsureRenderable();
                        await YieldFrameAsync(cancellationToken);
                        break;
                    case SceneTransitionPhase.UnloadingSource:
                        await UnloadAsync(request, cancellationToken);
                        _activeEntry = _targetEntry;
                        _targetEntry = null;
                        break;
                    case SceneTransitionPhase.Revealing:
                        transition.SetCovered(false);
                        _activeEntry?.PlayRevealPresentation();
                        await AwaitRevealPresentation(
                            _activeEntry,
                            transition,
                            cancellationToken);
                        break;
                    case SceneTransitionPhase.InputUnlocked:
                        inputGate.SetLocked(false);
                        _activeEntry?.SetInteractive(true);
                        break;
                    case SceneTransitionPhase.RollingBackTarget:
                        stateStore.Rollback(request);
                        await RollBackTargetAsync(request.Target);
                        break;
                    case SceneTransitionPhase.RestoringSource:
                        RestoreSource(request.Source);
                        break;
                    default:
                        return SceneFlowEffectResult.Failed("Unsupported scene-flow phase: " + phase + ".");
                }

                return SceneFlowEffectResult.Success();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return SceneFlowEffectResult.Failed(exception.Message);
            }
        }

        private async Task LoadTargetAsync(
            SceneId target,
            CancellationToken cancellationToken)
        {
            var operation = SceneManager.LoadSceneAsync(
                routes.GetSceneName(target),
                LoadSceneMode.Additive);
            if (operation == null)
            {
                throw new InvalidOperationException("The target scene could not be loaded.");
            }

            operation.allowSceneActivation = false;
            _pendingLoad = operation;
            while (operation.progress < 0.9f)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }

        private async Task ActivateTargetAsync(
            SceneId target,
            CancellationToken cancellationToken)
        {
            if (_pendingLoad == null)
            {
                throw new InvalidOperationException("No target load is pending activation.");
            }

            _pendingLoad.allowSceneActivation = true;
            await AwaitOperation(_pendingLoad, cancellationToken);
            _pendingLoad = null;
            var scene = SceneManager.GetSceneByName(routes.GetSceneName(target));
            _targetEntry = FindEntry(scene, target);
            SceneManager.SetActiveScene(scene);
        }

        private async Task UnloadAsync(
            SceneTransitionRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var scene = SceneManager.GetSceneByName(routes.GetSceneName(request.Source));
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException(
                    "The source scene is not loaded: " + request.Source + ".");
            }

            var operation = SceneManager.UnloadSceneAsync(scene);
            if (operation == null)
            {
                throw new InvalidOperationException("The source scene could not be unloaded.");
            }

            stateStore.Commit(request);
            await AwaitOperation(operation, CancellationToken.None);
        }

        private async Task RollBackTargetAsync(SceneId target)
        {
            if (_pendingLoad != null)
            {
                _pendingLoad.allowSceneActivation = true;
                await AwaitOperation(_pendingLoad, CancellationToken.None);
                _pendingLoad = null;
            }

            _targetEntry = null;
            var scene = SceneManager.GetSceneByName(routes.GetSceneName(target));
            if (scene.IsValid() && scene.isLoaded)
            {
                var operation = SceneManager.UnloadSceneAsync(scene);
                if (operation != null)
                {
                    await AwaitOperation(operation, CancellationToken.None);
                }
            }
        }

        private void RestoreSource(SceneId source)
        {
            var scene = SceneManager.GetSceneByName(routes.GetSceneName(source));
            _activeEntry = FindEntry(scene, source);
            _activeEntry.SetCameraEnabled(true);
            SceneManager.SetActiveScene(scene);
        }

        private static SceneContentEntry FindEntry(Scene scene, SceneId expectedScene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException("The content scene is not loaded: " + expectedScene + ".");
            }

            var matches = new List<SceneContentEntry>();
            var roots = scene.GetRootGameObjects();
            for (var index = 0; index < roots.Length; index++)
            {
                matches.AddRange(roots[index].GetComponentsInChildren<SceneContentEntry>(true));
            }

            if (matches.Count != 1 || matches[0].SceneId != expectedScene)
            {
                throw new InvalidOperationException(
                    "A content scene must contain exactly one matching SceneContentEntry.");
            }

            return matches[0];
        }

        private static async Task AwaitOperation(
            AsyncOperation operation,
            CancellationToken cancellationToken)
        {
            while (!operation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }

        private static async Task YieldFrameAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var initialFrame = Time.frameCount;
            do
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
            while (Time.frameCount == initialFrame);
            cancellationToken.ThrowIfCancellationRequested();
        }

        private static async Task AwaitRevealPresentation(
            SceneContentEntry entry,
            TransitionCanvasPresenter transition,
            CancellationToken cancellationToken)
        {
            while ((entry != null && !entry.IsRevealPresentationComplete()) ||
                   (transition != null && !transition.IsComplete))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        private static async Task AwaitTransitionPresentation(
            TransitionCanvasPresenter transition,
            CancellationToken cancellationToken)
        {
            while (transition != null && !transition.IsComplete)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
