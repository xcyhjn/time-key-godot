using System;
using TimeKey.Composition.SceneFlow;
using TimeKey.Presentation.Bindings;
using TimeKey.Presentation.EraClock;
using TimeKey.Presentation.SceneFlowFinale;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeKey.Editor.EraClock
{
    public static class EraClockFormalIntegrationAutomation
    {
        private const string MainMenuPrefabPath =
            "Assets/_Project/Prefabs/Shell/MainMenu.prefab";
        private const string OutOfBattlePrefabPath =
            "Assets/_Project/Prefabs/Shell/OutOfBattleShell.prefab";
        private const string TopHudPrefabPath =
            "Assets/_Project/Prefabs/Battle/CombatShell/CombatTopHUD.prefab";
        private const string MainMenuScenePath =
            "Assets/_Project/Scenes/Shell/MainMenu.unity";
        private const string CombatScenePath =
            "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity";
        private const string FacePath =
            "Assets/_Project/Resources/Art/Shell/MainMenu/clock_noring.png";
        private const string RingPath =
            "Assets/_Project/Resources/Art/Shell/MainMenu/ring.png";
        private const string PointerPath =
            "Assets/_Project/Resources/Art/Shell/MainMenu/point.png";
        private const string SilverPath =
            "Assets/_Project/Resources/Fonts/Silver.ttf";

        [MenuItem("TimeKey/Era Clock/Author Formal Integration")]
        public static void AuthorFormalIntegration()
        {
            AuthorMainMenuPrefab();
            AuthorTopHudPrefab();
            AuthorOutOfBattlePrefab();
            AuthorMainMenuScene();
            AuthorCombatScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("TIMEKEY_ERA_CLOCK_FORMAL_INTEGRATION_AUTHORED");
        }

        [MenuItem("TimeKey/Era Clock/Author Formal HUD Layout")]
        public static void AuthorFormalHudLayout()
        {
            AuthorTopHudPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("TIMEKEY_ERA_CLOCK_FORMAL_HUD_LAYOUT_AUTHORED");
        }

        private static void AuthorMainMenuPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MainMenuPrefabPath);
            try
            {
                var clockRoot = Required<RectTransform>(root.transform, "CentralClock");
                var entrance = Required<CanvasGroup>(root.transform, "ClockEntrance");
                entrance.interactable = false;
                entrance.blocksRaycasts = false;

                var face = Required<RawImage>(clockRoot, "Face");
                var ring = Required<RawImage>(clockRoot, "Ring");
                var pointer = Required<RawImage>(clockRoot, "Hand");
                SetNonRaycast(face, ring, pointer);

                var rootGroup = GetOrAdd<CanvasGroup>(clockRoot.gameObject);
                ConfigureNonRaycast(rootGroup);
                var progress = EnsureProgress(clockRoot, new Vector2(420f, 16f), -350f);
                var font = LoadRequired<Font>(SilverPath);
                var eraLabel = EnsureLabel(clockRoot, "EraLabel", font, 38, -390f);
                var phaseLabel = EnsureLabel(clockRoot, "PhaseLabel", font, 34, -432f);
                var pulse = EnsurePulse(clockRoot, LoadRequired<Texture>(RingPath));
                var anchorsRoot = EnsureStretchRect(entrance.transform, "EraClockAnchors");
                var centerAnchor = EnsureAnchor(
                    anchorsRoot,
                    "CenterAnchor",
                    new Vector2(0.5f, 0.48f),
                    Vector2.zero);
                var hudAnchor = EnsureAnchor(
                    anchorsRoot,
                    "HudAnchor",
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -220f));
                var presenter = GetOrAdd<EraClockPresenter>(clockRoot.gameObject);
                presenter.Rebind(
                    new EraClockViewBindings(
                        clockRoot,
                        face,
                        ring,
                        pointer.rectTransform,
                        pointer,
                        progress,
                        eraLabel,
                        phaseLabel,
                        centerAnchor,
                        hudAnchor,
                        rootGroup,
                        pulse),
                    new EraClockAnimationSettings(0.24f, 0.64f, 1f, 1f, 0.46f));
                PrefabUtility.SaveAsPrefabAsset(root, MainMenuPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AuthorTopHudPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(TopHudPrefabPath);
            try
            {
                var rootRect = root.GetComponent<RectTransform>();
                if (rootRect == null)
                {
                    throw new InvalidOperationException("CombatTopHUD requires a RectTransform.");
                }

                Transform staticClock = Find(root.transform, "ClockLabel");
                if (staticClock != null)
                {
                    staticClock.gameObject.SetActive(false);
                }

                var anchorsRoot = EnsureStretchRect(root.transform, "EraClockAnchors");
                var centerAnchor = EnsureAnchor(
                    anchorsRoot,
                    "CenterAnchor",
                    new Vector2(0.2f, 0.74f),
                    Vector2.zero);
                var clockPlate = Required<RectTransform>(root.transform, "ClockPlate");
                Transform existingHudAnchor = Find(root.transform, "HudAnchor");
                var hudAnchor = existingHudAnchor == null
                    ? EnsureRect(clockPlate, "HudAnchor")
                    : Required<RectTransform>(root.transform, "HudAnchor");
                hudAnchor.SetParent(clockPlate, false);
                hudAnchor.anchorMin = hudAnchor.anchorMax = new Vector2(0.5f, 0.5f);
                hudAnchor.anchoredPosition = Vector2.zero;
                hudAnchor.sizeDelta = Vector2.zero;
                var clockRoot = EnsureRect(root.transform, "EraClock");
                clockRoot.sizeDelta = new Vector2(320f, 320f);
                clockRoot.anchorMin = clockRoot.anchorMax = new Vector2(0.5f, 0.5f);
                var rootGroup = GetOrAdd<CanvasGroup>(clockRoot.gameObject);
                ConfigureNonRaycast(rootGroup);
                var face = EnsureRaw(clockRoot, "ClockFace", LoadRequired<Texture>(FacePath));
                ConfigureCentered(face.rectTransform, new Vector2(220f, 220f), new Vector2(-100f, 0f));
                var ring = EnsureRaw(clockRoot, "ClockRing", LoadRequired<Texture>(RingPath));
                ConfigureCentered(ring.rectTransform, new Vector2(220f, 220f), new Vector2(-100f, 0f));
                var pointerPivot = EnsureStretchRect(clockRoot, "PointerPivot");
                ConfigureCentered(pointerPivot, new Vector2(220f, 220f), new Vector2(-100f, 0f));
                var pointer = EnsureRaw(
                    pointerPivot,
                    "Pointer",
                    LoadRequired<Texture>(PointerPath));
                var progress = EnsureProgress(clockRoot, new Vector2(170f, 14f), 18f);
                progress.rectTransform.anchoredPosition = new Vector2(95f, 18f);
                var font = LoadRequired<Font>(SilverPath);
                var eraLabel = EnsureLabel(clockRoot, "EraLabel", font, 54, -16f);
                ConfigureCentered(eraLabel.rectTransform, new Vector2(210f, 42f), new Vector2(95f, -16f));
                var phaseLabel = EnsureLabel(clockRoot, "PhaseLabel", font, 46, -52f);
                ConfigureCentered(phaseLabel.rectTransform, new Vector2(210f, 42f), new Vector2(95f, -52f));
                var pulse = EnsurePulse(clockRoot, LoadRequired<Texture>(RingPath));
                ConfigureCentered(
                    pulse.GetComponent<RectTransform>(),
                    new Vector2(220f, 220f),
                    new Vector2(-100f, 0f));
                var presenter = GetOrAdd<EraClockPresenter>(clockRoot.gameObject);
                presenter.Rebind(
                    new EraClockViewBindings(
                        clockRoot,
                        face,
                        ring,
                        pointerPivot,
                        pointer,
                        progress,
                        eraLabel,
                        phaseLabel,
                        centerAnchor,
                        hudAnchor,
                        rootGroup,
                        pulse),
                    new EraClockAnimationSettings(0.24f, 0.64f, 1f, 0.5f, 0.32f));
                clockRoot.SetAsLastSibling();
                PrefabUtility.SaveAsPrefabAsset(root, TopHudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AuthorOutOfBattlePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(OutOfBattlePrefabPath);
            try
            {
                var navigation = root.GetComponent<OutOfBattleShellSceneNavigation>();
                if (navigation == null)
                {
                    throw new InvalidOperationException(
                        "OutOfBattleShell prefab is missing scene navigation.");
                }

                SetReference(
                    navigation,
                    "eraClockPresenter",
                    RequiredInChildren<EraClockPresenter>(root));
                SetReference(
                    navigation,
                    "revealPresenter",
                    RequiredInChildren<LayeredSceneRevealPresenter>(root));
                PrefabUtility.SaveAsPrefabAsset(root, OutOfBattlePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AuthorMainMenuScene()
        {
            Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            var navigation = RequiredInScene<MainMenuSceneNavigation>(scene);
            SetReference(
                navigation,
                "eraClockPresenter",
                RequiredInScene<EraClockPresenter>(scene));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void AuthorCombatScene()
        {
            Scene scene = EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Single);
            var binding = RequiredInScene<CombatPresentationBinding>(scene);
            SetReference(
                binding,
                "eraClockPresenter",
                RequiredInScene<EraClockPresenter>(scene));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CombatScenePath);
        }

        private static Image EnsureProgress(
            RectTransform parent,
            Vector2 size,
            float anchoredY)
        {
            var rect = EnsureRect(parent, "EraProgress");
            var image = GetOrAdd<Image>(rect.gameObject);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, anchoredY);
            rect.sizeDelta = size;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.color = new Color(0.26f, 0.82f, 0.72f, 1f);
            image.raycastTarget = false;
            return image;
        }

        private static Text EnsureLabel(
            RectTransform parent,
            string name,
            Font font,
            int fontSize,
            float anchoredY)
        {
            var rect = EnsureRect(parent, name);
            var label = GetOrAdd<Text>(rect.gameObject);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, anchoredY);
            rect.sizeDelta = new Vector2(420f, 42f);
            label.font = font;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 0.82f, 0.42f, 1f);
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static CanvasGroup EnsurePulse(RectTransform parent, Texture texture)
        {
            var rect = EnsureStretchRect(parent, "RolloverPulse");
            var group = GetOrAdd<CanvasGroup>(rect.gameObject);
            ConfigureNonRaycast(group);
            group.alpha = 0f;
            var image = GetOrAdd<RawImage>(rect.gameObject);
            image.texture = texture;
            image.color = new Color(1f, 0.72f, 0.24f, 0.8f);
            image.raycastTarget = false;
            return group;
        }

        private static RawImage EnsureRaw(
            RectTransform parent,
            string name,
            Texture texture)
        {
            var rect = EnsureStretchRect(parent, name);
            var image = GetOrAdd<RawImage>(rect.gameObject);
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform EnsureAnchor(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 position)
        {
            var rect = EnsureRect(parent, name);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        private static RectTransform EnsureStretchRect(Transform parent, string name)
        {
            var rect = EnsureRect(parent, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void ConfigureCentered(
            RectTransform rect,
            Vector2 size,
            Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static RectTransform EnsureRect(Transform parent, string name)
        {
            Transform existing = FindDirect(parent, name);
            if (existing != null)
            {
                var existingRect = existing.GetComponent<RectTransform>();
                if (existingRect == null)
                {
                    throw new InvalidOperationException(name + " requires a RectTransform.");
                }

                return existingRect;
            }

            var target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target.GetComponent<RectTransform>();
        }

        private static void SetNonRaycast(params Graphic[] graphics)
        {
            foreach (Graphic graphic in graphics)
            {
                graphic.raycastTarget = false;
            }
        }

        private static void ConfigureNonRaycast(CanvasGroup group)
        {
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T existing = target.GetComponent<T>();
            return existing != null ? existing : target.AddComponent<T>();
        }

        private static T Required<T>(Transform root, string name) where T : Component
        {
            Transform target = Find(root, name);
            T component = target == null ? null : target.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(
                    root.name + " is missing " + name + " (" + typeof(T).Name + ").");
            }

            return component;
        }

        private static T RequiredInChildren<T>(GameObject root) where T : Component
        {
            T[] matches = root.GetComponentsInChildren<T>(true);
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    root.name + " requires exactly one " + typeof(T).Name + ".");
            }

            return matches[0];
        }

        private static T RequiredInScene<T>(Scene scene) where T : Component
        {
            T result = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (T candidate in root.GetComponentsInChildren<T>(true))
                {
                    if (result != null)
                    {
                        throw new InvalidOperationException(
                            scene.name + " requires exactly one " + typeof(T).Name + ".");
                    }

                    result = candidate;
                }
            }

            return result ?? throw new InvalidOperationException(
                scene.name + " is missing " + typeof(T).Name + ".");
        }

        private static void SetReference(
            UnityEngine.Object owner,
            string fieldName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    owner.name + " is missing serialized field " + fieldName + ".");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null
                ? asset
                : throw new InvalidOperationException("Missing EraClock asset: " + path);
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == name)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Transform FindDirect(Transform parent, string name)
        {
            foreach (Transform child in parent)
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
