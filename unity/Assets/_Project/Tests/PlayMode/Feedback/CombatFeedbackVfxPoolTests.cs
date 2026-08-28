using System.Collections;
using System.Threading;
using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Presentation.Feedback;
using UnityEngine;
using UnityEngine.TestTools;

namespace TimeKey.Tests.PlayMode.Feedback
{
    public sealed class CombatFeedbackVfxPoolTests
    {
        [UnityTest]
        public IEnumerator Pool_ReusesSavedOneShotAndReturnsToZeroActiveInstances()
        {
            var root = new GameObject("one-shot VFX root");
            var prefabObject = new GameObject("CombatFeedbackOneShot");
            var prefab = prefabObject.AddComponent<CombatFeedbackOneShot>();
            prefabObject.SetActive(false);
            var pool = root.AddComponent<CombatFeedbackVfxPool>();
            pool.Configure(prefab, root.transform, 1);
            var projector = new CombatFeedbackProjector();

            var first = pool.Play(projector.ProjectCardConfirm(
                "lighting",
                "enemy-01",
                CombatFeedbackWorldAnchor.FromWorldPosition(new Vector3(2f, 1.64f, 3f))));

            Assert.That(first, Is.Not.Null);
            Assert.That(first.transform.position, Is.EqualTo(new Vector3(2f, 1.64f, 3f)));
            Assert.That(pool.CreatedCount, Is.EqualTo(1));
            Assert.That(pool.ActiveCount, Is.EqualTo(1));

            yield return new WaitForSecondsRealtime(0.4f);

            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(pool.PeakActiveCount, Is.EqualTo(1));
            Assert.That(pool.RecycledCount, Is.EqualTo(1));
            var second = pool.Play(projector.ProjectCardConfirm(
                "lighting",
                "enemy-01",
                CombatFeedbackWorldAnchor.FromWorldPosition(new Vector3(-1f, 0.32f, 0f))));
            Assert.That(second, Is.SameAs(first));
            Assert.That(pool.CreatedCount, Is.EqualTo(1));

            Object.Destroy(root);
            Object.Destroy(prefabObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pool_ReportsExhaustionWithoutCreatingPastLimit()
        {
            var root = new GameObject("one-shot VFX root");
            var prefabObject = new GameObject("CombatFeedbackOneShot");
            var prefab = prefabObject.AddComponent<CombatFeedbackOneShot>();
            prefabObject.SetActive(false);
            var pool = root.AddComponent<CombatFeedbackVfxPool>();
            pool.Configure(prefab, root.transform, 1);
            var projector = new CombatFeedbackProjector();
            var exhausted = 0;
            pool.PoolExhausted += _ => exhausted++;

            var first = pool.Play(projector.ProjectSceneTransition("Combat", "OutOfBattle"));
            var second = pool.Play(projector.ProjectSceneTransition("Combat", "OutOfBattle"));

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Null);
            Assert.That(exhausted, Is.EqualTo(1));
            Assert.That(pool.ExhaustedCount, Is.EqualTo(1));
            Assert.That(pool.CreatedCount, Is.EqualTo(1));
            Assert.That(pool.ActiveCount, Is.EqualTo(1));

            Object.Destroy(root);
            Object.Destroy(prefabObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pool_SkipsUnsupportedNoEffectAndNeverAllocatesInstance()
        {
            var root = new GameObject("one-shot VFX root");
            var prefabObject = new GameObject("CombatFeedbackOneShot");
            var prefab = prefabObject.AddComponent<CombatFeedbackOneShot>();
            prefabObject.SetActive(false);
            var pool = root.AddComponent<CombatFeedbackVfxPool>();
            pool.Configure(prefab, root.transform, 1);
            var projector = new CombatFeedbackProjector();
            var trace = new TimeKey.Application.CombatTraceEntry(
                "unsupported-source-command",
                "ResolvingTimeline",
                "ResolvingTimeline",
                "enemy-intent");
            var noEffect = projector.ProjectTrace(trace)[0];

            Assert.That(pool.Play(noEffect), Is.Null);
            Assert.That(pool.CreatedCount, Is.Zero);
            Assert.That(pool.ActiveCount, Is.Zero);

            Object.Destroy(root);
            Object.Destroy(prefabObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pool_CancellationTokenStopsActiveOneShot()
        {
            var root = new GameObject("one-shot VFX root");
            var prefabObject = new GameObject("CombatFeedbackOneShot");
            var prefab = prefabObject.AddComponent<CombatFeedbackOneShot>();
            prefabObject.SetActive(false);
            var pool = root.AddComponent<CombatFeedbackVfxPool>();
            pool.Configure(prefab, root.transform, 1);
            var projector = new CombatFeedbackProjector();
            using (var cancellation = new CancellationTokenSource())
            {
                var value = projector.ProjectCardConfirm(
                    "lighting",
                    "enemy-01",
                    CombatFeedbackWorldAnchor.FromWorldPosition(Vector3.one),
                    cancellation.Token);
                pool.Play(value);
                cancellation.Cancel();
            }

            yield return null;

            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(pool.RecycledCount, Is.EqualTo(1));
            Object.Destroy(root);
            Object.Destroy(prefabObject);
            yield return null;
        }
    }
}
