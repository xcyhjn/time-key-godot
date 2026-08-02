using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TimeKey.Presentation.GameStart;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace TimeKey.Tests.PlayMode.GameStart
{
    public sealed class StartLogoPresenterTests
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
        public IEnumerator Logo_ReachesSilverTerminalStateAndRaisesCompletionOnce()
        {
            _root = new GameObject("StartLogoRig");
            _root.SetActive(false);
            var canvasGroup = _root.AddComponent<CanvasGroup>();
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image))
                .GetComponent<Image>();
            background.transform.SetParent(_root.transform, false);
            var key = new GameObject("Key", typeof(RectTransform), typeof(RawImage))
                .GetComponent<RawImage>();
            key.transform.SetParent(_root.transform, false);
            var labels = new Text[3];
            for (var index = 0; index < labels.Length; index++)
            {
                labels[index] = new GameObject(
                    "Character" + index,
                    typeof(RectTransform),
                    typeof(Text)).GetComponent<Text>();
                labels[index].transform.SetParent(_root.transform, false);
                labels[index].rectTransform.anchoredPosition = new Vector2(index * 10f, 20f);
            }

            var presenter = _root.AddComponent<StartLogoPresenter>();
            SetField(presenter, "canvasGroup", canvasGroup);
            SetField(presenter, "background", background);
            SetField(presenter, "keyImage", key);
            SetField(presenter, "characterLabels", labels);
            SetField(presenter, "silverFont", Resources.Load<Font>("Fonts/Silver"));
            SetField(presenter, "duration", 0.15f);
            SetField(presenter, "allowSkip", true);
            var completionCount = 0;
            presenter.Completed += () => completionCount++;

            _root.SetActive(true);
            Assert.That(presenter.IsComplete, Is.False);
            Assert.That(labels[0].font, Is.EqualTo(Resources.Load<Font>("Fonts/Silver")));
            Assert.That(background.color.r, Is.LessThan(0.05f));
            Assert.That(background.color.g, Is.LessThan(0.05f));
            Assert.That(key.color.a, Is.LessThan(0.05f));
            Assert.That(labels[0].rectTransform.anchoredPosition.y, Is.LessThan(20f));
            Assert.That(labels[1].rectTransform.anchoredPosition.y, Is.GreaterThan(20f));
            Assert.That(labels[2].rectTransform.anchoredPosition.y, Is.LessThan(20f));

            var middleDeadline = Time.realtimeSinceStartup + 0.08f;
            while (Time.realtimeSinceStartup < middleDeadline)
            {
                yield return null;
            }

            Assert.That(key.color.a, Is.GreaterThan(0.25f));
            Assert.That(background.color.r, Is.GreaterThan(0.25f));

            presenter.PlayReveal();
            yield return null;
            presenter.CompleteImmediately();

            var deadline = Time.realtimeSinceStartup + 1f;
            while (!presenter.IsComplete && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(presenter.IsComplete, Is.True);
            Assert.That(canvasGroup.alpha, Is.EqualTo(1f));
            Assert.That(background.color.r, Is.LessThan(0.001f));
            Assert.That(background.color.g, Is.LessThan(0.001f));
            Assert.That(background.color.b, Is.LessThan(0.001f));
            Assert.That(background.color.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(key.color.a, Is.LessThan(0.001f));
            Assert.That(Vector2.Distance(
                labels[0].rectTransform.anchoredPosition,
                new Vector2(0f, 20f)), Is.LessThan(0.001f));
            Assert.That(Vector2.Distance(
                labels[1].rectTransform.anchoredPosition,
                new Vector2(10f, 20f)), Is.LessThan(0.001f));
            Assert.That(Vector2.Distance(
                labels[2].rectTransform.anchoredPosition,
                new Vector2(20f, 20f)), Is.LessThan(0.001f));
            Assert.That(presenter.CompletionTask.IsCompleted, Is.True);
            Assert.That(completionCount, Is.EqualTo(1));
            presenter.CompleteImmediately();
            Assert.That(completionCount, Is.EqualTo(1));
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
