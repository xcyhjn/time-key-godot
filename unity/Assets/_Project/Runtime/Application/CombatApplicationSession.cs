using System;
using System.Collections.Generic;
using TimeKey.Domain;

namespace TimeKey.Application
{
    public sealed class CombatApplicationSession : IDisposable
    {
        private readonly ICardCatalog _catalog;
        private readonly CombatSliceState _state;
        private readonly TimelineGrid _timeline;
        private readonly ICombatTraceSink _traceSink;

        private CombatSessionPhase _phase;
        private CardDefinition _selectedCard;
        private CardPlaySession _cardPlaySession;
        private CombatTargetKind? _requiredTargetKind;
        private CombatTarget? _target;
        private TimelineCell? _timelineOrigin;
        private bool _isPlacementValid;
        private ResolutionSnapshot _lastResolution;

        public CombatApplicationSession(
            ICardCatalog catalog,
            CombatSliceState state,
            TimelineGrid timeline,
            IReadOnlyList<TimelineAction> initialActions = null,
            ICombatTraceSink traceSink = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            _traceSink = traceSink;
            if (_catalog.Cards == null)
            {
                throw new ArgumentException("The card catalog must expose a card list.", nameof(catalog));
            }

            PlaceInitialActions(initialActions);
            _phase = CombatSessionPhase.Idle;
        }

        public IReadOnlyList<CardDefinition> Cards => _catalog.Cards;

        public CombatSessionView Current => new CombatSessionView(
            _phase,
            _selectedCard,
            _requiredTargetKind,
            _target,
            _timelineOrigin,
            _isPlacementValid,
            _lastResolution);

        public CombatCommandResult SelectCard(string stableId)
        {
            var before = _phase;
            if (_phase == CombatSessionPhase.Disposed)
            {
                return Failure("select-card", before, CombatCommandFailure.Disposed, stableId);
            }

            if (_phase == CombatSessionPhase.Committed)
            {
                return Failure("select-card", before, CombatCommandFailure.AlreadyCommitted, stableId);
            }

            if (_phase == CombatSessionPhase.Resolved)
            {
                return Failure("select-card", before, CombatCommandFailure.AlreadyResolved, stableId);
            }

            if (string.IsNullOrWhiteSpace(stableId) ||
                !_catalog.TryGet(stableId, out var card) ||
                card == null)
            {
                return Failure("select-card", before, CombatCommandFailure.UnknownCard, stableId);
            }

            if (!TryGetRequiredTargetKind(card, out var targetKind))
            {
                return Failure(
                    "select-card",
                    before,
                    CombatCommandFailure.UnsupportedCardTargetPolicy,
                    stableId,
                    reason: "The card does not use one ordinary entity or tile target policy.");
            }

            _selectedCard = card;
            _cardPlaySession = new CardPlaySession(card);
            _requiredTargetKind = targetKind;
            _target = null;
            _timelineOrigin = null;
            _isPlacementValid = false;
            _phase = CombatSessionPhase.CardSelected;
            return Success("select-card", before, stableId);
        }

        public CombatCommandResult SelectTarget(CombatTarget target)
        {
            var before = _phase;
            if (_phase == CombatSessionPhase.Disposed)
            {
                return Failure("select-target", before, CombatCommandFailure.Disposed, target: target);
            }

            if (_phase == CombatSessionPhase.Committed)
            {
                return Failure(
                    "select-target",
                    before,
                    CombatCommandFailure.AlreadyCommitted,
                    CurrentStableId,
                    target);
            }

            if (_phase == CombatSessionPhase.Resolved)
            {
                return Failure(
                    "select-target",
                    before,
                    CombatCommandFailure.AlreadyResolved,
                    target: target);
            }

            if (_selectedCard == null || _cardPlaySession == null || !_requiredTargetKind.HasValue)
            {
                return Failure("select-target", before, CombatCommandFailure.NoCardSelected, target: target);
            }

            if (target.Kind != _requiredTargetKind.Value)
            {
                return Failure(
                    "select-target",
                    before,
                    CombatCommandFailure.TargetKindMismatch,
                    CurrentStableId,
                    target);
            }

            if (!IsKnownTarget(target))
            {
                return Failure(
                    "select-target",
                    before,
                    CombatCommandFailure.InvalidTarget,
                    CurrentStableId,
                    target);
            }

            var domainTargetId = target.Kind == CombatTargetKind.Entity
                ? target.EntityId
                : _state.TargetId;
            var transition = _cardPlaySession.SelectTarget(domainTargetId, target.Coordinate);
            if (!transition.Succeeded)
            {
                return Failure(
                    "select-target",
                    before,
                    MapFailure(transition.Failure),
                    CurrentStableId,
                    target);
            }

            _target = target;
            _timelineOrigin = null;
            _isPlacementValid = false;
            _phase = CombatSessionPhase.TargetSelected;
            return Success("select-target", before, CurrentStableId, target);
        }

