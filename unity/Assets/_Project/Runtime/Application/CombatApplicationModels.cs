using TimeKey.Domain;

namespace TimeKey.Application
{
    public enum CombatSessionPhase
    {
        Idle,
        CardSelected,
        TargetSelected,
        TimelinePreview,
        Committed,
        Cancelled,
        Resolved,
        Disposed
    }

    public enum CombatCommandFailure
    {
        None,
        Disposed,
        UnknownCard,
        UnsupportedCardTargetPolicy,
        NoCardSelected,
        TargetKindMismatch,
        InvalidTarget,
        MissingTarget,
        PreviewRequired,
        InvalidTimelinePlacement,
        UnsupportedEffect,
        AlreadyCommitted,
        Cancelled,
        NoCommittedAction,
        AlreadyResolved
    }

    public sealed class CombatSessionView
    {
        internal CombatSessionView(
            CombatSessionPhase phase,
            CardDefinition selectedCard,
            CombatTargetKind? requiredTargetKind,
            CombatTarget? target,
            TimelineCell? timelineOrigin,
            bool isPlacementValid,
            ResolutionSnapshot lastResolution)
        {
            Phase = phase;
            SelectedCard = selectedCard;
            RequiredTargetKind = requiredTargetKind;
            Target = target;
            TimelineOrigin = timelineOrigin;
            IsPlacementValid = isPlacementValid;
            LastResolution = lastResolution;
        }

        public CombatSessionPhase Phase { get; }

        public CardDefinition SelectedCard { get; }

        public string SelectedStableId => SelectedCard == null ? null : SelectedCard.StableId;

        public CombatTargetKind? RequiredTargetKind { get; }

        public CombatTarget? Target { get; }

        public TimelineCell? TimelineOrigin { get; }

        public bool IsPlacementValid { get; }

        public ResolutionSnapshot LastResolution { get; }
    }

    public sealed class CombatCommandResult
    {
        internal CombatCommandResult(
            CombatCommandFailure failure,
            string failureReason,
            CombatSessionPhase phaseBefore,
            CombatSessionPhase phaseAfter,
            string stableId,
            CombatTargetKind? requiredTargetKind,
            CombatTarget? target,
            TimelineCell? timelineOrigin,
            bool isPlacementValid,
            ResolutionSnapshot resolution)
        {
            Failure = failure;
            FailureReason = failureReason;
            PhaseBefore = phaseBefore;
            PhaseAfter = phaseAfter;
            StableId = stableId;
            RequiredTargetKind = requiredTargetKind;
            Target = target;
            TimelineOrigin = timelineOrigin;
            IsPlacementValid = isPlacementValid;
            Resolution = resolution;
        }

        public bool Succeeded => Failure == CombatCommandFailure.None;

        public CombatCommandFailure Failure { get; }

        public string FailureReason { get; }

        public CombatSessionPhase PhaseBefore { get; }

        public CombatSessionPhase PhaseAfter { get; }

        public string StableId { get; }

        public CombatTargetKind? RequiredTargetKind { get; }

        public CombatTarget? Target { get; }

        public TimelineCell? TimelineOrigin { get; }

        public bool IsPlacementValid { get; }

        public ResolutionSnapshot Resolution { get; }
    }
}
