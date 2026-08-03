using System;
using System.IO;
using TimeKey.Composition.SceneFlow;
using TimeKey.Editor.ThemeMigration;
using TimeKey.Presentation.MainMenu;
using TimeKey.Presentation.OutOfBattleShell;
using TimeKey.Presentation.OverworldMovement;
using TimeKey.Presentation.Theming;
using TimeKey.Presentation.Actions;
using TimeKey.Presentation.Tooltips;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeKey.Editor.OverworldMovement
{
    public static class OverworldMovementThemeAuthoring
    {
        public const string OutOfBattlePrefabPath =
            "Assets/_Project/Prefabs/Shell/OutOfBattleShell.prefab";
        public const string NodePrefabPath =
            "Assets/_Project/Prefabs/Shell/OverworldNode.prefab";
        public const string ThemePath = UiThemeValidator.DefaultThemePath;
        private const string FontPath = UiThemeValidator.SilverFontPath;
        private const string TilePath =
            "Assets/_Project/Resources/Art/Shell/OutOfBattleShell/out-block_grass.png";
        private const string PlayerMarkerPath =
            "Assets/_Project/Resources/Art/Shell/MainMenu/point.png";
        private const string OutOfBattleScenePath =
            "Assets/_Project/Scenes/Shell/OutOfBattleShell.unity";

        [MenuItem("TimeKey/Migration/Author Overworld Movement and UI Theme")]
        public static void AuthorFormalAssetsMenu()
        {
            AuthorFormalAssets();
        }

        public static void AuthorFormalAssets()
        {
            var font = LoadRequired<Font>(FontPath);
            var tile = LoadRequired<Sprite>(TilePath);
            var marker = LoadSpriteOrImport(PlayerMarkerPath);
            var theme = EnsureDefaultThemeAsset(font, tile);

            EnsureFolder("Assets/_Project/Prefabs/Shell");
            var nodePrefab = AuthorNodePrefab(theme, font, tile);
            AuthorOutOfBattlePrefab(theme, font, marker, nodePrefab);
            AuthorConsumerPrefab(
                "Assets/_Project/Prefabs/Shell/MainMenu.prefab",
                UiStyleId.SecondaryButton,
                typeof(MainMenuButtonStateVisual));
            AuthorConsumerPrefab(
                "Assets/_Project/Prefabs/Battle/UI/CardEffectFrame.prefab",
                UiStyleId.CardEffectFrame,
                typeof(CardEffectFrame));
            AuthorConsumerPrefab(
                "Assets/_Project/Prefabs/Battle/UI/TimelineActionFrame.prefab",
                UiStyleId.PlayerActionFrame,
                typeof(TimelineActionFrame));
            RefreshFormalScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("TIMEKEY_OVERWORLD_MOVEMENT_THEME_AUTHORING_PASS");
        }

        private static TimeKeyUiTheme EnsureDefaultThemeAsset(Font font, Sprite frameSprite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TimeKeyUiTheme>(ThemePath);
            if (existing != null)
            {
                return existing;
            }

            AssetDatabase.DeleteAsset(ThemePath);
            var theme = ScriptableObject.CreateInstance<TimeKeyUiTheme>();
            theme.name = "TimeKeyDefaultUiTheme";
            var ids = (UiStyleId[])Enum.GetValues(typeof(UiStyleId));
            for (var index = 0; index < ids.Length; index++)
            {
                var style = theme.Styles[index];
                style.id = ids[index];
                ConfigureStyle(style, ids[index], font, frameSprite);
            }

            AssetDatabase.CreateAsset(theme, ThemePath);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
        }

        private static void ConfigureStyle(
            TimeKeyUiTheme.UiStyleDefinition style,
            UiStyleId id,
            Font font,
            Sprite frameSprite)
        {
            var fill = new Color(0.035f, 0.05f, 0.06f, 0.92f);
            var border = new Color(0.40f, 0.78f, 0.68f, 0.86f);
            var text = new Color(0.94f, 0.93f, 0.86f, 1f);
            var fontSize = 24;
            switch (id)
            {
                case UiStyleId.PrimaryButton:
                case UiStyleId.OverworldNode:
                    fill = new Color(0.12f, 0.40f, 0.36f, 0.98f);
                    border = new Color(0.64f, 0.92f, 0.88f, 1f);
                    break;
                case UiStyleId.DangerButton:
                case UiStyleId.EnemyIntentFrame:
                    fill = new Color(0.12f, 0.055f, 0.06f, 0.98f);
                    border = new Color(0.92f, 0.31f, 0.28f, 1f);
                    text = new Color(0.98f, 0.95f, 0.84f, 1f);
                    break;
                case UiStyleId.TopHud:
                case UiStyleId.ConfirmationDialog:
                    fill = new Color(0.025f, 0.10f, 0.11f, 0.94f);
                    break;
                case UiStyleId.CardEffectFrame:
                case UiStyleId.PlayerActionFrame:
                    fill = new Color(0.055f, 0.075f, 0.078f, 0.98f);
                    border = new Color(0.18f, 0.76f, 0.72f, 1f);
                    fontSize = 22;
                    break;
                case UiStyleId.TimelineCell:
                    fill = new Color(0.04f, 0.10f, 0.11f, 0.92f);
                    border = new Color(0.18f, 0.76f, 0.72f, 1f);
                    fontSize = 20;
                    break;
                case UiStyleId.Tooltip:
                    fill = new Color(0.05f, 0.06f, 0.08f, 0.92f);
                    border = new Color(0.95f, 0.82f, 0.28f, 1f);
                    fontSize = 18;
                    break;
            }

            style.text.font = font;
            style.text.fontSize = fontSize;
            style.text.normalColor = text;
            style.text.disabledColor = new Color(0.55f, 0.58f, 0.58f, 1f);
            style.frame.backgroundSprite = frameSprite;
            style.frame.imageType = Image.Type.Sliced;
            style.frame.fillColor = fill;
            style.frame.borderColor = border;
            style.frame.borderWidth = Vector4.one;
            style.frame.padding = new Vector4(18f, 12f, 18f, 12f);
            style.frame.shadowColor = new Color(0f, 0f, 0f, 0.45f);
            style.frame.shadowOffset = new Vector2(2f, -2f);
            style.frame.shadowSize = 2f;
            style.button.text.font = font;
            style.button.text.fontSize = fontSize;
            style.button.text.normalColor = text;
            style.button.frame.backgroundSprite = frameSprite;
            style.button.frame.imageType = Image.Type.Sliced;
            style.button.frame.fillColor = fill;
            style.button.normalColor = fill;
            style.button.highlightedColor = border;
            style.button.pressedColor = Color.Lerp(fill, Color.black, 0.25f);
            style.button.selectedColor = border;
            style.button.disabledColor = new Color(0.12f, 0.13f, 0.14f, 0.72f);
        }

        private static GameObject AuthorNodePrefab(
            TimeKeyUiTheme theme,
            Font font,
            Sprite tile)
        {
            var root = new GameObject(
                "OverworldNode",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(OverworldNodeView),
                typeof(UiThemeBinder));
            try
            {
                var image = root.GetComponent<Image>();
                image.sprite = tile;
                image.type = Image.Type.Sliced;
                image.raycastTarget = true;
                var button = root.GetComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(root.transform, false);
                var label = labelObject.GetComponent<Text>();
                label.font = font;
                label.fontSize = 18;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.raycastTarget = false;
                Stretch(label.rectTransform);

                var view = root.GetComponent<OverworldNodeView>();
                var serialized = new SerializedObject(view);
                serialized.FindProperty("nodeId").stringValue = "start";
                serialized.FindProperty("q").intValue = 0;
                serialized.FindProperty("r").intValue = 0;
                serialized.FindProperty("nodeType").enumValueIndex =
                    (int)TimeKey.Domain.OverworldMovement.MapNodeType.Start;
                SetReference(serialized, "button", button);
                SetReference(serialized, "background", image);
                SetReference(serialized, "label", label);
                SetReference(serialized, "themeBinder", root.GetComponent<UiThemeBinder>());
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var binder = root.GetComponent<UiThemeBinder>();
                serialized = new SerializedObject(binder);
                serialized.FindProperty("styleId").enumValueIndex = (int)UiStyleId.OverworldNode;
                SetReference(serialized, "background", image);
                SetReference(serialized, "text", label);
                SetReference(serialized, "selectable", button);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, NodePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            return LoadRequired<GameObject>(NodePrefabPath);
        }

        private static void AuthorOutOfBattlePrefab(
            TimeKeyUiTheme theme,
            Font font,
            Sprite markerSprite,
            GameObject nodePrefab)
        {
            var root = PrefabUtility.LoadPrefabContents(OutOfBattlePrefabPath);
            try
            {
                var scope = GetOrAdd<UiThemeScope>(root);
                scope.SetTheme(theme);

                var canvas = FindRequired(root.transform, "GateDCanvas");
                var roomLayer = FindRequired(canvas, "RoomRevealLayer");
                var oldMap = root.transform.Find("GateDCanvas/BackgroundRevealLayer/SavedMapDecoration");
                if (oldMap != null)
                {
                    oldMap.gameObject.SetActive(false);
                }

                var viewport = FindOrCreateRect(roomLayer, "MapViewport");
                SetRect(viewport, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-650f, -250f), new Vector2(650f, 250f));
                var viewportImage = GetOrAdd<Image>(viewport.gameObject);
                viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
                viewportImage.raycastTarget = true;
                var input = GetOrAdd<OverworldMapInputController>(viewport.gameObject);
                var status = FindOrCreateRect(viewport, "MovementStatus");
                status.anchorMin = new Vector2(0.5f, 0f);
                status.anchorMax = new Vector2(0.5f, 0f);
                status.pivot = new Vector2(0.5f, 0f);
                status.anchoredPosition = new Vector2(0f, 12f);
                status.sizeDelta = new Vector2(640f, 44f);
                var statusText = GetOrAdd<Text>(status.gameObject);
                statusText.font = font;
                statusText.fontSize = 20;
                statusText.alignment = TextAnchor.MiddleCenter;
                statusText.color = new Color(0.94f, 0.93f, 0.86f, 1f);
                statusText.raycastTarget = false;

                var mapHost = FindOrCreateRect(viewport, "MapHost");
                SetRect(mapHost, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-540f, -210f), new Vector2(540f, 210f));
                var edgeHost = FindOrCreateRect(mapHost, "EdgeHost");
                Stretch(edgeHost);
                edgeHost.SetAsFirstSibling();
                var marker = FindOrCreateRect(mapHost, "PlayerMarker");
                var markerImage = GetOrAdd<Image>(marker.gameObject);
                markerImage.sprite = markerSprite;
                markerImage.preserveAspect = true;
                markerImage.raycastTarget = false;
                marker.sizeDelta = new Vector2(66f, 66f);
                marker.SetAsLastSibling();

                var nodes = new OverworldNodeView[3];
                nodes[0] = AddNode(mapHost, nodePrefab, "Start", "start", 0, 0,
                    TimeKey.Domain.OverworldMovement.MapNodeType.Start,
                    new Vector2(0f, 0f));
                nodes[1] = AddNode(mapHost, nodePrefab, "Room01", "room-01", 1, 0,
                    TimeKey.Domain.OverworldMovement.MapNodeType.Normal,
                    new Vector2(180f, 80f));
                nodes[2] = AddNode(mapHost, nodePrefab, "Room02", "room-02", 0, 1,
                    TimeKey.Domain.OverworldMovement.MapNodeType.Normal,
                    new Vector2(0f, 160f));

                var presenter = GetOrAdd<OverworldMovementPresenter>(root);
                var serialized = new SerializedObject(presenter);
                SetObjectArray(serialized, "nodeViews", nodes);
                SetReference(serialized, "playerMarker", marker);
                SetReference(serialized, "mapHost", mapHost);
                SetReference(serialized, "mapCamera", root.transform.Find("OutOfBattleCamera")?.GetComponent<Camera>());
                SetReference(serialized, "mapInput", input);
                SetReference(serialized, "feedbackLabel", statusText);
                serialized.FindProperty("moveDuration").floatValue = 0.3f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                serialized = new SerializedObject(input);
                SetReference(serialized, "mapHost", mapHost);
                serialized.FindProperty("dragThreshold").floatValue = 5f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var room = root.transform.Find("GateDCanvas/RoomRevealLayer/CombatRoom");
                if (room != null)
                {
                    var roomBinder = GetOrAdd<UiThemeBinder>(room.gameObject);
                    serialized = new SerializedObject(roomBinder);
                    serialized.FindProperty("styleId").enumValueIndex = (int)UiStyleId.ConfirmationDialog;
                    SetReference(serialized, "background", room.GetComponent<Image>());
                    SetReference(serialized, "text", room.GetComponentInChildren<Text>(true));
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    var roomView = room.GetComponent<OutOfBattleRoomView>();
                    if (roomView != null)
                    {
                        serialized = new SerializedObject(roomView);
                        SetReference(serialized, "themeScope", scope);
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                ConfigureButtonTheme(root, "GateDCanvas/RoomRevealLayer/RoomActions/ConfirmButton",
                    UiStyleId.PrimaryButton, scope);
                ConfigureButtonTheme(root, "GateDCanvas/RoomRevealLayer/RoomActions/CancelButton",
                    UiStyleId.SecondaryButton, scope);

                var navigation = root.GetComponent<OutOfBattleShellSceneNavigation>();
                if (navigation != null)
                {
                    serialized = new SerializedObject(navigation);
                    SetReference(serialized, "movementPresenter", presenter);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, OutOfBattlePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static OverworldNodeView AddNode(
            RectTransform parent,
            GameObject nodePrefab,
            string name,
            string id,
            int q,
            int r,
            TimeKey.Domain.OverworldMovement.MapNodeType nodeType,
            Vector2 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(nodePrefab);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            var rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(150f, 120f);
            rect.anchoredPosition = position;
            var serialized = new SerializedObject(instance.GetComponent<OverworldNodeView>());
            serialized.FindProperty("nodeId").stringValue = id;
            serialized.FindProperty("q").intValue = q;
            serialized.FindProperty("r").intValue = r;
            serialized.FindProperty("nodeType").enumValueIndex = (int)nodeType;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return instance.GetComponent<OverworldNodeView>();
        }

        private static void ConfigureButtonTheme(
            GameObject root,
            string path,
            UiStyleId styleId,
            UiThemeScope scope)
        {
            var target = root.transform.Find(path);
            if (target == null)
            {
                return;
            }

            var binder = GetOrAdd<UiThemeBinder>(target.gameObject);
            var serialized = new SerializedObject(binder);
            serialized.FindProperty("styleId").enumValueIndex = (int)styleId;
            SetReference(serialized, "scope", scope);
            SetReference(serialized, "background", target.GetComponent<Image>());
            SetReference(serialized, "text", target.GetComponentInChildren<Text>(true));
            SetReference(serialized, "selectable", target.GetComponent<Button>());
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AuthorConsumerPrefab(string path, UiStyleId styleId, Type markerType)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var scope = GetOrAdd<UiThemeScope>(root);
                scope.SetTheme(LoadRequired<TimeKeyUiTheme>(ThemePath));
                foreach (var marker in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (marker == null || marker.GetType() != markerType)
                    {
                        continue;
                    }

                    var serialized = new SerializedObject(marker);
                    SetReference(serialized, "themeScope", scope);
                    if (markerType == typeof(MainMenuButtonStateVisual))
                    {
                        serialized.FindProperty("styleId").enumValueIndex = (int)styleId;
                    }

                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RefreshFormalScene()
        {
            var scene = EditorSceneManager.OpenScene(OutOfBattleScenePath, OpenSceneMode.Single);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static RectTransform FindOrCreateRect(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<RectTransform>();
            }

            var objectRoot = new GameObject(name, typeof(RectTransform));
            objectRoot.transform.SetParent(parent, false);
            return objectRoot.GetComponent<RectTransform>();
        }

        private static Transform FindRequired(Transform root, string path)
        {
            var target = root.Find(path);
            if (target == null)
            {
                throw new InvalidOperationException("Prefab is missing " + path + ".");
            }

            return target;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void SetObjectArray(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object[] values)
        {
            var property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (var index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
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

        private static Sprite LoadSpriteOrImport(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException("Required texture is missing.", path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return LoadRequired<Sprite>(path);
        }
    }
}
