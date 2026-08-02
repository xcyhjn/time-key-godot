using System.Collections.Generic;
using TimeKey.Application.BattleFlow;
using TimeKey.Domain;
using TimeKey.Domain.Deck;

namespace TimeKey.Application
{
    public enum CombatInteractionMode
    {
        OrdinaryTimeline,
        TimelineClear
    }

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
        AlreadyResolved,
        InteractionModeMismatch,
        LifecycleFailed
    }

    public sealed class CombatSessionView
    {
        internal CombatSessionView(
            CombatSessionPhase phase,
            CardDefinition selectedCard,
            TimelineActionIdentity? pendingActionId,
            IReadOnlyList<TimelineActionPresentationSnapshot> timelineActions,
            CombatTargetKind? requiredTargetKind,
            CombatTarget? target,
            TimelineCell? timelineOrigin,
            bool isPlacementValid,
            ResolutionSnapshot lastResolution,
            CombatInteractionMode? interactionMode,
            TimelineClearPreview clearPreview,
            TimelineClearResult lastClearResult,
            IReadOnlyList<LifecycleOccupantChangeResult> lifecycleChanges,
            CardInstanceId? selectedCardInstanceId = null,
            BattleFlowPresentationSnapshot battleFlow = null)
        {
            Phase = phase;
            SelectedCard = selectedCard;
            PendingActionId = pendingActionId;
            TimelineActions = timelineActions;
            RequiredTargetKind = requiredTargetKind;
            Target = target;
            TimelineOrigin = timelineOrigin;
            IsPlacementValid = isPlacementValid;
            LastResolution = lastResolution;
            InteractionMode = interactionMode;
            ClearPreview = clearPreview;
            LastClearResult = lastClearResult;
            LifecycleChanges = lifecycleChanges;
            SelectedCardInstanceId = selectedCardInstanceId;
            BattleFlow = battleFlow;
        }

        public CombatSessionPhase Phase { get; }

        public CardDefinition SelectedCard { get; }

        public string SelectedStableId => SelectedCard == null ? null : SelectedCard.StableId;

        public TimelineActionIdentity? PendingActionId { get; }

        public IReadOnlyList<TimelineActionPresentationSnapshot> TimelineActions { get; }

        public CombatTargetKind? RequiredTargetKind { get; }

        public CombatTarget? Target { get; }

        public TimelineCell? TimelineOrigin { get; }

        public bool IsPlacementValid { get; }

        public ResolutionSnapshot LastResolution { get; }

        public CombatInteractionMode? InteractionMode { get; }

        public TimelineClearPreview ClearPreview { get; }

        public TimelineClearResult LastClearResult { get; }

        public IReadOnlyList<LifecycleOccupantChangeResult> LifecycleChanges { get; }

        public CardInstanceId? SelectedCardInstanceId { get; }

        public BattleFlowPresentationSnapshot BattleFlow { get; }
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
            ResolutionSnapshot resolution,
            CombatInteractionMode? interactionMode,
            TimelineClearPreview clearPreview,
            TimelineClearResult clearResult)
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
            InteractionMode = interactionMode;
            ClearPreview = clearPreview;
            ClearResult = clearResult;
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

        public CombatInteractionMode? InteractionMode { get; }

        public TimelineClearPreview ClearPreview { get; }

        public TimelineClearResult ClearResult { get; }
    }
}
