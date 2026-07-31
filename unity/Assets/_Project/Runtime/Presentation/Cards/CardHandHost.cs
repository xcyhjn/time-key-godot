using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeKey.Presentation.Cards
{
    /// <summary>
    /// Coordinates independent single-card views without knowing card rules.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CardHandHost : MonoBehaviour
    {
        private const float HandHeight = 280f;
        private const float SlotWidth = 145f;
        private const float MinimumSpacing = 102f;
        private const float MaximumSpacing = 116f;

        private readonly List<CardHandView> _cards = new List<CardHandView>();
        private readonly Dictionary<string, CardHandView> _cardsById =
            new Dictionary<string, CardHandView>(StringComparer.Ordinal);
        private RectTransform _cardContainer;
        private string _selectedStableId;

        public event Action<string> CardSelected;

        public event Action<string> CardCancelRequested;

        public event Action<string, Vector2, CardDragPhase> CardDragChanged;

        public IReadOnlyList<CardHandView> Cards => _cards;

        public string SelectedStableId => _selectedStableId;

        public int CardCount => _cards.Count;

        public void Build(IReadOnlyList<CardViewModel> viewModels)
        {
            if (viewModels == null)
            {
                throw new ArgumentNullException(nameof(viewModels));
            }

            var selectedIndex = FindSelectedIndex(viewModels);
            EnsureContainer();
            var requestedIds = new HashSet<string>(StringComparer.Ordinal);
            var orderedCards = new List<CardHandView>(viewModels.Count);

            for (var index = 0; index < viewModels.Count; index++)
            {
                var model = viewModels[index] ??
                    throw new ArgumentException("Card view models cannot contain null entries.", nameof(viewModels));
                if (!requestedIds.Add(model.StableId))
                {
                    throw new ArgumentException("Card stable IDs must be unique.", nameof(viewModels));
                }

                if (!_cardsById.TryGetValue(model.StableId, out var view))
                {
                    view = CreateCardView(model.StableId);
                    _cardsById.Add(model.StableId, view);
                }

                orderedCards.Add(view);

                var isSelected = index == selectedIndex;
                view.Build(isSelected == model.IsSelected
                    ? model
                    : new CardViewModel(model.StableId, model.Artwork, isSelected, model.IsInteractable));
                view.transform.SetParent(_cardContainer, false);
                view.transform.SetSiblingIndex(index);
            }

            for (var index = _cards.Count - 1; index >= 0; index--)
            {
                if (requestedIds.Contains(_cards[index].StableId))
                {
                    continue;
                }

                var stale = _cards[index];
                _cards.RemoveAt(index);
                _cardsById.Remove(stale.StableId);
                if (stale != null)
                {
                    Destroy(stale.gameObject);
                }
            }

            _cards.Clear();
            _cards.AddRange(orderedCards);

            _selectedStableId = selectedIndex >= 0 && selectedIndex < viewModels.Count
                ? viewModels[selectedIndex].StableId
                : null;
            LayoutCards();
            if (_selectedStableId != null && _cardsById.TryGetValue(_selectedStableId, out var selected))
            {
                selected.transform.SetAsLastSibling();
            }
        }

        public CardHandView GetCard(string stableId)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                return null;
            }

            return _cardsById.TryGetValue(stableId, out var view) ? view : null;
        }

        public bool RequestCancelSelectedCard()
        {
            if (_selectedStableId == null || !_cardsById.TryGetValue(_selectedStableId, out var selected))
            {
                return false;
            }

            return selected.RequestCancelSelectedCard();
        }

        public void SetInteractionState(CardHandInteractionState state)
        {
            foreach (var card in _cards)
            {
                card.SetInteractionState(card.StableId == _selectedStableId && state != CardHandInteractionState.Idle
                    ? state
                    : state == CardHandInteractionState.Disabled
                        ? CardHandInteractionState.Disabled
                        : CardHandInteractionState.Idle);
            }
        }

        public void ApplyVisualStateImmediate()
        {
            foreach (var card in _cards)
            {
                card.ApplyVisualStateImmediate();
            }
        }

        private static int FindSelectedIndex(IReadOnlyList<CardViewModel> viewModels)
        {
            var selectedIndex = -1;
            for (var index = 0; index < viewModels.Count; index++)
            {
                if (viewModels[index] == null || !viewModels[index].IsSelected)
                {
                    continue;
                }

                if (selectedIndex >= 0)
                {
                    throw new ArgumentException("At most one card may be selected.", nameof(viewModels));
                }

                selectedIndex = index;
            }

            return selectedIndex;
        }

        private void EnsureContainer()
        {
            if (_cardContainer != null)
            {
                return;
            }

            var hostRect = (RectTransform)transform;
            hostRect.anchorMin = Vector2.zero;
            hostRect.anchorMax = Vector2.right;
            hostRect.pivot = new Vector2(0.5f, 0f);
            hostRect.anchoredPosition = Vector2.zero;
            hostRect.sizeDelta = new Vector2(0f, HandHeight);

            var containerObject = new GameObject("Cards", typeof(RectTransform));
            containerObject.transform.SetParent(transform, false);
            _cardContainer = containerObject.GetComponent<RectTransform>();
            _cardContainer.anchorMin = Vector2.zero;
            _cardContainer.anchorMax = Vector2.one;
            _cardContainer.offsetMin = Vector2.zero;
            _cardContainer.offsetMax = Vector2.zero;
        }

        private CardHandView CreateCardView(string stableId)
        {
            var cardObject = new GameObject("Card-" + stableId, typeof(RectTransform));
            cardObject.transform.SetParent(_cardContainer, false);
            var view = cardObject.AddComponent<CardHandView>();
            view.CardSelected += HandleCardSelected;
            view.CardCancelRequested += HandleCardCancelRequested;
            view.CardDragChanged += HandleCardDragChanged;
            return view;
        }

        private void HandleCardSelected(string stableId)
        {
            if (_selectedStableId != null &&
                _selectedStableId != stableId &&
                _cardsById.TryGetValue(_selectedStableId, out var previous))
            {
                previous.RequestCancelSelectedCard();
            }

            _selectedStableId = stableId;
            if (_cardsById.TryGetValue(stableId, out var selected))
            {
                selected.transform.SetAsLastSibling();
            }

            CardSelected?.Invoke(stableId);
        }

        private void HandleCardCancelRequested(string stableId)
        {
            if (string.Equals(_selectedStableId, stableId, StringComparison.Ordinal))
            {
                _selectedStableId = null;
            }

            CardCancelRequested?.Invoke(stableId);
        }

        private void HandleCardDragChanged(string stableId, Vector2 pointerPosition, CardDragPhase phase)
        {
            CardDragChanged?.Invoke(stableId, pointerPosition, phase);
        }

        private void LayoutCards()
        {
            if (_cardContainer == null)
            {
                return;
            }

            var width = _cardContainer.rect.width;
            if (width <= 0f)
            {
                width = Screen.width;
            }

            var spacing = Mathf.Clamp(
                width / Mathf.Max(1f, _cards.Count + 0.6f),
                MinimumSpacing,
                MaximumSpacing);
            var center = (_cards.Count - 1) * 0.5f;
            for (var index = 0; index < _cards.Count; index++)
            {
                var cardRect = (RectTransform)_cards[index].transform;
                cardRect.anchorMin = new Vector2(0.5f, 0f);
                cardRect.anchorMax = new Vector2(0.5f, 0f);
                cardRect.pivot = new Vector2(0.5f, 0f);
                cardRect.sizeDelta = new Vector2(SlotWidth, HandHeight);
                cardRect.anchoredPosition = new Vector2(
                    (index - center) * spacing,
                    Mathf.Abs(index - center) * 3f);
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_cards.Count > 0)
            {
                LayoutCards();
            }
        }
    }
}
