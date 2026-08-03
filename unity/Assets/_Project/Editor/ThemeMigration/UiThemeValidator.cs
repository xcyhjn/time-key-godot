using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TimeKey.Presentation.Theming;

namespace TimeKey.Editor.ThemeMigration
{
    public static class UiThemeValidator
    {
        public const string DefaultThemePath =
            "Assets/_Project/Resources/UIThemes/TimeKeyDefaultUiTheme.asset";
        public const string SilverFontPath =
            "Assets/_Project/Resources/Fonts/Silver.ttf";

        [MenuItem("TimeKey/Migration/Validate UI Theme")]
        public static void ValidateDefaultThemeMenu()
        {
            var diagnostics = ValidateDefaultTheme();
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.IsError)
                {
                    Debug.LogError(diagnostic.Message);
                }
                else
                {
                    Debug.Log(diagnostic.Message);
                }
            }
        }

        public static IReadOnlyList<Diagnostic> ValidateDefaultTheme()
        {
            var diagnostics = new List<Diagnostic>();
            var theme = AssetDatabase.LoadAssetAtPath<TimeKeyUiTheme>(DefaultThemePath);
            var silver = AssetDatabase.LoadAssetAtPath<Font>(SilverFontPath);
            if (theme == null)
            {
                diagnostics.Add(new Diagnostic(true, "Missing UI theme: " + DefaultThemePath));
                return diagnostics;
            }

            return Validate(theme, silver);
        }

        public static IReadOnlyList<Diagnostic> Validate(
            TimeKeyUiTheme theme,
            Font silver)
        {
            var diagnostics = new List<Diagnostic>();
            if (theme == null)
            {
                diagnostics.Add(new Diagnostic(true, "Theme reference is missing."));
                return diagnostics;
            }

            if (silver == null)
            {
                diagnostics.Add(new Diagnostic(true, "Silver font asset is missing."));
            }

            var seen = new HashSet<UiStyleId>();
            foreach (var style in theme.Styles)
            {
                if (style == null)
                {
                    diagnostics.Add(new Diagnostic(true, "Theme contains a null style."));
                    continue;
                }

                if (!seen.Add(style.id))
                {
                    diagnostics.Add(new Diagnostic(true, "Duplicate UI style: " + style.id));
                }

                if (style.text.font == null || style.text.font != silver)
                {
                    diagnostics.Add(new Diagnostic(true, style.id + " must use Silver font."));
                }

                if (style.frame.backgroundSprite == null)
                {
                    diagnostics.Add(new Diagnostic(true, style.id + " has no frame sprite."));
                }
                else if (style.frame.imageType == UnityEngine.UI.Image.Type.Sliced &&
                    IsZeroBorder(style.frame.backgroundSprite.border))
                {
                    diagnostics.Add(new Diagnostic(
                        true,
                        style.id + " uses 9-slice without a Sprite border."));
                }
            }

            if (seen.Count != System.Enum.GetValues(typeof(UiStyleId)).Length)
            {
                diagnostics.Add(new Diagnostic(true, "Theme does not define every UiStyleId."));
            }

            if (diagnostics.Count == 0)
            {
                diagnostics.Add(new Diagnostic(false, "TIMEKEY_UI_THEME_VALIDATION_PASS"));
            }

            return diagnostics;
        }

        private static bool IsZeroBorder(Vector4 border)
        {
            return Mathf.Approximately(border.x, 0f) &&
                Mathf.Approximately(border.y, 0f) &&
                Mathf.Approximately(border.z, 0f) &&
                Mathf.Approximately(border.w, 0f);
        }

        public readonly struct Diagnostic
        {
            public Diagnostic(bool isError, string message)
            {
                IsError = isError;
                Message = message;
            }

            public bool IsError { get; }
            public string Message { get; }
        }
    }
}
