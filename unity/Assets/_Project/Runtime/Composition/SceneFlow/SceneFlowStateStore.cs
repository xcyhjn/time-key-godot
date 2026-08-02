using System;
using TimeKey.Application.SceneFlow;
using UnityEngine;

namespace TimeKey.Composition.SceneFlow
{
    [DisallowMultipleComponent]
    public sealed class SceneFlowStateStore : MonoBehaviour
    {
        private PendingRecord _pending;

        public SceneTransitionRequest LastRequest { get; private set; }

        public ISceneTransitionPayload LastPayload { get; private set; }

        public CombatLaunchPayload ActiveLaunch { get; private set; }

        public CombatOutcome LastOutcome { get; private set; }

        public OutOfBattleShellState OutOfBattleState { get; private set; }

        public CombatOutcomeApplyResult LastOutcomeApplyResult { get; private set; }

        public void Record(SceneTransitionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (_pending != null)
            {
                throw new InvalidOperationException(
                    "The prior scene-flow state record is still pending.");
            }

            var nextOutOfBattleState = OutOfBattleState?.Copy();
            var nextActiveLaunch = ActiveLaunch;
            var nextLastOutcome = LastOutcome;
            var nextOutcomeApplyResult = LastOutcomeApplyResult;

            if (request.Payload is CombatLaunchPayload launch)
            {
                RecordLaunch(launch, ref nextOutOfBattleState, out nextActiveLaunch);
            }
            else if (request.Payload is RunStartPayload start)
            {
                nextOutOfBattleState = new OutOfBattleShellState(start);
                nextActiveLaunch = null;
                nextLastOutcome = null;
                nextOutcomeApplyResult = null;
            }
            else if (request.Payload is CombatOutcome outcome)
            {
                RecordOutcome(
                    outcome,
                    nextActiveLaunch,
                    nextLastOutcome,
                    nextOutOfBattleState,
                    out nextLastOutcome,
                    out nextOutcomeApplyResult);
                nextActiveLaunch = null;
            }
            else if (request.Payload is EmptySceneTransitionPayload &&
                     request.Source == SceneId.GameOver &&
                     request.Target == SceneId.MainMenu)
            {
                nextOutOfBattleState = null;
                nextActiveLaunch = null;
                nextLastOutcome = null;
                nextOutcomeApplyResult = null;
            }

            _pending = new PendingRecord(
                request.Fingerprint,
                LastRequest,
                LastPayload,
                ActiveLaunch,
                LastOutcome,
                OutOfBattleState,
                LastOutcomeApplyResult);
            LastRequest = request;
            LastPayload = request.Payload;
            ActiveLaunch = nextActiveLaunch;
            LastOutcome = nextLastOutcome;
            OutOfBattleState = nextOutOfBattleState;
            LastOutcomeApplyResult = nextOutcomeApplyResult;
        }

        public void Commit(SceneTransitionRequest request)
        {
            RequirePending(request);
            _pending = null;
        }

        public void Rollback(SceneTransitionRequest request)
        {
            if (_pending == null)
            {
                return;
            }

            RequirePending(request);
            LastRequest = _pending.LastRequest;
            LastPayload = _pending.LastPayload;
            ActiveLaunch = _pending.ActiveLaunch;
            LastOutcome = _pending.LastOutcome;
            OutOfBattleState = _pending.OutOfBattleState;
            LastOutcomeApplyResult = _pending.LastOutcomeApplyResult;
            _pending = null;
        }

        private static void RecordLaunch(
            CombatLaunchPayload launch,
            ref OutOfBattleShellState outOfBattleState,
            out CombatLaunchPayload activeLaunch)
        {
            if (outOfBattleState == null)
            {
                outOfBattleState = new OutOfBattleShellState(launch);
            }
            else
            {
                var failure = outOfBattleState.TryBeginCombat(launch);
                if (failure != CombatLaunchApplyFailure.None)
                {
                    throw new InvalidOperationException(
                        "Combat launch could not be applied to the out-of-battle state: " +
                        failure + ".");
                }
            }

            activeLaunch = launch;
        }

        private static void RecordOutcome(
            CombatOutcome outcome,
            CombatLaunchPayload activeLaunch,
            CombatOutcome priorOutcome,
            OutOfBattleShellState outOfBattleState,
            out CombatOutcome lastOutcome,
            out CombatOutcomeApplyResult outcomeApplyResult)
        {
            var isExactReplay = activeLaunch == null && priorOutcome != null &&
                string.Equals(
                    priorOutcome.Fingerprint,
                    outcome.Fingerprint,
                    StringComparison.Ordinal);
            if (outOfBattleState == null ||
                (!isExactReplay &&
                 (activeLaunch == null ||
                  outcome.RunId != activeLaunch.RunId ||
                  outcome.RoomId != activeLaunch.RoomId ||
                  outcome.LaunchCorrelationId != activeLaunch.LaunchCorrelationId)))
            {
                throw new InvalidOperationException(
                    "Combat outcome does not match the active combat launch.");
            }

            outcomeApplyResult = outOfBattleState.TryApplyOutcome(outcome);
            if (!outcomeApplyResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Combat outcome could not be consumed: " +
                    outcomeApplyResult.Failure + ".");
            }

            lastOutcome = outcome;
        }

        private void RequirePending(SceneTransitionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (_pending == null || _pending.RequestFingerprint != request.Fingerprint)
            {
                throw new InvalidOperationException(
                    "The scene-flow state record does not match the pending request.");
            }
        }

        private sealed class PendingRecord
        {
            public PendingRecord(
                string requestFingerprint,
                SceneTransitionRequest lastRequest,
                ISceneTransitionPayload lastPayload,
                CombatLaunchPayload activeLaunch,
                CombatOutcome lastOutcome,
                OutOfBattleShellState outOfBattleState,
                CombatOutcomeApplyResult lastOutcomeApplyResult)
            {
                RequestFingerprint = requestFingerprint;
                LastRequest = lastRequest;
                LastPayload = lastPayload;
                ActiveLaunch = activeLaunch;
                LastOutcome = lastOutcome;
                OutOfBattleState = outOfBattleState;
                LastOutcomeApplyResult = lastOutcomeApplyResult;
            }

            public string RequestFingerprint { get; }

            public SceneTransitionRequest LastRequest { get; }

            public ISceneTransitionPayload LastPayload { get; }

            public CombatLaunchPayload ActiveLaunch { get; }

            public CombatOutcome LastOutcome { get; }

            public OutOfBattleShellState OutOfBattleState { get; }

            public CombatOutcomeApplyResult LastOutcomeApplyResult { get; }
        }
    }
}
