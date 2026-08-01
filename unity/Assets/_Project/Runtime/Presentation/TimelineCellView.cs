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
        [SerializeField] private bool hasDefaultAppearance;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private string defaultText = string.Empty;

        private bool _defaultAppearanceCaptured;
        private Color _defaultColor;
        private string _defaultText;

        public event Action<TimelineCell> Clicked;

        public event Action<TimelineCell> PointerEntered;

        public event Action PointerExited;

        public TimelineCell Coordinate => new TimelineCell(column, row);

        public Button Button => button;

        public Graphic Graphic => button == null ? null : button.targetGraphic;

        public Color DisplayColor => Graphic == null ? default : Graphic.color;

        public string DisplayText => label == null ? null : label.text;

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

        public void SetPreviewContent(string value, Color color)
        {
            if (button == null || label == null)
            {
                throw new InvalidOperationException(name + " has incomplete serialized timeline-cell references.");
            }

            label.text = value;
            button.targetGraphic.color = color;
        }

        public void ClearContent()
        {
            CaptureDefaultAppearance();
            SetContent(_defaultText, _defaultColor);
        }

        public void CaptureCurrentAsDefault()
        {
            if (button == null || button.targetGraphic == null || label == null)
            {
                throw new InvalidOperationException(name + " has incomplete serialized timeline-cell references.");
            }

            defaultColor = button.targetGraphic.color;
            defaultText = label.text;
            hasDefaultAppearance = true;
            _defaultColor = defaultColor;
            _defaultText = defaultText;
            _defaultAppearanceCaptured = true;
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
            CaptureDefaultAppearance();
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

        private void CaptureDefaultAppearance()
        {
            if (_defaultAppearanceCaptured || button == null || button.targetGraphic == null || label == null)
            {
                return;
            }

            _defaultColor = hasDefaultAppearance ? defaultColor : button.targetGraphic.color;
            _defaultText = hasDefaultAppearance ? defaultText : label.text;
            _defaultAppearanceCaptured = true;
        }
    }
}