        public CombatCommandResult PreviewTimeline(TimelineCell origin)
        {
            var before = _phase;
            var unavailable = GetSessionUnavailableFailure();
            if (unavailable != CombatCommandFailure.None)
            {
                return Failure(
                    "preview-timeline",
                    before,
                    unavailable,
                    CurrentStableId,
                    _target,
                    origin);
            }

            try
            {
                var transition = _cardPlaySession.PreviewTimeline(_timeline, origin);
                _timelineOrigin = _cardPlaySession.TimelineOrigin;
                _isPlacementValid = transition.IsPlacementValid;
                _phase = MapPhase(transition.State);
                return transition.Succeeded
                    ? Success("preview-timeline", before, CurrentStableId, _target, origin)
                    : Failure(
                        "preview-timeline",
                        before,
                        MapFailure(transition.Failure),
                        CurrentStableId,
                        _target,
                        origin);
            }
            catch (UnsupportedCardEffectException exception)
            {
                _isPlacementValid = false;
                return Failure(
                    "preview-timeline",
                    before,
                    CombatCommandFailure.UnsupportedEffect,
                    CurrentStableId,
                    _target,
                    origin,
                    exception.Message);
            }
        }

        public CombatCommandResult CommitTimeline()
        {
            var before = _phase;
            var unavailable = GetSessionUnavailableFailure();
            if (unavailable != CombatCommandFailure.None)
            {
                return Failure(
                    "commit-timeline",
                    before,
                    unavailable,
                    CurrentStableId,
                    _target,
                    _timelineOrigin);
            }

            try
            {
                var transition = _cardPlaySession.Commit(_timeline);
                _timelineOrigin = _cardPlaySession.TimelineOrigin;
                _isPlacementValid = transition.IsPlacementValid;
                _phase = MapPhase(transition.State);
                return transition.Succeeded
                    ? Success(
                        "commit-timeline",
                        before,
                        CurrentStableId,
                        _target,
                        _timelineOrigin)
                    : Failure(
                        "commit-timeline",
                        before,
                        MapFailure(transition.Failure),
                        CurrentStableId,
                        _target,
                        _timelineOrigin);
            }
            catch (UnsupportedCardEffectException exception)
            {
                _isPlacementValid = false;
                return Failure(
                    "commit-timeline",
                    before,
                    CombatCommandFailure.UnsupportedEffect,
                    CurrentStableId,
                    _target,
                    _timelineOrigin,
                    exception.Message);
            }
        }

        public CombatCommandResult CancelCard()
        {
            var before = _phase;
            if (_phase == CombatSessionPhase.Disposed)
            {
                return Failure("cancel-card", before, CombatCommandFailure.Disposed);
            }

            if (_phase == CombatSessionPhase.Resolved)
            {
                return Failure("cancel-card", before, CombatCommandFailure.AlreadyResolved);
            }

            if (_phase == CombatSessionPhase.Committed)
            {
                return Failure(
                    "cancel-card",
                    before,
                    CombatCommandFailure.AlreadyCommitted,
                    CurrentStableId,
                    _target,
                    _timelineOrigin);
            }

            if (_phase == CombatSessionPhase.Cancelled)
            {
                return Success("cancel-card", before);
            }

            if (_cardPlaySession == null || _selectedCard == null)
            {
                return Failure("cancel-card", before, CombatCommandFailure.NoCardSelected);
            }

            var stableId = CurrentStableId;
            var target = _target;
            var origin = _timelineOrigin;
            var transition = _cardPlaySession.Cancel();
            if (!transition.Succeeded)
            {
                return Failure(
                    "cancel-card",
                    before,
                    MapFailure(transition.Failure),
                    stableId,
                    target,
                    origin);
            }

            _selectedCard = null;
            _requiredTargetKind = null;
            _target = null;
            _timelineOrigin = null;
            _isPlacementValid = false;
            _phase = CombatSessionPhase.Cancelled;
            return Success("cancel-card", before, stableId, target, origin);
        }

