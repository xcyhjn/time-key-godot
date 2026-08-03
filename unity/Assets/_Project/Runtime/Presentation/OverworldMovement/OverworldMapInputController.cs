using UnityEngine;
using UnityEngine.EventSystems;

namespace TimeKey.Presentation.OverworldMovement
{
    [DisallowMultipleComponent]
    public sealed class OverworldMapInputController : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IScrollHandler
    {
        [SerializeField] private RectTransform mapHost;
        [SerializeField] private float dragThreshold = 5f;
        [SerializeField] private float keyboardPanSpeed = 420f;
        [SerializeField] private float scrollStep = 0.1f;
        [SerializeField] private float minimumScale = 0.7f;
        [SerializeField] private float maximumScale = 1.25f;

        private Vector2 _dragStart;
        private Vector2 _hostStart;
        private bool _dragging;
        private bool _suppressClick;
        private bool _interactionLocked;

        public bool ShouldSuppressClick => _suppressClick;
        public bool IsDragging => _dragging;

        public void BeginPointerGesture()
        {
            _suppressClick = false;
        }

        private void Update()
        {
            if (_interactionLocked || mapHost == null)
            {
                return;
            }

            var direction = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            mapHost.anchoredPosition += direction *
                (keyboardPanSpeed * Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            _dragging = false;
            _suppressClick = false;
        }

        public void SetInteractionLocked(bool locked)
        {
            _interactionLocked = locked;
            if (locked)
            {
                _dragging = false;
                _suppressClick = false;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_interactionLocked || mapHost == null ||
                eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _dragging = true;
            _suppressClick = false;
            _dragStart = eventData.position;
            _hostStart = mapHost.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || _interactionLocked || mapHost == null)
            {
                return;
            }

            var delta = eventData.position - _dragStart;
            if (delta.magnitude > dragThreshold)
            {
                _suppressClick = true;
            }

            mapHost.anchoredPosition = _hostStart + delta;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (_interactionLocked || mapHost == null ||
                Mathf.Approximately(eventData.scrollDelta.y, 0f))
            {
                return;
            }

            var next = Mathf.Clamp(
                mapHost.localScale.x + Mathf.Sign(eventData.scrollDelta.y) * scrollStep,
                minimumScale,
                maximumScale);
            mapHost.localScale = new Vector3(next, next, 1f);
        }
    }
}
