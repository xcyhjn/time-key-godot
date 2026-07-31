using System;

namespace TimeKey.Domain
{
    public enum CardPlaySessionState
    {
        Idle,
        TargetSelected,
        TimelinePreview,
        Committed,
        Cancelled
    }

    public enum CardPlayFailure
    {
        None,
        InvalidTarget,
        MissingTarget,
        PreviewRequired,
        InvalidTimelinePlacement,
        AlreadyCommitted,
        Cancelled
    }

    public readonly struct CardPlayTransition
    {
        internal CardPlayTransition(
            CardPlaySessionState state,
            CardPlayFailure failure,
            bool isPlacementValid)
        {
            State = state;
            Failure = failure;
            IsPlacementValid = isPlacementValid;
        }

        public CardPlaySessionState State { get; }

        public CardPlayFailure Failure { get; }

        public bool IsPlacementValid { get; }

        public bool Succeeded => Failure == CardPlayFailure.None;
    }

    public sealed class CardPlaySession
    {
        private bool _isPlacementValid;

        public CardPlaySession(CardDefinition card)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            State = CardPlaySessionState.Idle;
        }

        public CardDefinition Card { get; }

        public CardPlaySessionState State { get; private set; }

        public string TargetId { get; private set; }

        public HexCoord? TargetCoord { get; private set; }

        public TimelineCell? TimelineOrigin { get; private set; }

        public CardPlayTransition SelectTarget(string targetId, HexCoord targetCoord)
        {
            var closed = GetClosedFailure();
            if (closed != CardPlayFailure.None)
            {
                return Failure(closed);
            }

            if (string.IsNullOrWhiteSpace(targetId))
            {
                return Failure(CardPlayFailure.InvalidTarget);
            }

            TargetId = targetId;
            TargetCoord = targetCoord;
            TimelineOrigin = null;
            _isPlacementValid = false;
            State = CardPlaySessionState.TargetSelected;
            return Success(false);
        }

        public CardPlayTransition PreviewTimeline(TimelineGrid grid, TimelineCell origin)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            var closed = GetClosedFailure();
            if (closed != CardPlayFailure.None)
            {
                return Failure(closed);
            }

            if (string.IsNullOrWhiteSpace(TargetId) || !TargetCoord.HasValue)
            {
                return Failure(CardPlayFailure.MissingTarget);
            }

            TimelineOrigin = origin;
            _isPlacementValid = grid.CanPlace(CreateAction(origin));
            State = CardPlaySessionState.TimelinePreview;
            return _isPlacementValid
                ? Success(true)
                : Failure(CardPlayFailure.InvalidTimelinePlacement);
        }

        public CardPlayTransition Commit(TimelineGrid grid)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            var closed = GetClosedFailure();
            if (closed != CardPlayFailure.None)
            {
                return Failure(closed);
            }

            if (State != CardPlaySessionState.TimelinePreview || !TimelineOrigin.HasValue)
            {
                return Failure(CardPlayFailure.PreviewRequired);
            }

            if (!_isPlacementValid || !grid.TryPlace(CreateAction(TimelineOrigin.Value)))
            {
                _isPlacementValid = false;
                return Failure(CardPlayFailure.InvalidTimelinePlacement);
            }

            State = CardPlaySessionState.Committed;
            return Success(true);
        }

        public CardPlayTransition Cancel()
        {
            if (State == CardPlaySessionState.Committed)
            {
                return Failure(CardPlayFailure.AlreadyCommitted);
            }

            State = CardPlaySessionState.Cancelled;
            TargetId = null;
            TargetCoord = null;
            TimelineOrigin = null;
            _isPlacementValid = false;
            return Success(false);
        }

        private TimelineAction CreateAction(TimelineCell origin)
        {
            return TimelineAction.FromCard(Card, TargetId, origin);
        }

        private CardPlayFailure GetClosedFailure()
        {
            if (State == CardPlaySessionState.Committed)
            {
                return CardPlayFailure.AlreadyCommitted;
            }

            return State == CardPlaySessionState.Cancelled
                ? CardPlayFailure.Cancelled
                : CardPlayFailure.None;
        }

        private CardPlayTransition Success(bool isPlacementValid)
        {
            return new CardPlayTransition(State, CardPlayFailure.None, isPlacementValid);
        }

        private CardPlayTransition Failure(CardPlayFailure failure)
        {
            return new CardPlayTransition(State, failure, _isPlacementValid);
        }
    }
}
