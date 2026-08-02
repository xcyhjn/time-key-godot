using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TimeKey.Composition.SceneFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TimeKey.Tests.EditMode.SceneFlow
{
    public sealed class SceneFlowSceneAssetTests
    {
        private static readonly string[] ExpectedBuildScenes =
        {
            "Assets/_Project/Scenes/Shell/Bootstrap.unity",
            "Assets/_Project/Scenes/Shell/GameStart.unity",
            "Assets/_Project/Scenes/Shell/MainMenu.unity",
            "Assets/_Project/Scenes/Shell/OutOfBattleShell.unity",
            "Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity",
            "Assets/_Project/Scenes/Shell/GameOver.unity"
        };

        [Test]
        public void BuildSettings_StartWithBootstrapAndContainFrozenOrder()
        {
            var enabled = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    enabled.Add(scene.path);
                }
            }

            Assert.That(enabled, Is.EqualTo(ExpectedBuildScenes));
        }

        [Test]
        public void Bootstrap_OwnsExactlyOnePersistentServiceSet()
        {
            var scene = EditorSceneManager.OpenScene(ExpectedBuildScenes[0], OpenSceneMode.Single);

            Assert.That(Find<BootstrapRoot>(scene), Has.Count.EqualTo(1));
            Assert.That(Find<UnitySceneFlowEffects>(scene), Has.Count.EqualTo(1));
            Assert.That(Find<PersistentInputGate>(scene), Has.Count.EqualTo(1));
            Assert.That(Find<TransitionCanvasPresenter>(scene), Has.Count.EqualTo(1));
            Assert.That(Find<EventSystem>(scene), Has.Count.EqualTo(1));
            Assert.That(Find<AudioSource>(scene), Has.Count.EqualTo(1));
            Assert.That(Find<SceneContentEntry>(scene), Is.Empty);
        }

        [Test]
        public void ContentScenes_HaveOneEntryAndNoPersistentDuplicates()
        {
            for (var index = 1; index < ExpectedBuildScenes.Length; index++)
            {
                var scene = EditorSceneManager.OpenScene(
                    ExpectedBuildScenes[index],
                    OpenSceneMode.Single);
                Assert.That(Find<SceneContentEntry>(scene), Has.Count.EqualTo(1),
                    ExpectedBuildScenes[index]);
                Assert.That(Find<EventSystem>(scene), Is.Empty, ExpectedBuildScenes[index]);
                Assert.That(Find<BootstrapRoot>(scene), Is.Empty, ExpectedBuildScenes[index]);
                Assert.That(Find<TransitionCanvasPresenter>(scene), Is.Empty,
                    ExpectedBuildScenes[index]);
            }
        }

        [Test]
        public void ApplicationSceneFlow_RemainsUnityFree()
        {
            var directory = Path.Combine(
                UnityEngine.Application.dataPath,
                "_Project/Runtime/Application/SceneFlow");
            foreach (var path in Directory.GetFiles(directory, "*.cs"))
            {
                var source = File.ReadAllText(path);
                Assert.That(source, Does.Not.Contain("using UnityEngine"), path);
                Assert.That(source, Does.Not.Contain("GameObject"), path);
                Assert.That(source, Does.Not.Contain("UnityEngine.SceneManagement"), path);
            }
        }

        private static List<T> Find<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
            {
                result.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return result;
        }
    }
}
