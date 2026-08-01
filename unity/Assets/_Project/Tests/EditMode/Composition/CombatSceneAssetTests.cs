using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Presentation;
using TimeKey.Presentation.Cards;
using TimeKey.Presentation.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeKey.Tests.EditMode.Composition
{
    public sealed class CombatSceneAssetTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity";

        [Test]
        public void SceneAsset_SerializesStableHierarchyAndAllControllerReferences()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = FindRoot(scene, "VerticalSliceRoot");
            Assert.That(root, Is.Not.Null);
            AssertPath(root.transform, "World/SliceCamera");
            AssertPath(root.transform, "World/Environment/KeyLight");
            AssertPath(root.transform, "World/Environment/FillLight");
            AssertPath(root.transform, "World/BattlefieldGround");
            AssertPath(root.transform, "World/CombatBoardRoot");
            AssertPath(root.transform, "World/CombatBoardRoot/TargetAnchor");
            AssertPath(root.transform, "World/CombatBoardRoot/BoardRangePreview");
            AssertPath(root.transform, "EventSystem");
            AssertPath(root.transform, "SliceCanvas/HUD/Header");
            AssertPath(root.transform, "SliceCanvas/HUD/Timeline");
            AssertPath(root.transform, "SliceCanvas/HUD/CardHandHost");
            AssertPath(root.transform, "SliceCanvas/HUD/DetailPanel");

            var controller = root.GetComponent<VerticalSliceController>();
            Assert.That(controller, Is.Not.Null);
            var serialized = new SerializedObject(controller);
            var requiredReferences = new[]
            {
                "sceneCamera", "boardCamera", "keyLight", "fillLight", "battlefieldGround",
                "boardRoot", "dynamicRoot", "targetAnchor", "sceneEventSystem", "sceneCanvas",
                "hudRoot", "timelineRoot", "statusText", "targetText", "resolveButton",
                "cardHandHost", "boardRangePreview", "timelinePlacementPreview", "hexColumnPrefab",
                "grassBlockPrefab", "dirtBlockPrefab", "targetViewPrefab"
            };
            for (var index = 0; index < requiredReferences.Length; index++)
            {
                var property = serialized.FindProperty(requiredReferences[index]);
                Assert.That(property, Is.Not.Null, requiredReferences[index]);
                Assert.That(property.objectReferenceValue, Is.Not.Null, requiredReferences[index]);
            }

            var cells = serialized.FindProperty("timelineCells");
            Assert.That(cells.arraySize, Is.EqualTo(36));
            var coordinates = new HashSet<TimelineCell>();
            for (var index = 0; index < cells.arraySize; index++)
            {
                var view = cells.GetArrayElementAtIndex(index).objectReferenceValue as TimelineCellView;
                Assert.That(view, Is.Not.Null, "timelineCells[" + index + "]");
                Assert.That(coordinates.Add(view.Coordinate), Is.True, view.Coordinate.ToString());
            }
        }

        [Test]
        public void ProductionPrefabs_AreSavedAssetsWithExpectedComponents()
        {
            AssertPrefab<TimelineCellView>("Assets/_Project/Prefabs/Battle/UI/TimelineCell.prefab");
            AssertPrefab<CardHandView>("Assets/_Project/Prefabs/Battle/Cards/CardView.prefab");
            AssertPrefab<MeshRenderer>("Assets/_Project/Prefabs/Battle/Terrain/HexBlockGrass.prefab");
            AssertPrefab<MeshRenderer>("Assets/_Project/Prefabs/Battle/Terrain/HexBlockDirt.prefab");
            AssertPrefab<HexTileColumn>("Assets/_Project/Prefabs/Battle/Terrain/HexColumn.prefab");
            AssertPrefab<WorldTargetView>("Assets/_Project/Prefabs/Battle/Targets/TargetView.prefab");
        }

        [Test]
        public void ControllerSource_DoesNotConstructStableSceneObjects()
        {
            var path = Path.Combine(Application.dataPath, "_Project/Runtime/Presentation/VerticalSliceController.cs");
            var source = File.ReadAllText(path);
            Assert.That(source, Does.Not.Contain("new GameObject"));
            Assert.That(source, Does.Not.Contain("GameObject.CreatePrimitive"));
            Assert.That(source, Does.Not.Contain(".AddComponent<"));
            Assert.That(source, Does.Not.Contain("GameObject.Find"));
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private static void AssertPath(Transform root, string path)
        {
            Assert.That(root.Find(path), Is.Not.Null, path);
        }

        private static void AssertPrefab<T>(string path) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(PrefabUtility.GetPrefabAssetType(prefab), Is.Not.EqualTo(PrefabAssetType.NotAPrefab));
            Assert.That(prefab.GetComponentInChildren<T>(true), Is.Not.Null, path);
        }
    }
}