        public CombatCommandResult ResolveTimeline()
        {
            var before = _phase;
            if (_phase == CombatSessionPhase.Disposed)
            {
                return Failure("resolve-timeline", before, CombatCommandFailure.Disposed);
            }

            if (_phase == CombatSessionPhase.Resolved)
            {
                return Failure("resolve-timeline", before, CombatCommandFailure.AlreadyResolved);
            }

            if (_phase != CombatSessionPhase.Committed || _selectedCard == null)
            {
                return Failure(
                    "resolve-timeline",
                    before,
                    CombatCommandFailure.NoCommittedAction,
                    CurrentStableId,
                    _target,
                    _timelineOrigin);
            }

            var stableId = CurrentStableId;
            var target = _target;
            var origin = _timelineOrigin;
            var resolvedCard = _selectedCard;
            try
            {
                _lastResolution = _timeline.Resolve(_state);
            }
            catch (UnsupportedCardEffectException exception)
            {
                return Failure(
                    "resolve-timeline",
                    before,
                    CombatCommandFailure.UnsupportedEffect,
                    stableId,
                    target,
                    origin,
                    exception.Message);
            }

            _selectedCard = null;
            _cardPlaySession = null;
            _requiredTargetKind = null;
            _target = null;
            _timelineOrigin = null;
            _isPlacementValid = false;
            _phase = CombatSessionPhase.Resolved;
            RecordResolutionEffects(resolvedCard, stableId, target, origin, _lastResolution);
            return Success(
                "resolve-timeline",
                before,
                stableId,
                target,
                origin,
                _lastResolution);
        }

