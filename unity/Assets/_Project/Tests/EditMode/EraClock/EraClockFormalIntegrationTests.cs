using NUnit.Framework;
using TimeKey.Composition.SceneFlow;
using TimeKey.Presentation.Bindings;
using TimeKey.Presentation.EraClock;
using TimeKey.Presentation.SceneFlowFinale;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeKey.Tests.EditMode.EraClock
{
    public sealed class EraClockFormalIntegrationTests
    {
        private const string MainMenuPrefabPath =
            "Assets/_Project/Prefabs/Shell/MainMenu.prefab";
        private const string OutOfBattlePrefabPath =
            "Assets/_Project/Prefabs/Shell/OutOfBattleShell.prefab";
        private const string TopHudPrefabPath =
            "Assets/_Project/Prefabs/Battle/CombatShell/CombatTopHUD.prefab";
        private const string MainMenuScenePath =
            "Assets/_Project/Scenes/Shell/MainMenu.unity";
        private const string OutOfBattleScenePath =
            "Assets/_Project/Scenes/Shell/OutOfBattleShell.unity";
        private const string CombatScenePath =
            "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity";
        private const string SilverPath = "Assets/_Project/Resources/Fonts/Silver.ttf";

        [Test]
        public void FormalPrefabs_SaveOneCompleteNonBlockingClockOwner()
        {
            GameObject menu = LoadPrefab(MainMenuPrefabPath);
            GameObject topHud = LoadPrefab(TopHudPrefabPath);
            AssertClock(menu, expectMainMenuEntrance: true);
            AssertClock(topHud, expectMainMenuEntrance: false);

            Transform staticClock = Find(topHud.transform, "ClockLabel");
            Assert.That(staticClock, Is.Not.Null);
            Assert.That(staticClock.gameObject.activeSelf, Is.False);
            Transform hudAnchor = Find(topHud.transform, "HudAnchor");
            Assert.That(hudAnchor, Is.Not.Null);
            Assert.That(hudAnchor.parent.name, Is.EqualTo("ClockPlate"));
            Transform centerAnchor = Find(topHud.transform, "CenterAnchor");
            Assert.That(centerAnchor, Is.Not.Null);
            var centerRect = centerAnchor.GetComponent<RectTransform>();
            Assert.That(centerRect.anchorMin, Is.EqualTo(new Vector2(0.2f, 0.74f)));
            Assert.That(centerRect.anchorMax, Is.EqualTo(new Vector2(0.2f, 0.74f)));
            var topHudClock = topHud.GetComponentInChildren<EraClockPresenter>(true);
            var settings = new SerializedObject(topHudClock)
                .FindProperty("animationSettings");
            Assert.That(settings.FindPropertyRelative("centerScale").floatValue,
                Is.EqualTo(0.5f));

            GameObject shell = LoadPrefab(OutOfBattlePrefabPath);
            var navigation = shell.GetComponent<OutOfBattleShellSceneNavigation>();
            Assert.That(navigation, Is.Not.Null);
            AssertReference(navigation, "eraClockPresenter");
            AssertReference(navigation, "revealPresenter");
            Assert.That(shell.GetComponentsInChildren<EraClockPresenter>(true), Has.Length.EqualTo(1));
        }

        [Test]
        public void FormalScenes_SaveSingleOwnerAndCompositionReferences()
        {
            Scene menu = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            AssertSceneReference<MainMenuSceneNavigation>(menu, "eraClockPresenter");

            Scene shell = EditorSceneManager.OpenScene(
                OutOfBattleScenePath,
                OpenSceneMode.Single);
            AssertSceneReference<OutOfBattleShellSceneNavigation>(shell, "eraClockPresenter");
            Assert.That(FindAll<LayeredSceneRevealPresenter>(shell), Has.Count.EqualTo(1));

            Scene combat = EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Single);
            AssertSceneReference<CombatPresentationBinding>(combat, "eraClockPresenter");
        }

        private static void AssertClock(GameObject root, bool expectMainMenuEntrance)
        {
            var clocks = root.GetComponentsInChildren<EraClockPresenter>(true);
            Assert.That(clocks, Has.Length.EqualTo(1), root.name);
            var serialized = new SerializedObject(clocks[0]);
            SerializedProperty bindings = serialized.FindProperty("bindings");
            Assert.That(bindings, Is.Not.Null);
            foreach (string field in new[]
                     {
                         "clockRoot", "clockFace", "clockRing", "pointerPivot",
                         "pointerImage", "progressFill", "eraLabel", "phaseLabel",
                         "centerAnchor", "hudAnchor", "rootGroup", "rolloverPulse"
                     })
            {
                SerializedProperty property = bindings.FindPropertyRelative(field);
                Assert.That(property, Is.Not.Null, field);
                Assert.That(property.objectReferenceValue, Is.Not.Null, field);
            }

            var silver = AssetDatabase.LoadAssetAtPath<Font>(SilverPath);
            Assert.That(bindings.FindPropertyRelative("eraLabel").objectReferenceValue,
                Is.TypeOf<Text>());
            foreach (Text label in clocks[0].GetComponentsInChildren<Text>(true))
            {
                Assert.That(label.font, Is.EqualTo(silver), label.name);
                Assert.That(label.raycastTarget, Is.False, label.name);
            }

            foreach (Graphic graphic in clocks[0].GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
            }

            var group = (CanvasGroup)bindings.FindPropertyRelative("rootGroup")
                .objectReferenceValue;
            Assert.That(group.interactable, Is.False);
            Assert.That(group.blocksRaycasts, Is.False);
            if (expectMainMenuEntrance)
            {
                var entrance = Find(root.transform, "ClockEntrance").GetComponent<CanvasGroup>();
                Assert.That(entrance.interactable, Is.False);
                Assert.That(entrance.blocksRaycasts, Is.False);
            }
        }

        private static void AssertSceneReference<T>(Scene scene, string fieldName)
            where T : Component
        {
            var clocks = FindAll<EraClockPresenter>(scene);
            Assert.That(clocks, Has.Count.EqualTo(1), scene.name);
            var owners = FindAll<T>(scene);
            Assert.That(owners, Has.Count.EqualTo(1), scene.name);
            var serialized = new SerializedObject(owners[0]);
            SerializedProperty property = serialized.FindProperty(fieldName);
            Assert.That(property, Is.Not.Null, fieldName);
            Assert.That(property.objectReferenceValue, Is.EqualTo(clocks[0]), fieldName);
        }

        private static void AssertReference(Component owner, string fieldName)
        {
            var property = new SerializedObject(owner).FindProperty(fieldName);
            Assert.That(property, Is.Not.Null, fieldName);
            Assert.That(property.objectReferenceValue, Is.Not.Null, fieldName);
        }

        private static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            return prefab;
        }

        private static System.Collections.Generic.List<T> FindAll<T>(Scene scene)
            where T : Component
        {
            var result = new System.Collections.Generic.List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                result.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return result;
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
