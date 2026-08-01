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
        [SerializeField] private RectTransform cardContainer = null;
        [SerializeField] private CardHandView cardViewPrefab = null;
        [SerializeField] private float slotWidth = 125f;
        [SerializeField] private float minimumSpacing = 92f;
        [SerializeField] private float maximumSpacing = 126f;
        [SerializeField] private float selectedReservedExtent = 40f;

        private readonly List<CardHandView> _cards = new List<CardHandView>();
        private readonly Dictionary<string, CardHandView> _cardsById =
            new Dictionary<string, CardHandView>(StringComparer.Ordinal);
        private string _selectedStableId;

        public event Action<string> CardSelected;

        public event Action<string> CardCancelRequested;

        public event Action<string, Vector2, CardDragPhase> CardDragChanged;

        public event Action<CardViewModel, bool> CardHovered;

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
                view.transform.SetParent(cardContainer, false);
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

        public void HighlightCard(string stableId, bool highlighted)
        {
            if (stableId != null && _cardsById.TryGetValue(stableId, out var card))
            {
                card.SetMappedHighlight(highlighted);
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
            if (cardContainer != null)
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
            cardContainer = containerObject.GetComponent<RectTransform>();
            cardContainer.anchorMin = Vector2.zero;
            cardContainer.anchorMax = Vector2.one;
            cardContainer.offsetMin = Vector2.zero;
            cardContainer.offsetMax = Vector2.zero;
        }

        private CardHandView CreateCardView(string stableId)
        {
            CardHandView view;
            if (cardViewPrefab != null)
            {
                view = Instantiate(cardViewPrefab, cardContainer, false);
                view.name = "Card-" + stableId;
            }
            else
            {
                var cardObject = new GameObject("Card-" + stableId, typeof(RectTransform));
                cardObject.transform.SetParent(cardContainer, false);
                view = cardObject.AddComponent<CardHandView>();
            }
            view.CardSelected += HandleCardSelected;
            view.CardCancelRequested += HandleCardCancelRequested;
            view.CardDragChanged += HandleCardDragChanged;
            view.CardHovered += HandleCardHovered;
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
            LayoutCards();
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
                LayoutCards();
            }

            CardCancelRequested?.Invoke(stableId);
        }

        private void HandleCardDragChanged(string stableId, Vector2 pointerPosition, CardDragPhase phase)
        {
            CardDragChanged?.Invoke(stableId, pointerPosition, phase);
        }

        private void HandleCardHovered(CardViewModel card, bool entered)
        {
            CardHovered?.Invoke(card, entered);
        }

        private void LayoutCards()
        {
            if (cardContainer == null)
            {
                return;
            }

            var width = cardContainer.rect.width;
            if (width <= 0f)
            {
                width = Screen.width;
            }

            var reserve = _selectedStableId == null ? 0f : selectedReservedExtent * 2f;
            var spacing = _cards.Count <= 1
                ? 0f
                : Mathf.Clamp(
                    (width - slotWidth - reserve) / (_cards.Count - 1f),
                    minimumSpacing,
                    maximumSpacing);
            var center = (_cards.Count - 1) * 0.5f;
            var selectedIndex = _selectedStableId == null
                ? -1
                : _cards.FindIndex(card => card.StableId == _selectedStableId);
            for (var index = 0; index < _cards.Count; index++)
            {
                var cardRect = (RectTransform)_cards[index].transform;
                cardRect.anchorMin = new Vector2(0.5f, 0f);
                cardRect.anchorMax = new Vector2(0.5f, 0f);
                cardRect.pivot = new Vector2(0.5f, 0f);
                cardRect.sizeDelta = new Vector2(slotWidth, HandHeight);
                var reservedOffset = selectedIndex < 0 || index == selectedIndex
                    ? 0f
                    : index < selectedIndex
                        ? -selectedReservedExtent
                        : selectedReservedExtent;
                cardRect.anchoredPosition = new Vector2(
                    ((index - center) * spacing) + reservedOffset,
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
