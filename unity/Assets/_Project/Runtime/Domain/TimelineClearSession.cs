using System;

namespace TimeKey.Domain
{
    public enum TimelineClearSessionState
    {
        Selected,
        Preview,
        Committed,
        Cancelled
    }

    public enum TimelineClearFailure
    {
        None,
        PreviewRequired,
        InvalidTimelinePlacement,
        AlreadyCommitted,
        Cancelled
    }

    public readonly struct TimelineClearTransition
    {
        internal TimelineClearTransition(
            TimelineClearSessionState state,
            TimelineClearFailure failure,
            TimelineClearPreview preview,
            TimelineClearResult result)
        {
            State = state;
            Failure = failure;
            Preview = preview;
            Result = result;
        }

        public TimelineClearSessionState State { get; }

        public TimelineClearFailure Failure { get; }

        public TimelineClearPreview Preview { get; }

        public TimelineClearResult Result { get; }

        public bool Succeeded => Failure == TimelineClearFailure.None;
    }

    public sealed class TimelineClearSession
    {
        private readonly CardEffect _clearEffect;

        public TimelineClearSession(CardDefinition card)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            if (card.Effects.Count != 1 || card.Effects[0].Kind != CardEffectKind.Clear)
            {
                throw new ArgumentException(
                    "A timeline clear session requires one typed Clear effect.",
                    nameof(card));
            }

            _clearEffect = card.Effects[0];
            State = TimelineClearSessionState.Selected;
        }

        public CardDefinition Card { get; }

        public TimelineClearSessionState State { get; private set; }

        public TimelineCell? Origin { get; private set; }

        public TimelineClearPreview LastPreview { get; private set; }

        public TimelineClearResult LastResult { get; private set; }

        public TimelineClearTransition Preview(TimelineGrid grid, TimelineCell origin)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            var closedFailure = GetClosedFailure();
            if (closedFailure != TimelineClearFailure.None)
            {
                return Failure(closedFailure);
            }

            Origin = origin;
            LastPreview = grid.PreviewClear(origin, _clearEffect);
            LastResult = null;
            State = TimelineClearSessionState.Preview;
            return LastPreview.IsInBounds
                ? Success(LastPreview, null)
                : Failure(TimelineClearFailure.InvalidTimelinePlacement);
        }

        public TimelineClearTransition Commit(TimelineGrid grid)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            var closedFailure = GetClosedFailure();
            if (closedFailure != TimelineClearFailure.None)
            {
                return Failure(closedFailure);
            }

            if (State != TimelineClearSessionState.Preview || !Origin.HasValue)
            {
                return Failure(TimelineClearFailure.PreviewRequired);
            }

            LastResult = grid.TryClear(Origin.Value, _clearEffect);
            LastPreview = LastResult.Preview;
            if (!LastResult.Succeeded)
            {
                return Failure(TimelineClearFailure.InvalidTimelinePlacement);
            }

            State = TimelineClearSessionState.Committed;
            return Success(LastPreview, LastResult);
        }

        public TimelineClearTransition Cancel()
        {
            if (State == TimelineClearSessionState.Committed)
            {
                return Failure(TimelineClearFailure.AlreadyCommitted);
            }

            if (State == TimelineClearSessionState.Cancelled)
            {
                return Success(null, null);
            }

            State = TimelineClearSessionState.Cancelled;
            Origin = null;
            LastPreview = null;
            LastResult = null;
            return Success(null, null);
        }

        private TimelineClearFailure GetClosedFailure()
        {
            if (State == TimelineClearSessionState.Committed)
            {
                return TimelineClearFailure.AlreadyCommitted;
            }

            return State == TimelineClearSessionState.Cancelled
                ? TimelineClearFailure.Cancelled
                : TimelineClearFailure.None;
        }

        private TimelineClearTransition Success(
            TimelineClearPreview preview,
            TimelineClearResult result)
        {
            return new TimelineClearTransition(
                State,
                TimelineClearFailure.None,
                preview,
                result);
        }

        private TimelineClearTransition Failure(TimelineClearFailure failure)
        {
            return new TimelineClearTransition(State, failure, LastPreview, LastResult);
        }
    }
}
