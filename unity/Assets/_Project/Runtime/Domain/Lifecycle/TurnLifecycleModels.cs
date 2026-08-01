using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain
{
    public enum TurnLifecyclePhase
    {
        PlayerReady,
        EndTurnRequested,
        ResolvingTimeline,
        RunningBuildingBehaviors,
        ClearingTimeline,
        ProcessingTurnStartStatuses,
        RefreshingEnemyIntents
    }

    public enum TurnLifecycleRequestKind
    {
        InitialStart,
        EndTurn
    }

    public enum TurnLifecycleFailure
    {
        None,
        Busy,
        Faulted,
        InvalidTimelinePlan,
        TimelineActionFailed,
        BuildingBehaviorFailed,
        TimelineClearingFailed,
        TurnStartStatusFailed,
        ReservedHookFailed,
        EnemyIntentRefreshFailed
    }

    public enum TimelinePlanFailure
    {
        None,
        InvalidActionIdentity,
        DuplicateActionIdentity,
        DuplicateOccupiedCell,
        OverlappingOccupiedCell,
        OutOfBounds
    }

    public sealed class TimelineActionPlanEntry
    {
        private readonly ReadOnlyCollection<TimelineCell> _occupiedCells;

        public TimelineActionPlanEntry(
            TimelineActionIdentity actionId,
            TimelineActorKind actorKind,
            string displayStableId,
            TimelineCell origin,
            IReadOnlyList<TimelineCell> occupiedCells)
        {
            if (!actionId.IsValid)
            {
                throw new ArgumentException("A valid action identity is required.", nameof(actionId));
            }

            if (string.IsNullOrWhiteSpace(displayStableId))
            {
                throw new ArgumentException("A display stable ID is required.", nameof(displayStableId));
            }

            if (occupiedCells == null || occupiedCells.Count == 0)
            {
                throw new ArgumentException("At least one occupied timeline cell is required.", nameof(occupiedCells));
            }

            var copiedCells = new List<TimelineCell>(occupiedCells.Count);
            for (var index = 0; index < occupiedCells.Count; index++)
            {
                copiedCells.Add(occupiedCells[index]);
            }

            ActionId = actionId;
            ActorKind = actorKind;
            DisplayStableId = displayStableId;
            Origin = origin;
            _occupiedCells = new ReadOnlyCollection<TimelineCell>(copiedCells);
        }

        public TimelineActionIdentity ActionId { get; }

        public TimelineActorKind ActorKind { get; }

        public string DisplayStableId { get; }

        public TimelineCell Origin { get; }

        public IReadOnlyList<TimelineCell> OccupiedCells => _occupiedCells;
    }

    public sealed class TimelineActionPlan
    {
        private readonly ReadOnlyCollection<TimelineActionPlanEntry> _actions;

        public TimelineActionPlan(
            IReadOnlyList<TimelineActionPlanEntry> actions,
            int width = TimelineGrid.DefaultWidth,
            int height = TimelineGrid.DefaultHeight)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            var copiedActions = new List<TimelineActionPlanEntry>(actions.Count);
            for (var index = 0; index < actions.Count; index++)
            {
                copiedActions.Add(actions[index] ??
                    throw new ArgumentException("Timeline plans cannot contain null actions.", nameof(actions)));
            }

            Width = width;
            Height = height;
            _actions = new ReadOnlyCollection<TimelineActionPlanEntry>(copiedActions);
        }

        public int Width { get; }

        public int Height { get; }

        public IReadOnlyList<TimelineActionPlanEntry> Actions => _actions;

        public bool TryGetResolutionOrder(
            out IReadOnlyList<TimelineActionPlanEntry> orderedActions,
            out TimelinePlanFailure failure,
            out string failureReason)
        {
            var identities = new HashSet<TimelineActionIdentity>();
            var occupiedCells = new Dictionary<TimelineCell, TimelineActionIdentity>();
            var result = new List<TimelineActionPlanEntry>(_actions.Count);

            for (var actionIndex = 0; actionIndex < _actions.Count; actionIndex++)
            {
                var action = _actions[actionIndex];
                if (!action.ActionId.IsValid)
                {
                    return Fail(
                        TimelinePlanFailure.InvalidActionIdentity,
                        "Every timeline action must have a valid identity.",
                        out orderedActions,
                        out failure,
                        out failureReason);
                }

                if (!identities.Add(action.ActionId))
                {
                    return Fail(
                        TimelinePlanFailure.DuplicateActionIdentity,
                        "Timeline action identity is duplicated: " + action.ActionId + ".",
                        out orderedActions,
                        out failure,
                        out failureReason);
                }

                if (!Contains(action.Origin))
                {
                    return Fail(
                        TimelinePlanFailure.OutOfBounds,
                        "Timeline action origin is outside the plan bounds: " + action.ActionId + ".",
                        out orderedActions,
                        out failure,
                        out failureReason);
                }

                var actionCells = new HashSet<TimelineCell>();
                for (var cellIndex = 0; cellIndex < action.OccupiedCells.Count; cellIndex++)
                {
                    var cell = action.OccupiedCells[cellIndex];
                    if (!actionCells.Add(cell))
                    {
                        return Fail(
                            TimelinePlanFailure.DuplicateOccupiedCell,
                            "An action contains the same occupied cell more than once: " + action.ActionId + ".",
                            out orderedActions,
                            out failure,
                            out failureReason);
                    }

                    if (!Contains(cell))
                    {
                        return Fail(
                            TimelinePlanFailure.OutOfBounds,
                            "An occupied timeline cell is outside the plan bounds: " + action.ActionId + ".",
                            out orderedActions,
                            out failure,
                            out failureReason);
                    }

                    if (occupiedCells.ContainsKey(cell))
                    {
                        return Fail(
                            TimelinePlanFailure.OverlappingOccupiedCell,
                            "Two timeline actions occupy the same cell.",
                            out orderedActions,
                            out failure,
                            out failureReason);
                    }

                    occupiedCells.Add(cell, action.ActionId);
                }

                result.Add(action);
            }

            result.Sort(CompareByFirstOccupiedCell);
            orderedActions = new ReadOnlyCollection<TimelineActionPlanEntry>(result);
            failure = TimelinePlanFailure.None;
            failureReason = null;
            return true;
        }

        private bool Contains(TimelineCell cell)
        {
            return cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
        }

        private static int CompareByFirstOccupiedCell(
            TimelineActionPlanEntry left,
            TimelineActionPlanEntry right)
        {
            var leftFirst = FindFirstOccupiedCell(left.OccupiedCells);
            var rightFirst = FindFirstOccupiedCell(right.OccupiedCells);
            var xComparison = leftFirst.X.CompareTo(rightFirst.X);
            if (xComparison != 0)
            {
                return xComparison;
            }

            var yComparison = leftFirst.Y.CompareTo(rightFirst.Y);
            return yComparison != 0
                ? yComparison
                : StringComparer.Ordinal.Compare(left.ActionId.Value, right.ActionId.Value);
        }

        private static TimelineCell FindFirstOccupiedCell(IReadOnlyList<TimelineCell> cells)
        {
            var result = cells[0];
            for (var index = 1; index < cells.Count; index++)
            {
                var candidate = cells[index];
                if (candidate.X < result.X ||
                    (candidate.X == result.X && candidate.Y < result.Y))
                {
                    result = candidate;
                }
            }

            return result;
        }

        private static bool Fail(
            TimelinePlanFailure planFailure,
            string reason,
            out IReadOnlyList<TimelineActionPlanEntry> orderedActions,
            out TimelinePlanFailure failure,
            out string failureReason)
        {
            orderedActions = Array.Empty<TimelineActionPlanEntry>();
            failure = planFailure;
            failureReason = reason;
            return false;
        }
    }

    public sealed class TurnLifecycleRequest
    {
        private TurnLifecycleRequest(TurnLifecycleRequestKind kind, TimelineActionPlan timelinePlan)
        {
            Kind = kind;
            TimelinePlan = timelinePlan;
        }

        public TurnLifecycleRequestKind Kind { get; }

        public TimelineActionPlan TimelinePlan { get; }

        public static TurnLifecycleRequest InitialStart()
        {
            return new TurnLifecycleRequest(TurnLifecycleRequestKind.InitialStart, null);
        }

        public static TurnLifecycleRequest EndTurn(TimelineActionPlan timelinePlan)
        {
            return new TurnLifecycleRequest(
                TurnLifecycleRequestKind.EndTurn,
                timelinePlan ?? throw new ArgumentNullException(nameof(timelinePlan)));
        }
    }

    public sealed class TurnLifecycleContext
    {
        internal TurnLifecycleContext(long sequence, TurnLifecycleRequestKind requestKind)
        {
            Sequence = sequence;
            RequestKind = requestKind;
        }

        public long Sequence { get; }

        public TurnLifecycleRequestKind RequestKind { get; }
    }

    public sealed class TurnLifecycleStepResult
    {
        private TurnLifecycleStepResult(bool succeeded, string failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static TurnLifecycleStepResult Successful { get; } =
            new TurnLifecycleStepResult(true, null);

        public bool Succeeded { get; }

        public string FailureReason { get; }

        public static TurnLifecycleStepResult Failed(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("A processor failure reason is required.", nameof(reason));
            }

            return new TurnLifecycleStepResult(false, reason);
        }
    }

    public sealed class TurnLifecycleResult
    {
        private readonly ReadOnlyCollection<TurnLifecyclePhase> _phaseHistory;
        private readonly ReadOnlyCollection<TimelineActionIdentity> _completedActionIds;

        internal TurnLifecycleResult(
            TurnLifecycleRequestKind requestKind,
            long sequence,
            TurnLifecycleFailure failure,
            TimelinePlanFailure timelinePlanFailure,
            string failureReason,
            TurnLifecyclePhase phaseBefore,
            TurnLifecyclePhase phaseAfter,
            IReadOnlyList<TurnLifecyclePhase> phaseHistory,
            IReadOnlyList<TimelineActionIdentity> completedActionIds)
        {
            RequestKind = requestKind;
            Sequence = sequence;
            Failure = failure;
            TimelinePlanFailure = timelinePlanFailure;
            FailureReason = failureReason;
            PhaseBefore = phaseBefore;
            PhaseAfter = phaseAfter;
            _phaseHistory = Copy(phaseHistory);
            _completedActionIds = Copy(completedActionIds);
        }

        public bool Succeeded => Failure == TurnLifecycleFailure.None;

        public TurnLifecycleRequestKind RequestKind { get; }

        public long Sequence { get; }

        public TurnLifecycleFailure Failure { get; }

        public TimelinePlanFailure TimelinePlanFailure { get; }

        public string FailureReason { get; }

        public TurnLifecyclePhase PhaseBefore { get; }

        public TurnLifecyclePhase PhaseAfter { get; }

        public bool IsInputLockedAfter => PhaseAfter != TurnLifecyclePhase.PlayerReady;

        public IReadOnlyList<TurnLifecyclePhase> PhaseHistory => _phaseHistory;

        public IReadOnlyList<TimelineActionIdentity> CompletedActionIds => _completedActionIds;

        private static ReadOnlyCollection<T> Copy<T>(IReadOnlyList<T> source)
        {
            var values = new List<T>(source.Count);
            for (var index = 0; index < source.Count; index++)
            {
                values.Add(source[index]);
            }

            return new ReadOnlyCollection<T>(values);
        }
    }
}
