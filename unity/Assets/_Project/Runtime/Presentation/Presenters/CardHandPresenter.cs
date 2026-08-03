using System;
using System.Collections.Generic;
using TimeKey.Application;
using TimeKey.Presentation.Cards;
using UnityEngine;

namespace TimeKey.Presentation.Presenters
{
    [DisallowMultipleComponent]
    public sealed class CardHandPresenter : MonoBehaviour
    {
        [SerializeField] private CardHandHost cardHand = null;

        private readonly List<CardViewModel> _cards = new List<CardViewModel>();
        private bool _isBound;

        public event Action<string> CardSelected;
        public event Action<string> CardCancelled;
        public event Action<string, Vector2, CardDragPhase> CardDragChanged;
        public event Action<CardViewModel, bool> CardHovered;

        public bool IsBound => _isBound;

        public void ConfigureCards(IReadOnlyList<CardViewModel> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            ValidateDependencies();
            _cards.Clear();
            for (var index = 0; index < cards.Count; index++)
            {
                _cards.Add(cards[index] ??
                    throw new ArgumentException("Card models cannot contain null.", nameof(cards)));
            }

            BuildCards(null);
        }

        public void Bind()
        {
            ValidateDependencies();
            if (_isBound)
            {
                return;
            }

            cardHand.CardSelected += HandleCardSelected;
            cardHand.CardCancelRequested += HandleCardCancelled;
            cardHand.CardDragChanged += HandleCardDragChanged;
            cardHand.CardHovered += HandleCardHovered;
            _isBound = true;
        }

        public void Unbind()
        {
            if (!_isBound)
            {
                return;
            }

            cardHand.CardSelected -= HandleCardSelected;
            cardHand.CardCancelRequested -= HandleCardCancelled;
            cardHand.CardDragChanged -= HandleCardDragChanged;
            cardHand.CardHovered -= HandleCardHovered;
            _isBound = false;
        }

        public void Refresh(CombatSessionView state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            ValidateDependencies();
            BuildCards(state);
            cardHand.SetInteractionState(ToInteractionState(state.Phase));
            cardHand.ApplyVisualStateImmediate();
        }

        private void BuildCards(CombatSessionView state)
        {
            if (state != null && state.BattleFlow != null)
            {
                BuildBattleFlowCards(state);
                return;
            }

            BuildCatalogCards(state == null ? null : state.SelectedStableId);
        }

        private void BuildCatalogCards(string selectedStableId)
        {
            var models = new List<CardViewModel>(_cards.Count);
            for (var index = 0; index < _cards.Count; index++)
            {
                var card = _cards[index];
                models.Add(new CardViewModel(
                    card.StableId,
                    card.Artwork,
                    string.Equals(card.StableId, selectedStableId, StringComparison.Ordinal),
                    card.IsInteractable,
                    card.DisplayName,
                    card.EffectDescription,
                    card.PlacementDescription));
            }

            cardHand.Build(models);
        }

        private void BuildBattleFlowCards(CombatSessionView state)
        {
            var hand = state.BattleFlow.Hand;
            var selectedId = state.SelectedCardInstanceId.HasValue
                ? state.SelectedCardInstanceId.Value.ToString()
                : null;
            var models = new List<CardViewModel>(hand.Count);
            for (var index = 0; index < hand.Count; index++)
            {
                var instance = hand[index];
                var template = GetCard(instance.StableId);
                if (template == null)
                {
                    throw new InvalidOperationException(
                        "The hand references an unknown card: " + instance.StableId + ".");
                }

                var viewId = instance.InstanceId.ToString();
                models.Add(new CardViewModel(
                    viewId,
                    template.StableId,
                    template.Artwork,
                    string.Equals(viewId, selectedId, StringComparison.Ordinal),
                    template.IsInteractable && !state.BattleFlow.IsInputLocked,
                    template.DisplayName,
                    template.EffectDescription,
                    template.PlacementDescription));
            }

            cardHand.Build(models);
        }

        private void OnEnable()
        {
            if (cardHand != null)
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
            if (cardHand == null)
            {
                throw new InvalidOperationException(
                    name + " is missing serialized cardHand reference.");
            }
        }

        private static CardHandInteractionState ToInteractionState(CombatSessionPhase phase)
        {
            switch (phase)
            {
                case CombatSessionPhase.CardSelected:
                    return CardHandInteractionState.Selected;
                case CombatSessionPhase.TargetSelected:
                    return CardHandInteractionState.Targeting;
                case CombatSessionPhase.TimelinePreview:
                    return CardHandInteractionState.Scheduling;
                case CombatSessionPhase.Committed:
                case CombatSessionPhase.Resolved:
                case CombatSessionPhase.Disposed:
                    return CardHandInteractionState.Disabled;
                default:
                    return CardHandInteractionState.Idle;
            }
        }

        private void HandleCardSelected(string stableId)
        {
            CardSelected?.Invoke(stableId);
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

        public void HighlightActionCard(string stableId, bool highlighted)
        {
            cardHand.HighlightCard(stableId, highlighted);
        }

        public void HighlightActionCardByViewId(string viewId, bool highlighted)
        {
            cardHand.HighlightCardByViewId(viewId, highlighted);
        }

        public CardViewModel GetCard(string stableId)
        {
            for (var index = 0; index < _cards.Count; index++)
            {
                if (string.Equals(_cards[index].StableId, stableId, StringComparison.Ordinal))
                {
                    return _cards[index];
                }
            }

            return null;
        }

        public CardViewModel GetCardByViewId(string viewId)
        {
            var card = cardHand.GetCardByViewId(viewId);
            return card == null ? null : card.ViewModel;
        }

        private void HandleCardHovered(CardViewModel card, bool entered)
        {
            CardHovered?.Invoke(card, entered);
        }
    }
}
