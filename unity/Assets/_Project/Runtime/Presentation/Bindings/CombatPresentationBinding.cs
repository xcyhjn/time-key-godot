using System;
using System.Collections.Generic;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Deck;
using TimeKey.Presentation.BattleFlow;
using TimeKey.Presentation.Cards;
using TimeKey.Presentation.CombatShell;
using TimeKey.Presentation.Occupants;
using TimeKey.Presentation.Presenters;
using TimeKey.Presentation.Terrain;
using TimeKey.Presentation.Tooltips;
using TimeKey.Presentation.Identity;
using TimeKey.Presentation.Interaction;
using UnityEngine;

namespace TimeKey.Presentation.Bindings
{
    [DisallowMultipleComponent]
    public sealed class CombatPresentationBinding : MonoBehaviour
    {
        [SerializeField] private CardHandPresenter cardHandPresenter = null;
        [SerializeField] private BoardRangePresenter boardRangePresenter = null;
        [SerializeField] private TimelinePresenter timelinePresenter = null;
        [SerializeField] private CombatHudPresenter hudPresenter = null;
        [SerializeField] private CombatOccupantPresenter occupantPresenter = null;
        [SerializeField] private CombatInteractionOverlayPresenter interactionOverlayPresenter = null;
        [SerializeField] private BattleFlowPresenter battleFlowPresenter = null;
        [SerializeField] private CombatTopHudPresenter combatTopHudPresenter = null;

        private bool _isBound;
        private CombatSessionView _currentState;
        private readonly ActionIdentityIndex _actionIdentityIndex = new ActionIdentityIndex();
        private readonly OverlayPriorityCoordinator _overlayCoordinator =
            new OverlayPriorityCoordinator();
        private readonly IdleTileInspectPort _idleTileInspect = new IdleTileInspectPort();

        public OverlayPriorityCoordinator OverlayCoordinator => _overlayCoordinator;

        public IdleTileInspectPort IdleTileInspect => _idleTileInspect;

        public event Action<string> CardSelected;
        public event Action<string> CardCancelled;
        public event Action<string, Vector2, CardDragPhase> CardDragChanged;
        public event Action<TimelineCell> TimelineSelected;
        public event Action<TimelineCell> TimelinePreviewRequested;
        public event Action TimelinePreviewCleared;
        public event Action ResolveRequested;
        public event Action<BattleRewardEntry> BattleRewardRequested;

        public bool IsBound => _isBound;

        public int TimelineCellCount => timelinePresenter == null ? 0 : timelinePresenter.CellCount;

        public CombatOccupantPresenter OccupantPresenter => occupantPresenter;

        public void ConfigureCards(IReadOnlyList<CardViewModel> cards)
        {
            ValidateDependencies();
            cardHandPresenter.ConfigureCards(cards);
        }

        public void Bind()
        {
            ValidateDependencies();
            if (_isBound)
            {
                return;
            }

            cardHandPresenter.Bind();
            timelinePresenter.Bind();
            hudPresenter.Bind();

            cardHandPresenter.CardSelected += HandleCardSelected;
            cardHandPresenter.CardCancelled += HandleCardCancelled;
            cardHandPresenter.CardDragChanged += HandleCardDragChanged;
            cardHandPresenter.CardHovered += HandleCardHovered;
            timelinePresenter.TimelineSelected += HandleTimelineSelected;
            timelinePresenter.TimelinePreviewRequested += HandleTimelinePreviewRequested;
            timelinePresenter.TimelinePreviewCleared += HandleTimelinePreviewCleared;
            timelinePresenter.ActionHovered += HandleActionHovered;
            hudPresenter.ResolveRequested += HandleResolveRequested;
            occupantPresenter.OccupantRemoved += HandleOccupantRemoved;
            if (battleFlowPresenter != null)
            {
                battleFlowPresenter.SettlementPresenter.RewardRequested +=
                    HandleBattleRewardRequested;
            }
            _isBound = true;
        }

