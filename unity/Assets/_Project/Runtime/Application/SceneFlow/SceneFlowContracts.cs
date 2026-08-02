using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace TimeKey.Application.SceneFlow
{
    public enum SceneId
    {
        None,
        GameStart,
        MainMenu,
        OutOfBattleShell,
        Combat,
        GameOver
    }

    public enum SceneTransitionPhase
    {
        Idle,
        InputLocked,
        Covering,
        LoadingTarget,
        ActivatingTarget,
        BindingPayload,
        WaitingForFirstRenderableFrame,
        UnloadingSource,
        Revealing,
        InputUnlocked,
        RollingBackTarget,
        RestoringSource
    }

    public enum SceneTransitionFailure
    {
        None,
        InvalidRequest,
        InvalidSequence,
        InvalidCorrelation,
        InvalidRoute,
        InvalidPayload,
        Busy,
        SequenceConflict,
        Stale,
        EffectFailed,
        Cancelled,
        Unexpected
    }

    public interface ISceneTransitionPayload
    {
        SceneId TargetScene { get; }

        string Fingerprint { get; }
    }

    public interface ISceneRevealPresentation
    {
        bool IsComplete { get; }

        void PlayReveal();

        void CompleteImmediately();
    }

    public static class SceneInputLockState
    {
        private static bool _explicitLock;
        private static int _leaseCount;

        public static bool IsLocked => _explicitLock || _leaseCount > 0;

        public static void SetLocked(bool value)
        {
            _explicitLock = value;
        }

        public static IDisposable Acquire()
        {
            _leaseCount++;
            return new LockLease();
        }

        private static void Release()
        {
            if (_leaseCount > 0)
            {
                _leaseCount--;
            }
        }

        private sealed class LockLease : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Release();
            }
        }
    }

    public sealed class EmptySceneTransitionPayload : ISceneTransitionPayload
    {
        public EmptySceneTransitionPayload(SceneId targetScene)
        {
            TargetScene = targetScene;
        }

        public SceneId TargetScene { get; }

        public string Fingerprint => "empty:" + TargetScene;
    }

    public sealed class SceneTransitionRequest
    {
        public SceneTransitionRequest(
            long sequence,
            string correlationId,
            SceneId source,
            SceneId target,
            ISceneTransitionPayload payload)
        {
            Sequence = sequence;
            CorrelationId = correlationId;
            Source = source;
            Target = target;
            Payload = payload;
        }

        public long Sequence { get; }

        public string CorrelationId { get; }

        public SceneId Source { get; }

        public SceneId Target { get; }

        public ISceneTransitionPayload Payload { get; }

        public string Fingerprint =>
            Source + "|" + Target + "|" + CorrelationId + "|" +
            (Payload == null ? "null" : Payload.Fingerprint);
    }

    public sealed class SceneFlowEffectResult
    {
        private SceneFlowEffectResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public bool Succeeded { get; }

        public string Message { get; }

        public static SceneFlowEffectResult Success()
        {
            return new SceneFlowEffectResult(true, null);
        }

        public static SceneFlowEffectResult Failed(string message)
        {
            return new SceneFlowEffectResult(
                false,
                string.IsNullOrWhiteSpace(message) ? "Scene effect failed." : message);
        }
    }

    public interface ISceneFlowEffects
    {
        Task<SceneFlowEffectResult> ExecuteAsync(
            SceneTransitionPhase phase,
            SceneTransitionRequest request,
            CancellationToken cancellationToken);
    }

    public sealed class SceneTransitionResult
    {
        private readonly ReadOnlyCollection<SceneTransitionPhase> _phaseHistory;

        internal SceneTransitionResult(
            SceneTransitionRequest request,
            SceneTransitionFailure failure,
            SceneTransitionPhase failedPhase,
            SceneId activeScene,
            bool isInputLocked,
            string message,
            IReadOnlyList<SceneTransitionPhase> phaseHistory)
        {
            Request = request;
            Failure = failure;
            FailedPhase = failedPhase;
            ActiveScene = activeScene;
            IsInputLocked = isInputLocked;
            Message = message;
            _phaseHistory = Copy(phaseHistory);
        }

        public bool Succeeded => Failure == SceneTransitionFailure.None;

        public SceneTransitionRequest Request { get; }

        public SceneTransitionFailure Failure { get; }

        public SceneTransitionPhase FailedPhase { get; }

        public SceneId ActiveScene { get; }

        public bool IsInputLocked { get; }

        public string Message { get; }

        public IReadOnlyList<SceneTransitionPhase> PhaseHistory => _phaseHistory;

        private static ReadOnlyCollection<SceneTransitionPhase> Copy(
            IReadOnlyList<SceneTransitionPhase> source)
        {
            return new ReadOnlyCollection<SceneTransitionPhase>(
                source == null
                    ? new List<SceneTransitionPhase>()
                    : new List<SceneTransitionPhase>(source));
        }
    }
}
