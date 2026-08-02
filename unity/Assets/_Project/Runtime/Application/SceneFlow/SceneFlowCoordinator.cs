using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TimeKey.Application.SceneFlow
{
    public sealed class SceneFlowCoordinator
    {
        private static readonly SceneTransitionPhase[] SuccessPhases =
        {
            SceneTransitionPhase.InputLocked,
            SceneTransitionPhase.Covering,
            SceneTransitionPhase.LoadingTarget,
            SceneTransitionPhase.ActivatingTarget,
            SceneTransitionPhase.BindingPayload,
            SceneTransitionPhase.WaitingForFirstRenderableFrame,
            SceneTransitionPhase.UnloadingSource,
            SceneTransitionPhase.Revealing,
            SceneTransitionPhase.InputUnlocked
        };

        private readonly object _sync = new object();
        private readonly ISceneFlowEffects _effects;
        private readonly Dictionary<long, RequestRecord> _requests =
            new Dictionary<long, RequestRecord>();
        private long _latestSequence;
        private RequestRecord _active;

        public SceneFlowCoordinator(SceneId initialScene, ISceneFlowEffects effects)
        {
            if (initialScene == SceneId.None)
            {
                throw new ArgumentOutOfRangeException(nameof(initialScene));
            }

            CurrentScene = initialScene;
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
        }

        public SceneId CurrentScene { get; private set; }

        public SceneTransitionPhase CurrentPhase { get; private set; } =
            SceneTransitionPhase.Idle;

        public Task<SceneTransitionResult> TransitionAsync(
            SceneTransitionRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = ValidateIdentity(request);
            if (validation != SceneTransitionFailure.None)
            {
                return Task.FromResult(Failed(request, validation, SceneTransitionPhase.Idle));
            }

            RequestRecord record;
            lock (_sync)
            {
                if (_requests.TryGetValue(request.Sequence, out var prior))
                {
                    return prior.Fingerprint == request.Fingerprint
                        ? prior.Completion.Task
                        : Task.FromResult(Failed(
                            request,
                            SceneTransitionFailure.SequenceConflict,
                            SceneTransitionPhase.Idle));
                }

                if (request.Sequence < _latestSequence)
                {
                    return Task.FromResult(Failed(
                        request,
                        SceneTransitionFailure.Stale,
                        SceneTransitionPhase.Idle));
                }

                if (_active != null)
                {
                    return Task.FromResult(Failed(
                        request,
                        SceneTransitionFailure.Busy,
                        CurrentPhase));
                }

                validation = ValidateRoute(request);
                if (validation != SceneTransitionFailure.None)
                {
                    return Task.FromResult(Failed(
                        request,
                        validation,
                        SceneTransitionPhase.Idle));
                }

                record = new RequestRecord(request.Fingerprint);
                _requests.Add(request.Sequence, record);
                _latestSequence = request.Sequence;
                _active = record;
            }

            RunAsync(record, request, cancellationToken);
            return record.Completion.Task;
        }

        private async void RunAsync(
            RequestRecord record,
            SceneTransitionRequest request,
            CancellationToken cancellationToken)
        {
            var history = new List<SceneTransitionPhase>();
            var isInputLocked = false;
            var sourceCommitted = false;
            SceneTransitionResult result;
            try
            {
                for (var index = 0; index < SuccessPhases.Length; index++)
                {
                    var phase = SuccessPhases[index];
                    CurrentPhase = phase;
                    history.Add(phase);
                    if (phase == SceneTransitionPhase.InputLocked)
                    {
                        isInputLocked = true;
                    }

                    var effect = await _effects.ExecuteAsync(
                        phase,
                        request,
                        cancellationToken);
                    if (effect == null || !effect.Succeeded)
                    {
                        result = await RecoverAsync(
                            request,
                            phase,
                            effect == null ? "Scene effect returned no result." : effect.Message,
                            history,
                            isInputLocked,
                            sourceCommitted,
                            CancellationToken.None);
                        Complete(record, result);
                        return;
                    }

                    if (phase == SceneTransitionPhase.InputUnlocked)
                    {
                        isInputLocked = false;
                    }

                    if (phase == SceneTransitionPhase.UnloadingSource)
                    {
                        sourceCommitted = true;
                        CurrentScene = request.Target;
                    }
                }

                result = new SceneTransitionResult(
                    request,
                    SceneTransitionFailure.None,
                    SceneTransitionPhase.Idle,
                    CurrentScene,
                    false,
                    null,
                    history);
            }
            catch (OperationCanceledException)
            {
                result = await RecoverAsync(
                    request,
                    CurrentPhase,
                    "Scene transition was cancelled.",
                    history,
                    isInputLocked,
                    sourceCommitted,
                    CancellationToken.None,
                    SceneTransitionFailure.Cancelled);
            }
            catch (Exception exception)
            {
                result = await RecoverAsync(
                    request,
                    CurrentPhase,
                    exception.Message,
                    history,
                    isInputLocked,
                    sourceCommitted,
                    CancellationToken.None,
                    SceneTransitionFailure.Unexpected);
            }

            Complete(record, result);
        }

        private async Task<SceneTransitionResult> RecoverAsync(
            SceneTransitionRequest request,
            SceneTransitionPhase failedPhase,
            string message,
            List<SceneTransitionPhase> history,
            bool isInputLocked,
            bool sourceCommitted,
            CancellationToken cancellationToken,
            SceneTransitionFailure failure = SceneTransitionFailure.EffectFailed)
        {
            var recoveryPhases = sourceCommitted
                ? new[]
                {
                    SceneTransitionPhase.Revealing,
                    SceneTransitionPhase.InputUnlocked
                }
                : new[]
                {
                    SceneTransitionPhase.RollingBackTarget,
                    SceneTransitionPhase.RestoringSource,
                    SceneTransitionPhase.Revealing,
                    SceneTransitionPhase.InputUnlocked
                };

            for (var index = 0; index < recoveryPhases.Length; index++)
            {
                CurrentPhase = recoveryPhases[index];
                history.Add(CurrentPhase);
                try
                {
                    var recovery = await _effects.ExecuteAsync(
                        CurrentPhase,
                        request,
                        cancellationToken);
                    if (CurrentPhase == SceneTransitionPhase.InputUnlocked &&
                        recovery != null && recovery.Succeeded)
                    {
                        isInputLocked = false;
                    }
                }
                catch
                {
                    // The original failure remains authoritative; final lock state records recovery.
                }
            }

            return new SceneTransitionResult(
                request,
                failure,
                failedPhase,
                sourceCommitted ? request.Target : request.Source,
                isInputLocked,
                message,
                history);
        }

        private void Complete(RequestRecord record, SceneTransitionResult result)
        {
            CurrentPhase = SceneTransitionPhase.Idle;
            lock (_sync)
            {
                if (ReferenceEquals(_active, record))
                {
                    _active = null;
                }
            }

            record.Completion.TrySetResult(result);
        }

        private static SceneTransitionFailure ValidateIdentity(SceneTransitionRequest request)
        {
            if (request == null)
            {
                return SceneTransitionFailure.InvalidRequest;
            }

            if (request.Sequence <= 0)
            {
                return SceneTransitionFailure.InvalidSequence;
            }

            if (string.IsNullOrWhiteSpace(request.CorrelationId))
            {
                return SceneTransitionFailure.InvalidCorrelation;
            }

            return SceneTransitionFailure.None;
        }

        private SceneTransitionFailure ValidateRoute(SceneTransitionRequest request)
        {
            if (request.Source == SceneId.None || request.Target == SceneId.None ||
                request.Source == request.Target || request.Source != CurrentScene)
            {
                return SceneTransitionFailure.InvalidRoute;
            }

            if (request.Payload == null || request.Payload.TargetScene != request.Target)
            {
                return SceneTransitionFailure.InvalidPayload;
            }

            if (!IsAllowedRouteAndPayload(request))
            {
                return SceneTransitionFailure.InvalidPayload;
            }

            return SceneTransitionFailure.None;
        }

        private static bool IsAllowedRouteAndPayload(SceneTransitionRequest request)
        {
            if (request.Payload is EmptySceneTransitionPayload)
            {
                return (request.Source == SceneId.GameStart && request.Target == SceneId.MainMenu) ||
                    (request.Source == SceneId.GameOver && request.Target == SceneId.MainMenu);
            }

            if (request.Payload is RunStartPayload)
            {
                return request.Source == SceneId.MainMenu &&
                    request.Target == SceneId.OutOfBattleShell;
            }

            if (request.Payload is CombatLaunchPayload)
            {
                return request.Source == SceneId.OutOfBattleShell &&
                    request.Target == SceneId.Combat;
            }

            if (request.Payload is CombatOutcome outcome)
            {
                return request.Source == SceneId.Combat && outcome.TargetScene == request.Target;
            }

            return false;
        }

        private SceneTransitionResult Failed(
            SceneTransitionRequest request,
            SceneTransitionFailure failure,
            SceneTransitionPhase failedPhase)
        {
            return new SceneTransitionResult(
                request,
                failure,
                failedPhase,
                CurrentScene,
                CurrentPhase != SceneTransitionPhase.Idle,
                failure.ToString(),
                Array.Empty<SceneTransitionPhase>());
        }

        private sealed class RequestRecord
        {
            public RequestRecord(string fingerprint)
            {
                Fingerprint = fingerprint;
                Completion = new TaskCompletionSource<SceneTransitionResult>();
            }

            public string Fingerprint { get; }

            public TaskCompletionSource<SceneTransitionResult> Completion { get; }
        }
    }
}