        private void RecordResolutionEffects(
            CardDefinition card,
            string stableId,
            CombatTarget? target,
            TimelineCell? origin,
            ResolutionSnapshot resolution)
        {
            for (var effectIndex = 0; effectIndex < card.Effects.Count; effectIndex++)
            {
                var effect = card.Effects[effectIndex];
                if (effect.Kind == CardEffectKind.Damage)
                {
                    TryRecord(new CombatTraceEntry(
                        "resolve-effect",
                        CombatSessionPhase.Resolved.ToString(),
                        CombatSessionPhase.Resolved.ToString(),
                        stableId,
                        target.HasValue ? target.Value.EntityId : null,
                        target.HasValue ? target.Value.Coordinate : (HexCoord?)null,
                        origin,
                        effectKind: effect.Kind,
                        beforeValue: resolution.TargetHpBefore,
                        afterValue: resolution.TargetHpAfter));
                }
                else if (effect.Kind == CardEffectKind.Elevation)
                {
                    for (var resultIndex = 0; resultIndex < resolution.EffectResults.Count; resultIndex++)
                    {
                        var tile = resolution.EffectResults[resultIndex];
                        TryRecord(new CombatTraceEntry(
                            "resolve-effect",
                            CombatSessionPhase.Resolved.ToString(),
                            CombatSessionPhase.Resolved.ToString(),
                            stableId,
                            targetId: null,
                            targetCoordinate: tile.Coordinate,
                            timelineOrigin: origin,
                            effectKind: effect.Kind,
                            beforeValue: tile.BeforeLayers,
                            afterValue: tile.AfterLayers));
                    }
                }
                else if (effect.Kind == CardEffectKind.Recover ||
                         effect.Kind == CardEffectKind.Built ||
                         effect.Kind == CardEffectKind.Poison)
                {
                    for (var resultIndex = 0;
                         resultIndex < resolution.OccupantEffectResults.Count;
                         resultIndex++)
                    {
                        var occupant = resolution.OccupantEffectResults[resultIndex];
                        if (occupant.EffectKind != effect.Kind || occupant.After == null)
                        {
                            continue;
                        }

                        int beforeValue;
                        int afterValue;
                        switch (effect.Kind)
                        {
                            case CardEffectKind.Recover:
                                if (occupant.Before == null)
                                {
                                    continue;
                                }

                                beforeValue = occupant.Before.Hp;
                                afterValue = occupant.After.Hp;
                                break;
                            case CardEffectKind.Built:
                                beforeValue = 0;
                                afterValue = occupant.After.Hp;
                                break;
                            case CardEffectKind.Poison:
                                if (occupant.Before == null)
                                {
                                    continue;
                                }

                                beforeValue = occupant.Before.PoisonStacks;
                                afterValue = occupant.After.PoisonStacks;
                                break;
                            default:
                                continue;
                        }

                        TryRecord(new CombatTraceEntry(
                            "resolve-effect",
                            CombatSessionPhase.Resolved.ToString(),
                            CombatSessionPhase.Resolved.ToString(),
                            stableId,
                            targetId: occupant.After.RuntimeId,
                            targetCoordinate: occupant.After.Coordinate,
                            timelineOrigin: origin,
                            effectKind: effect.Kind,
                            beforeValue: beforeValue,
                            afterValue: afterValue));
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_phase == CombatSessionPhase.Disposed)
            {
                return;
            }

            var before = _phase;
            _selectedCard = null;
            _cardPlaySession = null;
            _requiredTargetKind = null;
            _target = null;
            _timelineOrigin = null;
            _isPlacementValid = false;
            _phase = CombatSessionPhase.Disposed;
            TryRecord(new CombatTraceEntry("dispose", before.ToString(), _phase.ToString()));
        }

        private string CurrentStableId => _selectedCard == null ? null : _selectedCard.StableId;

        private bool IsKnownTarget(CombatTarget target)
        {
            if (target.Kind == CombatTargetKind.Entity)
            {
                for (var index = 0; index < _selectedCard.Effects.Count; index++)
                {
                    if (_selectedCard.Effects[index].Kind == CardEffectKind.Recover)
                    {
                        if (_selectedCard.Range.Count == 0 ||
                            !_state.TryGetOccupant(
                                target.EntityId,
                                target.Coordinate,
                                out var occupant) ||
                            !occupant.SupportsHealth ||
                            occupant.Hp >= occupant.MaxHp)
                        {
                            return false;
                        }
                    }
                    else if (_selectedCard.Effects[index].Kind == CardEffectKind.Poison)
                    {
                        if (_selectedCard.Range.Count == 0 ||
                            !_state.TryGetOccupant(
                                target.EntityId,
                                target.Coordinate,
                                out var poisonOccupant) ||
                            !poisonOccupant.SupportsHealth ||
                            poisonOccupant.Hp <= 0 ||
                            !poisonOccupant.SupportsStatus)
                        {
                            return false;
                        }
                    }
                    else if (!string.Equals(
                                 target.EntityId,
                                 _state.TargetId,
                                 StringComparison.Ordinal))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (!_state.Board.TryGetTile(target.Coordinate, out _))
            {
                return false;
            }

            for (var index = 0; index < _selectedCard.Effects.Count; index++)
            {
                if (_selectedCard.Effects[index].Kind == CardEffectKind.Built &&
                    (_selectedCard.Range.Count == 0 ||
                     _state.TryGetOccupant(target.Coordinate, out _)))
                {
                    return false;
                }
            }

            return true;
        }

        private CombatCommandFailure GetSessionUnavailableFailure()
        {
            if (_phase == CombatSessionPhase.Disposed)
            {
                return CombatCommandFailure.Disposed;
            }

            if (_phase == CombatSessionPhase.Resolved)
            {
                return CombatCommandFailure.AlreadyResolved;
            }

            if (_selectedCard == null || _cardPlaySession == null)
            {
                return CombatCommandFailure.NoCardSelected;
            }

            return CombatCommandFailure.None;
        }

        private void PlaceInitialActions(IReadOnlyList<TimelineAction> initialActions)
        {
            if (initialActions == null || initialActions.Count == 0)
            {
                return;
            }

            var initialCells = new HashSet<TimelineCell>();
            for (var actionIndex = 0; actionIndex < initialActions.Count; actionIndex++)
            {
                var action = initialActions[actionIndex];
                if (action == null || !_timeline.CanPlace(action))
                {
                    throw new ArgumentException("Every initial action must have a legal placement.", nameof(initialActions));
                }

                for (var shapeIndex = 0; shapeIndex < action.Shape.Count; shapeIndex++)
                {
                    if (!initialCells.Add(action.Origin + action.Shape[shapeIndex]))
                    {
                        throw new ArgumentException("Initial actions cannot overlap.", nameof(initialActions));
                    }
                }
            }

            for (var index = 0; index < initialActions.Count; index++)
            {
                if (!_timeline.TryPlace(initialActions[index]))
                {
                    throw new InvalidOperationException("The timeline changed while initial actions were placed.");
                }
            }
        }

        private static bool TryGetRequiredTargetKind(
            CardDefinition card,
            out CombatTargetKind targetKind)
        {
            targetKind = default;
            CombatTargetKind? required = null;
            for (var index = 0; index < card.Effects.Count; index++)
            {
                CombatTargetKind effectTarget;
                switch (card.Effects[index].Kind)
                {
                    case CardEffectKind.Damage:
                    case CardEffectKind.Recover:
                    case CardEffectKind.Poison:
                        effectTarget = CombatTargetKind.Entity;
                        break;
                    case CardEffectKind.Elevation:
                    case CardEffectKind.Built:
                        effectTarget = CombatTargetKind.Tile;
                        break;
                    default:
                        return false;
                }

                if (required.HasValue && required.Value != effectTarget)
                {
                    return false;
                }

                required = effectTarget;
            }

            if (!required.HasValue)
            {
                return false;
            }

            targetKind = required.Value;
            return true;
        }

        private CombatCommandResult Success(
            string command,
            CombatSessionPhase phaseBefore,
            string stableId = null,
            CombatTarget? target = null,
            TimelineCell? origin = null,
            ResolutionSnapshot resolution = null)
        {
            return Complete(
                command,
                phaseBefore,
                CombatCommandFailure.None,
                null,
                stableId,
                target,
                origin,
                resolution);
        }

        private CombatCommandResult Failure(
            string command,
            CombatSessionPhase phaseBefore,
            CombatCommandFailure failure,
            string stableId = null,
            CombatTarget? target = null,
            TimelineCell? origin = null,
            string reason = null)
        {
            return Complete(
                command,
                phaseBefore,
                failure,
                reason ?? failure.ToString(),
                stableId,
                target,
                origin,
                null);
        }

        private CombatCommandResult Complete(
            string command,
            CombatSessionPhase phaseBefore,
            CombatCommandFailure failure,
            string failureReason,
            string stableId,
            CombatTarget? target,
            TimelineCell? origin,
            ResolutionSnapshot resolution)
        {
            var result = new CombatCommandResult(
                failure,
                failureReason,
                phaseBefore,
                _phase,
                stableId,
                _requiredTargetKind,
                target,
                origin,
                _isPlacementValid,
                resolution);
            TryRecord(new CombatTraceEntry(
                command,
                phaseBefore.ToString(),
                _phase.ToString(),
                stableId,
                target.HasValue && target.Value.Kind == CombatTargetKind.Entity
                    ? target.Value.EntityId
                    : null,
                target.HasValue ? target.Value.Coordinate : (HexCoord?)null,
                origin,
                failureReason));
            return result;
        }

        private void TryRecord(CombatTraceEntry entry)
        {
            if (_traceSink == null)
            {
                return;
            }

            try
            {
                _traceSink.Record(entry);
            }
            catch (Exception)
            {
                // Diagnostics are observational and cannot change command behavior.
            }
        }

        private static CombatSessionPhase MapPhase(CardPlaySessionState state)
        {
            switch (state)
            {
                case CardPlaySessionState.Idle:
                    return CombatSessionPhase.CardSelected;
                case CardPlaySessionState.TargetSelected:
                    return CombatSessionPhase.TargetSelected;
                case CardPlaySessionState.TimelinePreview:
                    return CombatSessionPhase.TimelinePreview;
                case CardPlaySessionState.Committed:
                    return CombatSessionPhase.Committed;
                case CardPlaySessionState.Cancelled:
                    return CombatSessionPhase.Cancelled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private static CombatCommandFailure MapFailure(CardPlayFailure failure)
        {
            switch (failure)
            {
                case CardPlayFailure.None:
                    return CombatCommandFailure.None;
                case CardPlayFailure.InvalidTarget:
                    return CombatCommandFailure.InvalidTarget;
                case CardPlayFailure.MissingTarget:
                    return CombatCommandFailure.MissingTarget;
                case CardPlayFailure.PreviewRequired:
                    return CombatCommandFailure.PreviewRequired;
                case CardPlayFailure.InvalidTimelinePlacement:
                    return CombatCommandFailure.InvalidTimelinePlacement;
                case CardPlayFailure.AlreadyCommitted:
                    return CombatCommandFailure.AlreadyCommitted;
                case CardPlayFailure.Cancelled:
                    return CombatCommandFailure.Cancelled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(failure));
            }
        }
    }
}
