using System;
using TimeKey.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimeKey.Presentation
{
    [RequireComponent(typeof(RectTransform), typeof(Button))]
    public sealed class TimelineCellView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private int column;
        [SerializeField] private int row;
        [SerializeField] private Button button = null;
        [SerializeField] private Text label = null;

        public event Action<TimelineCell> Clicked;

        public event Action<TimelineCell> PointerEntered;

        public event Action PointerExited;

        public TimelineCell Coordinate => new TimelineCell(column, row);

        public Button Button => button;

        public Graphic Graphic => button == null ? null : button.targetGraphic;

        public void SetContent(string value, Color color)
        {
            if (button == null || label == null)
            {
                throw new InvalidOperationException(name + " has incomplete serialized timeline-cell references.");
            }

            label.text = value;
            button.targetGraphic.color = color;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.90f, 0.96f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.82f, 0.86f, 1f);
            colors.disabledColor = new Color(0.42f, 0.44f, 0.45f, 0.75f);
            button.colors = colors;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PointerEntered?.Invoke(Coordinate);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PointerExited?.Invoke();
        }

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            button.onClick.RemoveListener(RaiseClicked);
            button.onClick.AddListener(RaiseClicked);
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(RaiseClicked);
            }
        }

        private void RaiseClicked()
        {
            Clicked?.Invoke(Coordinate);
        }
    }
}
