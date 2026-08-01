using System;
using System.Collections.Generic;

namespace TimeKey.Domain
{
    public sealed class TurnLifecycleRunner
    {
        private readonly ITimelineActionProcessor _timelineActionProcessor;
        private readonly IBuildingBehaviorProcessor _buildingBehaviorProcessor;
        private readonly ITimelineClearingProcessor _timelineClearingProcessor;
        private readonly ITurnStartStatusProcessor _turnStartStatusProcessor;
        private readonly INextTurnHook _nextTurnHook;
        private readonly IEnemyIntentRefresher _enemyIntentRefresher;

        private TurnLifecyclePhase _phase = TurnLifecyclePhase.PlayerReady;
        private bool _isRunning;
        private bool _isFaulted;
        private long _sequence;

        public TurnLifecycleRunner(
            ITimelineActionProcessor timelineActionProcessor = null,
            IBuildingBehaviorProcessor buildingBehaviorProcessor = null,
            ITimelineClearingProcessor timelineClearingProcessor = null,
            ITurnStartStatusProcessor turnStartStatusProcessor = null,
            INextTurnHook nextTurnHook = null,
            IEnemyIntentRefresher enemyIntentRefresher = null)
        {
            var noOp = NoOpTurnLifecyclePorts.Instance;
            _timelineActionProcessor = timelineActionProcessor ?? noOp;
            _buildingBehaviorProcessor = buildingBehaviorProcessor ?? noOp;
            _timelineClearingProcessor = timelineClearingProcessor ?? noOp;
            _turnStartStatusProcessor = turnStartStatusProcessor ?? noOp;
            _nextTurnHook = nextTurnHook ?? noOp;
            _enemyIntentRefresher = enemyIntentRefresher ?? noOp;
        }

        public TurnLifecyclePhase CurrentPhase => _phase;

        public bool IsInputLocked => _phase != TurnLifecyclePhase.PlayerReady;

        public bool IsRunning => _isRunning;

        public bool IsFaulted => _isFaulted;

        public long Sequence => _sequence;

        public TurnLifecycleResult LastResult { get; private set; }

        public TurnLifecycleResult Run(TurnLifecycleRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (_isRunning)
            {
                return Reject(request.Kind, TurnLifecycleFailure.Busy, "The turn lifecycle is already running.");
            }

            if (_isFaulted || _phase != TurnLifecyclePhase.PlayerReady)
            {
                return Reject(
                    request.Kind,
                    TurnLifecycleFailure.Faulted,
                    "The turn lifecycle is faulted and cannot start another cycle.");
            }

            IReadOnlyList<TimelineActionPlanEntry> orderedActions =
                Array.Empty<TimelineActionPlanEntry>();
            if (request.Kind == TurnLifecycleRequestKind.EndTurn &&
                !request.TimelinePlan.TryGetResolutionOrder(
                    out orderedActions,
                    out var planFailure,
                    out var planFailureReason))
            {
                return CompleteResult(
                    request.Kind,
                    _sequence,
                    TurnLifecycleFailure.InvalidTimelinePlan,
                    planFailure,
                    planFailureReason,
                    _phase,
                    _phase,
                    Array.Empty<TurnLifecyclePhase>(),
                    Array.Empty<TimelineActionIdentity>());
            }

            var phaseBefore = _phase;
            var phaseHistory = new List<TurnLifecyclePhase>();
            var completedActionIds = new List<TimelineActionIdentity>();
            _isRunning = true;
            _sequence = checked(_sequence + 1);
            var context = new TurnLifecycleContext(_sequence, request.Kind);

            try
            {
                if (request.Kind == TurnLifecycleRequestKind.EndTurn)
                {
                    Enter(TurnLifecyclePhase.EndTurnRequested, phaseHistory);
                    Enter(TurnLifecyclePhase.ResolvingTimeline, phaseHistory);
                    for (var index = 0; index < orderedActions.Count; index++)
                    {
                        var action = orderedActions[index];
                        var actionResult = _timelineActionProcessor.Resolve(context, action);
                        if (!Succeeded(actionResult))
                        {
                            return Fail(
                                request.Kind,
                                TurnLifecycleFailure.TimelineActionFailed,
                                FailureReason(actionResult),
                                phaseBefore,
                                phaseHistory,
                                completedActionIds);
                        }

                        completedActionIds.Add(action.ActionId);
                    }

                    Enter(TurnLifecyclePhase.RunningBuildingBehaviors, phaseHistory);
                    var buildingResult = _buildingBehaviorProcessor.Run(context);
                    if (!Succeeded(buildingResult))
                    {
                        return Fail(
                            request.Kind,
                            TurnLifecycleFailure.BuildingBehaviorFailed,
                            FailureReason(buildingResult),
                            phaseBefore,
                            phaseHistory,
                            completedActionIds);
                    }

                    Enter(TurnLifecyclePhase.ClearingTimeline, phaseHistory);
                    var clearingResult = _timelineClearingProcessor.Clear(context);
                    if (!Succeeded(clearingResult))
                    {
                        return Fail(
                            request.Kind,
                            TurnLifecycleFailure.TimelineClearingFailed,
                            FailureReason(clearingResult),
                            phaseBefore,
                            phaseHistory,
                            completedActionIds);
                    }
                }

                Enter(TurnLifecyclePhase.ProcessingTurnStartStatuses, phaseHistory);
                var statusResult = _turnStartStatusProcessor.Process(context);
                if (!Succeeded(statusResult))
                {
                    return Fail(
                        request.Kind,
                        TurnLifecycleFailure.TurnStartStatusFailed,
                        FailureReason(statusResult),
                        phaseBefore,
                        phaseHistory,
                        completedActionIds);
                }

                var hookResult = _nextTurnHook.Run(context);
                if (!Succeeded(hookResult))
                {
                    return Fail(
                        request.Kind,
                        TurnLifecycleFailure.ReservedHookFailed,
                        FailureReason(hookResult),
                        phaseBefore,
                        phaseHistory,
                        completedActionIds);
                }

                Enter(TurnLifecyclePhase.RefreshingEnemyIntents, phaseHistory);
                var intentResult = _enemyIntentRefresher.Refresh(context);
                if (!Succeeded(intentResult))
                {
                    return Fail(
                        request.Kind,
                        TurnLifecycleFailure.EnemyIntentRefreshFailed,
                        FailureReason(intentResult),
                        phaseBefore,
                        phaseHistory,
                        completedActionIds);
                }

                Enter(TurnLifecyclePhase.PlayerReady, phaseHistory);
                _isFaulted = false;
                return CompleteResult(
                    request.Kind,
                    _sequence,
                    TurnLifecycleFailure.None,
                    TimelinePlanFailure.None,
                    null,
                    phaseBefore,
                    _phase,
                    phaseHistory,
                    completedActionIds);
            }
            finally
            {
                _isRunning = false;
            }
        }

