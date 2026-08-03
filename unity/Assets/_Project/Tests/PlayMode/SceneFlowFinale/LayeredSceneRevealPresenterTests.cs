using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TimeKey.Presentation.SceneFlowFinale;
using UnityEngine;
using UnityEngine.TestTools;

namespace TimeKey.Tests.PlayMode.SceneFlowFinale
{
    public sealed class LayeredSceneRevealPresenterTests
    {
        private GameObject _root;
        private GameObject _externalLayers;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }

            if (_externalLayers != null)
            {
                Object.DestroyImmediate(_externalLayers);
            }
        }

        [UnityTest]
        public IEnumerator Reveal_OrdersLayersAndReachesInteractiveTerminalState()
        {
            var rig = CreateRig(3, 0.12f, 0.08f);
            _root.SetActive(true);

            Assert.That(rig.Presenter.IsComplete, Is.False);
            Assert.That(rig.Layers[0].alpha, Is.Zero);
            Assert.That(rig.Layers[1].alpha, Is.Zero);
            Assert.That(rig.Layers[2].alpha, Is.Zero);

            var middleDeadline = Time.realtimeSinceStartup + 1f;
            while (rig.Layers[0].alpha < 0.2f &&
                   Time.realtimeSinceStartup < middleDeadline)
            {
                yield return null;
            }

            Assert.That(rig.Layers[0].alpha, Is.GreaterThan(rig.Layers[1].alpha));
            Assert.That(rig.Layers[1].alpha, Is.GreaterThanOrEqualTo(rig.Layers[2].alpha));
            Assert.That(rig.Layers[2].interactable, Is.False);
            Assert.That(rig.Layers[2].blocksRaycasts, Is.False);

            yield return WaitForCompletion(rig.Presenter);
            foreach (var layer in rig.Layers)
            {
                Assert.That(layer.alpha, Is.EqualTo(1f));
                Assert.That(layer.interactable, Is.True);
                Assert.That(layer.blocksRaycasts, Is.True);
            }
        }

        [Test]
        public void ZeroDuration_CompletesSynchronously()
        {
            var rig = CreateRig(2, 0f, 0.1f);
            _root.SetActive(true);

            Assert.That(rig.Presenter.IsComplete, Is.True);
            Assert.That(rig.Layers[0].alpha, Is.EqualTo(1f));
            Assert.That(rig.Layers[1].alpha, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Disable_CompletesEveryLayer()
        {
            var rig = CreateRig(3, 1f, 0.2f);
            _root.SetActive(true);
            yield return null;
            _root.SetActive(false);

            Assert.That(rig.Presenter.IsComplete, Is.True);
            foreach (var layer in rig.Layers)
            {
                Assert.That(layer.alpha, Is.EqualTo(1f));
                Assert.That(layer.blocksRaycasts, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator RepeatedPlay_RestartsThenCompletesOnceMore()
        {
            var rig = CreateRig(2, 0.06f, 0.02f);
            _root.SetActive(true);
            yield return WaitForCompletion(rig.Presenter);

            rig.Presenter.PlayReveal();
            Assert.That(rig.Presenter.IsComplete, Is.False);
            Assert.That(rig.Layers[0].alpha, Is.Zero);
            Assert.That(rig.Layers[1].alpha, Is.Zero);
            yield return WaitForCompletion(rig.Presenter);
            Assert.That(rig.Presenter.IsComplete, Is.True);
        }

        [UnityTest]
        public IEnumerator Destroy_CompletesReferencedLayers()
        {
            _externalLayers = new GameObject("ExternalLayers");
            var layer = new GameObject("Layer").AddComponent<CanvasGroup>();
            layer.transform.SetParent(_externalLayers.transform, false);
            _root = new GameObject("RevealRoot");
            _root.SetActive(false);
            var presenter = _root.AddComponent<LayeredSceneRevealPresenter>();
            SetField(presenter, "layers", new[] { layer });
            SetField(presenter, "layerDuration", 1f);
            _root.SetActive(true);
            yield return null;

            Object.Destroy(_root);
            _root = null;
            yield return null;
            Assert.That(layer.alpha, Is.EqualTo(1f));
            Assert.That(layer.interactable, Is.True);
            Assert.That(layer.blocksRaycasts, Is.True);
        }

        private RevealRig CreateRig(int layerCount, float duration, float interval)
        {
            _root = new GameObject("RevealRoot");
            _root.SetActive(false);
            var presenter = _root.AddComponent<LayeredSceneRevealPresenter>();
            var layers = new CanvasGroup[layerCount];
            for (var index = 0; index < layerCount; index++)
            {
                layers[index] = new GameObject("Layer" + index).AddComponent<CanvasGroup>();
                layers[index].transform.SetParent(_root.transform, false);
            }

            SetField(presenter, "layers", layers);
            SetField(presenter, "layerDuration", duration);
            SetField(presenter, "layerInterval", interval);
            return new RevealRig(presenter, layers);
        }

        private static IEnumerator WaitForCompletion(LayeredSceneRevealPresenter presenter)
        {
            var deadline = Time.realtimeSinceStartup + 2f;
            while (!presenter.IsComplete && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(presenter.IsComplete, Is.True);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private sealed class RevealRig
        {
            public RevealRig(
                LayeredSceneRevealPresenter presenter,
                CanvasGroup[] layers)
            {
                Presenter = presenter;
                Layers = layers;
            }

            public LayeredSceneRevealPresenter Presenter { get; }

            public CanvasGroup[] Layers { get; }
        }
    }
}
