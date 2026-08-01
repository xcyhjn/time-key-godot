using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TimeKey.Composition;
using TimeKey.Domain;
using TimeKey.Presentation;
using TimeKey.Presentation.Bindings;
using TimeKey.Presentation.Cards;
using TimeKey.Presentation.Localization;
using TimeKey.Presentation.Occupants;
using TimeKey.Presentation.Presenters;
using TimeKey.Presentation.Targeting;
using TimeKey.Presentation.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeKey.Editor
{
    public static class VerticalSliceSceneAuthoring
    {
        public const string ScenePath = "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity";
        public const string TimelineCellPrefabPath = "Assets/_Project/Prefabs/Battle/UI/TimelineCell.prefab";
        public const string CardViewPrefabPath = "Assets/_Project/Prefabs/Battle/Cards/CardView.prefab";
        public const string GrassBlockPrefabPath = "Assets/_Project/Prefabs/Battle/Terrain/HexBlockGrass.prefab";
        public const string DirtBlockPrefabPath = "Assets/_Project/Prefabs/Battle/Terrain/HexBlockDirt.prefab";
        public const string HexColumnPrefabPath = "Assets/_Project/Prefabs/Battle/Terrain/HexColumn.prefab";
        public const string TargetViewPrefabPath = "Assets/_Project/Prefabs/Battle/Targets/TargetView.prefab";
        public const string TowerPrefabPath = "Assets/_Project/Prefabs/Battle/Occupants/Tower.prefab";
        public const string PoisonStatusPrefabPath = "Assets/_Project/Prefabs/Battle/Status/PoisonStatus.prefab";
        public const string ChineseFontAssetPath =
            "Assets/_Project/Resources/Fonts/Silver.ttf";

        private const string TowerTexturePath =
            "Assets/_Project/Resources/Art/Battle/Occupants/tower.png";
        private const string PoisonStatusTexturePath =
            "Assets/_Project/Resources/Art/Battle/Status/poison_icon.png";

        private const string GeneratedAssetDirectory = "Assets/_Project/Art/Generated/VerticalSlice";
        private const string MaterialDirectory = "Assets/_Project/Materials/VerticalSlice";

        [MenuItem("Time Key/Author Editable Combat Scene")]
        public static void AuthorEditableCombatScene()
        {
            EnsureAssetDirectories();

            var groundMaterial = LoadOrCreateMaterial(
                MaterialDirectory + "/BattlefieldGround.mat",
                "Universal Render Pipeline/Unlit",
                new Color(0.055f, 0.07f, 0.065f, 1f));
            var targetMaterial = LoadOrCreateMaterial(
                MaterialDirectory + "/Target.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.86f, 0.24f, 0.24f, 1f));
            var centerAltar = LoadOrCreateSprite(
                GeneratedAssetDirectory + "/center_altar.asset",
                "Art/Battle/center_altar");
            var lightingCard = LoadOrCreateSprite(
                GeneratedAssetDirectory + "/lighting.asset",
                "Art/Battle/Cards/lighting");

            ConfigureSpriteImporter(TowerTexturePath);
            ConfigureSpriteImporter(PoisonStatusTexturePath);
            var towerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TowerTexturePath);
            var poisonStatusSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PoisonStatusTexturePath);
            if (towerSprite == null || poisonStatusSprite == null)
            {
                throw new InvalidOperationException("Tower or poison source artwork failed to import as a Sprite.");
            }

            var grassBlock = CreateHexBlockPrefab(
                GrassBlockPrefabPath,
                "HexBlockGrass",
                "Art/Battle/Models/HexTile_Grass");
            var dirtBlock = CreateHexBlockPrefab(
                DirtBlockPrefabPath,
                "HexBlockDirt",
                "Art/Battle/Models/HexTile_Dirt");
            var hexColumn = CreateHexColumnPrefab(grassBlock, dirtBlock);
            var targetView = CreateTargetViewPrefab(targetMaterial, centerAltar);
            var towerView = CreateTowerPrefab(towerSprite);
            var poisonStatusView = CreatePoisonStatusPrefab(poisonStatusSprite);
            CreateTimelineCellPrefab();
            CreateCardViewPrefab(lightingCard);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            grassBlock = AssetDatabase.LoadAssetAtPath<GameObject>(GrassBlockPrefabPath);
            dirtBlock = AssetDatabase.LoadAssetAtPath<GameObject>(DirtBlockPrefabPath);
            hexColumn = AssetDatabase.LoadAssetAtPath<GameObject>(HexColumnPrefabPath);
            targetView = AssetDatabase.LoadAssetAtPath<GameObject>(TargetViewPrefabPath);
            towerView = AssetDatabase.LoadAssetAtPath<GameObject>(TowerPrefabPath)
                .GetComponent<CombatOccupantView>();
            poisonStatusView = AssetDatabase.LoadAssetAtPath<GameObject>(PoisonStatusPrefabPath)
                .GetComponent<PoisonStatusView>();
            var timelineCellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TimelineCellPrefabPath);
            var cardViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardViewPrefabPath);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().SingleOrDefault(value => value.name == "VerticalSliceRoot");
            if (root == null)
            {
                throw new InvalidOperationException("VerticalSliceRoot is missing from " + ScenePath + ".");
            }

            var controller = root.GetComponent<VerticalSliceController>();
            if (controller == null)
            {
                throw new InvalidOperationException("VerticalSliceController is missing from VerticalSliceRoot.");
            }

            var compositionRoot = GetOrAddComponent<CombatCompositionRoot>(root);
            var traceSink = GetOrAddComponent<UnityCombatTraceSink>(root);

            for (var index = root.transform.childCount - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(index).gameObject);
            }

            var worldRoot = CreateTransform("World", root.transform);
            var cameraObject = new GameObject("SliceCamera", typeof(Camera), typeof(BoardOrbitCameraController));
            cameraObject.transform.SetParent(worldRoot, false);
            var sceneCamera = cameraObject.GetComponent<Camera>();
            sceneCamera.orthographic = false;
            sceneCamera.fieldOfView = 38f;
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = new Color(0.035f, 0.047f, 0.055f, 1f);
            sceneCamera.nearClipPlane = 0.1f;
            sceneCamera.farClipPlane = 100f;
            var boardCamera = cameraObject.GetComponent<BoardOrbitCameraController>();

            var environment = CreateTransform("Environment", worldRoot);
            var keyLight = CreateDirectionalLight(
                "KeyLight",
                environment,
                new Color(1f, 0.92f, 0.78f, 1f),
                1.4f,
                Quaternion.Euler(48f, -28f, 0f));
            var fillLight = CreateDirectionalLight(
                "FillLight",
                environment,
                new Color(0.35f, 0.66f, 0.92f, 1f),
                0.65f,
                Quaternion.Euler(36f, 145f, 0f));

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "BattlefieldGround";
            ground.transform.SetParent(worldRoot, false);
            ground.transform.position = new Vector3(0f, -0.04f, 0f);
            ground.transform.localScale = new Vector3(2.1f, 1f, 2.1f);
            var groundRenderer = ground.GetComponent<Renderer>();
            groundRenderer.sharedMaterial = groundMaterial;

            var boardRoot = CreateTransform("CombatBoardRoot", worldRoot);
            var targetAnchor = CreateTransform("TargetAnchor", boardRoot);
            var boardRangeObject = new GameObject("BoardRangePreview", typeof(BoardRangePreview));
            boardRangeObject.transform.SetParent(boardRoot, false);
            var boardRangePreview = boardRangeObject.GetComponent<BoardRangePreview>();
            var boardRangePresenter = boardRangeObject.AddComponent<BoardRangePresenter>();
            ConfigureReference(boardRangePresenter, "rangePreview", boardRangePreview);
            var occupantPresenter = boardRoot.gameObject.AddComponent<CombatOccupantPresenter>();
            ConfigureOccupantPresenter(
                occupantPresenter,
                sceneCamera,
                towerView.gameObject,
                poisonStatusView.gameObject);

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemObject.transform.SetParent(root.transform, false);
            var eventSystem = eventSystemObject.GetComponent<EventSystem>();

            var canvasObject = new GameObject(
                "SliceCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = sceneCamera;
            canvas.planeDistance = 1f;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var hudRoot = CreateRect("HUD", canvas.transform);
            Stretch(hudRoot);
            var header = CreatePanel(
                hudRoot,
                "Header",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(24f, -104f),
                new Vector2(-24f, -24f),
                new Color(0.06f, 0.08f, 0.09f, 1f));
            CreateText(header, "Title", CombatChineseText.SceneTitle, 28, TextAnchor.MiddleLeft,
                new Vector2(22f, 8f), new Vector2(-22f, -38f));
            var statusText = CreateText(header, "Status", string.Empty, 20, TextAnchor.MiddleLeft,
                new Vector2(22f, 42f), new Vector2(-22f, -8f));

            var timelineRoot = CreatePanel(
                hudRoot,
                "Timeline",
                new Vector2(0.18f, 0.73f),
                new Vector2(0.82f, 0.90f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.07f, 0.09f, 0.10f, 1f));
            var grid = timelineRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(20, 20, 22, 18);
            grid.cellSize = new Vector2(88f, 44f);
            grid.spacing = new Vector2(4f, 5f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = TimelineGrid.DefaultWidth;
            var timelinePreview = timelineRoot.gameObject.AddComponent<TimelinePlacementPreview>();
            var clearTimelinePreview = timelineRoot.gameObject.AddComponent<ClearTimelinePreview>();
            var timelinePresenter = timelineRoot.gameObject.AddComponent<TimelinePresenter>();
            var timelineCells = new List<TimelineCellView>(36);
            for (var row = 0; row < TimelineGrid.DefaultHeight; row++)
            {
                for (var column = 0; column < TimelineGrid.DefaultWidth; column++)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(timelineCellPrefab, scene);
                    instance.name = string.Format("Slot-{0}-{1}", column, row);
                    instance.transform.SetParent(timelineRoot, false);
                    var view = instance.GetComponent<TimelineCellView>();
                    ConfigureTimelineCell(view, column, row);
                    view.SetContent(string.Format("{0:00}", column + 1), new Color(0.16f, 0.19f, 0.20f, 1f));
                    view.CaptureCurrentAsDefault();
                    timelineCells.Add(view);
                }
            }

            var cardHandRect = CreateRect("CardHandHost", hudRoot);
            SetRect(
                cardHandRect,
                new Vector2(0f, 0f),
                new Vector2(0.67f, 0f),
                new Vector2(24f, 0f),
                new Vector2(-24f, 280f));
            var cardHandHost = cardHandRect.gameObject.AddComponent<CardHandHost>();
            var cardContainer = CreateRect("Cards", cardHandRect);
            Stretch(cardContainer);
            ConfigureCardHandHost(cardHandHost, cardContainer, cardViewPrefab.GetComponent<CardHandView>());
            var cardHandPresenter = cardHandRect.gameObject.AddComponent<CardHandPresenter>();
            ConfigureReference(cardHandPresenter, "cardHand", cardHandHost);

            var detailPanel = CreatePanel(
                hudRoot,
                "DetailPanel",
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-414f, 24f),
                new Vector2(-24f, 224f),
                new Color(0.08f, 0.10f, 0.11f, 0.96f));
            var targetText = CreateText(detailPanel, "TargetStatus", CombatChineseText.DefaultTargetStatus, 20,
                TextAnchor.MiddleLeft, new Vector2(20f, 112f), new Vector2(-20f, -18f));
            CreateText(detailPanel, "Intent", CombatChineseText.EnemyIntentDetail, 18,
                TextAnchor.MiddleLeft, new Vector2(20f, 72f), new Vector2(-20f, -60f));
            var resolveButton = CreateButton(
                detailPanel,
                "Resolve",
                CombatChineseText.ResolveTimeline,
                new Color(0.70f, 0.25f, 0.20f, 1f));
            SetRect(resolveButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero,
                new Vector2(20f, 18f), new Vector2(370f, 64f));
            resolveButton.interactable = false;

            var hudPresenter = detailPanel.gameObject.AddComponent<CombatHudPresenter>();
            ConfigureHudPresenter(hudPresenter, statusText, targetText, resolveButton);
            ConfigureTimelinePresenter(
                timelinePresenter,
                timelinePreview,
                clearTimelinePreview,
                timelineCells);
            var presentationBinding = GetOrAddComponent<CombatPresentationBinding>(root);
            ConfigurePresentationBinding(
                presentationBinding,
                cardHandPresenter,
                boardRangePresenter,
                timelinePresenter,
                hudPresenter,
                occupantPresenter);

            ConfigureController(
                controller,
                sceneCamera,
                boardCamera,
                keyLight,
                fillLight,
                groundRenderer,
                boardRoot,
                targetAnchor,
                eventSystem,
                canvas,
                hudRoot,
                timelineRoot,
                statusText,
                targetText,
                resolveButton,
                cardHandHost,
                boardRangePreview,
                timelinePreview,
                presentationBinding,
                hexColumn,
                grassBlock,
                dirtBlock,
                targetView);
            ConfigureCompositionRoot(compositionRoot, controller, traceSink);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("TIMEKEY_EDITABLE_SCENE_AUTHORING_PASS");
        }

        [MenuItem("Time Key/Author Remaining Cards Gate B Assets")]
        public static void AuthorRemainingCardsGateBAssets()
        {
            EnsureAssetDirectories();
            ConfigureSpriteImporter(TowerTexturePath);
            ConfigureSpriteImporter(PoisonStatusTexturePath);
            var towerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TowerTexturePath);
            var poisonStatusSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PoisonStatusTexturePath);
            if (towerSprite == null || poisonStatusSprite == null)
            {
                throw new InvalidOperationException("Tower or poison source artwork failed to import as a Sprite.");
            }

            CreateTowerPrefab(towerSprite);
            CreatePoisonStatusPrefab(poisonStatusSprite);
            AddStatusAnchorToTargetPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TowerPrefabPath);
            var poisonStatusPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PoisonStatusPrefabPath);
            if (towerPrefab == null || poisonStatusPrefab == null ||
                towerPrefab.GetComponent<CombatOccupantView>() == null ||
                poisonStatusPrefab.GetComponent<PoisonStatusView>() == null)
            {
                throw new InvalidOperationException(
                    "The saved Tower or PoisonStatus Prefab is missing its root view component.");
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().SingleOrDefault(value => value.name == "VerticalSliceRoot");
            if (root == null)
            {
                throw new InvalidOperationException("VerticalSliceRoot is missing from " + ScenePath + ".");
            }

            var boardRoot = root.transform.Find("World/CombatBoardRoot");
            var sceneCamera = root.transform.Find("World/SliceCamera")
                .GetComponent<Camera>();
            var binding = root.GetComponent<CombatPresentationBinding>();
            if (boardRoot == null || sceneCamera == null || binding == null)
            {
                throw new InvalidOperationException("The saved combat scene is missing a Gate B integration dependency.");
            }

            var occupantPresenter = GetOrAddComponent<CombatOccupantPresenter>(boardRoot.gameObject);
            ConfigureOccupantPresenter(
                occupantPresenter,
                sceneCamera,
                towerPrefab,
                poisonStatusPrefab);
            ConfigureReference(binding, "occupantPresenter", occupantPresenter);
            EditorUtility.SetDirty(binding);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("TIMEKEY_REMAINING_CARDS_GATE_B_AUTHORING_PASS");
        }

        [MenuItem("Time Key/Author Simplified Chinese Localization")]
        public static void AuthorSimplifiedChineseLocalization()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var font = LoadChineseFont();

            var prefabRoot = PrefabUtility.LoadPrefabContents(TimelineCellPrefabPath);
            try
            {
                var timelineText = prefabRoot.GetComponentInChildren<Text>(true);
                if (timelineText == null)
                {
                    throw new InvalidOperationException("TimelineCell Prefab is missing its Text component.");
                }

                timelineText.font = font;
                EditorUtility.SetDirty(timelineText);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, TimelineCellPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().SingleOrDefault(value => value.name == "VerticalSliceRoot");
            if (root == null)
            {
                throw new InvalidOperationException("VerticalSliceRoot is missing from " + ScenePath + ".");
            }

            SetLocalizedText(root.transform, "SliceCanvas/HUD/Header/Title", CombatChineseText.SceneTitle, font);
            SetLocalizedText(root.transform, "SliceCanvas/HUD/Header/Status", CombatChineseText.SelectCard, font);
            SetLocalizedText(
                root.transform,
                "SliceCanvas/HUD/DetailPanel/TargetStatus",
                CombatChineseText.DefaultTargetStatus,
                font);
            SetLocalizedText(
                root.transform,
                "SliceCanvas/HUD/DetailPanel/Intent",
                CombatChineseText.EnemyIntentDetail,
                font);
            SetLocalizedText(
                root.transform,
                "SliceCanvas/HUD/DetailPanel/Resolve/Label",
                CombatChineseText.ResolveTimeline,
                font);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("TIMEKEY_SIMPLIFIED_CHINESE_AUTHORING_PASS");
        }

        [MenuItem("Time Key/Author Remaining Cards Gate C Clear UI")]
        public static void AuthorRemainingCardsGateCClearUi()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().SingleOrDefault(value => value.name == "VerticalSliceRoot");
            if (root == null)
            {
                throw new InvalidOperationException("VerticalSliceRoot is missing from " + ScenePath + ".");
            }

            var timeline = root.transform.Find("SliceCanvas/HUD/Timeline");
            if (timeline == null)
            {
                throw new InvalidOperationException("The saved combat scene is missing SliceCanvas/HUD/Timeline.");
            }

            var presenter = timeline.GetComponent<TimelinePresenter>();
            if (presenter == null)
            {
                throw new InvalidOperationException("Timeline is missing TimelinePresenter.");
            }

            var clearPreview = GetOrAddComponent<ClearTimelinePreview>(timeline.gameObject);
            ConfigureReference(presenter, "clearTimelinePreview", clearPreview);
            var timelineCells = timeline.GetComponentsInChildren<TimelineCellView>(true);
            for (var index = 0; index < timelineCells.Length; index++)
            {
                timelineCells[index].CaptureCurrentAsDefault();
                EditorUtility.SetDirty(timelineCells[index]);
            }

            EditorUtility.SetDirty(presenter);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("TIMEKEY_REMAINING_CARDS_GATE_C_AUTHORING_PASS");
        }

        private static void EnsureAssetDirectories()
        {
            var directories = new[]
            {
                "Assets/_Project/Prefabs/Battle/UI",
                "Assets/_Project/Prefabs/Battle/Cards",
                "Assets/_Project/Prefabs/Battle/Terrain",
                "Assets/_Project/Prefabs/Battle/Targets",
                "Assets/_Project/Prefabs/Battle/Occupants",
                "Assets/_Project/Prefabs/Battle/Status",
                GeneratedAssetDirectory,
                MaterialDirectory
            };

            for (var index = 0; index < directories.Length; index++)
            {
                Directory.CreateDirectory(Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", directories[index])));
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static Material LoadOrCreateMaterial(string assetPath, string shaderName, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                var shader = Shader.Find(shaderName) ?? Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("No compatible shader exists for " + assetPath + ".");
                }

                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(assetPath) };
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.color = color;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Font LoadChineseFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(ChineseFontAssetPath);
            if (font == null)
            {
                throw new InvalidOperationException("The licensed Simplified Chinese font is missing: " + ChineseFontAssetPath);
            }

            return font;
        }

        private static void SetLocalizedText(
            Transform root,
            string path,
            string value,
            Font font)
        {
            var target = root.Find(path);
            var text = target == null ? null : target.GetComponent<Text>();
            if (text == null)
            {
                throw new InvalidOperationException("Localized Text is missing from scene path: " + path);
            }

            text.text = value;
            text.font = font;
            EditorUtility.SetDirty(text);
        }

        private static Sprite LoadOrCreateSprite(string assetPath, string resourcePath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Source texture is missing: " + resourcePath + ".");
            }

            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(sprite, assetPath);
            return sprite;
        }

        private static void ConfigureSpriteImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Texture importer is missing: " + assetPath + ".");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static GameObject CreateHexBlockPrefab(string path, string name, string resourcePath)
        {
            var source = Resources.Load<GameObject>(resourcePath);
            if (source == null)
            {
                throw new InvalidOperationException("Hex tile model is missing: " + resourcePath + ".");
            }

            var root = new GameObject(name);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            foreach (var filter in visual.GetComponentsInChildren<MeshFilter>(true))
            {
                var collider = filter.GetComponent<MeshCollider>();
                if (collider == null)
                {
                    collider = filter.gameObject.AddComponent<MeshCollider>();
                }

                collider.sharedMesh = filter.sharedMesh;
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static GameObject CreateHexColumnPrefab(GameObject grassBlock, GameObject dirtBlock)
        {
            var root = new GameObject("HexColumn", typeof(HexTileColumn), typeof(BoardTileView));
            var anchor = CreateTransform("OccupantAnchor", root.transform);
            var column = root.GetComponent<HexTileColumn>();
            var serialized = new SerializedObject(column);
            serialized.FindProperty("_grassPrefab").objectReferenceValue = grassBlock;
            serialized.FindProperty("_dirtPrefab").objectReferenceValue = dirtBlock;
            serialized.FindProperty("_layerSpacing").floatValue = VerticalSliceController.HexBlockHeight;
            serialized.FindProperty("_occupantAnchor").objectReferenceValue = anchor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, HexColumnPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(HexColumnPrefabPath);
        }

        private static GameObject CreateTargetViewPrefab(Material material, Sprite artwork)
        {
            var root = new GameObject(
                "TargetView",
                typeof(WorldTargetView),
                typeof(CombatOccupantView));
            var hitProxy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hitProxy.name = "HitProxy";
            hitProxy.transform.SetParent(root.transform, false);
            hitProxy.transform.localScale = new Vector3(0.58f, 0.10f, 0.58f);
            hitProxy.GetComponent<Renderer>().sharedMaterial = material;

            var art = new GameObject(
                "OriginalArt-center_altar",
                typeof(SpriteRenderer),
                typeof(BoxCollider),
                typeof(CameraFacingBillboard));
            art.transform.SetParent(root.transform, false);
            art.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            art.transform.localScale = Vector3.one * 0.62f;
            var renderer = art.GetComponent<SpriteRenderer>();
            renderer.sprite = artwork;
            renderer.color = Color.white;
            renderer.sortingOrder = 100;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            art.GetComponent<BoxCollider>().size = new Vector3(2.25f, 2.25f, 0.16f);

            var statusAnchor = CreateTransform("StatusAnchor", root.transform);
            statusAnchor.localPosition = new Vector3(0.62f, 1.48f, 0f);
            ConfigureReference(root.GetComponent<CombatOccupantView>(), "statusAnchor", statusAnchor);

            PrefabUtility.SaveAsPrefabAsset(root, TargetViewPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(TargetViewPrefabPath);
        }

        private static void AddStatusAnchorToTargetPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(TargetViewPrefabPath);
            try
            {
                var view = GetOrAddComponent<CombatOccupantView>(root);
                var statusAnchor = root.transform.Find("StatusAnchor");
                if (statusAnchor == null)
                {
                    statusAnchor = CreateTransform("StatusAnchor", root.transform);
                }

                statusAnchor.localPosition = new Vector3(0.62f, 1.48f, 0f);
                ConfigureReference(view, "statusAnchor", statusAnchor);
                PrefabUtility.SaveAsPrefabAsset(root, TargetViewPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static CombatOccupantView CreateTowerPrefab(Sprite artwork)
        {
            var root = new GameObject("Tower", typeof(CombatOccupantView));
            var art = new GameObject(
                "OriginalArt-tower",
                typeof(SpriteRenderer),
                typeof(CameraFacingBillboard));
            art.transform.SetParent(root.transform, false);
            art.transform.localPosition = new Vector3(0f, 0.86f, 0f);
            art.transform.localScale = Vector3.one * 0.65f;
            var renderer = art.GetComponent<SpriteRenderer>();
            renderer.sprite = artwork;
            renderer.color = Color.white;
            renderer.sortingOrder = 110;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var statusAnchor = CreateTransform("StatusAnchor", root.transform);
            statusAnchor.localPosition = new Vector3(0.54f, 1.58f, 0f);
            ConfigureReference(root.GetComponent<CombatOccupantView>(), "statusAnchor", statusAnchor);

            PrefabUtility.SaveAsPrefabAsset(root, TowerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(TowerPrefabPath)
                .GetComponent<CombatOccupantView>();
        }

        private static PoisonStatusView CreatePoisonStatusPrefab(Sprite artwork)
        {
            var root = new GameObject(
                "PoisonStatus",
                typeof(PoisonStatusView),
                typeof(CameraFacingBillboard));
            var iconObject = new GameObject("Icon", typeof(SpriteRenderer));
            iconObject.transform.SetParent(root.transform, false);
            iconObject.transform.localScale = Vector3.one * 0.20f;
            var icon = iconObject.GetComponent<SpriteRenderer>();
            icon.sprite = artwork;
            icon.color = Color.white;
            icon.sortingOrder = 150;

            var labelObject = new GameObject("Stacks", typeof(TextMesh));
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0.24f, -0.02f, -0.01f);
            var label = labelObject.GetComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 42;
            label.characterSize = 0.08f;
            label.color = Color.white;
            label.text = "2";
            label.GetComponent<MeshRenderer>().sortingOrder = 151;

            var view = root.GetComponent<PoisonStatusView>();
            ConfigureReference(view, "icon", icon);
            ConfigureReference(view, "stackLabel", label);
            PrefabUtility.SaveAsPrefabAsset(root, PoisonStatusPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PoisonStatusPrefabPath)
                .GetComponent<PoisonStatusView>();
        }

        private static TimelineCellView CreateTimelineCellPrefab()
        {
            var root = new GameObject(
                "TimelineCell",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(Outline),
                typeof(TimelineCellView));
            var button = root.GetComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            var label = CreateText(root.transform, "Label", "00", 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            var view = root.GetComponent<TimelineCellView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, TimelineCellPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(TimelineCellPrefabPath).GetComponent<TimelineCellView>();
        }

        private static CardHandView CreateCardViewPrefab(Sprite previewArtwork)
        {
            var root = new GameObject("CardView", typeof(RectTransform), typeof(CardHandView));
            var view = root.GetComponent<CardHandView>();
            view.Build(new CardViewModel("prefab-preview", previewArtwork, false, true));
            view.ApplyVisualStateImmediate();
            PrefabUtility.SaveAsPrefabAsset(root, CardViewPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(CardViewPrefabPath).GetComponent<CardHandView>();
        }

        private static void ConfigureTimelineCell(TimelineCellView view, int column, int row)
        {
            var serialized = new SerializedObject(view);
            serialized.FindProperty("column").intValue = column;
            serialized.FindProperty("row").intValue = row;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureCardHandHost(
            CardHandHost host,
            RectTransform container,
            CardHandView cardViewPrefab)
        {
            var serialized = new SerializedObject(host);
            serialized.FindProperty("cardContainer").objectReferenceValue = container;
            serialized.FindProperty("cardViewPrefab").objectReferenceValue = cardViewPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureController(
            VerticalSliceController controller,
            Camera sceneCamera,
            BoardOrbitCameraController boardCamera,
            Light keyLight,
            Light fillLight,
            Renderer ground,
            Transform boardRoot,
            Transform targetAnchor,
            EventSystem eventSystem,
            Canvas canvas,
            RectTransform hudRoot,
            RectTransform timelineRoot,
            Text statusText,
            Text targetText,
            Button resolveButton,
            CardHandHost cardHandHost,
            BoardRangePreview boardRangePreview,
            TimelinePlacementPreview timelinePreview,
            CombatPresentationBinding presentationBinding,
            GameObject hexColumnPrefab,
            GameObject grassBlockPrefab,
            GameObject dirtBlockPrefab,
            GameObject targetViewPrefab)
        {
            var serialized = new SerializedObject(controller);
            SetReference(serialized, "sceneCamera", sceneCamera);
            SetReference(serialized, "boardCamera", boardCamera);
            SetReference(serialized, "keyLight", keyLight);
            SetReference(serialized, "fillLight", fillLight);
            SetReference(serialized, "battlefieldGround", ground);
            SetReference(serialized, "boardRoot", boardRoot);
            SetReference(serialized, "dynamicRoot", boardRoot);
            SetReference(serialized, "targetAnchor", targetAnchor);
            SetReference(serialized, "sceneEventSystem", eventSystem);
            SetReference(serialized, "sceneCanvas", canvas);
            SetReference(serialized, "hudRoot", hudRoot);
            SetReference(serialized, "timelineRoot", timelineRoot);
            SetReference(serialized, "statusText", statusText);
            SetReference(serialized, "targetText", targetText);
            SetReference(serialized, "resolveButton", resolveButton);
            SetReference(serialized, "cardHandHost", cardHandHost);
            SetReference(serialized, "boardRangePreview", boardRangePreview);
            SetReference(serialized, "timelinePlacementPreview", timelinePreview);
            SetReference(serialized, "presentationBinding", presentationBinding);
            SetReference(serialized, "hexColumnPrefab", hexColumnPrefab);
            SetReference(serialized, "grassBlockPrefab", grassBlockPrefab);
            SetReference(serialized, "dirtBlockPrefab", dirtBlockPrefab);
            SetReference(serialized, "targetViewPrefab", targetViewPrefab);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void ConfigureCompositionRoot(
            CombatCompositionRoot compositionRoot,
            VerticalSliceController controller,
            UnityCombatTraceSink traceSink)
        {
            var serialized = new SerializedObject(compositionRoot);
            SetReference(serialized, "controller", controller);
            SetReference(
                serialized,
                "presentationBinding",
                controller.GetComponent<CombatPresentationBinding>());
            SetReference(serialized, "traceSink", traceSink);
            var fixtures = serialized.FindProperty("cardFixtures");
            var fixturePaths = AssetDatabase.FindAssets(
                    "t:TextAsset",
                    new[] { "Assets/_Project/Content/Cards" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            fixtures.arraySize = fixturePaths.Length;
            for (var index = 0; index < fixturePaths.Length; index++)
            {
                fixtures.GetArrayElementAtIndex(index).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<TextAsset>(fixturePaths[index]);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(compositionRoot);
        }

        private static void ConfigurePresentationBinding(
            CombatPresentationBinding binding,
            CardHandPresenter cardHandPresenter,
            BoardRangePresenter boardRangePresenter,
            TimelinePresenter timelinePresenter,
            CombatHudPresenter hudPresenter,
            CombatOccupantPresenter occupantPresenter)
        {
            var serialized = new SerializedObject(binding);
            SetReference(serialized, "cardHandPresenter", cardHandPresenter);
            SetReference(serialized, "boardRangePresenter", boardRangePresenter);
            SetReference(serialized, "timelinePresenter", timelinePresenter);
            SetReference(serialized, "hudPresenter", hudPresenter);
            SetReference(serialized, "occupantPresenter", occupantPresenter);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureOccupantPresenter(
            CombatOccupantPresenter presenter,
            Camera sceneCamera,
            GameObject towerPrefab,
            GameObject poisonStatusPrefab)
        {
            var serialized = new SerializedObject(presenter);
            SetReference(serialized, "sceneCamera", sceneCamera);
            SetReference(serialized, "poisonStatusPrefab", poisonStatusPrefab);
            var creationViews = serialized.FindProperty("creationViews");
            creationViews.arraySize = 1;
            var tower = creationViews.GetArrayElementAtIndex(0);
            tower.FindPropertyRelative("creationId").stringValue = "tower";
            tower.FindPropertyRelative("prefab").objectReferenceValue = towerPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTimelinePresenter(
            TimelinePresenter presenter,
            TimelinePlacementPreview preview,
            ClearTimelinePreview clearPreview,
            IReadOnlyList<TimelineCellView> timelineCells)
        {
            var serialized = new SerializedObject(presenter);
            SetReference(serialized, "timelinePreview", preview);
            SetReference(serialized, "clearTimelinePreview", clearPreview);
            var cells = serialized.FindProperty("timelineCells");
            cells.arraySize = timelineCells.Count;
            for (var index = 0; index < timelineCells.Count; index++)
            {
                cells.GetArrayElementAtIndex(index).objectReferenceValue = timelineCells[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureHudPresenter(
            CombatHudPresenter presenter,
            Text statusText,
            Text targetText,
            Button resolveButton)
        {
            var serialized = new SerializedObject(presenter);
            SetReference(serialized, "statusText", statusText);
            SetReference(serialized, "targetText", targetText);
            SetReference(serialized, "resolveButton", resolveButton);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureReference(
            UnityEngine.Object target,
            string fieldName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SetReference(serialized, fieldName, value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            return target.GetComponent<T>() ?? target.AddComponent<T>();
        }

        private static void SetReference(SerializedObject serialized, string name, UnityEngine.Object value)
        {
            var property = serialized.FindProperty(name);
            if (property == null)
            {
                throw new InvalidOperationException("Serialized controller field is missing: " + name + ".");
            }

            property.objectReferenceValue = value;
        }

        private static Transform CreateTransform(string name, Transform parent)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static Light CreateDirectionalLight(
            string name,
            Transform parent,
            Color color,
            float intensity,
            Quaternion rotation)
        {
            var child = new GameObject(name, typeof(Light));
            child.transform.SetParent(parent, false);
            child.transform.rotation = rotation;
            var light = child.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            return light;
        }

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            panel.GetComponent<Image>().color = color;
            return rect;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = LoadChineseFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.92f, 0.94f, 0.90f, 1f);
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, offsetMin, offsetMax);
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Color color)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.targetGraphic.color = color;
            CreateText(buttonObject.transform, "Label", label, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
