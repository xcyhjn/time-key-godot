using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TimeKey.Presentation.TransitionVisuals;
using UnityEngine;
using UnityEngine.TestTools;

namespace TimeKey.Tests.PlayMode.TransitionVisuals
{
    public sealed class TransitionVisualPresenterTests
    {
        private GameObject _root;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator CoverAndRevealCompleteWithoutFixedDelayAssumptions()
        {
            _root = new GameObject("TransitionRig");
            var group = _root.AddComponent<CanvasGroup>();
            var loading = new GameObject("Loading");
            loading.transform.SetParent(_root.transform, false);
            var presenter = _root.AddComponent<TransitionVisualPresenter>();
            SetField(presenter, "coverGroup", group);
            SetField(presenter, "loadingIndicator", loading);
            SetField(presenter, "duration", 0.05f);

            presenter.PlayCover();
            var deadline = Time.realtimeSinceStartup + 1f;
            while (!presenter.IsComplete && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(presenter.IsComplete, Is.True);
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(loading.activeSelf, Is.True);

            presenter.PlayReveal();
            deadline = Time.realtimeSinceStartup + 1f;
            while (!presenter.IsComplete && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(presenter.IsComplete, Is.True);
            Assert.That(group.alpha, Is.EqualTo(0f));
            Assert.That(loading.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ZeroDuration_AppliesEachRequestedTerminalStateImmediately()
        {
            _root = new GameObject("TransitionRig");
            var group = _root.AddComponent<CanvasGroup>();
            var loading = new GameObject("Loading");
            loading.transform.SetParent(_root.transform, false);
            var presenter = _root.AddComponent<TransitionVisualPresenter>();
            SetField(presenter, "coverGroup", group);
            SetField(presenter, "loadingIndicator", loading);
            SetField(presenter, "duration", 0f);

            presenter.PlayCover();
            Assert.That(presenter.IsComplete, Is.True);
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(group.blocksRaycasts, Is.True);
            Assert.That(loading.activeSelf, Is.True);

            presenter.PlayReveal();
            Assert.That(presenter.IsComplete, Is.True);
            Assert.That(group.alpha, Is.EqualTo(0f));
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(loading.activeSelf, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ImmediateCompletion_StopsTheActiveOpposingCoroutine()
        {
            _root = new GameObject("TransitionRig");
            var group = _root.AddComponent<CanvasGroup>();
            var loading = new GameObject("Loading");
            loading.transform.SetParent(_root.transform, false);
            var presenter = _root.AddComponent<TransitionVisualPresenter>();
            SetField(presenter, "coverGroup", group);
            SetField(presenter, "loadingIndicator", loading);
            SetField(presenter, "duration", 1f);

            presenter.PlayCover();
            yield return null;
            presenter.CompleteImmediately();
            yield return null;
            yield return null;
            Assert.That(group.alpha, Is.EqualTo(0f));
            Assert.That(group.blocksRaycasts, Is.False);

            presenter.PlayReveal();
            yield return null;
            presenter.CompleteCoverImmediately();
            yield return null;
            yield return null;
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(group.blocksRaycasts, Is.True);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