        public void Unbind()
        {
            if (!_isBound)
            {
                return;
            }

            cardHandPresenter.CardSelected -= HandleCardSelected;
            cardHandPresenter.CardCancelled -= HandleCardCancelled;
            cardHandPresenter.CardDragChanged -= HandleCardDragChanged;
            cardHandPresenter.CardHovered -= HandleCardHovered;
            timelinePresenter.TimelineSelected -= HandleTimelineSelected;
            timelinePresenter.TimelinePreviewRequested -= HandleTimelinePreviewRequested;
            timelinePresenter.TimelinePreviewCleared -= HandleTimelinePreviewCleared;
            timelinePresenter.ActionHovered -= HandleActionHovered;
            hudPresenter.ResolveRequested -= HandleResolveRequested;
            occupantPresenter.OccupantRemoved -= HandleOccupantRemoved;
            if (battleFlowPresenter != null)
            {
                battleFlowPresenter.SettlementPresenter.RewardRequested -=
                    HandleBattleRewardRequested;
            }

            cardHandPresenter.Unbind();
            timelinePresenter.Unbind();
            hudPresenter.Unbind();
            ClearInspectedTile();
            _actionIdentityIndex.Clear();
            _currentState = null;
            _isBound = false;
        }

        public void Refresh(CombatSessionView state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            ValidateDependencies();
            _currentState = state;
            RebuildActionIdentityIndex(state);
            UpdateOverlayOwnership(state);
            if (!CanInspectTile())
            {
                _idleTileInspect.ClearAll();
            }
            cardHandPresenter.Refresh(state);
            boardRangePresenter.Refresh(state);
            timelinePresenter.Refresh(state);
            hudPresenter.Refresh(state);
            if (battleFlowPresenter != null && state.BattleFlow != null)
            {
                battleFlowPresenter.Apply(state.BattleFlow);
            }
            if (combatTopHudPresenter != null)
            {
                combatTopHudPresenter.Refresh(state);
            }
            RestorePersistentDetail();
        }

        public bool SetInspectedTile(HexCoord coordinate)
        {
            return CanInspectTile() && _idleTileInspect.Set(coordinate);
        }

        public bool ToggleInspectedTile(HexCoord coordinate)
        {
            return CanInspectTile() && _idleTileInspect.Toggle(coordinate);
        }

        public void ClearInspectedTile()
        {
            _idleTileInspect.ClearAll();
            _overlayCoordinator.ClearAll();
        }

        public void RenderTimelineAction(
            TimelineCell origin,
            IReadOnlyList<TimelineCell> shape,
            string label,
            Color color)
        {
            ValidateDependencies();
            timelinePresenter.RenderAction(origin, shape, label, color);
        }

        public void ClearTimelinePreview()
        {
            ValidateDependencies();
            timelinePresenter.ClearPreview();
        }

        public void ApplyTimelineClearResult(TimelineClearResult result)
        {
            ValidateDependencies();
            timelinePresenter.ApplyClearResult(result);
        }

        public void RegisterOccupantColumn(HexCoord coordinate, HexTileColumn column)
        {
            ValidateDependencies();
            occupantPresenter.RegisterColumn(coordinate, column);
        }

        public void RegisterExistingOccupant(
            CombatOccupantSnapshot occupant,
            CombatOccupantView view)
        {
            ValidateDependencies();
            occupantPresenter.RegisterExisting(occupant, view);
        }

        public void ApplyOccupantEffects(IReadOnlyList<OccupantEffectResult> results)
        {
            ValidateDependencies();
            occupantPresenter.Apply(results);
        }

        public void ApplyLifecycleChanges(
            IReadOnlyList<LifecycleOccupantChangeResult> results)
        {
            ValidateDependencies();
            occupantPresenter.ApplyLifecycleChanges(results);
        }

