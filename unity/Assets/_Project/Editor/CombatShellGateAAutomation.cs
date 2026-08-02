using System;
using System.Collections.Generic;
using System.IO;
using TimeKey.Application.SceneFlow;
using TimeKey.Composition.SceneFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeKey.Editor
{
    public static class CombatShellGateAAutomation
    {
        public const string BootstrapScenePath =
            "Assets/_Project/Scenes/Shell/Bootstrap.unity";
        public const string GameStartScenePath =
            "Assets/_Project/Scenes/Shell/GameStart.unity";
        public const string MainMenuScenePath =
            "Assets/_Project/Scenes/Shell/MainMenu.unity";
        public const string OutOfBattleScenePath =
            "Assets/_Project/Scenes/Shell/OutOfBattleShell.unity";
        public const string CombatScenePath =
            "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity";
        public const string GameOverScenePath =
            "Assets/_Project/Scenes/Shell/GameOver.unity";
        public const string RouteCatalogPath =
            "Assets/_Project/Data/SceneFlow/SceneRouteCatalog.asset";

        [MenuItem("Time Key/Author Combat Shell Gate A")]
        public static void AuthorGateA()
        {
            EnsureDirectory("Assets/_Project/Scenes/Shell");
            EnsureDirectory("Assets/_Project/Data/SceneFlow");
            var routes = AuthorRouteCatalog();
            AuthorBootstrap(routes);
            AuthorContentScene(GameStartScenePath, "GameStartEntry", SceneId.GameStart);
            AuthorContentScene(MainMenuScenePath, "MainMenuEntry", SceneId.MainMenu);
            AuthorContentScene(
                OutOfBattleScenePath,
                "OutOfBattleShellEntry",
                SceneId.OutOfBattleShell);
            AuthorContentScene(GameOverScenePath, "GameOverEntry", SceneId.GameOver);
            AuthorCombatEntry();
            EditorBuildSettings.scenes = new[]
            {
                Enabled(BootstrapScenePath),
                Enabled(GameStartScenePath),
                Enabled(MainMenuScenePath),
                Enabled(OutOfBattleScenePath),
                Enabled(CombatScenePath),
                Enabled(GameOverScenePath)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("TIMEKEY_COMBAT_SHELL_GATE_A_AUTHORED");
        }

        private static SceneRouteCatalog AuthorRouteCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SceneRouteCatalog>(RouteCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SceneRouteCatalog>();
                AssetDatabase.CreateAsset(catalog, RouteCatalogPath);
            }

            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("entries");
            var routes = new[]
            {
                (SceneId.GameStart, "GameStart"),
                (SceneId.MainMenu, "MainMenu"),
                (SceneId.OutOfBattleShell, "OutOfBattleShell"),
                (SceneId.Combat, "CombatVerticalSlice"),
                (SceneId.GameOver, "GameOver")
            };
            entries.arraySize = routes.Length;
            for (var index = 0; index < routes.Length; index++)
            {
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("sceneId").enumValueIndex = (int)routes[index].Item1;
                entry.FindPropertyRelative("sceneName").stringValue = routes[index].Item2;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void AuthorBootstrap(SceneRouteCatalog routes)
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            scene.name = "Bootstrap";
            var root = new GameObject("BootstrapRoot");
            var stateStore = root.AddComponent<SceneFlowStateStore>();
            var effects = root.AddComponent<UnitySceneFlowEffects>();
            var bootstrap = root.AddComponent<BootstrapRoot>();
            var playerSmoke = root.AddComponent<SceneFlowPlayerSmoke>();

            var audioRoot = new GameObject("AudioRoot");
            audioRoot.transform.SetParent(root.transform, false);
            audioRoot.AddComponent<AudioSource>();

            var eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            eventSystemObject.transform.SetParent(root.transform, false);
            var eventSystem = eventSystemObject.GetComponent<EventSystem>();

            var canvasObject = new GameObject(
                "TransitionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            var overlay = new GameObject(
                "Overlay",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image));
            overlay.transform.SetParent(canvasObject.transform, false);
            Stretch(overlay.GetComponent<RectTransform>());
            var overlayGroup = overlay.GetComponent<CanvasGroup>();
            overlayGroup.alpha = 1f;
            overlayGroup.blocksRaycasts = true;
            overlayGroup.interactable = true;
            overlay.GetComponent<Image>().color = Color.black;

            var loading = new GameObject("LoadingIndicator");
            loading.transform.SetParent(overlay.transform, false);
            loading.SetActive(true);

            var inputGate = root.AddComponent<PersistentInputGate>();
            SetReference(inputGate, "overlayGroup", overlayGroup);
            SetReference(inputGate, "eventSystem", eventSystem);

            var transition = root.AddComponent<TransitionCanvasPresenter>();
            SetReference(transition, "overlayGroup", overlayGroup);
            SetReference(transition, "loadingIndicator", loading);

            SetReference(effects, "routes", routes);
            SetReference(effects, "inputGate", inputGate);
            SetReference(effects, "transition", transition);
            SetReference(effects, "stateStore", stateStore);
            SetReference(bootstrap, "effects", effects);
            SetReference(playerSmoke, "bootstrap", bootstrap);

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        [MenuItem("Time Key/Build Combat Shell Gate A")]
        public static void BuildGateA()
        {
            AuthorGateA();
            var outputDirectory = Path.Combine(
                Path.GetTempPath(),
                "timekey-combat-shell-gate-a");
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, "TimeKey.exe");
            var enabledScenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    enabledScenes.Add(scene.path);
                }
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = enabledScenes.ToArray(),
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            Debug.Log(
                "TIMEKEY_COMBAT_SHELL_GATE_A_BUILD result=" + report.summary.result +
                " bytes=" + report.summary.totalSize +
                " output=" + outputPath);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Combat Shell Gate A build failed: " + report.summary.result + ".");
            }
        }

        private static void AuthorContentScene(
            string path,
            string entryName,
            SceneId sceneId)
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var entryObject = new GameObject(entryName);
            var entry = entryObject.AddComponent<SceneContentEntry>();
            var contentRoot = new GameObject("ContentRoot", typeof(CanvasGroup));
            contentRoot.transform.SetParent(entryObject.transform, false);
            var cameraObject = new GameObject("ContentCamera", typeof(Camera));
            cameraObject.transform.SetParent(contentRoot.transform, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
            SetEnum(entry, "sceneId", sceneId);
            SetReference(entry, "contentRoot", contentRoot);
            SetReference(entry, "contentCamera", camera);
            SetReference(entry, "interactionGroup", contentRoot.GetComponent<CanvasGroup>());
            contentRoot.SetActive(false);
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void AuthorCombatEntry()
        {
            var scene = EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Single);
            var verticalSliceRoot = FindRoot(scene, "VerticalSliceRoot");
            if (verticalSliceRoot == null)
            {
                throw new InvalidOperationException("CombatVerticalSlice is missing VerticalSliceRoot.");
            }

            var localEventSystem = verticalSliceRoot.transform.Find("EventSystem");
            if (localEventSystem != null)
            {
                UnityEngine.Object.DestroyImmediate(localEventSystem.gameObject);
            }

            var existingEntry = FindRoot(scene, "CombatSceneEntry");
            if (existingEntry != null)
            {
                UnityEngine.Object.DestroyImmediate(existingEntry);
            }

            var entryObject = new GameObject("CombatSceneEntry");
            var entry = entryObject.AddComponent<SceneContentEntry>();
            var camera = verticalSliceRoot.transform.Find("World/SliceCamera")
                ?.GetComponent<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException("CombatVerticalSlice is missing SliceCamera.");
            }

            SetEnum(entry, "sceneId", SceneId.Combat);
            SetReference(entry, "contentRoot", verticalSliceRoot);
            SetReference(entry, "contentCamera", camera);
            var canvasGroup = verticalSliceRoot.GetComponentInChildren<CanvasGroup>(true);
            SetReference(entry, "interactionGroup", canvasGroup);
            verticalSliceRoot.SetActive(false);
            EditorSceneManager.SaveScene(scene, CombatScenePath);
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

        private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(UnityEngine.Object target, string field, SceneId value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).enumValueIndex = (int)value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureDirectory(string assetPath)
        {
            var relative = assetPath.Substring("Assets/".Length)
                .Replace('/', Path.DirectorySeparatorChar);
            Directory.CreateDirectory(Path.Combine(UnityEngine.Application.dataPath, relative));
        }

        private static EditorBuildSettingsScene Enabled(string path)
        {
            return new EditorBuildSettingsScene(path, true);
        }
    }
}
