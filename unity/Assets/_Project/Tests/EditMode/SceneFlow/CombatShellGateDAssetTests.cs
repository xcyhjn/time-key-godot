using System.Collections.Generic;
using NUnit.Framework;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition.SceneFlow;
using TimeKey.Presentation;
using TimeKey.Presentation.GameOver;
using TimeKey.Presentation.OutOfBattleShell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeKey.Tests.EditMode.SceneFlow
{
    public sealed class CombatShellGateDAssetTests
    {
        private const string OutOfBattlePrefabPath =
            "Assets/_Project/Prefabs/Shell/OutOfBattleShell.prefab";
        private const string GameOverPrefabPath =
            "Assets/_Project/Prefabs/Shell/GameOver.prefab";
        private const string OutOfBattleScenePath =
            "Assets/_Project/Scenes/Shell/OutOfBattleShell.unity";
        private const string GameOverScenePath =
            "Assets/_Project/Scenes/Shell/GameOver.unity";
        private const string CombatScenePath =
            "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity";
        private const string FontPath = "Assets/_Project/Resources/Fonts/Silver.ttf";

        [Test]
        public void GateDPrefabs_SaveTypedPresentationStandaloneCanvasAndSilver()
        {
            var silver = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Assert.That(silver, Is.Not.Null, FontPath);

            var outOfBattle = LoadPrefab(OutOfBattlePrefabPath);
            AssertStandaloneCanvas(outOfBattle, silver);
            var outPresenter = outOfBattle.GetComponent<OutOfBattleShellPresenter>();
            var outNavigation = outOfBattle.GetComponent<OutOfBattleShellSceneNavigation>();
            Assert.That(outPresenter, Is.Not.Null);
            Assert.That(outNavigation, Is.Not.Null);
            Assert.That(outOfBattle.GetComponentsInChildren<OutOfBattleRoomView>(true),
                Has.Length.EqualTo(1));

            var serialized = new SerializedObject(outPresenter);
            foreach (var field in new[]
                     {
                         "seedLabel", "eraLabel", "phaseLabel", "timecoinsLabel",
                         "roomView", "confirmButton", "cancelButton"
                     })
            {
                AssertReference(serialized, field);
            }

            AssertReference(serialized, "silverFont", silver);
            Assert.That(serialized.FindProperty("roomId").stringValue,
                Is.EqualTo("combat-room-01"));

            serialized = new SerializedObject(outNavigation);
            AssertReference(serialized, "presenter", outPresenter);
            AssertReference(serialized, "sharedTopHud");
            Assert.That(serialized.FindProperty("battleTag").stringValue,
                Is.EqualTo("combat-vertical-slice"));

            var gameOver = LoadPrefab(GameOverPrefabPath);
            AssertStandaloneCanvas(gameOver, silver);
            var gameOverPresenter = gameOver.GetComponent<GameOverPresenter>();
            var gameOverNavigation = gameOver.GetComponent<GameOverSceneNavigation>();
            Assert.That(gameOverPresenter, Is.Not.Null);
            Assert.That(gameOverNavigation, Is.Not.Null);

            serialized = new SerializedObject(gameOverPresenter);
            foreach (var field in new[]
                     {
                         "rootCanvasGroup", "title", "detail", "returnButton",
                         "returnButtonLabel"
                     })
            {
                AssertReference(serialized, field);
            }

            AssertReference(serialized, "silverFont", silver);
            serialized = new SerializedObject(gameOverNavigation);
            AssertReference(serialized, "presenter", gameOverPresenter);
        }

        [Test]
        public void GateDScenes_SavePrefabBackedTypedContentEntries()
        {
            AssertContentScene<OutOfBattleShellPresenter, OutOfBattleShellSceneNavigation>(
                OutOfBattleScenePath,
                OutOfBattlePrefabPath,
                SceneId.OutOfBattleShell);
            AssertContentScene<GameOverPresenter, GameOverSceneNavigation>(
                GameOverScenePath,
                GameOverPrefabPath,
                SceneId.GameOver);
        }

        [Test]
        public void CombatScene_SavesSingleTypedOutcomeNavigation()
        {
            var scene = EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Single);
            var navigations = FindAll<CombatSceneNavigation>(scene);
            Assert.That(navigations, Has.Count.EqualTo(1));

            var navigation = navigations[0];
            var controller = navigation.GetComponent<VerticalSliceController>();
            Assert.That(controller, Is.Not.Null);
            AssertReference(new SerializedObject(navigation), "controller", controller);
        }

        private static void AssertContentScene<TPresenter, TNavigation>(
            string scenePath,
            string prefabPath,
            SceneId sceneId)
            where TPresenter : Component
            where TNavigation : Component
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var entries = FindAll<SceneContentEntry>(scene);
            var presenters = FindAll<TPresenter>(scene);
            var navigations = FindAll<TNavigation>(scene);
            Assert.That(entries, Has.Count.EqualTo(1), scenePath);
            Assert.That(presenters, Has.Count.EqualTo(1), scenePath);
            Assert.That(navigations, Has.Count.EqualTo(1), scenePath);

            var contentRoot = presenters[0].gameObject;
            Assert.That(navigations[0].gameObject, Is.EqualTo(contentRoot), scenePath);
            Assert.That(contentRoot.activeSelf, Is.False, scenePath);
            var source = PrefabUtility.GetCorrespondingObjectFromSource(contentRoot);
            Assert.That(source, Is.Not.Null, scenePath);
            Assert.That(AssetDatabase.GetAssetPath(source), Is.EqualTo(prefabPath), scenePath);

            var entry = new SerializedObject(entries[0]);
            Assert.That(entry.FindProperty("sceneId").enumValueIndex,
                Is.EqualTo((int)sceneId), scenePath);
            AssertReference(entry, "contentRoot", contentRoot);
            AssertReference(entry, "contentCamera", contentRoot.GetComponentInChildren<Camera>(true));
            AssertReference(entry, "interactionGroup", contentRoot.GetComponent<CanvasGroup>());

            AssertStandaloneCanvas(
                contentRoot,
                AssetDatabase.LoadAssetAtPath<Font>(FontPath));
        }

        private static void AssertStandaloneCanvas(GameObject root, Font silver)
        {
            Assert.That(root.GetComponent<CanvasGroup>(), Is.Not.Null, root.name);
            Assert.That(root.GetComponent<Canvas>(), Is.Null, root.name);

            var canvases = root.GetComponentsInChildren<Canvas>(true);
            Assert.That(canvases, Has.Length.EqualTo(1), root.name);
            var canvas = canvases[0];
            Assert.That(canvas.name, Is.EqualTo("GateDCanvas"), root.name);
            Assert.That(canvas.transform.parent, Is.EqualTo(root.transform), root.name);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay), root.name);
            Assert.That(canvas.GetComponent<CanvasScaler>(), Is.Not.Null, root.name);
            Assert.That(canvas.GetComponent<GraphicRaycaster>(), Is.Not.Null, root.name);

            var cameras = root.GetComponentsInChildren<Camera>(true);
            Assert.That(cameras, Has.Length.EqualTo(1), root.name);
            Assert.That(cameras[0].transform.parent, Is.EqualTo(root.transform), root.name);

            var texts = root.GetComponentsInChildren<Text>(true);
            Assert.That(texts, Is.Not.Empty, root.name);
            foreach (var text in texts)
            {
                Assert.That(text.transform.IsChildOf(canvas.transform), Is.True, text.name);
                Assert.That(text.font, Is.EqualTo(silver), text.name);
            }
        }

        private static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(PrefabUtility.GetPrefabAssetType(prefab),
                Is.Not.EqualTo(PrefabAssetType.NotAPrefab), path);
            return prefab;
        }

        private static void AssertReference(
            SerializedObject serialized,
            string fieldName,
            Object expected = null)
        {
            var property = serialized.FindProperty(fieldName);
            Assert.That(property, Is.Not.Null, fieldName);
            Assert.That(property.objectReferenceValue, Is.Not.Null, fieldName);
            if (expected != null)
            {
                Assert.That(property.objectReferenceValue, Is.EqualTo(expected), fieldName);
            }
        }

        private static List<T> FindAll<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
            {
                result.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return result;
        }
    }
}
