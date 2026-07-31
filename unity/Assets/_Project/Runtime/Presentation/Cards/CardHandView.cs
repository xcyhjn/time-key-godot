using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimeKey.Presentation.Cards
{
    public enum CardHandInteractionState
    {
        Idle,
        Selected,
        Targeting,
        Scheduling,
        Dragging,
        Disabled
    }

    public enum CardDragPhase
    {
        Started,
        Moved,
        Ended,
        Cancelled
    }

    public sealed class CardViewModel
    {
        public CardViewModel(string stableId, Sprite artwork, bool isSelected, bool isInteractable)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                throw new ArgumentException("A stable card ID is required.", nameof(stableId));
            }

            StableId = stableId;
            Artwork = artwork != null
                ? artwork
                : throw new ArgumentNullException(nameof(artwork));
            IsSelected = isSelected;
            IsInteractable = isInteractable;
        }

        public string StableId { get; }

        public Sprite Artwork { get; }

        public bool IsSelected { get; }

        public bool IsInteractable { get; }
    }

    [RequireComponent(typeof(RectTransform))]
    public sealed class CardHandView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IScrollHandler
    {
        public static readonly Vector2 DesignCardSize = new Vector2(125f, 175f);

        private const float HandHeight = 280f;
        private const float BottomMargin = 8f;
        private const float HoverLift = 30f;
        private const float SelectedLift = 80f;
        private const float HoverScale = 1.10f;
        private const float SelectedScale = 1.50f;
        private const float DragScale = 1.55f;
        private const float AnimationResponse = 18f;

        private RectTransform _slot;
        private RectTransform _cardVisual;
        private Image _shadow;
        private Image _artwork;
        private Outline _selectionOutline;
        private CanvasGroup _canvasGroup;
        private CardViewModel _viewModel;
        private bool _isHovered;
        private bool _isDragging;
        private CardHandInteractionState _stateBeforeDrag;
        private Transform _dragOriginalParent;
        private int _dragOriginalSiblingIndex;
        private Vector2 _dragOriginalAnchoredPosition;
        private Vector3 _dragOriginalScale;
        private Quaternion _dragOriginalRotation;
        private Vector2 _lastDragScreenPosition;

        public event Action<string> CardSelected;

        public event Action<string> CardCancelRequested;

        public event Action<string, Vector2, CardDragPhase> CardDragChanged;

        public string StableId => _viewModel == null ? null : _viewModel.StableId;

        public CardHandInteractionState InteractionState { get; private set; }

        public bool IsHovered => _isHovered;

        public bool IsDragging => _isDragging;

        public RectTransform CardVisual => _cardVisual;

        public Image Artwork => _artwork;

        public void Build(CardViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            EnsureHierarchy();

            if (_isDragging)
            {
                RestoreAfterDrag();
            }

            _artwork.sprite = viewModel.Artwork;
            _shadow.sprite = viewModel.Artwork;
            _artwork.preserveAspect = true;
            _shadow.preserveAspect = true;
            _canvasGroup.alpha = viewModel.IsInteractable ? 1f : 0.58f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = viewModel.IsInteractable;
            _isHovered = false;
            SetInteractionState(viewModel.IsSelected
                ? CardHandInteractionState.Selected
                : CardHandInteractionState.Idle);
        }

        public void SetInteractionState(CardHandInteractionState state)
        {
            if (_isDragging && state != CardHandInteractionState.Dragging)
            {
                if (state == CardHandInteractionState.Idle || state == CardHandInteractionState.Disabled)
                {
                    RestoreAfterDrag();
                }
                else
                {
                    _stateBeforeDrag = state;
                    return;
                }
            }

            InteractionState = state;
            if (state != CardHandInteractionState.Idle)
            {
                _isHovered = false;
            }

            if (_selectionOutline != null)
            {
                _selectionOutline.enabled = IsSelectionActive(state);
            }
        }

        public bool RequestCancelSelectedCard()
        {
            if (_viewModel == null || !IsSelectionActive(InteractionState))
            {
                return false;
            }

            var stableId = _viewModel.StableId;
            if (_isDragging)
            {
                CardDragChanged?.Invoke(stableId, _lastDragScreenPosition, CardDragPhase.Cancelled);
                RestoreAfterDrag();
            }

            SetInteractionState(CardHandInteractionState.Idle);
            CardCancelRequested?.Invoke(stableId);
            return true;
        }

        public void ApplyVisualStateImmediate()
        {
            if (_cardVisual == null || _isDragging)
            {
                return;
            }

            var selected = IsSelectionActive(InteractionState);
            var targetLift = selected ? SelectedLift : _isHovered ? HoverLift : 0f;
            var targetScale = selected ? SelectedScale : _isHovered ? HoverScale : 1f;
            _cardVisual.anchoredPosition = new Vector2(0f, targetLift);
            _cardVisual.localScale = Vector3.one * targetScale;
            _cardVisual.localRotation = Quaternion.identity;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            eventData.Use();
            if (CanInteract() && InteractionState == CardHandInteractionState.Idle)
            {
                _isHovered = true;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            eventData.Use();
            _isHovered = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            eventData.Use();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            eventData.Use();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            eventData.Use();
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                RequestCancelSelectedCard();
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left ||
                !CanInteract() ||
                InteractionState != CardHandInteractionState.Idle)
            {
                return;
            }

            SetInteractionState(CardHandInteractionState.Selected);
            CardSelected?.Invoke(_viewModel.StableId);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            eventData.Use();
            if (!CanInteract() || !IsSelectionActive(InteractionState) || _isDragging)
            {
                return;
            }

            _isDragging = true;
            _stateBeforeDrag = InteractionState;
            InteractionState = CardHandInteractionState.Dragging;
            _lastDragScreenPosition = eventData.position;
            CaptureDragPose();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _cardVisual.SetParent(canvas.transform, true);
                _cardVisual.SetAsLastSibling();
            }

            MoveDragVisual(eventData);
            CardDragChanged?.Invoke(_viewModel.StableId, eventData.position, CardDragPhase.Started);
        }

        public void OnDrag(PointerEventData eventData)
        {
            eventData.Use();
            if (!_isDragging)
            {
                return;
            }

            _lastDragScreenPosition = eventData.position;
            MoveDragVisual(eventData);
            CardDragChanged?.Invoke(_viewModel.StableId, eventData.position, CardDragPhase.Moved);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            eventData.Use();
            if (!_isDragging)
            {
                return;
            }

            _lastDragScreenPosition = eventData.position;
            CardDragChanged?.Invoke(_viewModel.StableId, eventData.position, CardDragPhase.Ended);
            RestoreAfterDrag();
            SetInteractionState(_stateBeforeDrag);
        }

        public void OnScroll(PointerEventData eventData)
        {
            eventData.Use();
        }

        private void Update()
        {
            if (IsSelectionActive(InteractionState) &&
                (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
            {
                RequestCancelSelectedCard();
            }
        }

        private void LateUpdate()
        {
            if (_cardVisual == null || _isDragging)
            {
                return;
            }

            var selected = IsSelectionActive(InteractionState);
            var targetLift = selected ? SelectedLift : _isHovered ? HoverLift : 0f;
            var targetScale = selected ? SelectedScale : _isHovered ? HoverScale : 1f;
            var blend = 1f - Mathf.Exp(-AnimationResponse * Mathf.Max(Time.unscaledDeltaTime, 0.0001f));

            _cardVisual.anchoredPosition = Vector2.Lerp(
                _cardVisual.anchoredPosition,
                new Vector2(0f, targetLift),
                blend);
            _cardVisual.localScale = Vector3.Lerp(
                _cardVisual.localScale,
                Vector3.one * targetScale,
                blend);
            _cardVisual.localRotation = Quaternion.Slerp(
                _cardVisual.localRotation,
                Quaternion.identity,
                blend);

            if (Vector2.Distance(_cardVisual.anchoredPosition, new Vector2(0f, targetLift)) < 0.75f)
            {
                _cardVisual.anchoredPosition = new Vector2(0f, targetLift);
            }

            if (Vector3.Distance(_cardVisual.localScale, Vector3.one * targetScale) < 0.005f)
            {
                _cardVisual.localScale = Vector3.one * targetScale;
            }

            if (Quaternion.Angle(_cardVisual.localRotation, Quaternion.identity) < 0.1f)
            {
                _cardVisual.localRotation = Quaternion.identity;
            }
        }

        private bool CanInteract()
        {
            return _viewModel != null &&
                _viewModel.IsInteractable &&
                InteractionState != CardHandInteractionState.Disabled;
        }

        private static bool IsSelectionActive(CardHandInteractionState state)
        {
            return state == CardHandInteractionState.Selected ||
                state == CardHandInteractionState.Targeting ||
                state == CardHandInteractionState.Scheduling ||
                state == CardHandInteractionState.Dragging;
        }

        private void EnsureHierarchy()
        {
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.right;
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(0f, HandHeight);

            if (_slot == null)
            {
                _slot = CreateRect("CardSlot", root);
                _slot.anchorMin = new Vector2(0.5f, 0f);
                _slot.anchorMax = new Vector2(0.5f, 0f);
                _slot.pivot = new Vector2(0.5f, 0.5f);
                _slot.sizeDelta = DesignCardSize;
                _slot.anchoredPosition = new Vector2(0f, BottomMargin + (DesignCardSize.y * 0.5f));
            }

            if (_cardVisual == null)
            {
                _cardVisual = CreateRect("CardVisual", _slot);
                Stretch(_cardVisual);

                var shadowRect = CreateRect("Shadow", _cardVisual);
                Stretch(shadowRect);
                shadowRect.anchoredPosition = new Vector2(8f, -10f);
                _shadow = shadowRect.gameObject.AddComponent<Image>();
                _shadow.color = new Color(0f, 0f, 0f, 0.48f);
                _shadow.raycastTarget = false;

                var artworkRect = CreateRect("Artwork", _cardVisual);
                Stretch(artworkRect);
                _artwork = artworkRect.gameObject.AddComponent<Image>();
                _artwork.color = Color.white;
                _artwork.raycastTarget = true;
                _selectionOutline = artworkRect.gameObject.AddComponent<Outline>();
                _selectionOutline.effectColor = new Color(1f, 0.78f, 0.24f, 0.95f);
                _selectionOutline.effectDistance = new Vector2(3f, -3f);
                _selectionOutline.useGraphicAlpha = true;
                _selectionOutline.enabled = false;
                _canvasGroup = _cardVisual.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void CaptureDragPose()
        {
            _dragOriginalParent = _cardVisual.parent;
            _dragOriginalSiblingIndex = _cardVisual.GetSiblingIndex();
            _dragOriginalAnchoredPosition = _cardVisual.anchoredPosition;
            _dragOriginalScale = _cardVisual.localScale;
            _dragOriginalRotation = _cardVisual.localRotation;
        }

        private void RestoreAfterDrag()
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;
            _cardVisual.SetParent(_dragOriginalParent, false);
            _cardVisual.SetSiblingIndex(_dragOriginalSiblingIndex);
            _cardVisual.anchoredPosition = _dragOriginalAnchoredPosition;
            _cardVisual.localScale = _dragOriginalScale;
            _cardVisual.localRotation = _dragOriginalRotation;
        }

        private void MoveDragVisual(PointerEventData eventData)
        {
            var canvas = GetComponentInParent<Canvas>();
            var canvasRect = canvas == null ? null : canvas.transform as RectTransform;
            if (canvasRect == null)
            {
                _cardVisual.position = eventData.position;
                return;
            }

            var eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : eventData.pressEventCamera ?? canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventCamera,
                out var localPoint))
            {
                _cardVisual.anchoredPosition = localPoint;
            }

            _cardVisual.localScale = Vector3.one * DragScale;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
