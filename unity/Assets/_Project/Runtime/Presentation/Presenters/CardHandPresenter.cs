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
            _isBound = false;
        }

        public void Refresh(CombatSessionView state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            ValidateDependencies();
            BuildCards(state.SelectedStableId);
            cardHand.SetInteractionState(ToInteractionState(state.Phase));
            cardHand.ApplyVisualStateImmediate();
        }

        private void BuildCards(string selectedStableId)
        {
            var models = new List<CardViewModel>(_cards.Count);
            for (var index = 0; index < _cards.Count; index++)
            {
                var card = _cards[index];
                models.Add(new CardViewModel(
                    card.StableId,
                    card.Artwork,
                    string.Equals(card.StableId, selectedStableId, StringComparison.Ordinal),
                    card.IsInteractable));
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
    }
}
