using System;
using System.IO;
using System.Linq;
using TimeKey.Presentation.Bindings;
using TimeKey.Presentation.CombatShell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TimeKey.Editor
{
    public static class CombatShellGateBAutomation
    {
        public const string CombatScenePath =
            "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity";
        public const string BackgroundPrefabPath =
            "Assets/_Project/Prefabs/Battle/CombatShell/CombatBattleBackground.prefab";
        public const string TopHudPrefabPath =
            "Assets/_Project/Prefabs/Battle/CombatShell/CombatTopHUD.prefab";

        private const string FontPath = "Assets/_Project/Resources/Fonts/Silver.ttf";
        private const string TextureDirectory =
            "Assets/_Project/Resources/Art/Battle/Background";
        private const string MaterialDirectory =
            "Assets/_Project/Materials/Battle/Background";
        private const string PrefabDirectory =
            "Assets/_Project/Prefabs/Battle/CombatShell";

        private const string MapTexturePath = TextureDirectory + "/BG.png";
        private const string TitleTexturePath = TextureDirectory + "/titleBG.png";
        private const string SeaTexturePath = TextureDirectory + "/out-bg_sea.png";
        private const string ShallowTexturePath =
            TextureDirectory + "/out-bg_shallow_layer.png";

        private const string SeaMaterialPath = MaterialDirectory + "/CombatSea.mat";
        private const string ShallowMaterialPath =
            MaterialDirectory + "/CombatShallow.mat";
        private const string MapMaterialPath =
            MaterialDirectory + "/CombatHorizonMap.mat";
        private const string TitleMaterialPath =
            MaterialDirectory + "/CombatHorizonTitle.mat";

        [MenuItem("Time Key/Author Combat Shell Gate B")]
        public static void AuthorGateB()
        {
            EnsureDirectories();
            ImportSourceTextures();

            var seaTexture = LoadRequiredAsset<Texture2D>(SeaTexturePath);
            var shallowTexture = LoadRequiredAsset<Texture2D>(ShallowTexturePath);
            var mapTexture = LoadRequiredAsset<Texture2D>(MapTexturePath);
            var titleTexture = LoadRequiredAsset<Texture2D>(TitleTexturePath);
            var font = LoadRequiredAsset<Font>(FontPath);

            var seaMaterial = LoadOrCreateUnlitMaterial(
                SeaMaterialPath,
                seaTexture,
                new Color(0.72f, 0.78f, 0.85f, 1f),
                new Vector2(48f, 48f),
                true,
                false);
            var shallowMaterial = LoadOrCreateUnlitMaterial(
                ShallowMaterialPath,
                shallowTexture,
                new Color(0.38f, 0.52f, 0.54f, 1f),
                new Vector2(9f, 9f),
                true,
                true);
            var mapMaterial = LoadOrCreateUnlitMaterial(
                MapMaterialPath,
                mapTexture,
                new Color(0.48f, 0.50f, 0.45f, 1f),
                Vector2.one,
                true,
                false);
            var titleMaterial = LoadOrCreateUnlitMaterial(
                TitleMaterialPath,
                titleTexture,
                new Color(0.50f, 0.53f, 0.48f, 1f),
                Vector2.one,
                true,
                false);

            var backgroundPrefab = AuthorBackgroundPrefab(
                seaMaterial,
                shallowMaterial,
                mapMaterial,
                titleMaterial);
            var topHudPrefab = AuthorTopHudPrefab(font);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            IntegrateCombatScene(backgroundPrefab, topHudPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("TIMEKEY_COMBAT_SHELL_GATE_B_AUTHORED");
        }

        private static void EnsureDirectories()
        {
            EnsureDirectory(TextureDirectory);
            EnsureDirectory(MaterialDirectory);
            EnsureDirectory(PrefabDirectory);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ImportSourceTextures()
        {
            CopySourceTexture("image/BG.png", MapTexturePath);
            CopySourceTexture("image/titleBG.png", TitleTexturePath);
            CopySourceTexture("image/outscene_block/out-bg_sea.png", SeaTexturePath);
            CopySourceTexture(
                "image/outscene_block/out-bg_shallow_layer.png",
                ShallowTexturePath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureTextureImporter(MapTexturePath, TextureWrapMode.Clamp);
            ConfigureTextureImporter(TitleTexturePath, TextureWrapMode.Clamp);
            ConfigureTextureImporter(SeaTexturePath, TextureWrapMode.Repeat);
            ConfigureTextureImporter(ShallowTexturePath, TextureWrapMode.Repeat);
        }

        private static void CopySourceTexture(string sourceRelativePath, string targetAssetPath)
        {
            var repositoryRoot = Environment.GetEnvironmentVariable(
                "TIMEKEY_REPOSITORY_ROOT");
            if (string.IsNullOrWhiteSpace(repositoryRoot))
            {
                repositoryRoot = Path.GetFullPath(Path.Combine(
                    UnityEngine.Application.dataPath,
                    "..",
                    ".."));
            }

            var sourcePath = Path.Combine(
                repositoryRoot,
                sourceRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(
                    "The Godot source artwork is missing.",
                    sourcePath);
            }

            var targetPath = AssetPathToPhysicalPath(targetAssetPath);
            if (!FilesMatch(sourcePath, targetPath))
            {
                File.Copy(sourcePath, targetPath, true);
            }
        }

        private static bool FilesMatch(string sourcePath, string targetPath)
        {
            if (!File.Exists(targetPath))
            {
                return false;
            }

            var source = new FileInfo(sourcePath);
            var target = new FileInfo(targetPath);
            if (source.Length != target.Length)
            {
                return false;
            }

            const int bufferSize = 81920;
            var sourceBuffer = new byte[bufferSize];
            var targetBuffer = new byte[bufferSize];
            using (var sourceStream = File.OpenRead(sourcePath))
            using (var targetStream = File.OpenRead(targetPath))
            {
                while (true)
                {
                    var sourceRead = sourceStream.Read(sourceBuffer, 0, sourceBuffer.Length);
                    var targetRead = targetStream.Read(targetBuffer, 0, targetBuffer.Length);
                    if (sourceRead != targetRead)
                    {
                        return false;
                    }

                    if (sourceRead == 0)
                    {
                        return true;
                    }

                    for (var index = 0; index < sourceRead; index++)
                    {
                        if (sourceBuffer[index] != targetBuffer[index])
                        {
                            return false;
                        }
                    }
                }
            }
        }

        private static void ConfigureTextureImporter(
            string assetPath,
            TextureWrapMode wrapMode)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    "Texture importer is missing: " + assetPath + ".");
            }

            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = wrapMode;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static Material LoadOrCreateUnlitMaterial(
            string assetPath,
            Texture2D texture,
            Color tint,
            Vector2 tiling,
            bool doubleSided,
            bool transparent)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Universal Render Pipeline/Unlit is unavailable.");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = Path.GetFileNameWithoutExtension(assetPath)
                };
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.shader = shader;
            material.mainTexture = texture;
            material.mainTextureScale = tiling;
            material.color = tint;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", transparent ? 1f : 0f);
            }

            material.SetFloat("_SrcBlend", transparent
                ? (float)BlendMode.SrcAlpha
                : (float)BlendMode.One);
            material.SetFloat("_DstBlend", transparent
                ? (float)BlendMode.OneMinusSrcAlpha
                : (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);
            material.renderQueue = transparent ? (int)RenderQueue.Transparent : -1;
            if (transparent)
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", doubleSided ? 0f : 2f);
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject AuthorBackgroundPrefab(
            Material seaMaterial,
            Material shallowMaterial,
            Material mapMaterial,
            Material titleMaterial)
        {
            var root = new GameObject(
                "CombatBattleBackground",
                typeof(CombatBattleBackground));
            try
            {
                var sea = CreateQuad(
                    root.transform,
                    "SeaPlane",
                    new Vector3(0f, -1.85f, 0f),
                    Quaternion.Euler(90f, 0f, 0f),
                    new Vector3(180f, 180f, 1f),
                    seaMaterial);
                var shallow = CreateQuad(
                    root.transform,
                    "ShallowHexLayer",
                    new Vector3(0f, -1.80f, 0f),
                    Quaternion.Euler(90f, 0f, 0f),
                    new Vector3(34f, 34f, 1f),
                    shallowMaterial);

                var horizonRoot = new GameObject("HorizonRing").transform;
                horizonRoot.SetParent(root.transform, false);
                var horizons = new[]
                {
                    CreateQuad(
                        horizonRoot,
                        "HorizonNorth",
                        new Vector3(0f, 13f, 44f),
                        Quaternion.Euler(0f, 180f, 0f),
                        new Vector3(94f, 32f, 1f),
                        titleMaterial),
                    CreateQuad(
                        horizonRoot,
                        "HorizonEast",
                        new Vector3(44f, 13f, 0f),
                        Quaternion.Euler(0f, -90f, 0f),
                        new Vector3(94f, 32f, 1f),
                        mapMaterial),
                    CreateQuad(
                        horizonRoot,
                        "HorizonSouth",
                        new Vector3(0f, 13f, -44f),
                        Quaternion.identity,
                        new Vector3(94f, 32f, 1f),
                        titleMaterial),
                    CreateQuad(
                        horizonRoot,
                        "HorizonWest",
                        new Vector3(-44f, 13f, 0f),
                        Quaternion.Euler(0f, 90f, 0f),
                        new Vector3(94f, 32f, 1f),
                        mapMaterial)
                };

                var serialized = new SerializedObject(
                    root.GetComponent<CombatBattleBackground>());
                SetReference(serialized, "seaRenderer", sea);
                SetReference(serialized, "shallowRenderer", shallow);
                var horizonProperty = serialized.FindProperty("horizonRenderers");
                if (horizonProperty == null)
                {
                    throw new InvalidOperationException(
                        "CombatBattleBackground.horizonRenderers is missing.");
                }

                horizonProperty.arraySize = horizons.Length;
                for (var index = 0; index < horizons.Length; index++)
                {
                    horizonProperty.GetArrayElementAtIndex(index).objectReferenceValue =
                        horizons[index];
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, BackgroundPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            return LoadRequiredAsset<GameObject>(BackgroundPrefabPath);
        }

        private static Renderer CreateQuad(
            Transform parent,
            string name,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Material material)
        {
            var child = GameObject.CreatePrimitive(PrimitiveType.Quad);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
            child.transform.localRotation = rotation;
            child.transform.localScale = scale;
            var collider = child.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            var renderer = child.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowOcclusionWhenDynamic = false;
            return renderer;
        }

        private static GameObject AuthorTopHudPrefab(Font font)
        {
            var root = new GameObject(
                "CombatTopHUD",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(CombatTopHudPresenter));
            try
            {
                Stretch(root.GetComponent<RectTransform>());
                root.GetComponent<CanvasGroup>().alpha = 1f;

                var topBar = CreatePanel(
                    root.transform,
                    "TopBar",
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(16f, -84f),
                    new Vector2(-16f, -12f),
                    new Color(0.10f, 0.085f, 0.07f, 0.97f),
                    false);
                AddOutline(topBar.gameObject, new Color(0.72f, 0.56f, 0.28f, 0.9f));

                var leftSection = CreateRect("RunIdentity", topBar);
                SetRect(
                    leftSection,
                    Vector2.zero,
                    new Vector2(0.39f, 1f),
                    new Vector2(16f, 8f),
                    new Vector2(-12f, -8f));
                var help = CreateText(
                    leftSection,
                    "HelpLabel",
                    "帮助",
                    font,
                    17,
                    TextAnchor.MiddleLeft,
                    new Vector2(0f, 28f),
                    new Vector2(68f, 0f),
                    new Color(0.88f, 0.73f, 0.40f, 1f));
                var identity = CreateText(
                    leftSection,
                    "IdentityLabel",
                    "银 · 时钥行者",
                    font,
                    20,
                    TextAnchor.MiddleLeft,
                    new Vector2(78f, 28f),
                    new Vector2(0f, 0f),
                    new Color(0.96f, 0.92f, 0.78f, 1f));
                var round = CreateText(
                    leftSection,
                    "RoundLabel",
                    "时代 01  /  阶段 01",
                    font,
                    16,
                    TextAnchor.MiddleLeft,
                    new Vector2(78f, 0f),
                    new Vector2(0f, -28f),
                    new Color(0.70f, 0.79f, 0.75f, 1f));

                var clockPlate = CreatePanel(
                    topBar,
                    "ClockPlate",
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(-118f, -42f),
                    new Vector2(118f, 42f),
                    new Color(0.05f, 0.16f, 0.17f, 1f),
                    false);
                AddOutline(clockPlate.gameObject, new Color(0.26f, 0.76f, 0.71f, 0.92f));
                var clock = CreateText(
                    clockPlate,
                    "ClockLabel",
                    "01 : 01",
                    font,
                    31,
                    TextAnchor.MiddleCenter,
                    new Vector2(8f, 8f),
                    new Vector2(-8f, -8f),
                    new Color(0.90f, 1f, 0.91f, 1f));

                var rightSection = CreateRect("Commands", topBar);
                SetRect(
                    rightSection,
                    new Vector2(0.62f, 0f),
                    Vector2.one,
                    new Vector2(12f, 9f),
                    new Vector2(-12f, -9f));
                var timecoins = CreateText(
                    clockPlate,
                    "TimecoinsLabel",
                    "时间币 0",
                    font,
                    16,
                    TextAnchor.LowerCenter,
                    new Vector2(8f, 6f),
                    new Vector2(-8f, -57f),
                    new Color(0.93f, 0.76f, 0.31f, 1f));
                var deckZones = CreateText(
                    rightSection,
                    "DeckZonesLabel",
                    "牌库 7  手牌 5  弃牌 0",
                    font,
                    16,
                    TextAnchor.MiddleRight,
                    new Vector2(0f, 8f),
                    new Vector2(-302f, -8f),
                    new Color(0.78f, 0.86f, 0.80f, 1f));
                var pause = CreateButton(
                    rightSection,
                    "PauseButton",
                    "暂停",
                    font,
                    new Vector2(1f, 0f),
                    Vector2.one,
                    new Vector2(-290f, -56f),
                    new Vector2(-154f, -8f),
                    new Color(0.20f, 0.27f, 0.27f, 1f));
                var settings = CreateButton(
                    rightSection,
                    "SettingsButton",
                    "设置",
                    font,
                    new Vector2(1f, 0f),
                    Vector2.one,
                    new Vector2(-142f, -56f),
                    new Vector2(-6f, -8f),
                    new Color(0.27f, 0.22f, 0.18f, 1f));

                var enemyPanel = CreatePanel(
                    root.transform,
                    "EnemyHealthPanel",
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(16f, -142f),
                    new Vector2(260f, -100f),
                    new Color(0.16f, 0.065f, 0.055f, 0.94f),
                    false);
                AddOutline(enemyPanel.gameObject, new Color(0.78f, 0.25f, 0.20f, 0.92f));
                var enemyHealth = CreateText(
                    enemyPanel,
                    "EnemyHealthLabel",
                    "敌方总生命  0 / 0",
                    font,
                    16,
                    TextAnchor.MiddleCenter,
                    new Vector2(10f, 4f),
                    new Vector2(-10f, -4f),
                    new Color(1f, 0.83f, 0.77f, 1f));

                var modal = CreateModal(root.transform, font);
                var presenter = root.GetComponent<CombatTopHudPresenter>();
                var serialized = new SerializedObject(presenter);
                SetReference(serialized, "helpLabel", help);
                SetReference(serialized, "identityLabel", identity);
                SetReference(serialized, "roundLabel", round);
                SetReference(serialized, "clockLabel", clock);
                SetReference(serialized, "timecoinsLabel", timecoins);
                SetReference(serialized, "deckZonesLabel", deckZones);
                SetReference(serialized, "enemyHealthLabel", enemyHealth);
                SetReference(serialized, "pauseButton", pause);
                SetReference(serialized, "settingsButton", settings);
                SetReference(serialized, "modalGroup", modal.Group);
                SetReference(serialized, "modalTitle", modal.Title);
                SetReference(serialized, "modalBody", modal.Body);
                SetReference(serialized, "modalCloseButton", modal.CloseButton);
                SetReference(serialized, "silverFont", font);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, TopHudPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            return LoadRequiredAsset<GameObject>(TopHudPrefabPath);
        }

        private static ModalReferences CreateModal(Transform parent, Font font)
        {
            var modal = new GameObject(
                "Modal",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup));
            modal.transform.SetParent(parent, false);
            Stretch(modal.GetComponent<RectTransform>());
            modal.GetComponent<Image>().color = new Color(0.015f, 0.02f, 0.022f, 0.82f);
            var group = modal.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var panel = CreatePanel(
                modal.transform,
                "Dialog",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-240f, -145f),
                new Vector2(240f, 145f),
                new Color(0.075f, 0.09f, 0.085f, 1f),
                true);
            AddOutline(panel.gameObject, new Color(0.74f, 0.57f, 0.29f, 1f));
            var title = CreateText(
                panel,
                "Title",
                "战斗暂停",
                font,
                28,
                TextAnchor.MiddleCenter,
                new Vector2(24f, 204f),
                new Vector2(-24f, -20f),
                new Color(0.96f, 0.87f, 0.66f, 1f));
            var body = CreateText(
                panel,
                "Body",
                "战场状态已保留。关闭面板后继续行动。",
                font,
                19,
                TextAnchor.MiddleCenter,
                new Vector2(34f, 88f),
                new Vector2(-34f, -82f),
                new Color(0.86f, 0.90f, 0.86f, 1f));
            var close = CreateButton(
                panel,
                "CloseButton",
                "返回战斗",
                font,
                Vector2.zero,
                Vector2.one,
                new Vector2(98f, 22f),
                new Vector2(-98f, -210f),
                new Color(0.20f, 0.43f, 0.40f, 1f));
            return new ModalReferences(group, title, body, close);
        }

        private static void IntegrateCombatScene(
            GameObject backgroundPrefab,
            GameObject topHudPrefab)
        {
            var scene = EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects()
                .SingleOrDefault(value => value.name == "VerticalSliceRoot");
            if (root == null)
            {
                throw new InvalidOperationException(
                    "CombatVerticalSlice is missing VerticalSliceRoot.");
            }

            var world = root.transform.Find("World");
            var hud = root.transform.Find("SliceCanvas/HUD");
            var sceneCanvas = root.transform.Find("SliceCanvas")?.GetComponent<Canvas>();
            var camera = root.transform.Find("World/SliceCamera")?.GetComponent<Camera>();
            var binding = root.GetComponent<CombatPresentationBinding>();
            if (world == null || hud == null || sceneCanvas == null ||
                camera == null || binding == null)
            {
                throw new InvalidOperationException(
                    "CombatVerticalSlice is missing a Gate B integration dependency.");
            }

            DestroyChild(world, "CombatShellBackground");
            DestroyChild(world, "CombatBattleBackground");
            DestroyChild(hud, "CombatTopHUD");

            var background = (GameObject)PrefabUtility.InstantiatePrefab(
                backgroundPrefab,
                scene);
            background.name = "CombatShellBackground";
            background.transform.SetParent(world, false);
            background.transform.SetAsFirstSibling();

            var topHud = (GameObject)PrefabUtility.InstantiatePrefab(topHudPrefab, scene);
            topHud.name = "CombatTopHUD";
            topHud.transform.SetParent(hud, false);
            Stretch(topHud.GetComponent<RectTransform>());
            topHud.transform.SetAsLastSibling();

            var legacyHeader = hud.Find("Header");
            if (legacyHeader != null)
            {
                legacyHeader.gameObject.SetActive(false);
            }

            HideLegacyBattleFlowCounters(hud.Find("BattleFlowPanel"));

            var battlefieldGround = world.Find("BattlefieldGround")?.GetComponent<Renderer>();
            if (battlefieldGround == null)
            {
                throw new InvalidOperationException(
                    "CombatVerticalSlice is missing BattlefieldGround.");
            }

            battlefieldGround.enabled = false;
            sceneCanvas.sortingOrder = 500;

            var topHudPresenter = topHud.GetComponent<CombatTopHudPresenter>();
            ConfigureReference(binding, "combatTopHudPresenter", topHudPresenter);

            var entrance = root.GetComponent<CombatShellEntrancePresenter>() ??
                root.AddComponent<CombatShellEntrancePresenter>();
            var entranceSerialized = new SerializedObject(entrance);
            SetReference(entranceSerialized, "topHud", topHud.GetComponent<CanvasGroup>());
            SetReference(entranceSerialized, "backgroundRoot", background.transform);
            var duration = entranceSerialized.FindProperty("duration");
            if (duration == null)
            {
                throw new InvalidOperationException(
                    "CombatShellEntrancePresenter.duration is missing.");
            }

            duration.floatValue = 0.45f;
            entranceSerialized.ApplyModifiedPropertiesWithoutUndo();

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.04f, 1f);

            EditorUtility.SetDirty(binding);
            EditorUtility.SetDirty(entrance);
            EditorUtility.SetDirty(battlefieldGround);
            EditorUtility.SetDirty(sceneCanvas);
            EditorUtility.SetDirty(camera);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CombatScenePath))
            {
                throw new InvalidOperationException(
                    "CombatVerticalSlice could not be saved after Gate B authoring.");
            }
        }

        private static void HideLegacyBattleFlowCounters(Transform battleFlowRoot)
        {
            if (battleFlowRoot == null)
            {
                throw new InvalidOperationException(
                    "CombatVerticalSlice is missing BattleFlowPanel.");
            }

            foreach (var childName in new[]
                     {
                         "DrawPileCount", "HandCount", "DiscardPileCount",
                         "RoundLabel", "TimecoinsLabel"
                     })
            {
                var child = battleFlowRoot.Find(childName);
                if (child == null)
                {
                    throw new InvalidOperationException(
                        "BattleFlowPanel is missing " + childName + ".");
                }

                child.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
            }
        }

        private static void DestroyChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
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
            Font font,
            int fontSize,
            TextAnchor alignment,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Text));
            child.transform.SetParent(parent, false);
            var text = child.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, offsetMin, offsetMax);
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
            var child = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            child.transform.SetParent(parent, false);
            var rect = child.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            var image = child.GetComponent<Image>();
            image.color = color;
            var button = child.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.34f, 0.55f, 0.48f, 1f);
            colors.pressedColor = new Color(0.12f, 0.20f, 0.19f, 1f);
            colors.disabledColor = new Color(0.13f, 0.14f, 0.14f, 0.72f);
            button.colors = colors;
            CreateText(
                child.transform,
                "Label",
                label,
                font,
                17,
                TextAnchor.MiddleCenter,
                new Vector2(8f, 4f),
                new Vector2(-8f, -4f),
                new Color(0.95f, 0.94f, 0.84f, 1f));
            return button;
        }

        private static void AddOutline(GameObject target, Color color)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
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

        private static void SetReference(
            SerializedObject serialized,
            string fieldName,
            UnityEngine.Object value)
        {
            var property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    serialized.targetObject.GetType().Name + "." + fieldName +
                    " is missing.");
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

        private static T LoadRequiredAsset<T>(string assetPath)
            where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    "Required asset is missing: " + assetPath + ".");
            }

            return asset;
        }

        private static void EnsureDirectory(string assetPath)
        {
            Directory.CreateDirectory(AssetPathToPhysicalPath(assetPath));
        }

        private static string AssetPathToPhysicalPath(string assetPath)
        {
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Asset path must begin with Assets/.",
                    nameof(assetPath));
            }

            var relative = assetPath.Substring("Assets/".Length)
                .Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(UnityEngine.Application.dataPath, relative);
        }

        private readonly struct ModalReferences
        {
            public ModalReferences(
                CanvasGroup group,
                Text title,
                Text body,
                Button closeButton)
            {
                Group = group;
                Title = title;
                Body = body;
                CloseButton = closeButton;
            }

            public CanvasGroup Group { get; }

            public Text Title { get; }

            public Text Body { get; }

            public Button CloseButton { get; }
        }
    }
}
