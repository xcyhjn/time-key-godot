using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimeKey.Presentation.MainMenu
{
    [DisallowMultipleComponent]
    public sealed class MainMenuButtonStateVisual : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [SerializeField] private Button button = null;
        [SerializeField] private Graphic background = null;
        [SerializeField] private Text label = null;
        [SerializeField] private Texture normalTexture = null;
        [SerializeField] private Texture activeTexture = null;

        private bool _hovered;
        private bool _pressed;
        private bool _selected;
        private bool _lastInteractable;

        private void OnEnable()
        {
            _lastInteractable = button != null && button.interactable;
            Refresh();
        }

        private void Update()
        {
            var interactable = button != null && button.interactable;
            if (interactable != _lastInteractable)
            {
                _lastInteractable = interactable;
                Refresh();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Refresh();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            Refresh();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            Refresh();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            Refresh();
        }

        public void OnSelect(BaseEventData eventData)
        {
            _selected = true;
            Refresh();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
            _pressed = false;
            Refresh();
        }

        private void Refresh()
        {
            if (button == null || background == null || label == null)
            {
                return;
            }

            if (!button.interactable)
            {
                ApplyTexture(normalTexture);
                background.color = new Color(0.30f, 0.32f, 0.32f, 0.72f);
                label.color = new Color(0.46f, 0.48f, 0.48f, 1f);
            }
            else if (_pressed)
            {
                ApplyTexture(activeTexture);
                background.color = activeTexture == null
                    ? new Color(0.12f, 0.36f, 0.34f, 0.98f)
                    : new Color(0.64f, 0.92f, 0.88f, 1f);
                label.color = new Color(0.96f, 1f, 0.98f, 1f);
            }
            else if (_hovered || _selected)
            {
                ApplyTexture(activeTexture);
                background.color = activeTexture == null
                    ? new Color(0.54f, 0.36f, 0.10f, 0.98f)
                    : Color.white;
                label.color = new Color(1f, 0.96f, 0.78f, 1f);
            }
            else
            {
                ApplyTexture(normalTexture);
                background.color = normalTexture == null
                    ? new Color(0.035f, 0.05f, 0.06f, 0.91f)
                    : Color.white;
                label.color = new Color(0.98f, 0.95f, 0.84f, 1f);
            }
        }

        private void ApplyTexture(Texture texture)
        {
            if (background is RawImage rawImage && texture != null)
            {
                rawImage.texture = texture;
            }
        }
    }
}
