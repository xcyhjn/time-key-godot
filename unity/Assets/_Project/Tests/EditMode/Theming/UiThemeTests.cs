using System;
using System.Linq;
using NUnit.Framework;
using TimeKey.Editor.ThemeMigration;
using TimeKey.Presentation.Theming;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Tests.EditMode.Theming
{
    public sealed class UiThemeTests
    {
        [Test]
        public void DefaultTheme_DefinesEveryStyleWithSilver()
        {
            var theme = AssetDatabase.LoadAssetAtPath<TimeKeyUiTheme>(
                UiThemeValidator.DefaultThemePath);
            var silver = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/_Project/Resources/Fonts/Silver.ttf");

            Assert.That(theme, Is.Not.Null);
            Assert.That(theme.Styles.Count, Is.EqualTo(Enum.GetValues(typeof(UiStyleId)).Length));
            Assert.That(theme.Styles.Select(style => style.id).Distinct().Count(),
                Is.EqualTo(theme.Styles.Count));
            Assert.That(theme.Styles.All(style => style.text.font == silver), Is.True);
        }

        [Test]
        public void Binder_RebindIsIdempotentAndDoesNotCreateObjects()
        {
            var root = new GameObject("ThemeRig", typeof(RectTransform), typeof(UiThemeScope));
            try
            {
                var target = new GameObject(
                    "Target",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(UiThemeBinder));
                target.transform.SetParent(root.transform, false);
                var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(target.transform, false);
                var theme = AssetDatabase.LoadAssetAtPath<TimeKeyUiTheme>(
                    UiThemeValidator.DefaultThemePath);
                root.GetComponent<UiThemeScope>().SetTheme(theme);

                var binder = target.GetComponent<UiThemeBinder>();
                var serialized = new SerializedObject(binder);
                serialized.FindProperty("background").objectReferenceValue = target.GetComponent<Image>();
                serialized.FindProperty("text").objectReferenceValue = textObject.GetComponent<Text>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var childCount = root.transform.childCount;

                binder.Rebind();
                binder.Rebind();

                Assert.That(binder.IsBound, Is.True);
                Assert.That(root.transform.childCount, Is.EqualTo(childCount));
                Assert.That(textObject.GetComponent<Text>().font, Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Validator_HasNoErrorForDefaultTheme()
        {
            var diagnostics = UiThemeValidator.ValidateDefaultTheme();
            Assert.That(diagnostics.Any(item => item.IsError), Is.False,
                string.Join("\n", diagnostics.Select(item => item.Message)));
        }

        [Test]
        public void Binder_LocalOverrideChangesOnlyRequestedFields()
        {
            var root = new GameObject("ThemeRig", typeof(RectTransform), typeof(UiThemeScope));
            var target = new GameObject(
                "Target",
                typeof(RectTransform),
                typeof(Image),
                typeof(UiThemeBinder));
            target.transform.SetParent(root.transform, false);
            try
            {
                var theme = AssetDatabase.LoadAssetAtPath<TimeKeyUiTheme>(
                    UiThemeValidator.DefaultThemePath);
                root.GetComponent<UiThemeScope>().SetTheme(theme);
                var binder = target.GetComponent<UiThemeBinder>();
                var serialized = new SerializedObject(binder);
                serialized.FindProperty("background").objectReferenceValue =
                    target.GetComponent<Image>();
                var localOverride = serialized.FindProperty("localOverride");
                localOverride.FindPropertyRelative("overrideFillColor").boolValue = true;
                localOverride.FindPropertyRelative("fillColor").colorValue = Color.magenta;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                binder.Rebind();

                Assert.That(target.GetComponent<Image>().color, Is.EqualTo(Color.magenta));
                Assert.That(target.GetComponent<Image>().sprite, Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
