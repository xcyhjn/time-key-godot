using System.IO;
using NUnit.Framework;
using TimeKey.Editor.AssetTooling.SceneContracts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TimeKey.Tests.EditMode.AssetTooling
{
    public sealed class SceneContractValidatorTests
    {
        private const string FixtureDirectory = "Assets/__TimeKeySceneContractValidatorTests__";
        private const string MissingRootScenePath = FixtureDirectory + "/MissingRoot.unity";

        private SceneSetup[] _setup;

        [SetUp]
        public void SetUp()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            _setup = EditorSceneManager.GetSceneManagerSetup();
        }

        [TearDown]
        public void TearDown()
        {
            if (_setup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(_setup);
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            AssetDatabase.DeleteAsset(FixtureDirectory);
            var window = Resources.FindObjectsOfTypeAll<SceneContractValidatorWindow>();
            for (var index = 0; index < window.Length; index++)
            {
                window[index].Close();
            }
        }

        [Test]
        public void ProductionCombatScene_PassesFrozenContract()
        {
            var report = SceneContractValidator.ValidateCombatScene();

            Assert.That(report.IsValid, Is.True, report.ToJson());
            Assert.That(report.ErrorCount, Is.Zero);
            Assert.That(report.InfoCount, Is.GreaterThan(10));
        }

        [Test]
        public void ProductionCombatScene_IsDeterministicAndReadOnly()
        {
            var fullPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                SceneContractValidator.CombatScenePath));
            var before = File.ReadAllBytes(fullPath);
            var setupBefore = EditorSceneManager.GetSceneManagerSetup();

            var first = SceneContractValidator.ValidateCombatScene();
            var second = SceneContractValidator.ValidateCombatScene();

            CollectionAssert.AreEqual(before, File.ReadAllBytes(fullPath));
            AssertSceneSetupEqual(setupBefore, EditorSceneManager.GetSceneManagerSetup());
            Assert.That(second.ToJson(), Is.EqualTo(first.ToJson()));
        }

        [Test]
        public void MissingRoot_ReturnsStructuredPathWithoutRepairingScene()
        {
            AssetDatabase.CreateFolder("Assets", "__TimeKeySceneContractValidatorTests__");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("UnrelatedRoot");
            Assert.That(EditorSceneManager.SaveScene(scene, MissingRootScenePath), Is.True);

            var report = SceneContractValidator.ValidateCombatScene(MissingRootScenePath);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.ToJson(), Does.Contain("NODE_REQUIRED"));
            Assert.That(report.ToJson(), Does.Contain("VerticalSliceRoot"));
            Assert.That(scene.GetRootGameObjects(), Has.Length.EqualTo(1));
            Assert.That(scene.GetRootGameObjects()[0].name, Is.EqualTo("UnrelatedRoot"));
            Assert.That(scene.isDirty, Is.False);
        }

        [Test]
        public void WrongNonNullSceneReference_ReturnsExactPropertyWithoutSavingScene()
        {
            var fullPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                SceneContractValidator.CombatScenePath));
            var bytesBefore = File.ReadAllBytes(fullPath);
            var scene = EditorSceneManager.OpenScene(
                SceneContractValidator.CombatScenePath,
                OpenSceneMode.Single);
            var root = System.Array.Find(
                scene.GetRootGameObjects(),
                candidate => candidate.name == "VerticalSliceRoot");
            var controller = root.GetComponent<TimeKey.Presentation.VerticalSliceController>();
            var serialized = new SerializedObject(controller);
            var statusText = serialized.FindProperty("statusText");
            var targetText = serialized.FindProperty("targetText");
            var originalTarget = targetText.objectReferenceValue;

            try
            {
                targetText.objectReferenceValue = statusText.objectReferenceValue;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var report = SceneContractValidator.ValidateCombatScene();

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.ToJson(), Does.Contain("\"propertyPath\": \"targetText\""));
                CollectionAssert.AreEqual(bytesBefore, File.ReadAllBytes(fullPath));
            }
            finally
            {
                targetText.objectReferenceValue = originalTarget;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void DuplicateTimelineCoordinateAndInvalidOccupantMapping_ReturnDiagnostics()
        {
            var fullPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                SceneContractValidator.CombatScenePath));
            var bytesBefore = File.ReadAllBytes(fullPath);
            var scene = EditorSceneManager.OpenScene(
                SceneContractValidator.CombatScenePath,
                OpenSceneMode.Single);
            var root = System.Array.Find(
                scene.GetRootGameObjects(),
                candidate => candidate.name == "VerticalSliceRoot");
            var timeline = root.GetComponentInChildren<
                TimeKey.Presentation.Presenters.TimelinePresenter>(true);
            var timelineSerialized = new SerializedObject(timeline);
            var cells = timelineSerialized.FindProperty("timelineCells");
            var firstCell = cells.GetArrayElementAtIndex(0).objectReferenceValue as Component;
            var secondCell = cells.GetArrayElementAtIndex(1).objectReferenceValue as Component;
            var firstCellSerialized = new SerializedObject(firstCell);
            var secondCellSerialized = new SerializedObject(secondCell);
            var secondColumn = secondCellSerialized.FindProperty("column");
            var secondRow = secondCellSerialized.FindProperty("row");
            var originalColumn = secondColumn.intValue;
            var originalRow = secondRow.intValue;
            var occupant = root.GetComponentInChildren<
                TimeKey.Presentation.Presenters.CombatOccupantPresenter>(true);
            var occupantSerialized = new SerializedObject(occupant);
            var creationId = occupantSerialized.FindProperty("creationViews")
                .GetArrayElementAtIndex(0)
                .FindPropertyRelative("creationId");
            var originalCreationId = creationId.stringValue;

            try
            {
                secondColumn.intValue = firstCellSerialized.FindProperty("column").intValue;
                secondRow.intValue = firstCellSerialized.FindProperty("row").intValue;
                secondCellSerialized.ApplyModifiedPropertiesWithoutUndo();
                creationId.stringValue = string.Empty;
                occupantSerialized.ApplyModifiedPropertiesWithoutUndo();

                var report = SceneContractValidator.ValidateCombatScene();

                Assert.That(report.IsValid, Is.False);
                Assert.That(report.ToJson(), Does.Contain("TIMELINE_COORDINATE_UNIQUE"));
                Assert.That(report.ToJson(), Does.Contain("creationViews[0].prefab"));
                CollectionAssert.AreEqual(bytesBefore, File.ReadAllBytes(fullPath));
            }
            finally
            {
                secondColumn.intValue = originalColumn;
                secondRow.intValue = originalRow;
                secondCellSerialized.ApplyModifiedPropertiesWithoutUndo();
                creationId.stringValue = originalCreationId;
                occupantSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void EditorWindow_DisplaysStructuredSummaryAtResponsiveSizes()
        {
            var report = SceneContractValidator.ValidateCombatScene();
            var window = SceneContractValidatorWindow.OpenWindow();
            foreach (var size in new[]
                     {
                         new Vector2(1280f, 720f),
                         new Vector2(1920f, 1080f),
                         new Vector2(2560f, 1080f)
                     })
            {
                window.position = new Rect(40f, 40f, size.x, size.y);
                window.DisplayReport(report);
                var summary = window.rootVisualElement.Q<Label>("summary");
                var scroll = window.rootVisualElement.Q<ScrollView>("diagnostics");
                Assert.That(summary, Is.Not.Null);
                Assert.That(summary.text, Does.StartWith("PASS"));
                Assert.That(scroll, Is.Not.Null);
                Assert.That(scroll.contentContainer.childCount, Is.EqualTo(report.Diagnostics.Count));
            }
        }

        private static void AssertSceneSetupEqual(SceneSetup[] expected, SceneSetup[] actual)
        {
            Assert.That(actual, Has.Length.EqualTo(expected.Length));
            for (var index = 0; index < expected.Length; index++)
            {
                Assert.That(actual[index].path, Is.EqualTo(expected[index].path), index.ToString());
                Assert.That(actual[index].isLoaded, Is.EqualTo(expected[index].isLoaded), index.ToString());
                Assert.That(actual[index].isActive, Is.EqualTo(expected[index].isActive), index.ToString());
            }
        }
    }
}
