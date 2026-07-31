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
            Assert.That(controller.SceneCamera.orthographic, Is.False);
            Assert.That(controller.BoardCamera, Is.Not.Null);
            Assert.That(controller.SceneCanvas, Is.Not.Null);
            Assert.That(controller.TimelineSlotCount, Is.EqualTo(36));
            Assert.That(controller.BoardTileCount, Is.EqualTo(19));
            Assert.That(Object.FindObjectsByType<MeshFilter>().Length, Is.GreaterThanOrEqualTo(19));
            Assert.That(GameObject.Find("OriginalArt-center_altar"), Is.Not.Null);

            var elevatedTile = GameObject.Find("Hex-1-0");
            Assert.That(elevatedTile, Is.Not.Null);
            Assert.That(elevatedTile.transform.position.x, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(elevatedTile.transform.position.z, Is.EqualTo(Mathf.Sqrt(3f) * 0.5f).Within(0.001f));
            Assert.That(elevatedTile.transform.position.y, Is.Zero.Within(0.001f));
            Assert.That(elevatedTile.transform.Find("Block-0"), Is.Not.Null);
            Assert.That(elevatedTile.transform.Find("Block-1"), Is.Not.Null);
            Assert.That(elevatedTile.transform.Find("Block-0/Visual-Dirt"), Is.Not.Null);
            Assert.That(elevatedTile.transform.Find("Block-1/Visual-Dirt"), Is.Not.Null);
            Assert.That(elevatedTile.transform.Find("Block-0").localPosition.y, Is.Zero.Within(0.001f));
            Assert.That(elevatedTile.transform.Find("Block-1").localPosition.y,
                Is.EqualTo(VerticalSliceController.HexBlockHeight).Within(0.001f));

            var grassTile = GameObject.Find("Hex-0-0");
            Assert.That(grassTile.transform.Find("Block-0/Visual-Grass"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator OrbitCamera_ClampsBoundsAndKeepsTargetSelectableAtCardinalAngles()
        {
            yield return LoadSlice();
            var controller = GetController();
            Assert.That(controller.SelectCard(VerticalSliceController.LightingCardId), Is.True);

            foreach (var yaw in new[] { 0f, 90f, 180f, 270f })
            {
                controller.SetBoardView(yaw);
                Physics.SyncTransforms();
                var screenPoint = controller.SceneCamera.WorldToScreenPoint(controller.TargetWorldPosition);
                Assert.That(screenPoint.z, Is.GreaterThan(0f), "Target is behind the camera at yaw " + yaw);
                Assert.That(controller.IsScreenPointOverInterface(screenPoint), Is.False,
                    "Target is obscured by UI at yaw " + yaw);
                Assert.That(controller.TrySelectWorldAtScreenPoint(screenPoint), Is.True,
                    "Target raycast failed at yaw " + yaw);
            }

            controller.SetBoardView(-90f, 0f, 99f);
            Assert.That(controller.BoardCamera.Yaw, Is.EqualTo(270f).Within(0.001f));
            Assert.That(controller.BoardCamera.Pitch, Is.EqualTo(BoardOrbitCameraController.MinimumPitch).Within(0.001f));
            Assert.That(controller.BoardCamera.Distance, Is.EqualTo(BoardOrbitCameraController.MaximumDistance).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator BoardSelection_IsStableAndInterfaceBlocksWorldInput()
        {
            yield return LoadSlice();
            var controller = GetController();
            Canvas.ForceUpdateCanvases();

            Assert.That(controller.IsScreenPointOverInterface(new Vector2(80f, 80f)), Is.True);
            Assert.That(controller.TrySelectWorldAtScreenPoint(new Vector2(80f, 80f)), Is.False);

            controller.SetBoardView(180f);
            Physics.SyncTransforms();
            var centerTile = GameObject.Find("Hex-0-0");
            Assert.That(centerTile, Is.Not.Null);
            var screenPoint = controller.SceneCamera.WorldToScreenPoint(
                centerTile.transform.position + new Vector3(0f, 0.22f, 0f));
            Assert.That(controller.TrySelectWorldAtScreenPoint(screenPoint), Is.True);
            Assert.That(controller.SelectedTile, Is.EqualTo(new TimeKey.Domain.HexCoord(0, 0)));

            var billboard = GameObject.Find("OriginalArt-center_altar");
            var direction = controller.SceneCamera.transform.position - billboard.transform.position;
            Assert.That(Vector3.Dot(-billboard.transform.forward, direction.normalized), Is.GreaterThan(0.99f));
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