        private void OnEnable()
        {
            if (cardHandPresenter != null &&
                boardRangePresenter != null &&
                timelinePresenter != null &&
                hudPresenter != null &&
                occupantPresenter != null &&
                interactionOverlayPresenter != null)
            {
                Bind();
            }
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void ValidateDependencies()
        {
            if (cardHandPresenter == null)
            {
                throw MissingReference(nameof(cardHandPresenter));
            }

            if (boardRangePresenter == null)
            {
                throw MissingReference(nameof(boardRangePresenter));
            }

            if (timelinePresenter == null)
            {
                throw MissingReference(nameof(timelinePresenter));
            }

            if (hudPresenter == null)
            {
                throw MissingReference(nameof(hudPresenter));
            }

            if (occupantPresenter == null)
            {
                throw MissingReference(nameof(occupantPresenter));
            }

            if (interactionOverlayPresenter == null)
            {
                throw MissingReference(nameof(interactionOverlayPresenter));
            }
        }

        private InvalidOperationException MissingReference(string fieldName)
        {
            return new InvalidOperationException(
                name + " is missing serialized " + fieldName + " reference.");
        }

        private void HandleCardSelected(string stableId)
        {
            ClearInspectedTile();
            CardSelected?.Invoke(stableId);
        }

        private void HandleBattleRewardRequested(BattleRewardEntry rewardEntry)
        {
            BattleRewardRequested?.Invoke(rewardEntry);
        }

        private void HandleCardCancelled(string stableId)
        {
            CardCancelled?.Invoke(stableId);
        }

        private void HandleCardDragChanged(
            string stableId,
            Vector2 pointerPosition,
            CardDragPhase phase)
        {
            CardDragChanged?.Invoke(stableId, pointerPosition, phase);
        }

        private void HandleCardHovered(CardViewModel card, bool entered)
        {
            if (entered)
            {
                if (!_overlayCoordinator.TryAcquire(
                        CombatOverlayOwner.IdleActionHover,
                        card.ViewId,
                        out _))
                {
                    return;
                }

                interactionOverlayPresenter.ShowCard(card);
                var action = FindPlayerAction(card);
                if (action != null)
                {
                    timelinePresenter.HighlightAction(action.ActionId, true);
                    boardRangePresenter.HighlightAction(action, true);
                }

                return;
            }

            _overlayCoordinator.Release(CombatOverlayOwner.IdleActionHover, card.ViewId);
            var actionOnExit = FindPlayerAction(card);
            if (actionOnExit != null)
            {
                timelinePresenter.HighlightAction(actionOnExit.ActionId, false);
            }
            boardRangePresenter.HighlightAction(null, false);
            RestorePersistentDetail();
        }

        private void HandleActionHovered(
            TimelineActionPresentationSnapshot action,
            bool entered)
        {
            if (entered)
            {
                if (!_overlayCoordinator.TryAcquire(
                        CombatOverlayOwner.IdleActionHover,
                        action.ActionId.Value,
                        out _))
                {
                    return;
                }

                interactionOverlayPresenter.ShowAction(action);
                if (action.CardInstanceId.HasValue)
                {
                    cardHandPresenter.HighlightActionCardByViewId(
                        action.CardInstanceId.Value.ToString(),
                        true);
                }
                else if (action.CardStableId != null)
                {
                    cardHandPresenter.HighlightActionCard(action.CardStableId, true);
                }

                boardRangePresenter.HighlightAction(action, true);
                return;
            }

            _overlayCoordinator.Release(
                CombatOverlayOwner.IdleActionHover,
                action.ActionId.Value);
            if (action.CardInstanceId.HasValue)
            {
                cardHandPresenter.HighlightActionCardByViewId(
                    action.CardInstanceId.Value.ToString(),
                    false);
            }
            else if (action.CardStableId != null)
            {
                cardHandPresenter.HighlightActionCard(action.CardStableId, false);
            }

            boardRangePresenter.HighlightAction(null, false);
            RestorePersistentDetail();
        }

        private TimelineActionPresentationSnapshot FindPlayerAction(CardViewModel card)
        {
            if (_currentState == null || card == null)
            {
                return null;
            }

            if (TryParseCardInstanceId(card.ViewId, out var cardInstanceId))
            {
                for (var index = 0; index < _currentState.TimelineActions.Count; index++)
                {
                    var action = _currentState.TimelineActions[index];
                    if (action.CardInstanceId.HasValue &&
                        action.CardInstanceId.Value == cardInstanceId)
                    {
                        return action;
                    }
                }
            }

            TimelineActionPresentationSnapshot only = null;
            for (var index = 0; index < _currentState.TimelineActions.Count; index++)
            {
                var action = _currentState.TimelineActions[index];
                if (string.Equals(action.CardStableId, card.StableId, StringComparison.Ordinal))
                {
                    if (only != null)
                    {
                        return null;
                    }

                    only = action;
                }
            }

            return only;
        }

        private void RestorePersistentDetail()
        {
            if (_currentState != null && _currentState.SelectedStableId != null)
            {
                var card = _currentState.SelectedCardInstanceId.HasValue
                    ? cardHandPresenter.GetCardByViewId(
                        _currentState.SelectedCardInstanceId.Value.ToString())
                    : cardHandPresenter.GetCard(_currentState.SelectedStableId);
                if (card != null)
                {
                    interactionOverlayPresenter.ShowCard(card);
                    return;
                }
            }

            interactionOverlayPresenter.Clear();
        }

        private void HandleTimelineSelected(TimelineCell coordinate)
        {
            TimelineSelected?.Invoke(coordinate);
        }

        private void HandleTimelinePreviewRequested(TimelineCell coordinate)
        {
            TimelinePreviewRequested?.Invoke(coordinate);
        }

        private void HandleTimelinePreviewCleared()
        {
            TimelinePreviewCleared?.Invoke();
        }

        private void HandleResolveRequested()
        {
            ResolveRequested?.Invoke();
        }

        private void HandleOccupantRemoved(string runtimeId)
        {
            timelinePresenter.RemoveActionsBySource(runtimeId);
            boardRangePresenter.HighlightAction(null, false);
            ClearInspectedTile();
            interactionOverlayPresenter.Clear();
        }

        private void RebuildActionIdentityIndex(CombatSessionView state)
        {
            _actionIdentityIndex.Clear();
            for (var index = 0; index < state.TimelineActions.Count; index++)
            {
                var action = state.TimelineActions[index];
                _actionIdentityIndex.Add(new ActionIdentityLink(
                    action.ActionId,
                    action.CardStableId,
                    action.CardInstanceId,
                    action.SourceId,
                    action.TargetId));
            }
        }

        private void UpdateOverlayOwnership(CombatSessionView state)
        {
            _overlayCoordinator.ClearAll();
            if (state.BattleFlow != null && state.BattleFlow.IsInputLocked)
            {
                _overlayCoordinator.TryAcquire(
                    CombatOverlayOwner.Disabled,
                    "battle-input-lock",
                    out _);
                return;
            }

            switch (state.Phase)
            {
                case CombatSessionPhase.Disposed:
                case CombatSessionPhase.Committed:
                    _overlayCoordinator.TryAcquire(
                        CombatOverlayOwner.Disabled,
                        state.Phase.ToString(),
                        out _);
                    return;
                case CombatSessionPhase.Resolved:
                    _overlayCoordinator.TryAcquire(
                        CombatOverlayOwner.Resolving,
                        state.Phase.ToString(),
                        out _);
                    return;
                case CombatSessionPhase.TimelinePreview:
                    _overlayCoordinator.TryAcquire(
                        state.InteractionMode == CombatInteractionMode.TimelineClear
                            ? CombatOverlayOwner.Clear
                            : CombatOverlayOwner.Scheduling,
                        state.SelectedCardInstanceId.HasValue
                            ? state.SelectedCardInstanceId.Value.ToString()
                            : state.SelectedStableId,
                        out _);
                    return;
                case CombatSessionPhase.CardSelected:
                case CombatSessionPhase.TargetSelected:
                    _overlayCoordinator.TryAcquire(
                        state.InteractionMode == CombatInteractionMode.TimelineClear
                            ? CombatOverlayOwner.Clear
                            : CombatOverlayOwner.CardTargeting,
                        state.SelectedCardInstanceId.HasValue
                            ? state.SelectedCardInstanceId.Value.ToString()
                            : state.SelectedStableId,
                        out _);
                    return;
            }
        }

        private bool CanInspectTile()
        {
            return _currentState != null &&
                (_currentState.BattleFlow == null || !_currentState.BattleFlow.IsInputLocked) &&
                _currentState.SelectedCard == null &&
                (_currentState.Phase == CombatSessionPhase.Idle ||
                 _currentState.Phase == CombatSessionPhase.Cancelled);
        }

        private static bool TryParseCardInstanceId(string value, out CardInstanceId instanceId)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.IndexOf("/card:", StringComparison.Ordinal) < 0)
            {
                instanceId = default;
                return false;
            }

            instanceId = new CardInstanceId(value);
            return true;
        }
    }
}
