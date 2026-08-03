using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Presentation.Theming
{
    [DisallowMultipleComponent]
    public sealed class UiThemeBinder : MonoBehaviour
    {
        [System.Serializable]
        public sealed class LocalOverride
        {
            public bool overrideFillColor;
            public Color fillColor = Color.white;
            public bool overrideTextColor;
            public Color textColor = Color.white;
        }

        [SerializeField] private UiThemeScope scope;
        [SerializeField] private UiStyleId styleId = UiStyleId.Panel;
        [SerializeField] private Graphic background;
        [SerializeField] private Text text;
        [SerializeField] private Selectable selectable;
        [SerializeField] private bool applyOnEnable = true;
        [SerializeField] private LocalOverride localOverride = new LocalOverride();

        private bool _bound;

        public UiStyleId StyleId => styleId;

        public bool IsBound => _bound;

        private void OnEnable()
        {
            if (applyOnEnable)
            {
                Rebind();
            }
        }

        private void OnDisable()
        {
            _bound = false;
        }

        public void Rebind()
        {
            if (scope == null)
            {
                scope = GetComponentInParent<UiThemeScope>();
            }

            if (scope == null || !scope.TryGet(styleId, out var style))
            {
                _bound = false;
                return;
            }

            Apply(style);
            _bound = true;
        }

        private void Apply(TimeKeyUiTheme.UiStyleDefinition style)
        {
            if (background != null)
            {
                var image = background as Image;
                if (image != null)
                {
                    image.sprite = style.frame.backgroundSprite;
                    image.type = style.frame.imageType;
                }

                background.color = localOverride.overrideFillColor
                    ? localOverride.fillColor
                    : style.frame.fillColor;
            }

            if (text != null)
            {
                text.font = style.text.font;
                text.fontSize = style.text.fontSize;
                text.color = localOverride.overrideTextColor
                    ? localOverride.textColor
                    : style.text.normalColor;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
            }

            if (selectable is Button)
            {
                var button = (Button)selectable;
                var colors = button.colors;
                colors.normalColor = style.button.normalColor;
                colors.highlightedColor = style.button.highlightedColor;
                colors.pressedColor = style.button.pressedColor;
                colors.selectedColor = style.button.selectedColor;
                colors.disabledColor = style.button.disabledColor;
                button.colors = colors;
            }
        }
    }
}
