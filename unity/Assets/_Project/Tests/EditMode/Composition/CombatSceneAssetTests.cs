using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TimeKey.Composition;
using TimeKey.Domain;
using TimeKey.Presentation;
using TimeKey.Presentation.Actions;
using TimeKey.Presentation.BattleFlow;
using TimeKey.Presentation.Bindings;
using TimeKey.Presentation.Cards;
using TimeKey.Presentation.Localization;
using TimeKey.Presentation.Occupants;
using TimeKey.Presentation.Presenters;
using TimeKey.Presentation.Targeting;
using TimeKey.Presentation.Terrain;
using TimeKey.Presentation.Tooltips;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeKey.Tests.EditMode.Composition
{
    public sealed class CombatSceneAssetTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity";
        private const string ChineseFontPath =
            "Assets/_Project/Resources/Fonts/Silver.ttf";

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
            AssertPath(root.transform, "SliceCanvas/HUD/Header");
            AssertPath(root.transform, "SliceCanvas/HUD/Timeline");
            AssertPath(root.transform, "SliceCanvas/HUD/Timeline/ActionLayer");
            AssertPath(root.transform, "SliceCanvas/HUD/CardHandHost");
            AssertPath(root.transform, "SliceCanvas/HUD/EffectFrameHost");
            AssertPath(root.transform, "SliceCanvas/HUD/DetailPanel");
            AssertPath(root.transform, "SliceCanvas/HUD/BattleFlowPanel");

            var controller = root.GetComponent<VerticalSliceController>();
            Assert.That(controller, Is.Not.Null);
            var serialized = new SerializedObject(controller);
            var requiredReferences = new[]
            {
                "sceneCamera", "boardCamera", "keyLight", "fillLight", "battlefieldGround",
                "boardRoot", "dynamicRoot", "targetAnchor", "sceneCanvas",
                "hudRoot", "timelineRoot", "statusText", "targetText", "resolveButton",
                "cardHandHost", "boardRangePreview", "timelinePlacementPreview", "presentationBinding", "hexColumnPrefab",
                "grassBlockPrefab", "dirtBlockPrefab", "targetViewPrefab"
            };
            for (var index = 0; index < requiredReferences.Length; index++)
            {
                var property = serialized.FindProperty(requiredReferences[index]);
                Assert.That(property, Is.Not.Null, requiredReferences[index]);
                Assert.That(property.objectReferenceValue, Is.Not.Null, requiredReferences[index]);
            }

            var composition = root.GetComponent<CombatCompositionRoot>();
            var binding = root.GetComponent<CombatPresentationBinding>();
            var traceSink = root.GetComponent<UnityCombatTraceSink>();
            Assert.That(composition, Is.Not.Null);
            Assert.That(binding, Is.Not.Null);
            Assert.That(traceSink, Is.Not.Null);
            Assert.That(root.GetComponentInChildren<CardHandPresenter>(true), Is.Not.Null);
            Assert.That(root.GetComponentInChildren<BoardRangePresenter>(true), Is.Not.Null);
            var timelinePresenter = root.GetComponentInChildren<TimelinePresenter>(true);
            Assert.That(timelinePresenter, Is.Not.Null);
            Assert.That(root.GetComponentInChildren<ClearTimelinePreview>(true), Is.Not.Null);
            Assert.That(root.GetComponentInChildren<CombatHudPresenter>(true), Is.Not.Null);
            Assert.That(root.GetComponentInChildren<CombatOccupantPresenter>(true), Is.Not.Null);
            Assert.That(root.GetComponentInChildren<CombatInteractionOverlayPresenter>(true), Is.Not.Null);
            Assert.That(root.GetComponentInChildren<BattleFlowPresenter>(true), Is.Not.Null);
            Assert.That(root.GetComponentInChildren<BattleSettlementPresenter>(true), Is.Not.Null);

            var bindingSerialized = new SerializedObject(binding);
            AssertReference(bindingSerialized, "occupantPresenter");
            AssertReference(bindingSerialized, "interactionOverlayPresenter");
            AssertReference(bindingSerialized, "battleFlowPresenter");

            var compositionSerialized = new SerializedObject(composition);
            AssertReference(compositionSerialized, "controller");
            AssertReference(compositionSerialized, "presentationBinding");
            AssertReference(compositionSerialized, "traceSink");
            var fixtures = compositionSerialized.FindProperty("cardFixtures");
            Assert.That(fixtures.arraySize, Is.EqualTo(7));
            var fixtureNames = new HashSet<string>();
            for (var index = 0; index < fixtures.arraySize; index++)
            {
                var fixture = fixtures.GetArrayElementAtIndex(index).objectReferenceValue as TextAsset;
                Assert.That(fixture, Is.Not.Null, "cardFixtures[" + index + "]");
                Assert.That(fixtureNames.Add(fixture.name), Is.True, fixture.name);
            }

            var timelineSerialized = new SerializedObject(timelinePresenter);
            AssertReference(timelineSerialized, "timelinePreview");
            AssertReference(timelineSerialized, "clearTimelinePreview");
            AssertReference(timelineSerialized, "actionLayer");
            AssertReference(timelineSerialized, "actionFramePrefab");
            var cells = timelineSerialized.FindProperty("timelineCells");
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
            AssertPrefab<CombatOccupantView>("Assets/_Project/Prefabs/Battle/Occupants/Tower.prefab");
            AssertPrefab<PoisonStatusView>("Assets/_Project/Prefabs/Battle/Status/PoisonStatus.prefab");
            AssertPrefab<CardEffectFrame>("Assets/_Project/Prefabs/Battle/UI/CardEffectFrame.prefab");
            AssertPrefab<TimelineActionFrame>("Assets/_Project/Prefabs/Battle/UI/TimelineActionFrame.prefab");
            AssertPrefab<BattleFlowPresenter>(
                "Assets/_Project/Prefabs/Battle/BattleFlow/BattleFlowPanel.prefab");

            var tower = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Battle/Occupants/Tower.prefab");
            Assert.That(tower.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(tower.transform.Find("OriginalArt-tower"), Is.Not.Null);

            AssertPointSprite("Assets/_Project/Resources/Art/Battle/Occupants/tower.png");
            AssertPointSprite("Assets/_Project/Resources/Art/Battle/Status/poison_icon.png");
        }

        [Test]
        public void SceneAndTimelinePrefab_UseLicensedChineseFontAndLocalizedDefaults()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(ChineseFontPath);
            Assert.That(font, Is.Not.Null);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = FindRoot(scene, "VerticalSliceRoot");
            Assert.That(root, Is.Not.Null);
            AssertLocalizedText(root.transform, "SliceCanvas/HUD/Header/Title", CombatChineseText.SceneTitle, font);
            AssertLocalizedText(root.transform, "SliceCanvas/HUD/Header/Status", CombatChineseText.SelectCard, font);
            AssertLocalizedText(
                root.transform,
                "SliceCanvas/HUD/DetailPanel/TargetStatus",
                CombatChineseText.DefaultTargetStatus,
                font);
            AssertLocalizedText(
                root.transform,
                "SliceCanvas/HUD/DetailPanel/Intent",
                CombatChineseText.EnemyIntentDetail,
                font);
            AssertLocalizedText(
                root.transform,
                "SliceCanvas/HUD/DetailPanel/Resolve/Label",
                CombatChineseText.ResolveTimeline,
                font);

            var timelinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Battle/UI/TimelineCell.prefab");
            var timelineText = timelinePrefab.GetComponentInChildren<Text>(true);
            Assert.That(timelineText, Is.Not.Null);
            Assert.That(timelineText.font, Is.EqualTo(font));

            var effectFrame = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Battle/UI/CardEffectFrame.prefab");
            var actionFrame = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Battle/UI/TimelineActionFrame.prefab");
            foreach (var text in effectFrame.GetComponentsInChildren<Text>(true))
            {
                Assert.That(text.font, Is.EqualTo(font), text.name);
            }

            foreach (var text in actionFrame.GetComponentsInChildren<Text>(true))
            {
                Assert.That(text.font, Is.EqualTo(font), text.name);
            }

            var battleFlowPanel = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Battle/BattleFlow/BattleFlowPanel.prefab");
            foreach (var text in battleFlowPanel.GetComponentsInChildren<Text>(true))
            {
                Assert.That(text.font, Is.EqualTo(font), text.name);
            }

            var tower = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Battle/Occupants/Tower.prefab");
            var towerHealth = tower.transform.Find("Health").GetComponent<TextMesh>();
            Assert.That(towerHealth, Is.Not.Null);
            Assert.That(towerHealth.font, Is.EqualTo(font));
            Assert.That(towerHealth.GetComponent<MeshRenderer>().sharedMaterial, Is.EqualTo(font.material));

            var poison = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Battle/Status/PoisonStatus.prefab");
            var poisonStacks = poison.transform.Find("Stacks").GetComponent<TextMesh>();
            Assert.That(poisonStacks, Is.Not.Null);
            Assert.That(poisonStacks.font, Is.EqualTo(font));
            Assert.That(poisonStacks.GetComponent<MeshRenderer>().sharedMaterial, Is.EqualTo(font.material));

            const string requiredGlyphs =
                "时之钥战斗棋盘目标生命敌方意图第格二行结算轴已选择清除牌请位置锁定有效点击确认范围超出无效行动放置取消完成更新推进结束无需地图预览命中移除个高塔中毒层块正持有未能入雷击地震台风恢复龙卷风未知卡·";
            for (var index = 0; index < requiredGlyphs.Length; index++)
            {
                Assert.That(font.HasCharacter(requiredGlyphs[index]), Is.True, requiredGlyphs[index].ToString());
            }
        }

        [Test]
        public void ControllerSource_DoesNotConstructStableSceneObjects()
        {
            var path = Path.Combine(UnityEngine.Application.dataPath, "_Project/Runtime/Presentation/VerticalSliceController.cs");
            var source = File.ReadAllText(path);
            Assert.That(source, Does.Not.Contain("new GameObject"));
            Assert.That(source, Does.Not.Contain("GameObject.CreatePrimitive"));
            Assert.That(source, Does.Not.Contain(".AddComponent<"));
            Assert.That(source, Does.Not.Contain("GameObject.Find"));
            Assert.That(source, Does.Not.Contain("CardJsonAdapter"));
            Assert.That(source, Does.Not.Contain("Resources.Load"));
            Assert.That(source, Does.Not.Contain("Path.GetFileNameWithoutExtension"));
            Assert.That(source, Does.Not.Contain("ControllerCardCatalog"));
            Assert.That(source, Does.Not.Contain("lightingFixture"));
            Assert.That(source, Does.Not.Contain("earthquakeFixture"));
        }

        private static void AssertReference(SerializedObject serialized, string fieldName)
        {
            var property = serialized.FindProperty(fieldName);
            Assert.That(property, Is.Not.Null, fieldName);
            Assert.That(property.objectReferenceValue, Is.Not.Null, fieldName);
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

        private static void AssertLocalizedText(Transform root, string path, string expected, Font font)
        {
            var target = root.Find(path);
            Assert.That(target, Is.Not.Null, path);
            var text = target.GetComponent<Text>();
            Assert.That(text, Is.Not.Null, path);
            Assert.That(text.text, Is.EqualTo(expected), path);
            Assert.That(text.font, Is.EqualTo(font), path);
        }

        private static void AssertPrefab<T>(string path) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(PrefabUtility.GetPrefabAssetType(prefab), Is.Not.EqualTo(PrefabAssetType.NotAPrefab));
            Assert.That(prefab.GetComponentInChildren<T>(true), Is.Not.Null, path);
        }

        private static void AssertPointSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(importer.textureCompression,
                Is.EqualTo(TextureImporterCompression.Uncompressed), path);
        }
    }
}
