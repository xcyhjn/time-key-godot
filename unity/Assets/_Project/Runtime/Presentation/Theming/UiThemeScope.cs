using UnityEngine;

namespace TimeKey.Presentation.Theming
{
    [DisallowMultipleComponent]
    public sealed class UiThemeScope : MonoBehaviour
    {
        [SerializeField] private TimeKeyUiTheme theme;
        [SerializeField] private UiThemeScope parentScope;

        public TimeKeyUiTheme Theme => theme != null
            ? theme
            : parentScope == null ? null : parentScope.Theme;

        public void SetTheme(TimeKeyUiTheme value)
        {
            theme = value;
        }

        public bool TryGet(UiStyleId id, out TimeKeyUiTheme.UiStyleDefinition style)
        {
            var resolved = Theme;
            if (resolved != null)
            {
                return resolved.TryGet(id, out style);
            }

            style = null;
            return false;
        }
    }
}
