using NUnit.Framework;
using TimeKey.Presentation.Bindings;
using TimeKey.Presentation.CombatShell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeKey.Tests.EditMode.Composition
{
    public sealed class CombatShellGateBAssetTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity";
        private const string TopHudPrefabPath =
            "Assets/_Project/Prefabs/Battle/CombatShell/CombatTopHUD.prefab";
        private const string BackgroundPrefabPath =
            "Assets/_Project/Prefabs/Battle/CombatShell/CombatBattleBackground.prefab";
        private const string FontPath = "Assets/_Project/Resources/Fonts/Silver.ttf";
        private const string TextureRoot =
            "Assets/_Project/Resources/Art/Battle/Background/";
        private const string MaterialRoot =
            "Assets/_Project/Materials/Battle/Background/";

        [Test]
        public void GateBPrefabs_SaveTopHudFontAndBackgroundRendererContracts()
        {
            var silver = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Assert.That(silver, Is.Not.Null, FontPath);

            var topHud = LoadPrefab(TopHudPrefabPath);
            Assert.That(topHud.GetComponent<CombatTopHudPresenter>(), Is.Not.Null);
            Assert.That(topHud.GetComponent<CanvasGroup>(), Is.Not.Null);
            var texts = topHud.GetComponentsInChildren<Text>(true);
            Assert.That(texts, Is.Not.Empty);
            foreach (var text in texts)
            {
                Assert.That(text.font, Is.EqualTo(silver), text.name);
            }

            var presenter = new SerializedObject(topHud.GetComponent<CombatTopHudPresenter>());
            foreach (var field in new[]
                     {
                         "helpLabel", "identityLabel", "roundLabel", "clockLabel",
                         "timecoinsLabel", "deckZonesLabel", "enemyHealthLabel", "pauseButton",
                         "settingsButton", "modalGroup", "modalTitle", "modalBody",
                         "modalCloseButton", "silverFont"
                     })
            {
                AssertReference(presenter, field);
            }

            var backgroundPrefab = LoadPrefab(BackgroundPrefabPath);
            var background = backgroundPrefab.GetComponent<CombatBattleBackground>();
            Assert.That(background, Is.Not.Null);
            Assert.That(background.IsConfigured, Is.True);
            Assert.That(background.HorizonRenderers, Has.Length.EqualTo(4));
            Assert.That(backgroundPrefab.GetComponentsInChildren<Collider>(true), Is.Empty);
            var renderers = backgroundPrefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Has.Length.EqualTo(6));
            for (var index = 0; index < background.HorizonRenderers.Length; index++)
            {
                Assert.That(background.HorizonRenderers[index], Is.Not.Null,
                    "horizonRenderers[" + index + "]");
            }

            foreach (var renderer in renderers)
            {
                Assert.That(renderer.sharedMaterial, Is.Not.Null, renderer.name);
                Assert.That(AssetDatabase.GetAssetPath(renderer.sharedMaterial),
                    Does.StartWith(MaterialRoot), renderer.name);
            }
        }

        [Test]
        public void GateBBackgroundAssets_PreserveSourceTexturesAndSavedMaterials()
        {
            foreach (var file in new[]
                     {
                         "BG.png", "titleBG.png", "out-bg_sea.png",
                         "out-bg_shallow_layer.png"
                     })
            {
                var path = TextureRoot + file;
                Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(path), Is.Not.Null, path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
            }

            foreach (var file in new[]
                     {
                         "CombatSea.mat", "CombatShallow.mat", "CombatHorizonMap.mat",
                         "CombatHorizonTitle.mat"
                     })
            {
                var path = MaterialRoot + file;
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.That(material, Is.Not.Null, path);
                Assert.That(material.mainTexture, Is.Not.Null, path);
                Assert.That(AssetDatabase.GetAssetPath(material.mainTexture),
                    Does.StartWith(TextureRoot), path);
            }

            var sea = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "CombatSea.mat");
            var shallow = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "CombatShallow.mat");
            Assert.That(sea.GetFloat("_Surface"), Is.EqualTo(0f));
            Assert.That(shallow.GetFloat("_Surface"), Is.EqualTo(1f));
            Assert.That(shallow.renderQueue, Is.EqualTo((int)UnityEngine.Rendering.RenderQueue.Transparent));
        }

        [Test]
        public void CombatScene_SavesGateBPathsBindingEntranceAndSolidColorCamera()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = FindRoot(scene, "VerticalSliceRoot");
            Assert.That(root, Is.Not.Null);

            var backgroundRoot = root.transform.Find("World/CombatShellBackground");
            var topHudRoot = root.transform.Find("SliceCanvas/HUD/CombatTopHUD");
            Assert.That(backgroundRoot, Is.Not.Null);
            Assert.That(topHudRoot, Is.Not.Null);
            Assert.That(backgroundRoot.GetComponent<CombatBattleBackground>(), Is.Not.Null);
            var topHud = topHudRoot.GetComponent<CombatTopHudPresenter>();
            Assert.That(topHud, Is.Not.Null);

            var battlefieldGround = root.transform.Find("World/BattlefieldGround");
            Assert.That(battlefieldGround, Is.Not.Null);
            Assert.That(battlefieldGround.GetComponent<Renderer>().enabled, Is.False);
            Assert.That(battlefieldGround.GetComponent<Collider>().enabled, Is.True);

            var sceneCanvas = root.transform.Find("SliceCanvas").GetComponent<Canvas>();
            Assert.That(sceneCanvas.sortingOrder, Is.EqualTo(500));
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(backgroundRoot.gameObject),
                Is.Not.Null);
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(topHudRoot.gameObject),
                Is.Not.Null);

            var binding = root.GetComponent<CombatPresentationBinding>();
            Assert.That(binding, Is.Not.Null);
            var bindingSerialized = new SerializedObject(binding);
            var topHudReference = bindingSerialized.FindProperty("combatTopHudPresenter");
            Assert.That(topHudReference, Is.Not.Null);
            Assert.That(topHudReference.objectReferenceValue, Is.EqualTo(topHud));

            var entrance = root.GetComponentInChildren<CombatShellEntrancePresenter>(true);
            Assert.That(entrance, Is.Not.Null);
            var entranceSerialized = new SerializedObject(entrance);
            AssertReference(entranceSerialized, "topHud");
            AssertReference(entranceSerialized, "backgroundRoot");

            var cameraRoot = root.transform.Find("World/SliceCamera");
            Assert.That(cameraRoot, Is.Not.Null);
            var sceneCamera = cameraRoot.GetComponent<Camera>();
            Assert.That(sceneCamera, Is.Not.Null);
            Assert.That(sceneCamera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
            Assert.That(cameraRoot.GetComponent<Skybox>(), Is.Null);
        }

        private static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(PrefabUtility.GetPrefabAssetType(prefab),
                Is.Not.EqualTo(PrefabAssetType.NotAPrefab), path);
            return prefab;
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
    }
}
