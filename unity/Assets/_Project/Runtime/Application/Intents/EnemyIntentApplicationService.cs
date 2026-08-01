using System;
using System.Collections.Generic;
using TimeKey.Domain;
using TimeKey.Domain.Intents;

namespace TimeKey.Application.Intents
{
    public sealed class EnemyIntentApplicationService
    {
        private readonly EnemyIntentScheduler _scheduler;
        private readonly EnemyIntentResolver _resolver;

        public EnemyIntentApplicationService()
            : this(new EnemyIntentScheduler(), new EnemyIntentResolver())
        {
        }

        public EnemyIntentApplicationService(
            EnemyIntentScheduler scheduler,
            EnemyIntentResolver resolver)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public EnemyIntentGenerationResult Generate(
            EnemyIntentWorldSnapshot world,
            long lifecycleSequence,
            int startingActionOrdinal,
            int seed,
            IReadOnlyList<TimelineCell> occupiedCells = null,
            int width = TimelineGrid.DefaultWidth,
            int height = TimelineGrid.DefaultHeight,
            int maximumIntentCount = EnemyIntentScheduler.DefaultMaximumIntentCount)
        {
            return _scheduler.Generate(
                world,
                lifecycleSequence,
                startingActionOrdinal,
                seed,
                occupiedCells,
                width,
                height,
                maximumIntentCount: maximumIntentCount);
        }

        public EnemyIntentResolveResult Resolve(
            EnemyIntentScheduledAction scheduled,
            EnemyIntentWorldSnapshot currentWorld)
        {
            return _resolver.Resolve(scheduled, currentWorld);
        }

        public TimelineActionPresentationSnapshot CreateScheduledSnapshot(
            EnemyIntentScheduledAction scheduled)
        {
            if (scheduled == null)
            {
                throw new ArgumentNullException(nameof(scheduled));
            }

            return CreateSnapshot(
                scheduled,
                TimelineActionValidity.Unsupported,
                scheduled.Candidate.Source.Effect.SourceCommand == null
                    ? TimelineActionInvalidReason.UnsupportedSourceCommand
                    : TimelineActionInvalidReason.UnsupportedEffect,
                TimelineActionResolveState.Scheduled);
        }

        public TimelineActionPresentationSnapshot CreateResolvedSnapshot(
            EnemyIntentScheduledAction scheduled,
            EnemyIntentResolveResult result)
        {
            if (scheduled == null)
            {
                throw new ArgumentNullException(nameof(scheduled));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.ActionId != scheduled.Action.ActionId)
            {
                throw new ArgumentException(
                    "The resolve result must belong to the scheduled action.",
                    nameof(result));
            }

            if (result.Outcome == EnemyIntentResolveOutcome.RemovedInvalid)
            {
                return CreateSnapshot(
                    scheduled,
                    TimelineActionValidity.Invalid,
                    MapInvalidReason(result.InvalidReason),
                    TimelineActionResolveState.Removed);
            }

            return CreateSnapshot(
                scheduled,
                TimelineActionValidity.Unsupported,
                result.Outcome == EnemyIntentResolveOutcome.UnsupportedSourceCommand
                    ? TimelineActionInvalidReason.UnsupportedSourceCommand
                    : TimelineActionInvalidReason.UnsupportedEffect,
                TimelineActionResolveState.Resolved);
        }

        private static TimelineActionPresentationSnapshot CreateSnapshot(
            EnemyIntentScheduledAction scheduled,
            TimelineActionValidity validity,
            TimelineActionInvalidReason invalidReason,
            TimelineActionResolveState resolveState)
        {
            var source = scheduled.Candidate.Source;
            var display = source.Display;
            return new TimelineActionPresentationSnapshot(
                scheduled.Action.ActionId,
                TimelineActorKind.Enemy,
                source.Priority,
                source.RuntimeId,
                source.Coordinate,
                source.Target.TargetId,
                source.Target.Coordinate,
                null,
                source.Effect.StableId,
                new TimelineActionDisplayPayload(
                    display.Title,
                    display.Description,
                    display.IconStableId,
                    display.SourceLabel,
                    display.TargetLabel),
                scheduled.Origin,
                source.LiteralShape,
                scheduled.OccupiedCells,
                scheduled.Candidate.EffectRange,
                validity,
                invalidReason,
                resolveState);
        }

        private static TimelineActionInvalidReason MapInvalidReason(
            EnemyIntentInvalidReason reason)
        {
            switch (reason)
            {
                case EnemyIntentInvalidReason.MissingSource:
                    return TimelineActionInvalidReason.MissingSource;
                case EnemyIntentInvalidReason.SourceUnavailable:
                case EnemyIntentInvalidReason.EffectMismatch:
                    return TimelineActionInvalidReason.SourceUnavailable;
                case EnemyIntentInvalidReason.MissingTarget:
                    return TimelineActionInvalidReason.MissingTarget;
                case EnemyIntentInvalidReason.TargetUnavailable:
                    return TimelineActionInvalidReason.TargetUnavailable;
                case EnemyIntentInvalidReason.ShapeMismatch:
                    return TimelineActionInvalidReason.ShapeMismatch;
                case EnemyIntentInvalidReason.OutOfBounds:
                    return TimelineActionInvalidReason.OutOfBounds;
                case EnemyIntentInvalidReason.TimelineConflict:
                case EnemyIntentInvalidReason.GenerationLimitReached:
                    return TimelineActionInvalidReason.TimelineConflict;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason));
            }
        }
    }
}
