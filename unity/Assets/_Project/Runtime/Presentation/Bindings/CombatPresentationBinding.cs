using System;
using System.Collections.Generic;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Domain.BattleFlow;
using TimeKey.Presentation.BattleFlow;
using TimeKey.Presentation.Cards;
using TimeKey.Presentation.Occupants;
using TimeKey.Presentation.Presenters;
using TimeKey.Presentation.Terrain;
using TimeKey.Presentation.Tooltips;
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

        private bool _isBound;
        private CombatSessionView _currentState;

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
            cardHandPresenter.Refresh(state);
            boardRangePresenter.Refresh(state);
            timelinePresenter.Refresh(state);
            hudPresenter.Refresh(state);
            if (battleFlowPresenter != null && state.BattleFlow != null)
            {
                battleFlowPresenter.Apply(state.BattleFlow);
            }
            RestorePersistentDetail();
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
                interactionOverlayPresenter.ShowCard(card);
                var action = FindPlayerAction(card.StableId);
                if (action != null)
                {
                    timelinePresenter.HighlightAction(action.ActionId, true);
                    boardRangePresenter.HighlightAction(action, true);
                }

                return;
            }

            timelinePresenter.HighlightCardActions(card.StableId, false);
            boardRangePresenter.HighlightAction(null, false);
            RestorePersistentDetail();
        }

        private void HandleActionHovered(
            TimelineActionPresentationSnapshot action,
            bool entered)
        {
            if (entered)
            {
                interactionOverlayPresenter.ShowAction(action);
                if (action.CardStableId != null)
                {
                    cardHandPresenter.HighlightActionCard(action.CardStableId, true);
                }

                boardRangePresenter.HighlightAction(action, true);
                return;
            }

            if (action.CardStableId != null)
            {
                cardHandPresenter.HighlightActionCard(action.CardStableId, false);
            }

            boardRangePresenter.HighlightAction(null, false);
            RestorePersistentDetail();
        }

        private TimelineActionPresentationSnapshot FindPlayerAction(string stableId)
        {
            if (_currentState == null)
            {
                return null;
            }

            for (var index = 0; index < _currentState.TimelineActions.Count; index++)
            {
                var action = _currentState.TimelineActions[index];
                if (string.Equals(action.CardStableId, stableId, StringComparison.Ordinal))
                {
                    return action;
                }
            }

            return null;
        }

        private void RestorePersistentDetail()
        {
            if (_currentState != null && _currentState.SelectedStableId != null)
            {
                var card = cardHandPresenter.GetCard(_currentState.SelectedStableId);
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
            interactionOverlayPresenter.Clear();
        }
    }
}
