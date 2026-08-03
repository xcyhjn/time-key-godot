using System;
using System.Collections.Generic;
using TimeKey.Presentation.Theming;
using UnityEditor;
using UnityEngine;

namespace TimeKey.Editor.ThemeMigration
{
    public sealed class UiThemePreviewWindow : EditorWindow
    {
        private const float SwatchWidth = 84f;
        private const float SwatchHeight = 22f;

        private TimeKeyUiTheme _theme;
        private IReadOnlyList<UiThemeValidator.Diagnostic> _diagnostics;
        private Vector2 _scrollPosition;

        [MenuItem("TimeKey/Migration/Open UI Theme Preview")]
        public static void ShowWindow()
        {
            var window = GetWindow<UiThemePreviewWindow>();
            window.titleContent = new GUIContent("UI Theme Preview");
            window.minSize = new Vector2(760f, 480f);
            window.RefreshTheme();
            window.Show();
        }

        private void OnEnable()
        {
            RefreshTheme();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("TimeKey UI Theme Preview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(UiThemeValidator.DefaultThemePath, EditorStyles.miniLabel);

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", GUILayout.Width(90f)))
                {
                    RefreshTheme();
                }

                if (GUILayout.Button("Validate", GUILayout.Width(90f)))
                {
                    _diagnostics = UiThemeValidator.ValidateDefaultTheme();
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(
                    _theme == null ? "Theme not loaded" : _theme.name,
                    EditorStyles.miniLabel,
                    GUILayout.MaxWidth(260f));
            }

            DrawDiagnostics();
            EditorGUILayout.Space(4f);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (UiStyleId styleId in Enum.GetValues(typeof(UiStyleId)))
            {
                DrawStyle(styleId);
            }

            EditorGUILayout.EndScrollView();
        }

        private void RefreshTheme()
        {
            _theme = AssetDatabase.LoadAssetAtPath<TimeKeyUiTheme>(UiThemeValidator.DefaultThemePath);
            _diagnostics = null;
            Repaint();
        }

        private void DrawDiagnostics()
        {
            if (_diagnostics == null)
            {
                return;
            }

            for (var index = 0; index < _diagnostics.Count; index++)
            {
                var diagnostic = _diagnostics[index];
                EditorGUILayout.HelpBox(
                    diagnostic.Message,
                    diagnostic.IsError ? MessageType.Error : MessageType.Info);
            }
        }

        private void DrawStyle(UiStyleId styleId)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(styleId.ToString(), EditorStyles.boldLabel);
                if (_theme == null || !_theme.TryGet(styleId, out var style) || style == null)
                {
                    EditorGUILayout.HelpBox("Missing style definition.", MessageType.Error);
                    return;
                }

                DrawTextStyle(style.text);
                DrawFrameStyle(style.frame);
                DrawButtonStyle(style.button);
            }
        }

        private static void DrawTextStyle(TimeKeyUiTheme.UiTextStyle textStyle)
        {
            EditorGUILayout.LabelField("Text", EditorStyles.miniBoldLabel);
            if (textStyle == null)
            {
                EditorGUILayout.LabelField("Missing text style", EditorStyles.miniLabel);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    "Font: " + (textStyle.font == null ? "None" : textStyle.font.name),
                    GUILayout.MinWidth(220f));
                EditorGUILayout.LabelField("Size: " + textStyle.fontSize, GUILayout.Width(100f));
                DrawColorSwatch("Color", textStyle.normalColor);
            }
        }

        private static void DrawFrameStyle(TimeKeyUiTheme.UiFrameStyle frameStyle)
        {
            EditorGUILayout.LabelField("Frame", EditorStyles.miniBoldLabel);
            if (frameStyle == null)
            {
                EditorGUILayout.LabelField("Missing frame style", EditorStyles.miniLabel);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    "Sprite: " + (frameStyle.backgroundSprite == null
                        ? "None"
                        : frameStyle.backgroundSprite.name),
                    GUILayout.MinWidth(320f));
                DrawColorSwatch("Fill", frameStyle.fillColor);
            }
        }

        private static void DrawButtonStyle(TimeKeyUiTheme.UiButtonStyle buttonStyle)
        {
            EditorGUILayout.LabelField("Button states", EditorStyles.miniBoldLabel);
            if (buttonStyle == null)
            {
                EditorGUILayout.LabelField("Missing button style", EditorStyles.miniLabel);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawColorSwatch("Normal", buttonStyle.normalColor);
                DrawColorSwatch("Highlight", buttonStyle.highlightedColor);
                DrawColorSwatch("Pressed", buttonStyle.pressedColor);
                DrawColorSwatch("Disabled", buttonStyle.disabledColor);
            }
        }

        private static void DrawColorSwatch(string label, Color color)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(SwatchWidth)))
            {
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(SwatchWidth));
                var rect = GUILayoutUtility.GetRect(SwatchWidth, SwatchHeight, GUILayout.Width(SwatchWidth));
                EditorGUI.DrawRect(rect, color);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Color.black);
                EditorGUILayout.LabelField(
                    "#" + ColorUtility.ToHtmlStringRGBA(color),
                    EditorStyles.miniLabel,
                    GUILayout.Width(SwatchWidth));
            }
        }
    }
}
