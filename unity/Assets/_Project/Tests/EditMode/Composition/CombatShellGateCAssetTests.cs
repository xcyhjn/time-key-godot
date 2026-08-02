using System.IO;
using System;
using NUnit.Framework;
using TimeKey.Composition.SceneFlow;
using TimeKey.Presentation.GameStart;
using TimeKey.Presentation.MainMenu;
using TimeKey.Presentation.TransitionVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeKey.Tests.EditMode.Composition
{
    public sealed class CombatShellGateCAssetTests
    {
        private const string GameStartPrefabPath =
            "Assets/_Project/Prefabs/Shell/GameStartLogo.prefab";
        private const string MainMenuPrefabPath =
            "Assets/_Project/Prefabs/Shell/MainMenu.prefab";
        private const string TransitionPrefabPath =
            "Assets/_Project/Prefabs/Shell/TransitionVisual.prefab";
        private const string FontPath = "Assets/_Project/Resources/Fonts/Silver.ttf";

        [Test]
        public void GateCPrefabs_SaveSilverAndFrozenMenuSemantics()
        {
            var silver = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Assert.That(silver, Is.Not.Null);
            var start = LoadPrefab(GameStartPrefabPath);
            var startPresenter = start.GetComponent<StartLogoPresenter>();
            Assert.That(startPresenter, Is.Not.Null);
            var startSerialized = new SerializedObject(startPresenter);
            Assert.That(startSerialized.FindProperty("duration").floatValue,
                Is.EqualTo(3f));
            Assert.That(startSerialized.FindProperty("allowSkip").boolValue, Is.False);
            AssertReference(startSerialized, "background");
            AssertReference(startSerialized, "keyImage");
            AssertReference(startSerialized, "silverFont");
            Assert.That(startSerialized.FindProperty("characterLabels").arraySize,
                Is.EqualTo(3));

            var menu = LoadPrefab(MainMenuPrefabPath);
            var menuPresenter = menu.GetComponent<MainMenuPresenter>();
            Assert.That(menuPresenter, Is.Not.Null);
            var menuSerialized = new SerializedObject(menuPresenter);
            foreach (var field in new[]
                     {
                         "newGameButton", "seedGameButton", "continueButton",
                         "settingsButton", "databaseButton", "exitButton",
                         "seedGroup", "seedInput", "seedConfirmButton", "seedCancelButton",
                         "modalGroup", "modalTitle", "modalBody", "modalConfirmButton",
                         "modalCloseButton", "settingsGroup", "masterVolumeSlider",
                         "musicVolumeSlider", "sfxVolumeSlider", "fullscreenToggle",
                         "settingsCloseButton", "silverFont", "backgroundEntranceGroup",
                         "titleEntranceGroup", "clockEntranceGroup", "buttonsEntranceGroup"
                     })
            {
                AssertReference(menuSerialized, field);
            }

            foreach (var name in new[]
                     {
                         "NewGame", "SeedGame", "Continue", "Settings", "Database", "Exit"
                     })
            {
                Assert.That(Find(menu.transform, name)?.GetComponent<Button>(), Is.Not.Null, name);
            }

            var buttonTexturePaths = new System.Collections.Generic.HashSet<string>();
            foreach (var name in new[]
                     {
                         "NewGame", "SeedGame", "Continue", "Settings", "Database", "Exit"
                     })
            {
                var image = Find(menu.transform, name)?.GetComponent<RawImage>();
                Assert.That(image, Is.Not.Null, name);
                Assert.That(image.texture, Is.Not.Null, name);
                buttonTexturePaths.Add(AssetDatabase.GetAssetPath(image.texture));
            }

            Assert.That(buttonTexturePaths, Has.Count.EqualTo(6));
            AssertMenuButton(menu, "NewGame", new Vector2(0.16f, 0.67f), "u_l_1.png");
            AssertMenuButton(menu, "SeedGame", new Vector2(0.16f, 0.48f), "m_L_1.png");
            AssertMenuButton(menu, "Settings", new Vector2(0.16f, 0.29f), "d_l_1.png");
            AssertMenuButton(menu, "Continue", new Vector2(0.84f, 0.67f), "u_r_1.png");
            AssertMenuButton(menu, "Database", new Vector2(0.84f, 0.48f), "m_r_1.png");
            AssertMenuButton(menu, "Exit", new Vector2(0.84f, 0.29f), "d_r_1.png");
            var clockRoot = Find(menu.transform, "CentralClock").GetComponent<RectTransform>();
            Assert.That(clockRoot.sizeDelta.x, Is.GreaterThanOrEqualTo(800f));
            Assert.That(clockRoot.sizeDelta.y, Is.GreaterThanOrEqualTo(800f));
            var title = Find(menu.transform, "Title").GetComponent<Text>();
            Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(144));
            var backdrop = Find(menu.transform, "HexMapBackdrop").GetComponent<RawImage>();
            Assert.That(backdrop.color, Is.EqualTo(Color.white));
            var veil = Find(menu.transform, "Veil").GetComponent<Image>();
            Assert.That(veil.color.a, Is.LessThanOrEqualTo(0.1f));
            Assert.That(menuSerialized.FindProperty("seedInput").objectReferenceValue,
                Is.TypeOf<InputField>());
            var seedInput = (InputField)menuSerialized.FindProperty("seedInput").objectReferenceValue;
            Assert.That(seedInput.characterValidation,
                Is.EqualTo(InputField.CharacterValidation.None));
            Assert.That(((Slider)menuSerialized.FindProperty("masterVolumeSlider")
                .objectReferenceValue).interactable, Is.True);
            Assert.That(((Slider)menuSerialized.FindProperty("musicVolumeSlider")
                .objectReferenceValue).interactable, Is.False);
            Assert.That(((Slider)menuSerialized.FindProperty("sfxVolumeSlider")
                .objectReferenceValue).interactable, Is.False);

            foreach (var prefab in new[] { start, menu, LoadPrefab(TransitionPrefabPath) })
            {
                var texts = prefab.GetComponentsInChildren<Text>(true);
                Assert.That(texts, Is.Not.Empty, prefab.name);
                foreach (var text in texts)
                {
                    Assert.That(text.font, Is.EqualTo(silver), text.name);
                }
            }

            Assert.That(LoadPrefab(TransitionPrefabPath)
                .GetComponent<TransitionVisualPresenter>(), Is.Not.Null);
        }

        [Test]
        public void GateCScenes_SaveFormalPresentationAndPersistentTransitionOwnership()
        {
            var gameStart = EditorSceneManager.OpenScene(
                "Assets/_Project/Scenes/Shell/GameStart.unity",
                OpenSceneMode.Single);
            Assert.That(FindAll<StartLogoPresenter>(gameStart), Has.Count.EqualTo(1));
            Assert.That(FindAll<GameStartSceneNavigation>(gameStart), Has.Count.EqualTo(1));
            Assert.That(FindAll<CanvasScaler>(gameStart), Has.Count.EqualTo(1));
            Assert.That(FindAll<EventSystem>(gameStart), Is.Empty);

            var menu = EditorSceneManager.OpenScene(
                "Assets/_Project/Scenes/Shell/MainMenu.unity",
                OpenSceneMode.Single);
            Assert.That(FindAll<MainMenuPresenter>(menu), Has.Count.EqualTo(1));
            Assert.That(FindAll<MainMenuSceneNavigation>(menu), Has.Count.EqualTo(1));
            Assert.That(FindAll<CanvasScaler>(menu), Has.Count.EqualTo(1));
            Assert.That(FindAll<EventSystem>(menu), Is.Empty);

            var bootstrap = EditorSceneManager.OpenScene(
                "Assets/_Project/Scenes/Shell/Bootstrap.unity",
                OpenSceneMode.Single);
            Assert.That(FindAll<TransitionCanvasPresenter>(bootstrap), Has.Count.EqualTo(1));
            Assert.That(FindAll<TransitionVisualPresenter>(bootstrap), Has.Count.EqualTo(1));
            Assert.That(FindAll<EventSystem>(bootstrap), Has.Count.EqualTo(1));
            var transition = new SerializedObject(FindAll<TransitionCanvasPresenter>(bootstrap)[0]);
            AssertReference(transition, "visualPresenter");
            var loading = FindAll<Text>(bootstrap)
                .Find(text => text.name == "LoadingIndicator");
            Assert.That(loading, Is.Not.Null);
            Assert.That(loading.text, Is.EqualTo("载入中"));
            Assert.That(loading.font, Is.EqualTo(AssetDatabase.LoadAssetAtPath<Font>(FontPath)));
        }

        [Test]
        public void GateCClockCopies_MatchReadOnlyGodotSources()
        {
            var repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            Assert.That(repositoryRoot, Is.Not.Null.And.Not.Empty,
                "TIMEKEY_REPOSITORY_ROOT is required when Unity runs through the ASCII junction.");
            foreach (var file in new[] { "clock_noring.png", "ring.png", "point.png" })
            {
                var source = Path.Combine(repositoryRoot, "image", "clock", file);
                var target = Path.Combine(
                    UnityEngine.Application.dataPath,
                    "_Project", "Resources", "Art", "Shell", "MainMenu", file);
                Assert.That(File.Exists(source), Is.True, source);
                Assert.That(File.Exists(target), Is.True, target);
                CollectionAssert.AreEqual(File.ReadAllBytes(source), File.ReadAllBytes(target), file);
            }

            var keySource = Path.Combine(repositoryRoot, "image", "key.png");
            var keyTarget = Path.Combine(
                UnityEngine.Application.dataPath,
                "_Project", "Resources", "Art", "Shell", "GameStart", "key.png");
            Assert.That(File.Exists(keySource), Is.True, keySource);
            Assert.That(File.Exists(keyTarget), Is.True, keyTarget);
            CollectionAssert.AreEqual(
                File.ReadAllBytes(keySource),
                File.ReadAllBytes(keyTarget),
                "key.png");

            foreach (var file in new[]
                     {
                         "u_l_1.png", "u_l_a.png", "m_L_1.png", "m_l_a.png",
                         "d_l_1.png", "d_l_a.png", "u_r_1.png", "u_r_a.png",
                         "m_r_1.png", "r_m_a.png", "d_r_1.png", "d_r_a.png"
                     })
            {
                var source = Path.Combine(repositoryRoot, "image", "main_menu", file);
                var target = Path.Combine(
                    UnityEngine.Application.dataPath,
                    "_Project", "Resources", "Art", "Shell", "MainMenu", "Buttons", file);
                Assert.That(File.Exists(source), Is.True, source);
                Assert.That(File.Exists(target), Is.True, target);
                CollectionAssert.AreEqual(
                    File.ReadAllBytes(source),
                    File.ReadAllBytes(target),
                    file);
            }
        }

        [Test]
        public void Bootstrap_ReservesStrictlyIncreasingTransitionSequences()
        {
            var target = new GameObject("SequenceFixture");
            target.SetActive(false);
            var bootstrap = target.AddComponent<BootstrapRoot>();
            Assert.That(bootstrap.ReserveTransitionSequence(), Is.EqualTo(1));
            Assert.That(bootstrap.ReserveTransitionSequence(), Is.EqualTo(2));
            UnityEngine.Object.DestroyImmediate(target);
        }

        private static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(PrefabUtility.GetPrefabAssetType(prefab),
                Is.Not.EqualTo(PrefabAssetType.NotAPrefab), path);
            return prefab;
        }

        private static void AssertReference(SerializedObject serialized, string fieldName)
        {
            var property = serialized.FindProperty(fieldName);
            Assert.That(property, Is.Not.Null, fieldName);
            Assert.That(property.objectReferenceValue, Is.Not.Null, fieldName);
        }

        private static void AssertMenuButton(
            GameObject menu,
            string name,
            Vector2 expectedAnchor,
            string expectedNormalTexture)
        {
            var target = Find(menu.transform, name);
            Assert.That(target, Is.Not.Null, name);
            var rect = target.GetComponent<RectTransform>();
            Assert.That(rect.anchorMin.x, Is.EqualTo(expectedAnchor.x).Within(0.001f), name);
            Assert.That(rect.anchorMin.y, Is.EqualTo(expectedAnchor.y).Within(0.001f), name);
            Assert.That(rect.sizeDelta.x, Is.GreaterThanOrEqualTo(512f), name);
            Assert.That(rect.sizeDelta.y, Is.GreaterThanOrEqualTo(168f), name);
            var texturePath = AssetDatabase.GetAssetPath(target.GetComponent<RawImage>().texture);
            Assert.That(texturePath, Does.EndWith("/" + expectedNormalTexture), name);
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static System.Collections.Generic.List<T> FindAll<T>(Scene scene)
            where T : Component
        {
            var result = new System.Collections.Generic.List<T>();
            foreach (var root in scene.GetRootGameObjects())
            {
                result.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return result;
        }
    }
}
