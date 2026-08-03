using System;
using System.IO;
using TimeKey.Composition.SceneFlow;
using TimeKey.Editor.OverworldMovement;
using TimeKey.Presentation;
using TimeKey.Presentation.CombatShell;
using TimeKey.Presentation.GameOver;
using TimeKey.Presentation.OutOfBattleShell;
using TimeKey.Presentation.SceneFlowFinale;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeKey.Editor
{
    public static class CombatShellGateDAutomation
    {
        public const string OutOfBattlePrefabPath =
            "Assets/_Project/Prefabs/Shell/OutOfBattleShell.prefab";
        public const string GameOverPrefabPath =
            "Assets/_Project/Prefabs/Shell/GameOver.prefab";

        private const string OutOfBattleScenePath =
            "Assets/_Project/Scenes/Shell/OutOfBattleShell.unity";
        private const string GameOverScenePath =
            "Assets/_Project/Scenes/Shell/GameOver.unity";
        private const string CombatScenePath =
            "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity";
        private const string TopHudPrefabPath =
            "Assets/_Project/Prefabs/Battle/CombatShell/CombatTopHUD.prefab";
        private const string FontPath = "Assets/_Project/Resources/Fonts/Silver.ttf";
        private const string OceanBackgroundPath =
            "Assets/_Project/Resources/Art/Battle/Background/out-bg_sea.png";
        private const string TitleBackgroundPath =
            "Assets/_Project/Resources/Art/Battle/Background/titleBG.png";
        private const string TileDirectory =
            "Assets/_Project/Resources/Art/Shell/OutOfBattleShell";

        private static readonly string[] TileNames =
        {
            "out-block_desert.png",
            "out-block_forest.png",
            "out-block_grass_dark.png",
            "out-block_grass.png",
            "out-block_mystery.png",
            "out-block_snow_mount.png",
            "out-block_snow.png",
            "out-block_stone.png"
        };

        [MenuItem("TimeKey/Migration/Author Combat Shell Gate D")]
        public static void AuthorGateD()
        {
            var font = LoadRequired<Font>(FontPath);
            CopyOutOfBattleTiles();
            var outPrefab = AuthorOutOfBattlePrefab(font);
            var gameOverPrefab = AuthorGameOverPrefab(font);
            AuthorContentScene(
                OutOfBattleScenePath,
                "OutOfBattleShellEntry",
                TimeKey.Application.SceneFlow.SceneId.OutOfBattleShell,
                outPrefab,
                "CombatRoom");
            AuthorContentScene(
                GameOverScenePath,
                "GameOverEntry",
                TimeKey.Application.SceneFlow.SceneId.GameOver,
                gameOverPrefab,
                "ReturnButton");
            IntegrateCombatNavigation();
            OverworldMovementThemeAuthoring.AuthorFormalAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("TIMEKEY_COMBAT_SHELL_GATE_D_AUTHORING_PASS");
        }

        private static GameObject AuthorOutOfBattlePrefab(Font font)
        {
            var root = CreateContentRoot(
                "OutOfBattleShell",
                out var canvas,
                typeof(OutOfBattleShellPresenter),
                typeof(OutOfBattleShellSceneNavigation),
                typeof(LayeredSceneRevealPresenter));
            try
            {
                var camera = CreateCamera(root.transform, "OutOfBattleCamera");
                canvas.worldCamera = camera;
                var uiRoot = canvas.transform;
                var backgroundLayer = CreateRevealLayer(uiRoot, "BackgroundRevealLayer");
                var mapBackground = CreateBackground(
                    backgroundLayer.transform,
                    "MapBackground",
                    OceanBackgroundPath,
                    Color.white);
                mapBackground.gameObject.AddComponent<OutOfBattleOceanBackground>();
                CreatePanel(
                    backgroundLayer.transform,
                    "MapTint",
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero,
                    new Color(0.01f, 0.06f, 0.07f, 0.28f),
                    false);

                CreateMapDecoration(backgroundLayer.transform);
                var contextLayer = CreateRevealLayer(uiRoot, "ContextRevealLayer");
                var topHudPrefab = LoadRequired<GameObject>(TopHudPrefabPath);
                var topHud = (GameObject)PrefabUtility.InstantiatePrefab(topHudPrefab);
                topHud.name = "SharedTopHUD";
                topHud.transform.SetParent(contextLayer.transform, false);
                Stretch(topHud.GetComponent<RectTransform>());
                var enemyPanel = topHud.transform.Find("EnemyHealthPanel");
                if (enemyPanel != null)
                {
                    enemyPanel.gameObject.SetActive(false);
                }

                var runPanel = CreatePanel(
                    contextLayer.transform,
                    "RunContext",
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(-390f, -184f),
                    new Vector2(390f, -114f),
                    new Color(0.025f, 0.10f, 0.11f, 0.94f),
                    false);
                AddOutline(runPanel.gameObject, new Color(0.40f, 0.78f, 0.68f, 0.86f));
                var seed = CreateText(
                    runPanel,
                    "SeedLabel",
                    "种子 731",
                    font,
                    18,
                    TextAnchor.MiddleCenter,
                    new Vector2(14f, 8f),
                    new Vector2(-570f, -8f));
                var era = CreateText(
                    runPanel,
                    "EraLabel",
                    "时代 1",
                    font,
                    18,
                    TextAnchor.MiddleCenter,
                    new Vector2(205f, 8f),
                    new Vector2(-382f, -8f));
                var phase = CreateText(
                    runPanel,
                    "PhaseLabel",
                    "阶段 1",
                    font,
                    18,
                    TextAnchor.MiddleCenter,
                    new Vector2(394f, 8f),
                    new Vector2(-193f, -8f));
                var timecoins = CreateText(
                    runPanel,
                    "TimecoinsLabel",
                    "时间币 0",
                    font,
                    18,
                    TextAnchor.MiddleCenter,
                    new Vector2(583f, 8f),
                    new Vector2(-14f, -8f));

                var roomLayer = CreateRevealLayer(uiRoot, "RoomRevealLayer");
                var room = CreateRoom(roomLayer.transform, font);
                var actions = CreatePanel(
                    roomLayer.transform,
                    "RoomActions",
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(-250f, 34f),
                    new Vector2(250f, 102f),
                    new Color(0.02f, 0.055f, 0.06f, 0.92f),
                    false);
                var cancel = CreateButton(
                    actions,
                    "CancelButton",
                    "取消",
                    font,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(12f, 10f),
                    new Vector2(-260f, -10f),
                    new Color(0.22f, 0.24f, 0.24f, 1f));
                var confirm = CreateButton(
                    actions,
                    "ConfirmButton",
                    "进入战斗",
                    font,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(260f, 10f),
                    new Vector2(-12f, -10f),
                    new Color(0.12f, 0.42f, 0.38f, 1f));

                var presenter = root.GetComponent<OutOfBattleShellPresenter>();
                var serialized = new SerializedObject(presenter);
                SetReference(serialized, "seedLabel", seed);
                SetReference(serialized, "eraLabel", era);
                SetReference(serialized, "phaseLabel", phase);
                SetReference(serialized, "timecoinsLabel", timecoins);
                SetReference(serialized, "roomView", room.View);
                SetReference(serialized, "confirmButton", confirm);
                SetReference(serialized, "cancelButton", cancel);
                SetReference(serialized, "silverFont", font);
                serialized.FindProperty("roomId").stringValue = "combat-room-01";
                serialized.FindProperty("roomTitle").stringValue = "时隙战场";
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var navigation = root.GetComponent<OutOfBattleShellSceneNavigation>();
                serialized = new SerializedObject(navigation);
                SetReference(serialized, "presenter", presenter);
                SetReference(
                    serialized,
                    "sharedTopHud",
                    topHud.GetComponent<CombatTopHudPresenter>());
                serialized.FindProperty("battleTag").stringValue = "combat-vertical-slice";
                serialized.ApplyModifiedPropertiesWithoutUndo();

                ConfigureLayeredReveal(
                    root.GetComponent<LayeredSceneRevealPresenter>(),
                    0.24f,
                    0.09f,
                    backgroundLayer,
                    contextLayer,
                    roomLayer);

                PrefabUtility.SaveAsPrefabAsset(root, OutOfBattlePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            return LoadRequired<GameObject>(OutOfBattlePrefabPath);
        }

        private static GameObject AuthorGameOverPrefab(Font font)
        {
            var root = CreateContentRoot(
                "GameOver",
                out var canvas,
                typeof(GameOverPresenter),
                typeof(GameOverSceneNavigation),
                typeof(LayeredSceneRevealPresenter));
            try
            {
                var camera = CreateCamera(root.transform, "GameOverCamera");
                canvas.worldCamera = camera;
                var uiRoot = canvas.transform;
                var backgroundLayer = CreateRevealLayer(uiRoot, "BackgroundRevealLayer");
                CreateBackground(
                    backgroundLayer.transform,
                    "GameOverBackground",
                    TitleBackgroundPath,
                    new Color(0.48f, 0.48f, 0.48f, 1f));
                CreatePanel(
                    backgroundLayer.transform,
                    "DarkVeil",
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero,
                    new Color(0.02f, 0.01f, 0.015f, 0.70f),
                    false);
                var panelLayer = CreateRevealLayer(uiRoot, "PanelRevealLayer");
                var panel = CreatePanel(
                    panelLayer.transform,
                    "GameOverPanel",
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(-340f, -210f),
                    new Vector2(340f, 210f),
                    new Color(0.08f, 0.035f, 0.04f, 0.96f),
                    false);
                AddOutline(panel.gameObject, new Color(0.74f, 0.28f, 0.25f, 0.94f));
                var title = CreateText(
                    panel,
                    "Title",
                    "时序断裂",
                    font,
                    58,
                    TextAnchor.MiddleCenter,
                    new Vector2(24f, 244f),
                    new Vector2(-24f, -54f),
                    new Color(1f, 0.76f, 0.70f, 1f));
                var detail = CreateText(
                    panel,
                    "Detail",
                    "本次战斗失败",
                    font,
                    24,
                    TextAnchor.MiddleCenter,
                    new Vector2(36f, 124f),
                    new Vector2(-36f, -176f),
                    new Color(0.92f, 0.86f, 0.82f, 1f));
                var returnButton = CreateButton(
                    panel,
                    "ReturnButton",
                    "返回主界面",
                    font,
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(-170f, 34f),
                    new Vector2(170f, 104f),
                    new Color(0.32f, 0.11f, 0.11f, 1f));

                var presenter = root.GetComponent<GameOverPresenter>();
                var serialized = new SerializedObject(presenter);
                SetReference(serialized, "rootCanvasGroup", root.GetComponent<CanvasGroup>());
                SetReference(serialized, "title", title);
                SetReference(serialized, "detail", detail);
                SetReference(serialized, "returnButton", returnButton);
                SetReference(
                    serialized,
                    "returnButtonLabel",
                    returnButton.GetComponentInChildren<Text>(true));
                SetReference(serialized, "silverFont", font);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var navigation = root.GetComponent<GameOverSceneNavigation>();
                serialized = new SerializedObject(navigation);
                SetReference(serialized, "presenter", presenter);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                ConfigureLayeredReveal(
                    root.GetComponent<LayeredSceneRevealPresenter>(),
                    0.28f,
                    0.12f,
                    backgroundLayer,
                    panelLayer);
                PrefabUtility.SaveAsPrefabAsset(root, GameOverPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            return LoadRequired<GameObject>(GameOverPrefabPath);
        }

        private static void AuthorContentScene(
            string scenePath,
            string entryName,
            TimeKey.Application.SceneFlow.SceneId sceneId,
            GameObject contentPrefab,
            string defaultFocusId)
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var entryRoot = new GameObject(entryName, typeof(SceneContentEntry));
            SceneManager.MoveGameObjectToScene(entryRoot, scene);
            var content = (GameObject)PrefabUtility.InstantiatePrefab(contentPrefab, scene);
            content.name = contentPrefab.name;
            content.transform.SetParent(entryRoot.transform, false);
            var camera = content.GetComponentInChildren<Camera>(true);
            var group = content.GetComponent<CanvasGroup>();
            content.SetActive(false);

            var entry = entryRoot.GetComponent<SceneContentEntry>();
            var serialized = new SerializedObject(entry);
            serialized.FindProperty("sceneId").enumValueIndex = (int)sceneId;
            SetReference(serialized, "contentRoot", content);
            SetReference(serialized, "contentCamera", camera);
            SetReference(serialized, "interactionGroup", group);
            serialized.FindProperty("defaultFocusId").stringValue = defaultFocusId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void IntegrateCombatNavigation()
        {
            var scene = EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Single);
            var controller = FindInScene<VerticalSliceController>(scene);
            if (controller == null)
            {
                throw new InvalidOperationException("Combat scene is missing VerticalSliceController.");
            }

            var navigation = controller.GetComponent<CombatSceneNavigation>();
            if (navigation == null)
            {
                navigation = controller.gameObject.AddComponent<CombatSceneNavigation>();
            }

            var serialized = new SerializedObject(navigation);
            SetReference(serialized, "controller", controller);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var entrance = controller.GetComponent<CombatShellEntrancePresenter>();
            if (entrance == null)
            {
                throw new InvalidOperationException(
                    "Combat scene is missing CombatShellEntrancePresenter.");
            }

            var stagedGroups = new[]
            {
                GetOrAddCanvasGroup(FindDescendant(controller.transform, "Status")),
                GetOrAddCanvasGroup(FindDescendant(controller.transform, "Timeline")),
                GetOrAddCanvasGroup(FindDescendant(controller.transform, "DetailPanel")),
                GetOrAddCanvasGroup(FindDescendant(controller.transform, "EffectFrameHost")),
                GetOrAddCanvasGroup(FindDescendant(controller.transform, "CardHandHost"))
            };
            serialized = new SerializedObject(entrance);
            var groups = serialized.FindProperty("stagedUiGroups");
            groups.arraySize = stagedGroups.Length;
            for (var index = 0; index < stagedGroups.Length; index++)
            {
                groups.GetArrayElementAtIndex(index).objectReferenceValue = stagedGroups[index];
            }

            serialized.FindProperty("layerInterval").floatValue = 0.10f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static CanvasGroup CreateRevealLayer(Transform parent, string name)
        {
            var layer = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            layer.transform.SetParent(parent, false);
            Stretch(layer.GetComponent<RectTransform>());
            return layer.GetComponent<CanvasGroup>();
        }

        private static void ConfigureLayeredReveal(
            LayeredSceneRevealPresenter presenter,
            float duration,
            float interval,
            params CanvasGroup[] layers)
        {
            var serialized = new SerializedObject(presenter);
            var property = serialized.FindProperty("layers");
            property.arraySize = layers.Length;
            for (var index = 0; index < layers.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = layers[index];
            }

            serialized.FindProperty("layerDuration").floatValue = duration;
            serialized.FindProperty("layerInterval").floatValue = interval;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            throw new InvalidOperationException("Combat scene is missing " + name + ".");
        }

        private static CanvasGroup GetOrAddCanvasGroup(Transform target)
        {
            var group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.gameObject.AddComponent<CanvasGroup>();
        }

        private static RoomReferences CreateRoom(Transform parent, Font font)
        {
            var room = new GameObject(
                "CombatRoom",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(Outline),
                typeof(OutOfBattleRoomView));
            room.transform.SetParent(parent, false);
            var rect = room.GetComponent<RectTransform>();
            SetRect(
                rect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-170f, -120f),
                new Vector2(170f, 160f));
            var image = room.GetComponent<Image>();
            image.sprite = LoadRequired<Sprite>(TileDirectory + "/out-block_grass.png");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.035f, 0.05f, 0.06f, 0.92f);
            var outline = room.GetComponent<Outline>();
            outline.effectColor = new Color(0.38f, 0.80f, 0.69f, 0.92f);
            outline.effectDistance = new Vector2(3f, -3f);
            var title = CreateText(
                rect,
                "RoomTitle",
                "时隙战场",
                font,
                30,
                TextAnchor.MiddleCenter,
                new Vector2(18f, 78f),
                new Vector2(-18f, -38f),
                Color.white);
            var status = CreateText(
                rect,
                "RoomStatus",
                "可进入",
                font,
                20,
                TextAnchor.MiddleCenter,
                new Vector2(18f, 22f),
                new Vector2(-18f, -96f),
                new Color(0.84f, 0.94f, 0.86f, 1f));
            var view = room.GetComponent<OutOfBattleRoomView>();
            var serialized = new SerializedObject(view);
            SetReference(serialized, "roomButton", room.GetComponent<Button>());
            SetReference(serialized, "background", image);
            SetReference(serialized, "titleLabel", title);
            SetReference(serialized, "statusLabel", status);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return new RoomReferences(view);
        }

        private static void CreateMapDecoration(Transform parent)
        {
            var map = new GameObject("SavedMapDecoration", typeof(RectTransform));
            map.transform.SetParent(parent, false);
            var rect = map.GetComponent<RectTransform>();
            SetRect(
                rect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-610f, -330f),
                new Vector2(610f, 330f));
            var positions = new[]
            {
                new Vector2(-360f, 80f), new Vector2(-240f, 150f),
                new Vector2(-120f, 80f), new Vector2(0f, 150f),
                new Vector2(120f, 80f), new Vector2(240f, 150f),
                new Vector2(360f, 80f), new Vector2(-300f, -100f),
                new Vector2(-180f, -170f), new Vector2(180f, -170f),
                new Vector2(300f, -100f)
            };
            for (var index = 0; index < positions.Length; index++)
            {
                var tile = new GameObject(
                    "MapTile" + index.ToString("00"),
                    typeof(RectTransform),
                    typeof(Image));
                tile.transform.SetParent(rect, false);
                var tileRect = tile.GetComponent<RectTransform>();
                tileRect.anchorMin = tileRect.anchorMax = new Vector2(0.5f, 0.5f);
                tileRect.sizeDelta = new Vector2(150f, 132f);
                tileRect.anchoredPosition = positions[index];
                var image = tile.GetComponent<Image>();
                image.sprite = LoadRequired<Sprite>(
                    TileDirectory + "/" + TileNames[index % TileNames.Length]);
                image.preserveAspect = true;
                image.color = new Color(0.72f, 0.82f, 0.78f, 0.62f);
                image.raycastTarget = false;
            }
        }

        private static GameObject CreateContentRoot(
            string name,
            out Canvas canvas,
            params Type[] extraTypes)
        {
            var types = new Type[1 + extraTypes.Length];
            types[0] = typeof(CanvasGroup);
            Array.Copy(extraTypes, 0, types, 1, extraTypes.Length);
            var root = new GameObject(name, types);
            var canvasRoot = new GameObject(
                "GateDCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(root.transform, false);
            canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return root;
        }

        private static Camera CreateCamera(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(Camera));
            root.transform.SetParent(parent, false);
            var camera = root.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.01f, 0.02f, 0.025f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.depth = 0f;
            return camera;
        }

        private static RawImage CreateBackground(
            Transform parent,
            string name,
            string texturePath,
            Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            image.transform.SetParent(parent, false);
            Stretch(image.GetComponent<RectTransform>());
            var graphic = image.GetComponent<RawImage>();
            graphic.texture = LoadRequired<Texture2D>(texturePath);
            graphic.color = color;
            graphic.raycastTarget = false;
            return graphic;
        }

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color,
            bool raycastTarget)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            var image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return rect;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            Font font,
            int fontSize,
            TextAnchor alignment,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color? color = null)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Text));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            SetRect(rect, Vector2.zero, Vector2.one, offsetMin, offsetMax);
            var text = root.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color ?? new Color(0.94f, 0.93f, 0.86f, 1f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Math.Max(12, fontSize - 8);
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Font font,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color)
        {
            var root = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(Outline));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            root.GetComponent<Image>().color = color;
            AddOutline(root, new Color(0.72f, 0.62f, 0.40f, 0.82f));
            CreateText(
                root.transform,
                "Label",
                label,
                font,
                22,
                TextAnchor.MiddleCenter,
                new Vector2(8f, 5f),
                new Vector2(-8f, -5f));
            return root.GetComponent<Button>();
        }

        private static void AddOutline(GameObject target, Color color)
        {
            var outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = new Vector2(2f, -2f);
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
            SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void SetReference(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Missing serialized property: " + propertyName);
            }

            property.objectReferenceValue = value;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static void CopyOutOfBattleTiles()
        {
            EnsureFolder(TileDirectory);
            var repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                throw new InvalidOperationException("TIMEKEY_REPOSITORY_ROOT is required.");
            }

            foreach (var tileName in TileNames)
            {
                var source = Path.Combine(repositoryRoot, "image", "outscene_block", tileName);
                var destination = Path.GetFullPath(
                    Path.Combine(UnityEngine.Application.dataPath, "_Project", "Resources", "Art",
                        "Shell", "OutOfBattleShell", tileName));
                File.Copy(source, destination, true);
                var assetPath = TileDirectory + "/" + tileName;
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    throw new InvalidOperationException("Tile texture import failed: " + assetPath);
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureFolder(string path)
        {
            var current = "Assets";
            var parts = path.Split('/');
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new FileNotFoundException("Required asset is missing.", path);
            }

            return asset;
        }

        private sealed class RoomReferences
        {
            public RoomReferences(OutOfBattleRoomView view)
            {
                View = view;
            }

            public OutOfBattleRoomView View { get; }
        }
    }
}