        private TurnLifecycleResult Fail(
            TurnLifecycleRequestKind requestKind,
            TurnLifecycleFailure failure,
            string failureReason,
            TurnLifecyclePhase phaseBefore,
            IReadOnlyList<TurnLifecyclePhase> phaseHistory,
            IReadOnlyList<TimelineActionIdentity> completedActionIds)
        {
            _isFaulted = true;
            return CompleteResult(
                requestKind,
                _sequence,
                failure,
                TimelinePlanFailure.None,
                failureReason,
                phaseBefore,
                _phase,
                phaseHistory,
                completedActionIds);
        }

        private TurnLifecycleResult Reject(
            TurnLifecycleRequestKind requestKind,
            TurnLifecycleFailure failure,
            string failureReason)
        {
            return CompleteResult(
                requestKind,
                _sequence,
                failure,
                TimelinePlanFailure.None,
                failureReason,
                _phase,
                _phase,
                Array.Empty<TurnLifecyclePhase>(),
                Array.Empty<TimelineActionIdentity>());
        }

        private TurnLifecycleResult CompleteResult(
            TurnLifecycleRequestKind requestKind,
            long sequence,
            TurnLifecycleFailure failure,
            TimelinePlanFailure planFailure,
            string failureReason,
            TurnLifecyclePhase phaseBefore,
            TurnLifecyclePhase phaseAfter,
            IReadOnlyList<TurnLifecyclePhase> phaseHistory,
            IReadOnlyList<TimelineActionIdentity> completedActionIds)
        {
            LastResult = new TurnLifecycleResult(
                requestKind,
                sequence,
                failure,
                planFailure,
                failureReason,
                phaseBefore,
                phaseAfter,
                phaseHistory,
                completedActionIds);
            return LastResult;
        }

        private void Enter(TurnLifecyclePhase phase, ICollection<TurnLifecyclePhase> phaseHistory)
        {
            _phase = phase;
            phaseHistory.Add(phase);
        }

        private static bool Succeeded(TurnLifecycleStepResult result)
        {
            return result != null && result.Succeeded;
        }

        private static string FailureReason(TurnLifecycleStepResult result)
        {
            return result == null ? "A lifecycle processor returned no result." : result.FailureReason;
        }
    }
}
