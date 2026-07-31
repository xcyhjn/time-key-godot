using System.Collections;
using System.Linq;
using NUnit.Framework;
using TimeKey.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TimeKey.Tests.PlayMode
{
    public sealed class CombatVerticalSliceTests
    {
        [UnityTest]
        public IEnumerator SceneBuild_IsCompleteAndIdempotent()
        {
            yield return LoadSlice();
            var controller = GetController();
            var childCount = controller.transform.childCount;

            controller.BuildSceneGraph();

            Assert.That(controller.transform.childCount, Is.EqualTo(childCount));
            Assert.That(controller.SceneCamera, Is.Not.Null);
            Assert.That(controller.SceneCamera.orthographic, Is.True);
            Assert.That(controller.SceneCanvas, Is.Not.Null);
            Assert.That(controller.TimelineSlotCount, Is.EqualTo(36));
            Assert.That(Object.FindObjectsByType<MeshFilter>().Length, Is.GreaterThanOrEqualTo(9));

            var elevatedTile = GameObject.Find("Hex-1-0");
            Assert.That(elevatedTile, Is.Not.Null);
            Assert.That(elevatedTile.transform.position.x, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(elevatedTile.transform.position.z, Is.EqualTo(Mathf.Sqrt(3f) * 0.5f).Within(0.001f));
            Assert.That(elevatedTile.transform.position.y, Is.EqualTo(0.35f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator CompletePath_ResolvesLightingBeforeEnemyIntent()
        {
            yield return LoadSlice();
            var controller = GetController();

            Assert.That(controller.SelectCard(VerticalSliceController.LightingCardId), Is.True);
            Assert.That(controller.SelectTarget(VerticalSliceController.TargetId), Is.True);
            Assert.That(controller.TryPlaceSelected(0, 0), Is.True);

            var snapshot = controller.ResolveTimeline();

            Assert.That(controller.CurrentTargetHp, Is.Zero);
            Assert.That(controller.EnemyIntentResolved, Is.True);
            Assert.That(snapshot.TargetHpBefore, Is.EqualTo(10));
            Assert.That(snapshot.TargetHpAfter, Is.Zero);
            Assert.That(snapshot.ResolutionOrder.Select(item => item.CardId),
                Is.EqualTo(new[] { "lighting", "enemy-intent" }));
        }

        private static IEnumerator LoadSlice()
        {
            SceneManager.LoadScene("CombatVerticalSlice", LoadSceneMode.Single);
            yield return null;
        }

        private static VerticalSliceController GetController()
        {
            var root = GameObject.Find("VerticalSliceRoot");
            Assert.That(root, Is.Not.Null);
            var controller = root.GetComponent<VerticalSliceController>();
            Assert.That(controller, Is.Not.Null);
            return controller;
        }
    }
}
