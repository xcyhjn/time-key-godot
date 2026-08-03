using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using TimeKey.Composition;
using TimeKey.Composition.SceneFlow;
using TimeKey.Presentation;
using TimeKey.Presentation.BattleFlow;
using TimeKey.Presentation.Bindings;
using TimeKey.Presentation.Cards;
using TimeKey.Presentation.CombatShell;
using TimeKey.Presentation.Occupants;
using TimeKey.Presentation.Presenters;
using TimeKey.Presentation.Targeting;
using TimeKey.Presentation.Tooltips;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TimeKey.Editor.AssetTooling.SceneContracts
{
    public static class SceneContractValidator
    {
        public const string CombatScenePath =
            "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity";

        private const string TimelineCellPrefabPath =
            "Assets/_Project/Prefabs/Battle/UI/TimelineCell.prefab";
        private const string BattleFlowPrefabPath =
            "Assets/_Project/Prefabs/Battle/BattleFlow/BattleFlowPanel.prefab";
        private const string BackgroundPrefabPath =
            "Assets/_Project/Prefabs/Battle/CombatShell/CombatBattleBackground.prefab";
        private const string TopHudPrefabPath =
            "Assets/_Project/Prefabs/Battle/CombatShell/CombatTopHUD.prefab";

        public static SceneContractReport ValidateCombatScene(
            string assetPath = CombatScenePath)
        {
            var diagnostics = new List<SceneContractDiagnostic>();
            var setupBefore = EditorSceneManager.GetSceneManagerSetup();
            var activeBefore = SceneManager.GetActiveScene();
            var hashBefore = ReadAssetHash(assetPath);
            var targetScene = default(Scene);
            var openedByValidator = false;
            var dirtyBefore = false;

            try
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(assetPath) == null)
                {
                    AddError(
                        diagnostics,
                        "SCENE_ASSET_REQUIRED",
                        assetPath,
                        string.Empty,
                        string.Empty,
                        "Scene asset was not found.",
                        "Restore the scene asset at the expected path; do not generate it at runtime.");
                }
                else
                {
                    targetScene = SceneManager.GetSceneByPath(assetPath);
                    if (!targetScene.IsValid() || !targetScene.isLoaded)
                    {
                        targetScene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
                        openedByValidator = true;
                    }

                    dirtyBefore = targetScene.isDirty;
                    ValidateLoadedCombatScene(targetScene, assetPath, diagnostics);
                    if (targetScene.isDirty != dirtyBefore)
                    {
                        AddError(
                            diagnostics,
                            "NO_MUTATION",
                            assetPath,
                            string.Empty,
                            string.Empty,
                            "Validation changed the scene dirty state.",
                            "Keep validation read-only and remove every write or SetDirty call.");
                    }
                }
            }
            catch (Exception exception)
            {
                AddError(
                    diagnostics,
                    "VALIDATOR_EXCEPTION",
                    assetPath,
                    string.Empty,
                    string.Empty,
                    exception.GetType().Name + ": " + exception.Message,
                    "Inspect the reported path and validator implementation; do not run authoring as a repair.");
            }
            finally
            {
                if (openedByValidator && targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }

                if (activeBefore.IsValid() && activeBefore.isLoaded &&
                    SceneManager.GetActiveScene() != activeBefore)
                {
                    SceneManager.SetActiveScene(activeBefore);
                }
            }

            var hashAfter = ReadAssetHash(assetPath);
            if (!ByteArraysEqual(hashBefore, hashAfter))
            {
                AddError(
                    diagnostics,
                    "NO_MUTATION",
                    assetPath,
                    string.Empty,
                    string.Empty,
                    "Validation changed the scene asset bytes.",
                    "Remove scene saving and asset import side effects from the validator.");
            }

            if (!SceneSetupsEqual(setupBefore, EditorSceneManager.GetSceneManagerSetup()))
            {
                AddError(
                    diagnostics,
                    "NO_MUTATION",
                    assetPath,
                    string.Empty,
                    string.Empty,
                    "Validation changed the open or active Scene setup.",
                    "Open target scenes additively and restore the caller's Scene setup.");
            }

            if (!ContainsError(diagnostics, "NO_MUTATION"))
            {
                AddInfo(
                    diagnostics,
                    "NO_MUTATION",
                    assetPath,
                    string.Empty,
                    string.Empty,
                    "Scene bytes, dirty state, open scenes, and active scene were preserved.");
            }

            diagnostics.Sort(CompareDiagnostics);
            return new SceneContractReport(assetPath, diagnostics);
        }

        private static void ValidateLoadedCombatScene(
            Scene scene,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            var root = FindUniqueRoot(scene, "VerticalSliceRoot", assetPath, diagnostics);
            var entryRoot = FindUniqueRoot(scene, "CombatSceneEntry", assetPath, diagnostics);
            if (root == null)
            {
                return;
            }

            if (root.activeSelf)
            {
                AddError(
                    diagnostics,
                    "SCENE_DEFAULT_STATE",
                    assetPath,
                    root.name,
                    "activeSelf",
                    "Combat content root must be inactive before SceneFlow binds it.",
                    "Set the saved content root inactive in the formal Scene.");
            }

            var controller = CheckComponent<VerticalSliceController>(
                root, root.name, assetPath, diagnostics);
            var composition = CheckComponent<CombatCompositionRoot>(
                root, root.name, assetPath, diagnostics);
            var binding = CheckComponent<CombatPresentationBinding>(
                root, root.name, assetPath, diagnostics);
            CheckComponent<UnityCombatTraceSink>(root, root.name, assetPath, diagnostics);

            var camera = CheckObject(
                root,
                "World/SliceCamera",
                new[] { typeof(Camera), typeof(BoardOrbitCameraController) },
                assetPath,
                diagnostics);
            var keyLight = CheckObject(root, "World/Environment/KeyLight", new[] { typeof(Light) }, assetPath, diagnostics);
            var fillLight = CheckObject(root, "World/Environment/FillLight", new[] { typeof(Light) }, assetPath, diagnostics);
            var ground = CheckObject(root, "World/BattlefieldGround", new[] { typeof(Renderer), typeof(Collider) }, assetPath, diagnostics);
            var board = CheckObject(root, "World/CombatBoardRoot", new[] { typeof(CombatOccupantPresenter) }, assetPath, diagnostics);
            var targetAnchor = CheckObject(root, "World/CombatBoardRoot/TargetAnchor", Array.Empty<Type>(), assetPath, diagnostics);
            var boardRange = CheckObject(
                root,
                "World/CombatBoardRoot/BoardRangePreview",
                new[] { typeof(BoardRangePreview), typeof(BoardRangePresenter) },
                assetPath,
                diagnostics);
            var canvas = CheckObject(root, "SliceCanvas", new[] { typeof(Canvas) }, assetPath, diagnostics);
            var hud = CheckObject(root, "SliceCanvas/HUD", Array.Empty<Type>(), assetPath, diagnostics);
            var header = CheckObject(root, "SliceCanvas/HUD/Header", Array.Empty<Type>(), assetPath, diagnostics);
            var timeline = CheckObject(
                root,
                "SliceCanvas/HUD/Timeline",
                new[] { typeof(TimelinePresenter), typeof(TimelinePlacementPreview), typeof(ClearTimelinePreview) },
                assetPath,
                diagnostics);
            var actionLayer = CheckObject(root, "SliceCanvas/HUD/Timeline/ActionLayer", Array.Empty<Type>(), assetPath, diagnostics);
            var hand = CheckObject(
                root,
                "SliceCanvas/HUD/CardHandHost",
                new[] { typeof(CardHandHost), typeof(CardHandPresenter) },
                assetPath,
                diagnostics);
            var overlay = CheckObject(
                root,
                "SliceCanvas/HUD/EffectFrameHost",
                new[] { typeof(CombatInteractionOverlayPresenter) },
                assetPath,
                diagnostics);
            var cards = CheckObject(root, "SliceCanvas/HUD/CardHandHost/Cards", Array.Empty<Type>(), assetPath, diagnostics);
            var detail = CheckObject(root, "SliceCanvas/HUD/DetailPanel", new[] { typeof(CombatHudPresenter) }, assetPath, diagnostics);
            var battleFlow = CheckObject(root, "SliceCanvas/HUD/BattleFlowPanel", new[] { typeof(BattleFlowPresenter) }, assetPath, diagnostics);
            var topHud = CheckObject(root, "SliceCanvas/HUD/CombatTopHUD", new[] { typeof(CombatTopHudPresenter) }, assetPath, diagnostics);
            CheckObject(root, "World/CombatShellBackground", new[] { typeof(CombatBattleBackground) }, assetPath, diagnostics);

            CheckPrefabInstance(root, "World/CombatShellBackground", BackgroundPrefabPath, assetPath, diagnostics);
            CheckPrefabInstance(root, "SliceCanvas/HUD/CombatTopHUD", TopHudPrefabPath, assetPath, diagnostics);
            CheckPrefabInstance(root, "SliceCanvas/HUD/BattleFlowPanel", BattleFlowPrefabPath, assetPath, diagnostics);

            CheckControllerReferences(
                controller,
                root,
                camera,
                keyLight,
                fillLight,
                ground,
                board,
                targetAnchor,
                boardRange,
                canvas,
                hud,
                header,
                timeline,
                hand,
                detail,
                binding,
                assetPath,
                diagnostics);
            CheckCompositionReferences(composition, controller, binding, root.name, assetPath, diagnostics);
            CheckBindingReferences(
                binding,
                hand,
                boardRange,
                timeline,
                detail,
                board,
                overlay,
                battleFlow,
                topHud,
                root.name,
                assetPath,
                diagnostics);
            CheckTimelineReferences(timeline, actionLayer, assetPath, diagnostics);
            CheckHandReferences(hand, cards, assetPath, diagnostics);
            CheckOverlayReferences(overlay, assetPath, diagnostics);
            CheckOccupantReferences(root, camera, assetPath, diagnostics);
            CheckEntry(entryRoot, root, camera, assetPath, diagnostics);
            CheckNoEventSystem(scene, assetPath, diagnostics);
        }

        private static GameObject FindUniqueRoot(
            Scene scene,
            string name,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            var matches = new List<GameObject>();
            foreach (var candidate in scene.GetRootGameObjects())
            {
                if (candidate.name == name)
                {
                    matches.Add(candidate);
                }
            }

            if (matches.Count == 0)
            {
                AddError(
                    diagnostics,
                    "NODE_REQUIRED",
                    assetPath,
                    name,
                    string.Empty,
                    "Required Scene root is missing.",
                    "Restore the saved root in the formal Scene; do not create it at runtime.");
                return null;
            }

            if (matches.Count > 1)
            {
                AddError(
                    diagnostics,
                    "NODE_UNIQUE",
                    assetPath,
                    name,
                    string.Empty,
                    "Expected one Scene root but found " + matches.Count + ".",
                    "Remove the duplicate saved root after reviewing its ownership.");
            }
            else
            {
                AddInfo(diagnostics, "NODE_REQUIRED", assetPath, name, string.Empty, "Required Scene root exists.");
            }

            return matches[0];
        }

        private static GameObject CheckObject(
            GameObject root,
            string path,
            Type[] requiredComponents,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            var transform = FindUniquePath(root.transform, path, assetPath, diagnostics);
            if (transform == null)
            {
                return null;
            }

            for (var index = 0; index < requiredComponents.Length; index++)
            {
                CheckComponent(
                    transform.gameObject,
                    requiredComponents[index],
                    root.name + "/" + path,
                    assetPath,
                    diagnostics);
            }

            AddInfo(
                diagnostics,
                "NODE_REQUIRED",
                assetPath,
                root.name + "/" + path,
                string.Empty,
                "Stable Scene path exists and is unique.");
            return transform.gameObject;
        }

        private static Transform FindUniquePath(
            Transform root,
            string path,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            var current = root;
            var resolved = root.name;
            var segments = path.Split('/');
            for (var segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
            {
                var matches = new List<Transform>();
                for (var childIndex = 0; childIndex < current.childCount; childIndex++)
                {
                    var child = current.GetChild(childIndex);
                    if (child.name == segments[segmentIndex])
                    {
                        matches.Add(child);
                    }
                }

                resolved += "/" + segments[segmentIndex];
                if (matches.Count == 0)
                {
                    AddError(
                        diagnostics,
                        "NODE_REQUIRED",
                        assetPath,
                        resolved,
                        string.Empty,
                        "Required stable Scene path is missing.",
                        "Restore the saved node or Prefab instance; do not generate it at runtime.");
                    return null;
                }

                if (matches.Count > 1)
                {
                    AddError(
                        diagnostics,
                        "NODE_UNIQUE",
                        assetPath,
                        resolved,
                        string.Empty,
                        "Expected one direct child but found " + matches.Count + ".",
                        "Remove duplicate stable nodes after reviewing their Prefab ownership.");
                }

                current = matches[0];
            }

            return current;
        }

        private static T CheckComponent<T>(
            GameObject target,
            string objectPath,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics) where T : Component
        {
            return CheckComponent(target, typeof(T), objectPath, assetPath, diagnostics) as T;
        }

        private static Component CheckComponent(
            GameObject target,
            Type componentType,
            string objectPath,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            var components = target.GetComponents(componentType);
            if (components.Length == 0)
            {
                AddError(
                    diagnostics,
                    "COMPONENT_REQUIRED",
                    assetPath,
                    objectPath,
                    componentType.FullName,
                    "Required component is missing.",
                    "Restore the component on the saved Scene or Prefab object.");
                return null;
            }

            if (components.Length > 1)
            {
                AddError(
                    diagnostics,
                    "COMPONENT_UNIQUE",
                    assetPath,
                    objectPath,
                    componentType.FullName,
                    "Expected one component but found " + components.Length + ".",
                    "Remove duplicate components after reviewing serialized references.");
            }

            return components[0];
        }

        private static void CheckControllerReferences(
            VerticalSliceController controller,
            GameObject root,
            GameObject camera,
            GameObject keyLight,
            GameObject fillLight,
            GameObject ground,
            GameObject board,
            GameObject targetAnchor,
            GameObject boardRange,
            GameObject canvas,
            GameObject hud,
            GameObject header,
            GameObject timeline,
            GameObject hand,
            GameObject detail,
            CombatPresentationBinding binding,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            var objectPath = root.name;
            CheckReferences(
                controller,
                objectPath,
                assetPath,
                diagnostics,
                new[]
                {
                    "sceneCamera", "boardCamera", "keyLight", "fillLight", "battlefieldGround",
                    "boardRoot", "dynamicRoot", "targetAnchor", "sceneCanvas", "hudRoot",
                    "timelineRoot", "statusText", "targetText", "resolveButton", "cardHandHost",
                    "boardRangePreview", "timelinePlacementPreview", "presentationBinding",
                    "hexColumnPrefab", "grassBlockPrefab", "dirtBlockPrefab", "targetViewPrefab"
                },
                new Dictionary<string, string>
                {
                    { "hexColumnPrefab", "Assets/_Project/Prefabs/Battle/Terrain/HexColumn.prefab" },
                    { "grassBlockPrefab", "Assets/_Project/Prefabs/Battle/Terrain/HexBlockGrass.prefab" },
                    { "dirtBlockPrefab", "Assets/_Project/Prefabs/Battle/Terrain/HexBlockDirt.prefab" },
                    { "targetViewPrefab", "Assets/_Project/Prefabs/Battle/Targets/TargetView.prefab" }
                });
            CheckExpectedReferences(
                controller,
                objectPath,
                assetPath,
                diagnostics,
                new Dictionary<string, UnityEngine.Object>
                {
                    { "sceneCamera", camera == null ? null : camera.GetComponent<Camera>() },
                    { "boardCamera", camera == null ? null : camera.GetComponent<BoardOrbitCameraController>() },
                    { "keyLight", keyLight == null ? null : keyLight.GetComponent<Light>() },
                    { "fillLight", fillLight == null ? null : fillLight.GetComponent<Light>() },
                    { "battlefieldGround", ground == null ? null : ground.GetComponent<Renderer>() },
                    { "boardRoot", board == null ? null : board.transform },
                    { "dynamicRoot", board == null ? null : board.transform },
                    { "targetAnchor", targetAnchor == null ? null : targetAnchor.transform },
                    { "sceneCanvas", canvas == null ? null : canvas.GetComponent<Canvas>() },
                    { "hudRoot", hud == null ? null : hud.GetComponent<RectTransform>() },
                    { "timelineRoot", timeline == null ? null : timeline.GetComponent<RectTransform>() },
                    { "statusText", FindComponentAtPath<UnityEngine.UI.Text>(header, "Status") },
                    { "targetText", FindComponentAtPath<UnityEngine.UI.Text>(detail, "TargetStatus") },
                    { "resolveButton", FindComponentAtPath<UnityEngine.UI.Button>(detail, "Resolve") },
                    { "cardHandHost", hand == null ? null : hand.GetComponent<CardHandHost>() },
                    { "boardRangePreview", boardRange == null ? null : boardRange.GetComponent<BoardRangePreview>() },
                    { "timelinePlacementPreview", timeline == null ? null : timeline.GetComponent<TimelinePlacementPreview>() },
                    { "presentationBinding", binding }
                });
        }

        private static void CheckCompositionReferences(
            CombatCompositionRoot composition,
            VerticalSliceController controller,
            CombatPresentationBinding binding,
            string objectPath,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            CheckReferences(
                composition,
                objectPath,
                assetPath,
                diagnostics,
                new[] { "controller", "presentationBinding", "traceSink" },
                null);
            CheckExpectedReferences(
                composition,
                objectPath,
                assetPath,
                diagnostics,
                new Dictionary<string, UnityEngine.Object>
                {
                    { "controller", controller },
                    { "presentationBinding", binding },
                    { "traceSink", composition == null ? null : composition.GetComponent<UnityCombatTraceSink>() }
                });
            if (composition == null)
            {
                return;
            }

            var fixtures = new SerializedObject(composition).FindProperty("cardFixtures");
            if (fixtures == null || !fixtures.isArray || fixtures.arraySize != 7)
            {
                AddError(
                    diagnostics,
                    "SERIALIZED_REFERENCE_REQUIRED",
                    assetPath,
                    objectPath,
                    "cardFixtures",
                    "Combat composition must serialize exactly seven card fixtures.",
                    "Restore the seven reviewed TextAsset references in the formal Scene.");
                return;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < fixtures.arraySize; index++)
            {
                var fixture = fixtures.GetArrayElementAtIndex(index).objectReferenceValue as TextAsset;
                if (fixture == null || !names.Add(fixture.name))
                {
                    AddError(
                        diagnostics,
                        "SERIALIZED_REFERENCE_REQUIRED",
                        assetPath,
                        objectPath,
                        "cardFixtures[" + index + "]",
                        "Card fixture is null or duplicates another stable ID.",
                        "Restore one unique reviewed TextAsset per stable card ID.");
                }
            }
        }

        private static void CheckBindingReferences(
            CombatPresentationBinding binding,
            GameObject hand,
            GameObject boardRange,
            GameObject timeline,
            GameObject detail,
            GameObject board,
            GameObject overlay,
            GameObject battleFlow,
            GameObject topHud,
            string objectPath,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            CheckReferences(
                binding,
                objectPath,
                assetPath,
                diagnostics,
                new[]
                {
                    "cardHandPresenter", "boardRangePresenter", "timelinePresenter", "hudPresenter",
                    "occupantPresenter", "interactionOverlayPresenter", "battleFlowPresenter",
                    "combatTopHudPresenter"
                },
                null);
            CheckExpectedReferences(
                binding,
                objectPath,
                assetPath,
                diagnostics,
                new Dictionary<string, UnityEngine.Object>
                {
                    { "cardHandPresenter", hand == null ? null : hand.GetComponent<CardHandPresenter>() },
                    { "boardRangePresenter", boardRange == null ? null : boardRange.GetComponent<BoardRangePresenter>() },
                    { "timelinePresenter", timeline == null ? null : timeline.GetComponent<TimelinePresenter>() },
                    { "hudPresenter", detail == null ? null : detail.GetComponent<CombatHudPresenter>() },
                    { "occupantPresenter", board == null ? null : board.GetComponent<CombatOccupantPresenter>() },
                    { "interactionOverlayPresenter", overlay == null ? null : overlay.GetComponent<CombatInteractionOverlayPresenter>() },
                    { "battleFlowPresenter", battleFlow == null ? null : battleFlow.GetComponent<BattleFlowPresenter>() },
                    { "combatTopHudPresenter", topHud == null ? null : topHud.GetComponent<CombatTopHudPresenter>() }
                });
        }

        private static void CheckTimelineReferences(
            GameObject timelineObject,
            GameObject actionLayer,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            if (timelineObject == null)
            {
                return;
            }

            var presenter = timelineObject.GetComponent<TimelinePresenter>();
            CheckReferences(
                presenter,
                "VerticalSliceRoot/SliceCanvas/HUD/Timeline",
                assetPath,
                diagnostics,
                new[] { "timelinePreview", "clearTimelinePreview", "timelineCells", "actionLayer", "actionFramePrefab" },
                new Dictionary<string, string>
                {
                    { "actionFramePrefab", "Assets/_Project/Prefabs/Battle/UI/TimelineActionFrame.prefab" }
                },
                skipArray: true);
            CheckExpectedReferences(
                presenter,
                "VerticalSliceRoot/SliceCanvas/HUD/Timeline",
                assetPath,
                diagnostics,
                new Dictionary<string, UnityEngine.Object>
                {
                    { "timelinePreview", timelineObject.GetComponent<TimelinePlacementPreview>() },
                    { "clearTimelinePreview", timelineObject.GetComponent<ClearTimelinePreview>() },
                    { "actionLayer", actionLayer == null ? null : actionLayer.GetComponent<RectTransform>() }
                });
            if (presenter == null)
            {
                return;
            }

            var cells = new SerializedObject(presenter).FindProperty("timelineCells");
            if (cells == null || !cells.isArray || cells.arraySize != 36)
            {
                AddError(
                    diagnostics,
                    "PREFAB_SOURCE_REQUIRED",
                    assetPath,
                    "VerticalSliceRoot/SliceCanvas/HUD/Timeline",
                    "timelineCells",
                    "Timeline must serialize exactly 36 cell views.",
                    "Restore the 12 by 3 saved TimelineCell Prefab instances.");
                return;
            }

            var uniqueViews = new HashSet<Component>();
            var uniqueCoordinates = new HashSet<int>();
            for (var index = 0; index < cells.arraySize; index++)
            {
                var view = cells.GetArrayElementAtIndex(index).objectReferenceValue as Component;
                if (view == null || !uniqueViews.Add(view))
                {
                    AddError(
                        diagnostics,
                        "SERIALIZED_REFERENCE_REQUIRED",
                        assetPath,
                        "VerticalSliceRoot/SliceCanvas/HUD/Timeline",
                        "timelineCells[" + index + "]",
                        "Timeline cell reference is null or duplicated.",
                        "Restore one unique saved cell reference for every coordinate.");
                    continue;
                }

                CheckPrefabSource(
                    view.gameObject,
                    TimelineCellPrefabPath,
                    assetPath,
                    "VerticalSliceRoot/SliceCanvas/HUD/Timeline",
                    "timelineCells[" + index + "]",
                    diagnostics);
                var serializedView = new SerializedObject(view);
                var column = serializedView.FindProperty("column");
                var row = serializedView.FindProperty("row");
                var coordinateId = column == null || row == null
                    ? -1
                    : row.intValue * 12 + column.intValue;
                if (column == null || row == null ||
                    column.intValue < 0 || column.intValue >= 12 ||
                    row.intValue < 0 || row.intValue >= 3 ||
                    !uniqueCoordinates.Add(coordinateId))
                {
                    AddError(
                        diagnostics,
                        "TIMELINE_COORDINATE_UNIQUE",
                        assetPath,
                        "VerticalSliceRoot/SliceCanvas/HUD/Timeline",
                        "timelineCells[" + index + "]",
                        "Timeline cell coordinate is missing, outside 12 by 3, or duplicated.",
                        "Restore exactly one reviewed TimelineCell for every coordinate.");
                }
            }
        }

        private static void CheckHandReferences(
            GameObject handObject,
            GameObject cardContainer,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            if (handObject == null)
            {
                return;
            }

            CheckReferences(
                handObject.GetComponent<CardHandHost>(),
                "VerticalSliceRoot/SliceCanvas/HUD/CardHandHost",
                assetPath,
                diagnostics,
                new[] { "cardContainer", "cardViewPrefab" },
                new Dictionary<string, string>
                {
                    { "cardViewPrefab", "Assets/_Project/Prefabs/Battle/Cards/CardView.prefab" }
                });
            CheckExpectedReferences(
                handObject.GetComponent<CardHandHost>(),
                "VerticalSliceRoot/SliceCanvas/HUD/CardHandHost",
                assetPath,
                diagnostics,
                new Dictionary<string, UnityEngine.Object>
                {
                    { "cardContainer", cardContainer == null ? null : cardContainer.GetComponent<RectTransform>() }
                });
        }

        private static void CheckOverlayReferences(
            GameObject overlayObject,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            if (overlayObject == null)
            {
                return;
            }

            CheckReferences(
                overlayObject.GetComponent<CombatInteractionOverlayPresenter>(),
                "VerticalSliceRoot/SliceCanvas/HUD/EffectFrameHost",
                assetPath,
                diagnostics,
                new[] { "frameHost", "framePrefab" },
                new Dictionary<string, string>
                {
                    { "framePrefab", "Assets/_Project/Prefabs/Battle/UI/CardEffectFrame.prefab" }
                });
            CheckExpectedReferences(
                overlayObject.GetComponent<CombatInteractionOverlayPresenter>(),
                "VerticalSliceRoot/SliceCanvas/HUD/EffectFrameHost",
                assetPath,
                diagnostics,
                new Dictionary<string, UnityEngine.Object>
                {
                    { "frameHost", overlayObject.GetComponent<RectTransform>() }
                });
        }

        private static void CheckOccupantReferences(
            GameObject root,
            GameObject camera,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            var presenter = root.GetComponentInChildren<CombatOccupantPresenter>(true);
            CheckReferences(
                presenter,
                "VerticalSliceRoot/World/CombatBoardRoot",
                assetPath,
                diagnostics,
                new[] { "sceneCamera", "creationViews", "poisonStatusPrefab" },
                new Dictionary<string, string>
                {
                    { "poisonStatusPrefab", "Assets/_Project/Prefabs/Battle/Status/PoisonStatus.prefab" }
                },
                skipArray: true);
            CheckExpectedReferences(
                presenter,
                "VerticalSliceRoot/World/CombatBoardRoot",
                assetPath,
                diagnostics,
                new Dictionary<string, UnityEngine.Object>
                {
                    { "sceneCamera", camera == null ? null : camera.GetComponent<Camera>() }
                });
            if (presenter == null)
            {
                return;
            }

            var registrations = new SerializedObject(presenter).FindProperty("creationViews");
            if (registrations == null || !registrations.isArray || registrations.arraySize != 1)
            {
                AddError(
                    diagnostics,
                    "SERIALIZED_REFERENCE_REQUIRED",
                    assetPath,
                    "VerticalSliceRoot/World/CombatBoardRoot",
                    "creationViews",
                    "Exactly one reviewed tower Prefab registration is required.",
                    "Restore the frozen tower creation ID to Prefab mapping.");
                return;
            }

            for (var index = 0; index < registrations.arraySize; index++)
            {
                var item = registrations.GetArrayElementAtIndex(index);
                var creationId = item.FindPropertyRelative("creationId")?.stringValue;
                var prefab = item.FindPropertyRelative("prefab")?.objectReferenceValue as GameObject;
                var prefabPath = prefab == null ? string.Empty : AssetDatabase.GetAssetPath(prefab);
                if (creationId != "tower" ||
                    prefabPath != "Assets/_Project/Prefabs/Battle/Occupants/Tower.prefab" ||
                    prefab.GetComponent<CombatOccupantView>() == null)
                {
                    AddError(
                        diagnostics,
                        "PREFAB_SOURCE_REQUIRED",
                        assetPath,
                        "VerticalSliceRoot/World/CombatBoardRoot",
                        "creationViews[" + index + "].prefab",
                        "Occupant registration must map 'tower' to the reviewed Tower Prefab with CombatOccupantView.",
                        "Restore the frozen creation ID and Tower Prefab in the formal Scene.");
                }
            }
        }

        private static void CheckEntry(
            GameObject entryRoot,
            GameObject contentRoot,
            GameObject cameraObject,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            if (entryRoot == null)
            {
                return;
            }

            if (!entryRoot.activeSelf)
            {
                AddError(
                    diagnostics,
                    "SCENE_DEFAULT_STATE",
                    assetPath,
                    entryRoot.name,
                    "activeSelf",
                    "Scene content entry must be active so Bootstrap can discover it.",
                    "Set the saved entry root active.");
            }

            var entry = CheckComponent<SceneContentEntry>(
                entryRoot, entryRoot.name, assetPath, diagnostics);
            if (entry == null)
            {
                return;
            }

            var serialized = new SerializedObject(entry);
            CheckExpectedReference(serialized, "contentRoot", contentRoot, entryRoot.name, assetPath, diagnostics);
            CheckExpectedReference(
                serialized,
                "contentCamera",
                cameraObject == null ? null : cameraObject.GetComponent<Camera>(),
                entryRoot.name,
                assetPath,
                diagnostics);
            CheckNonNullReference(serialized, "interactionGroup", entryRoot.name, assetPath, diagnostics, null);
            var sceneId = serialized.FindProperty("sceneId");
            if (sceneId == null ||
                sceneId.enumValueIndex < 0 ||
                sceneId.enumValueIndex >= sceneId.enumDisplayNames.Length ||
                sceneId.enumDisplayNames[sceneId.enumValueIndex] != "Combat")
            {
                AddError(
                    diagnostics,
                    "SERIALIZED_REFERENCE_REQUIRED",
                    assetPath,
                    entryRoot.name,
                    "sceneId",
                    "Scene content entry must identify the Combat scene.",
                    "Restore the typed Combat SceneId on the saved entry.");
            }
        }

        private static void CheckNoEventSystem(
            Scene scene,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            var count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                count += root.GetComponentsInChildren<EventSystem>(true).Length;
            }

            if (count != 0)
            {
                AddError(
                    diagnostics,
                    "BOOTSTRAP_SERVICE_UNIQUE",
                    assetPath,
                    string.Empty,
                    typeof(EventSystem).FullName,
                    "Combat content Scene contains " + count + " EventSystem component(s).",
                    "Keep the single persistent EventSystem in Bootstrap only.");
            }
            else
            {
                AddInfo(
                    diagnostics,
                    "BOOTSTRAP_SERVICE_UNIQUE",
                    assetPath,
                    string.Empty,
                    typeof(EventSystem).FullName,
                    "Combat content Scene contains no local EventSystem.");
            }
        }

        private static void CheckPrefabInstance(
            GameObject root,
            string path,
            string expectedPrefabPath,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            var target = root.transform.Find(path);
            if (target != null)
            {
                CheckPrefabSource(
                    target.gameObject,
                    expectedPrefabPath,
                    assetPath,
                    root.name + "/" + path,
                    string.Empty,
                    diagnostics);
            }
        }

        private static void CheckReferences(
            Component owner,
            string objectPath,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics,
            string[] propertyNames,
            Dictionary<string, string> expectedPrefabPaths,
            bool skipArray = false)
        {
            if (owner == null)
            {
                return;
            }

            var serialized = new SerializedObject(owner);
            for (var index = 0; index < propertyNames.Length; index++)
            {
                var property = serialized.FindProperty(propertyNames[index]);
                if (skipArray && property != null && property.isArray)
                {
                    continue;
                }

                string expectedPrefab = null;
                if (expectedPrefabPaths != null)
                {
                    expectedPrefabPaths.TryGetValue(propertyNames[index], out expectedPrefab);
                }

                CheckNonNullReference(
                    serialized,
                    propertyNames[index],
                    objectPath,
                    assetPath,
                    diagnostics,
                    expectedPrefab);
            }
        }

        private static T FindComponentAtPath<T>(GameObject root, string path)
            where T : Component
        {
            if (root == null)
            {
                return null;
            }

            var child = root.transform.Find(path);
            return child == null ? null : child.GetComponent<T>();
        }

        private static void CheckExpectedReferences(
            Component owner,
            string objectPath,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics,
            Dictionary<string, UnityEngine.Object> expectedReferences)
        {
            if (owner == null)
            {
                return;
            }

            var serialized = new SerializedObject(owner);
            foreach (var expected in expectedReferences)
            {
                CheckExpectedReference(
                    serialized,
                    expected.Key,
                    expected.Value,
                    objectPath,
                    assetPath,
                    diagnostics);
            }
        }

        private static void CheckNonNullReference(
            SerializedObject serialized,
            string propertyName,
            string objectPath,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics,
            string expectedPrefabPath)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference ||
                property.objectReferenceValue == null)
            {
                AddError(
                    diagnostics,
                    "SERIALIZED_REFERENCE_REQUIRED",
                    assetPath,
                    objectPath,
                    propertyName,
                    "Required serialized object reference is missing.",
                    "Restore the reviewed Scene or Prefab reference in the Inspector.");
                return;
            }

            if (!string.IsNullOrEmpty(expectedPrefabPath))
            {
                var actualPath = AssetDatabase.GetAssetPath(property.objectReferenceValue);
                if (actualPath != expectedPrefabPath)
                {
                    AddError(
                        diagnostics,
                        "PREFAB_SOURCE_REQUIRED",
                        assetPath,
                        objectPath,
                        propertyName,
                        "Expected Prefab '" + expectedPrefabPath + "' but found '" + actualPath + "'.",
                        "Restore the reviewed saved Prefab reference.");
                }
            }
        }

        private static void CheckExpectedReference(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object expected,
            string objectPath,
            string assetPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue != expected)
            {
                AddError(
                    diagnostics,
                    "SERIALIZED_REFERENCE_REQUIRED",
                    assetPath,
                    objectPath,
                    propertyName,
                    "Serialized reference does not point to the frozen Scene object.",
                    "Restore the exact saved Scene reference in the Inspector.");
            }
        }

        private static void CheckPrefabSource(
            GameObject instance,
            string expectedPrefabPath,
            string assetPath,
            string objectPath,
            string propertyPath,
            List<SceneContractDiagnostic> diagnostics)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            var actualPath = source == null ? string.Empty : AssetDatabase.GetAssetPath(source);
            if (actualPath != expectedPrefabPath)
            {
                AddError(
                    diagnostics,
                    "PREFAB_SOURCE_REQUIRED",
                    assetPath,
                    objectPath,
                    propertyPath,
                    "Expected Prefab source '" + expectedPrefabPath + "' but found '" + actualPath + "'.",
                    "Restore the reviewed Prefab instance; do not reconstruct the stable object in code.");
            }
        }

        private static byte[] ReadAssetHash(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return Array.Empty<byte>();
            }

            var fullPath = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", assetPath));
            if (!File.Exists(fullPath))
            {
                return Array.Empty<byte>();
            }

            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(fullPath))
            {
                return sha.ComputeHash(stream);
            }
        }

        private static bool SceneSetupsEqual(SceneSetup[] left, SceneSetup[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (var index = 0; index < left.Length; index++)
            {
                if (left[index].path != right[index].path ||
                    left[index].isLoaded != right[index].isLoaded ||
                    left[index].isActive != right[index].isActive)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ByteArraysEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (var index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsError(
            List<SceneContractDiagnostic> diagnostics,
            string contractId)
        {
            for (var index = 0; index < diagnostics.Count; index++)
            {
                if (diagnostics[index].Severity == SceneContractSeverity.Error &&
                    diagnostics[index].ContractId == contractId)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CompareDiagnostics(
            SceneContractDiagnostic left,
            SceneContractDiagnostic right)
        {
            var severity = right.Severity.CompareTo(left.Severity);
            if (severity != 0)
            {
                return severity;
            }

            var contract = string.CompareOrdinal(left.ContractId, right.ContractId);
            if (contract != 0)
            {
                return contract;
            }

            var objectPath = string.CompareOrdinal(left.ObjectPath, right.ObjectPath);
            return objectPath != 0
                ? objectPath
                : string.CompareOrdinal(left.PropertyPath, right.PropertyPath);
        }

        private static void AddInfo(
            List<SceneContractDiagnostic> diagnostics,
            string contractId,
            string assetPath,
            string objectPath,
            string propertyPath,
            string message)
        {
            diagnostics.Add(new SceneContractDiagnostic(
                SceneContractSeverity.Info,
                contractId,
                assetPath,
                objectPath,
                propertyPath,
                message,
                string.Empty));
        }

        private static void AddError(
            List<SceneContractDiagnostic> diagnostics,
            string contractId,
            string assetPath,
            string objectPath,
            string propertyPath,
            string message,
            string remediation)
        {
            diagnostics.Add(new SceneContractDiagnostic(
                SceneContractSeverity.Error,
                contractId,
                assetPath,
                objectPath,
                propertyPath,
                message,
                remediation));
        }
    }
}
