using System;
using System.Collections.Generic;
using System.IO;
using TimeKey.Composition.SceneFlow;
using TimeKey.Presentation.GameStart;
using TimeKey.Presentation.MainMenu;
using TimeKey.Presentation.TransitionVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeKey.Editor
{
    public static class CombatShellGateCAutomation
    {
        public const string GameStartPrefabPath =
            "Assets/_Project/Prefabs/Shell/GameStartLogo.prefab";
        public const string MainMenuPrefabPath =
            "Assets/_Project/Prefabs/Shell/MainMenu.prefab";
        public const string TransitionPrefabPath =
            "Assets/_Project/Prefabs/Shell/TransitionVisual.prefab";

        private const string BootstrapScenePath =
            "Assets/_Project/Scenes/Shell/Bootstrap.unity";
        private const string GameStartScenePath =
            "Assets/_Project/Scenes/Shell/GameStart.unity";
        private const string MainMenuScenePath =
            "Assets/_Project/Scenes/Shell/MainMenu.unity";
        private const string FontPath = "Assets/_Project/Resources/Fonts/Silver.ttf";
        private const string BackgroundPath =
            "Assets/_Project/Resources/Art/Battle/Background/BG.png";
        private const string ShellArtDirectory =
            "Assets/_Project/Resources/Art/Shell/MainMenu";
        private const string GameStartArtDirectory =
            "Assets/_Project/Resources/Art/Shell/GameStart";
        private const string KeyPath = GameStartArtDirectory + "/key.png";
        private const string ClockPath = ShellArtDirectory + "/clock_noring.png";
        private const string RingPath = ShellArtDirectory + "/ring.png";
        private const string HandPath = ShellArtDirectory + "/point.png";
        private const string MenuButtonArtDirectory =
            "Assets/_Project/Resources/Art/Shell/MainMenu/Buttons";
        private static readonly string[] MenuButtonFiles =
        {
            "u_l_1.png", "u_l_a.png", "m_L_1.png", "m_l_a.png",
            "d_l_1.png", "d_l_a.png", "u_r_1.png", "u_r_a.png",
            "m_r_1.png", "r_m_a.png", "d_r_1.png", "d_r_a.png"
        };

        [MenuItem("Time Key/Author Combat Shell Gate C")]
        public static void AuthorGateC()
        {
            EnsureDirectory("Assets/_Project/Prefabs/Shell");
            EnsureDirectory("Assets/_Project/Animations/Shell");
            EnsureDirectory(ShellArtDirectory);
            EnsureDirectory(GameStartArtDirectory);
            EnsureDirectory(MenuButtonArtDirectory);
            CopySourceTexture("image/key.png", KeyPath);
            CopySourceTexture("image/clock/clock_noring.png", ClockPath);
            CopySourceTexture("image/clock/ring.png", RingPath);
            CopySourceTexture("image/clock/point.png", HandPath);
            foreach (var file in MenuButtonFiles)
            {
                CopySourceTexture(
                    "image/main_menu/" + file,
                    MenuButtonArtDirectory + "/" + file);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureTexture(KeyPath);
            ConfigureTexture(ClockPath);
            ConfigureTexture(RingPath);
            ConfigureTexture(HandPath);
            foreach (var file in MenuButtonFiles)
            {
                ConfigureTexture(MenuButtonArtDirectory + "/" + file);
            }

            var font = LoadRequired<Font>(FontPath);
            var background = LoadRequired<Texture2D>(BackgroundPath);
            var key = LoadRequired<Texture2D>(KeyPath);
            var clock = LoadRequired<Texture2D>(ClockPath);
            var ring = LoadRequired<Texture2D>(RingPath);
            var hand = LoadRequired<Texture2D>(HandPath);
            var buttonTextures = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
            foreach (var file in MenuButtonFiles)
            {
                buttonTextures.Add(
                    file,
                    LoadRequired<Texture2D>(MenuButtonArtDirectory + "/" + file));
            }

            var gameStartPrefab = AuthorGameStartPrefab(font, key);
            var mainMenuPrefab = AuthorMainMenuPrefab(
                font,
                background,
                clock,
                ring,
                hand,
                buttonTextures);
            AuthorTransitionPrefab(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            IntegrateGameStartScene(gameStartPrefab);
            IntegrateMainMenuScene(mainMenuPrefab);
            IntegrateBootstrapTransition();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("TIMEKEY_COMBAT_SHELL_GATE_C_AUTHORED");
        }

        private static GameObject AuthorGameStartPrefab(Font font, Texture2D key)
        {
            var root = new GameObject(
                "GameStartLogo",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(StartLogoPresenter));
            Stretch(root.GetComponent<RectTransform>());

            var backdrop = CreateImage(root.transform, "Background", Color.black);
            Stretch(backdrop.rectTransform);
            var keyImage = CreateRawImage(root.transform, "Key", key);
            Place(keyImage.rectTransform, new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(246f, 400f));
            keyImage.color = Color.white;
            var characters = new Text[3];
            var values = new[] { "时", "之", "钥" };
            var anchors = new[] { 0.42f, 0.5f, 0.58f };
            for (var index = 0; index < characters.Length; index++)
            {
                characters[index] = CreateText(
                    root.transform,
                    "Character" + index,
                    values[index],
                    font,
                    112,
                    TextAnchor.MiddleCenter);
                Place(
                    characters[index].rectTransform,
                    new Vector2(anchors[index], 0.16f),
                    Vector2.zero,
                    new Vector2(150f, 170f));
                characters[index].color = new Color(0.008f, 0.008f, 0.008f, 1f);
            }

            var presenter = root.GetComponent<StartLogoPresenter>();
            var serialized = new SerializedObject(presenter);
            SetReference(serialized, "canvasGroup", root.GetComponent<CanvasGroup>());
            SetReference(serialized, "background", backdrop);
            SetReference(serialized, "keyImage", keyImage);
            var characterLabels = serialized.FindProperty("characterLabels");
            characterLabels.arraySize = characters.Length;
            for (var index = 0; index < characters.Length; index++)
            {
                characterLabels.GetArrayElementAtIndex(index).objectReferenceValue =
                    characters[index];
            }
            SetReference(serialized, "silverFont", font);
            serialized.FindProperty("duration").floatValue = 3f;
            serialized.FindProperty("allowSkip").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, GameStartPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject AuthorMainMenuPrefab(
            Font font,
            Texture2D background,
            Texture2D clock,
            Texture2D ring,
            Texture2D hand,
            IReadOnlyDictionary<string, Texture2D> buttonTextures)
        {
            var root = new GameObject(
                "MainMenu",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(MainMenuPresenter));
            Stretch(root.GetComponent<RectTransform>());

            var backgroundLayer = CreateEntranceLayer(root.transform, "BackgroundEntrance");
            var titleLayer = CreateEntranceLayer(root.transform, "TitleEntrance");
            var clockLayer = CreateEntranceLayer(root.transform, "ClockEntrance");
            var buttonsLayer = CreateEntranceLayer(root.transform, "ButtonsEntrance");

            var backdrop = CreateRawImage(backgroundLayer.transform, "HexMapBackdrop", background);
            Stretch(backdrop.rectTransform);
            backdrop.color = Color.white;
            var veil = CreateImage(
                backgroundLayer.transform,
                "Veil",
                new Color(0.02f, 0.028f, 0.04f, 0.10f));
            Stretch(veil.rectTransform);

            var title = CreateText(titleLayer.transform, "Title", "时之钥", font, 144, TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(900f, 170f));
            title.color = new Color(0.98f, 0.91f, 0.69f, 1f);
            var subtitle = CreateText(titleLayer.transform, "Subtitle", "穿行时代  改写命运", font, 28, TextAnchor.MiddleCenter);
            Place(subtitle.rectTransform, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(620f, 64f));
            subtitle.color = new Color(0.78f, 0.84f, 0.86f, 1f);

            var clockRoot = new GameObject("CentralClock", typeof(RectTransform));
            clockRoot.transform.SetParent(clockLayer.transform, false);
            Place(clockRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(800f, 800f));
            var ringImage = CreateRawImage(clockRoot.transform, "Ring", ring);
            Stretch(ringImage.rectTransform);
            ringImage.color = new Color(1f, 0.82f, 0.42f, 0.85f);
            var face = CreateRawImage(clockRoot.transform, "Face", clock);
            Place(face.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 650f));
            var handImage = CreateRawImage(clockRoot.transform, "Hand", hand);
            Place(handImage.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 650f));

            var newGame = CreateMenuButton(buttonsLayer.transform, "NewGame", "新游戏", font, new Vector2(0.16f, 0.67f), buttonTextures["u_l_1.png"], buttonTextures["u_l_a.png"]);
            var seedGame = CreateMenuButton(buttonsLayer.transform, "SeedGame", "种子游戏", font, new Vector2(0.16f, 0.48f), buttonTextures["m_L_1.png"], buttonTextures["m_l_a.png"]);
            var settings = CreateMenuButton(buttonsLayer.transform, "Settings", "设置", font, new Vector2(0.16f, 0.29f), buttonTextures["d_l_1.png"], buttonTextures["d_l_a.png"]);
            var continueButton = CreateMenuButton(buttonsLayer.transform, "Continue", "继续游戏", font, new Vector2(0.84f, 0.67f), buttonTextures["u_r_1.png"], buttonTextures["u_r_a.png"]);
            var database = CreateMenuButton(buttonsLayer.transform, "Database", "数据库", font, new Vector2(0.84f, 0.48f), buttonTextures["m_r_1.png"], buttonTextures["r_m_a.png"]);
            var exit = CreateMenuButton(buttonsLayer.transform, "Exit", "退出游戏", font, new Vector2(0.84f, 0.29f), buttonTextures["d_r_1.png"], buttonTextures["d_r_a.png"]);

            var seedOverlay = CreateOverlay(root.transform, "SeedOverlay");
            var seedPanel = CreatePanel(seedOverlay.transform, "SeedPanel", new Vector2(560f, 300f));
            var seedTitle = CreateText(seedPanel.transform, "SeedTitle", "种子游戏", font, 40, TextAnchor.MiddleCenter);
            Place(seedTitle.rectTransform, new Vector2(0.5f, 0.79f), Vector2.zero, new Vector2(460f, 64f));
            var seedLabel = CreateText(seedPanel.transform, "SeedLabel", "输入种子编号", font, 24, TextAnchor.MiddleLeft);
            Place(seedLabel.rectTransform, new Vector2(0.5f, 0.59f), Vector2.zero, new Vector2(420f, 50f));
            var seedInput = CreateInputField(seedPanel.transform, font);
            Place(seedInput.GetComponent<RectTransform>(), new Vector2(0.5f, 0.43f), Vector2.zero, new Vector2(420f, 58f));
            var seedConfirm = CreateDialogButton(seedPanel.transform, "SeedConfirm", "开始", font, new Vector2(0.36f, 0.18f));
            var seedCancel = CreateDialogButton(seedPanel.transform, "SeedCancel", "取消", font, new Vector2(0.64f, 0.18f));

            var settingsOverlay = CreateOverlay(root.transform, "SettingsOverlay");
            var settingsPanel = CreatePanel(
                settingsOverlay.transform,
                "SettingsPanel",
                new Vector2(760f, 620f));
            var settingsTitle = CreateText(
                settingsPanel.transform,
                "SettingsTitle",
                "设置",
                font,
                42,
                TextAnchor.MiddleCenter);
            Place(settingsTitle.rectTransform, new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(620f, 70f));
            var masterLabel = CreateText(settingsPanel.transform, "MasterLabel", "总音量", font, 26, TextAnchor.MiddleRight);
            Place(masterLabel.rectTransform, new Vector2(0.27f, 0.67f), Vector2.zero, new Vector2(210f, 54f));
            var masterSlider = CreateSlider(settingsPanel.transform, "MasterVolume");
            Place(masterSlider.GetComponent<RectTransform>(), new Vector2(0.64f, 0.67f), Vector2.zero, new Vector2(330f, 46f));
            var musicLabel = CreateText(settingsPanel.transform, "MusicLabel", "音乐（未接入）", font, 24, TextAnchor.MiddleRight);
            Place(musicLabel.rectTransform, new Vector2(0.27f, 0.53f), Vector2.zero, new Vector2(240f, 54f));
            var musicSlider = CreateSlider(settingsPanel.transform, "MusicVolume");
            musicSlider.interactable = false;
            Place(musicSlider.GetComponent<RectTransform>(), new Vector2(0.64f, 0.53f), Vector2.zero, new Vector2(330f, 46f));
            var sfxLabel = CreateText(settingsPanel.transform, "SfxLabel", "音效（未接入）", font, 24, TextAnchor.MiddleRight);
            Place(sfxLabel.rectTransform, new Vector2(0.27f, 0.39f), Vector2.zero, new Vector2(240f, 54f));
            var sfxSlider = CreateSlider(settingsPanel.transform, "SfxVolume");
            sfxSlider.interactable = false;
            Place(sfxSlider.GetComponent<RectTransform>(), new Vector2(0.64f, 0.39f), Vector2.zero, new Vector2(330f, 46f));
            var fullscreen = CreateToggle(settingsPanel.transform, "Fullscreen", "全屏显示", font);
            Place(fullscreen.GetComponent<RectTransform>(), new Vector2(0.5f, 0.24f), Vector2.zero, new Vector2(300f, 58f));
            var settingsClose = CreateDialogButton(settingsPanel.transform, "SettingsClose", "返回", font, new Vector2(0.5f, 0.10f));

            var modalOverlay = CreateOverlay(root.transform, "ModalOverlay");
            var modalPanel = CreatePanel(modalOverlay.transform, "ModalPanel", new Vector2(620f, 330f));
            var modalTitle = CreateText(modalPanel.transform, "ModalTitle", "提示", font, 40, TextAnchor.MiddleCenter);
            Place(modalTitle.rectTransform, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(500f, 70f));
            var modalBody = CreateText(modalPanel.transform, "ModalBody", "", font, 26, TextAnchor.MiddleCenter);
            Place(modalBody.rectTransform, new Vector2(0.5f, 0.51f), Vector2.zero, new Vector2(510f, 120f));
            var modalConfirm = CreateDialogButton(modalPanel.transform, "ModalConfirm", "确定", font, new Vector2(0.36f, 0.17f));
            var modalClose = CreateDialogButton(modalPanel.transform, "ModalClose", "返回", font, new Vector2(0.64f, 0.17f));

            var presenter = root.GetComponent<MainMenuPresenter>();
            var serialized = new SerializedObject(presenter);
            SetReference(serialized, "canvasGroup", root.GetComponent<CanvasGroup>());
            SetReference(serialized, "titleLabel", title);
            SetReference(serialized, "subtitleLabel", subtitle);
            SetReference(serialized, "seedLabel", seedLabel);
            SetReference(serialized, "seedInput", seedInput);
            SetReference(serialized, "silverFont", font);
            SetReference(serialized, "newGameButton", newGame);
            SetReference(serialized, "seedGameButton", seedGame);
            SetReference(serialized, "continueButton", continueButton);
            SetReference(serialized, "databaseButton", database);
            SetReference(serialized, "settingsButton", settings);
            SetReference(serialized, "exitButton", exit);
            SetReference(serialized, "seedGroup", seedOverlay);
            SetReference(serialized, "seedConfirmButton", seedConfirm);
            SetReference(serialized, "seedCancelButton", seedCancel);
            SetReference(serialized, "modalGroup", modalOverlay);
            SetReference(serialized, "modalTitle", modalTitle);
            SetReference(serialized, "modalBody", modalBody);
            SetReference(serialized, "modalConfirmButton", modalConfirm);
            SetReference(serialized, "modalCloseButton", modalClose);
            SetReference(serialized, "settingsGroup", settingsOverlay);
            SetReference(serialized, "masterVolumeSlider", masterSlider);
            SetReference(serialized, "musicVolumeSlider", musicSlider);
            SetReference(serialized, "sfxVolumeSlider", sfxSlider);
            SetReference(serialized, "fullscreenToggle", fullscreen);
            SetReference(serialized, "settingsCloseButton", settingsClose);
            SetReference(serialized, "backgroundEntranceGroup", backgroundLayer);
            SetReference(serialized, "titleEntranceGroup", titleLayer);
            SetReference(serialized, "clockEntranceGroup", clockLayer);
            SetReference(serialized, "buttonsEntranceGroup", buttonsLayer);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, MainMenuPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AuthorTransitionPrefab(Font font)
        {
            var root = new GameObject(
                "TransitionVisual",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image),
                typeof(TransitionVisualPresenter));
            Stretch(root.GetComponent<RectTransform>());
            root.GetComponent<Image>().color = Color.black;
            var loading = CreateText(root.transform, "LoadingIndicator", "载入中", font, 28, TextAnchor.MiddleCenter);
            Place(loading.rectTransform, new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(280f, 64f));
            var presenter = root.GetComponent<TransitionVisualPresenter>();
            var serialized = new SerializedObject(presenter);
            SetReference(serialized, "coverGroup", root.GetComponent<CanvasGroup>());
            SetReference(serialized, "loadingIndicator", loading.gameObject);
            serialized.FindProperty("duration").floatValue = 0.35f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, TransitionPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void IntegrateGameStartScene(GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(GameStartScenePath, OpenSceneMode.Single);
            var entry = FindRoot(scene, "GameStartEntry");
            var contentRoot = RequireChild(entry, "ContentRoot");
            ReplaceCanvas(contentRoot, prefab, out var instance);
            var navigation = contentRoot.GetComponent<GameStartSceneNavigation>();
            if (navigation == null)
            {
                navigation = contentRoot.AddComponent<GameStartSceneNavigation>();
            }

            var serialized = new SerializedObject(navigation);
            SetReference(serialized, "presenter", instance.GetComponent<StartLogoPresenter>());
            serialized.ApplyModifiedPropertiesWithoutUndo();

            contentRoot.SetActive(false);
            EditorSceneManager.SaveScene(scene, GameStartScenePath);
        }

        private static void IntegrateMainMenuScene(GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            var entry = FindRoot(scene, "MainMenuEntry");
            var contentRoot = RequireChild(entry, "ContentRoot");
            ReplaceCanvas(contentRoot, prefab, out var instance);
            var navigation = contentRoot.GetComponent<MainMenuSceneNavigation>();
            if (navigation == null)
            {
                navigation = contentRoot.AddComponent<MainMenuSceneNavigation>();
            }

            var serialized = new SerializedObject(navigation);
            SetReference(serialized, "presenter", instance.GetComponent<MainMenuPresenter>());
            serialized.ApplyModifiedPropertiesWithoutUndo();
            contentRoot.SetActive(false);
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void IntegrateBootstrapTransition()
        {
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            var root = FindRoot(scene, "BootstrapRoot");
            var overlay = RequireChild(root.transform.Find("TransitionCanvas")?.gameObject, "Overlay");
            var existingLoading = overlay.transform.Find("LoadingIndicator");
            if (existingLoading != null)
            {
                UnityEngine.Object.DestroyImmediate(existingLoading.gameObject);
            }

            var font = LoadRequired<Font>(FontPath);
            var loading = CreateText(
                overlay.transform,
                "LoadingIndicator",
                "载入中",
                font,
                28,
                TextAnchor.MiddleCenter);
            Place(
                loading.rectTransform,
                new Vector2(0.5f, 0.14f),
                Vector2.zero,
                new Vector2(280f, 64f));
            loading.color = new Color(0.98f, 0.91f, 0.69f, 1f);
            var visual = overlay.GetComponent<TransitionVisualPresenter>();
            if (visual == null)
            {
                visual = overlay.AddComponent<TransitionVisualPresenter>();
            }

            var visualSerialized = new SerializedObject(visual);
            SetReference(visualSerialized, "coverGroup", overlay.GetComponent<CanvasGroup>());
            SetReference(visualSerialized, "loadingIndicator", loading.gameObject);
            visualSerialized.FindProperty("duration").floatValue = 0.35f;
            visualSerialized.ApplyModifiedPropertiesWithoutUndo();

            var transition = root.GetComponent<TransitionCanvasPresenter>();
            var transitionSerialized = new SerializedObject(transition);
            SetReference(transitionSerialized, "visualPresenter", visual);
            transitionSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static void ReplaceCanvas(
            GameObject contentRoot,
            GameObject prefab,
            out GameObject instance)
        {
            var existing = contentRoot.transform.Find("GateCCanvas");
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var canvasObject = new GameObject(
                "GateCCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(contentRoot.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasObject.transform);
            instance.name = prefab.name;
        }

        private static CanvasGroup CreateOverlay(Transform parent, string name)
        {
            var overlay = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image));
            overlay.transform.SetParent(parent, false);
            Stretch(overlay.GetComponent<RectTransform>());
            overlay.GetComponent<Image>().color = new Color(0.01f, 0.015f, 0.02f, 0.82f);
            var group = overlay.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        private static CanvasGroup CreateEntranceLayer(Transform parent, string name)
        {
            var layer = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            layer.transform.SetParent(parent, false);
            Stretch(layer.GetComponent<RectTransform>());
            return layer.GetComponent<CanvasGroup>();
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            Place(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            panel.GetComponent<Image>().color = new Color(0.07f, 0.09f, 0.11f, 0.98f);
            return panel;
        }

        private static InputField CreateInputField(Transform parent, Font font)
        {
            var target = new GameObject("SeedInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            target.transform.SetParent(parent, false);
            target.GetComponent<Image>().color = new Color(0.88f, 0.84f, 0.72f, 1f);
            var value = CreateText(target.transform, "Value", "731", font, 28, TextAnchor.MiddleCenter);
            Stretch(value.rectTransform);
            value.color = new Color(0.08f, 0.09f, 0.10f, 1f);
            var placeholder = CreateText(target.transform, "Placeholder", "731", font, 28, TextAnchor.MiddleCenter);
            Stretch(placeholder.rectTransform);
            placeholder.color = new Color(0.24f, 0.26f, 0.27f, 0.55f);
            var input = target.GetComponent<InputField>();
            input.textComponent = value;
            input.placeholder = placeholder;
            input.text = "731";
            input.characterValidation = InputField.CharacterValidation.None;
            return input;
        }

        private static Button CreateMenuButton(
            Transform parent,
            string name,
            string label,
            Font font,
            Vector2 anchor,
            Texture normalTexture = null,
            Texture activeTexture = null)
        {
            var target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            Place(
                target.GetComponent<RectTransform>(),
                anchor,
                Vector2.zero,
                normalTexture == null ? new Vector2(310f, 86f) : new Vector2(512f, 168f));
            Graphic background;
            if (normalTexture == null)
            {
                var image = target.AddComponent<Image>();
                image.color = new Color(0.08f, 0.12f, 0.14f, 0.94f);
                background = image;
            }
            else
            {
                var image = target.AddComponent<RawImage>();
                image.texture = normalTexture;
                image.color = Color.white;
                background = image;
            }

            var button = target.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            var text = CreateText(target.transform, "Label", label, font, 44, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.color = new Color(0.98f, 0.95f, 0.84f, 1f);
            var visual = target.AddComponent<MainMenuButtonStateVisual>();
            var serialized = new SerializedObject(visual);
            SetReference(serialized, "button", button);
            SetReference(serialized, "background", background);
            SetReference(serialized, "label", text);
            SetReference(serialized, "normalTexture", normalTexture);
            SetReference(serialized, "activeTexture", activeTexture);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return button;
        }

        private static Button CreateDialogButton(
            Transform parent,
            string name,
            string label,
            Font font,
            Vector2 anchor)
        {
            var button = CreateMenuButton(parent, name, label, font, anchor);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 58f);
            button.GetComponentInChildren<Text>().fontSize = 26;
            return button;
        }

        private static Slider CreateSlider(Transform parent, string name)
        {
            var target = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
            target.transform.SetParent(parent, false);
            var background = target.GetComponent<Image>();
            background.color = new Color(0.18f, 0.22f, 0.23f, 1f);

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(target.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>());
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Stretch(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = new Color(0.18f, 0.62f, 0.58f, 1f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(target.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            Place(handleRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 54f));
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = new Color(0.98f, 0.88f, 0.57f, 1f);

            var slider = target.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private static Toggle CreateToggle(
            Transform parent,
            string name,
            string label,
            Font font)
        {
            var target = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            target.transform.SetParent(parent, false);
            var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(target.transform, false);
            Place(box.GetComponent<RectTransform>(), new Vector2(0.16f, 0.5f), Vector2.zero, new Vector2(42f, 42f));
            box.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.23f, 1f);
            var check = new GameObject("Check", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(box.transform, false);
            Stretch(check.GetComponent<RectTransform>());
            check.GetComponent<RectTransform>().offsetMin = new Vector2(8f, 8f);
            check.GetComponent<RectTransform>().offsetMax = new Vector2(-8f, -8f);
            check.GetComponent<Image>().color = new Color(0.98f, 0.88f, 0.57f, 1f);
            var text = CreateText(target.transform, "Label", label, font, 26, TextAnchor.MiddleLeft);
            Place(text.rectTransform, new Vector2(0.62f, 0.5f), Vector2.zero, new Vector2(210f, 54f));
            var toggle = target.GetComponent<Toggle>();
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            return toggle;
        }

        private static RawImage CreateRawImage(Transform parent, string name, Texture texture)
        {
            var target = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            target.transform.SetParent(parent, false);
            var image = target.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var target = new GameObject(name, typeof(RectTransform), typeof(Image));
            target.transform.SetParent(parent, false);
            var image = target.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            Font font,
            int fontSize,
            TextAnchor alignment)
        {
            var target = new GameObject(name, typeof(RectTransform), typeof(Text));
            target.transform.SetParent(parent, false);
            var text = target.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void CopySourceTexture(string sourceRelativePath, string targetAssetPath)
        {
            var repositoryRoot = Environment.GetEnvironmentVariable("TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                repositoryRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ".."));
            }

            var source = Path.Combine(repositoryRoot, sourceRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(source))
            {
                throw new FileNotFoundException("Required Godot source artwork is missing.", source);
            }

            File.Copy(source, AssetPathToPhysicalPath(targetAssetPath), true);
        }

        private static void ConfigureTexture(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Texture importer is missing: " + assetPath + ".");
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static void SetReference(SerializedObject serialized, string name, UnityEngine.Object value)
        {
            var property = serialized.FindProperty(name);
            if (property == null)
            {
                throw new InvalidOperationException(serialized.targetObject.GetType().Name + "." + name + " is missing.");
            }

            property.objectReferenceValue = value;
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

            throw new InvalidOperationException(scene.name + " is missing " + name + ".");
        }

        private static GameObject RequireChild(GameObject parent, string name)
        {
            if (parent == null)
            {
                throw new InvalidOperationException("Cannot find " + name + " without a parent.");
            }

            var child = parent.transform.Find(name);
            if (child == null)
            {
                throw new InvalidOperationException(parent.name + " is missing " + name + ".");
            }

            return child.gameObject;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Required asset is missing: " + path + ".");
            }

            return asset;
        }

        private static void EnsureDirectory(string assetPath)
        {
            Directory.CreateDirectory(AssetPathToPhysicalPath(assetPath));
        }

        private static string AssetPathToPhysicalPath(string assetPath)
        {
            var relative = assetPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(UnityEngine.Application.dataPath, relative);
        }
    }
}
