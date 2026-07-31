using System.Collections;
using System.Linq;
using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Presentation;
using TimeKey.Presentation.Cards;
using UnityEngine;
using UnityEngine.EventSystems;
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

            var artwork = controller.CardHand.Artwork.rectTransform;
            var artworkCenter = artwork.TransformPoint(artwork.rect.center);
            var interfacePoint = RectTransformUtility.WorldToScreenPoint(controller.SceneCamera, artworkCenter);
            Assert.That(controller.IsScreenPointOverInterface(interfacePoint), Is.True);
            Assert.That(controller.TrySelectWorldAtScreenPoint(interfacePoint), Is.False);

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

        [UnityTest]
        public IEnumerator CardSelectionAndCancel_RestoreHandAndOrbitInput()
        {
            yield return LoadSlice();
            var controller = GetController();

            Assert.That(controller.CardHand, Is.Not.Null);
            Assert.That(controller.CardHand.gameObject.activeSelf, Is.True);
            Assert.That(controller.CardHand.InteractionState, Is.EqualTo(CardHandInteractionState.Idle));
            Assert.That(controller.BoardCamera.InputEnabled, Is.True);
            Assert.That(controller.SelectTarget(VerticalSliceController.TargetId), Is.False);

            Assert.That(controller.SelectCard(VerticalSliceController.LightingCardId), Is.True);
            Assert.That(controller.CardHand.InteractionState, Is.EqualTo(CardHandInteractionState.Selected));
            Assert.That(controller.CardPlayState, Is.EqualTo(CardPlaySessionState.Idle));
            Assert.That(controller.BoardCamera.InputEnabled, Is.False);

            controller.CardHand.OnPointerClick(new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Right
            });
            Assert.That(controller.CardPlayState, Is.Null);
            Assert.That(controller.CardHand.InteractionState, Is.EqualTo(CardHandInteractionState.Idle));
            Assert.That(controller.CardHand.gameObject.activeSelf, Is.True);
            Assert.That(controller.BoardCamera.InputEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator TargetAndTimelinePreview_DoNotMutateUntilCommitAndSurviveOrbitChanges()
        {
            yield return LoadSlice();
            var controller = GetController();

            Assert.That(controller.TimelineOccupiedCellCount, Is.EqualTo(1), "Enemy intent is the only initial action.");
            Assert.That(controller.SelectCard(VerticalSliceController.LightingCardId), Is.True);
            Assert.That(controller.SelectTarget(VerticalSliceController.TargetId), Is.True);
            Assert.That(controller.CardPlayState, Is.EqualTo(CardPlaySessionState.TargetSelected));
            Assert.That(controller.CardHand.InteractionState, Is.EqualTo(CardHandInteractionState.Targeting));
            Assert.That(controller.BoardRangePreview.ActiveCoordinates,
                Is.EqualTo(new[] { new HexCoord(1, 0), new HexCoord(2, 0) }));
            Assert.That(controller.BoardRangePreview.MissingCoordinates,
                Is.EqualTo(new[] { new HexCoord(3, 0) }));

            foreach (var yaw in new[] { 0f, 90f, 180f, 270f })
            {
                controller.SetBoardView(yaw);
                Assert.That(controller.BoardRangePreview.ActiveCoordinates,
                    Is.EqualTo(new[] { new HexCoord(1, 0), new HexCoord(2, 0) }));
            }

            Assert.That(controller.PreviewTimelineSelected(2, 1), Is.False);
            Assert.That(controller.TimelinePreview.IsValid, Is.False);
            Assert.That(controller.TimelineOccupiedCellCount, Is.EqualTo(1));

            Assert.That(controller.PreviewTimelineSelected(0, 0), Is.True);
            Assert.That(controller.TimelinePreview.IsValid, Is.True);
            Assert.That(controller.TimelineOccupiedCellCount, Is.EqualTo(1));
            Assert.That(controller.CardHand.InteractionState, Is.EqualTo(CardHandInteractionState.Scheduling));

            Assert.That(controller.TryPlaceSelected(0, 0), Is.True);
            Assert.That(controller.TimelineOccupiedCellCount, Is.EqualTo(2));
            Assert.That(controller.BoardRangePreview.ActiveCoordinates, Is.Empty);
            Assert.That(controller.TimelinePreview.ActiveCoordinates, Is.Empty);
            Assert.That(controller.CardHand.gameObject.activeSelf, Is.False);
            Assert.That(controller.BoardCamera.InputEnabled, Is.True);

            var snapshot = controller.ResolveTimeline();
            Assert.That(snapshot.TargetHpAfter, Is.Zero);
            Assert.That(snapshot.EnemyIntentResolved, Is.True);
        }

        [UnityTest]
        public IEnumerator CardArtwork_BlocksWorldInputAndKeepsOriginalAspect()
        {
            yield return LoadSlice();
            var controller = GetController();
            Canvas.ForceUpdateCanvases();

            var artwork = controller.CardHand.Artwork;
            Assert.That(artwork, Is.Not.Null);
            Assert.That(artwork.preserveAspect, Is.True);
            Assert.That(artwork.sprite.texture.width, Is.EqualTo(1135));
            Assert.That(artwork.sprite.texture.height, Is.EqualTo(1590));

            var worldCenter = artwork.rectTransform.TransformPoint(artwork.rectTransform.rect.center);
            var screenCenter = RectTransformUtility.WorldToScreenPoint(controller.SceneCamera, worldCenter);
            Assert.That(controller.IsScreenPointOverInterface(screenCenter), Is.True);
            Assert.That(controller.TrySelectWorldAtScreenPoint(screenCenter), Is.False);
            Assert.That(controller.SelectedTile, Is.Null);
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
