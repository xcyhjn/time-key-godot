using System.Collections;
using System.Linq;
using NUnit.Framework;
using TimeKey.Domain;
using TimeKey.Presentation;
using TimeKey.Presentation.Cards;
using TimeKey.Presentation.Localization;
using TimeKey.Presentation.Occupants;
using TimeKey.Presentation.Targeting;
using TimeKey.Presentation.Terrain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
        public IEnumerator SerializedScene_DisableEnableAndRepeatedBuildDoNotDuplicateRuntimeContent()
        {
            yield return LoadSlice();
            var controller = GetController();
            var root = controller.gameObject;
            var stableChildCount = root.transform.childCount;

            controller.enabled = false;
            yield return null;
            controller.enabled = true;
            yield return null;
            controller.enabled = false;
            yield return null;
            controller.enabled = true;
            yield return null;
            controller.BuildSceneGraph();

            Assert.That(root.transform.childCount, Is.EqualTo(stableChildCount));
            Assert.That(controller.BoardTileCount, Is.EqualTo(19));
            Assert.That(controller.TimelineSlotCount, Is.EqualTo(36));
            Assert.That(controller.TimelineOccupiedCellCount, Is.EqualTo(2));
            Assert.That(Object.FindObjectsByType<BoardTileView>(FindObjectsSortMode.None).Length, Is.EqualTo(19));
            Assert.That(Object.FindObjectsByType<WorldTargetView>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator OrbitCamera_ClampsBoundsAndKeepsTargetSelectableAtCardinalAngles()
        {
            yield return LoadSlice();
            var controller = GetController();
            AdvanceToSecondHand(controller);
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
            AdvanceToSecondHand(controller);

            Assert.That(controller.SelectCard(VerticalSliceController.LightingCardId), Is.True);
            Assert.That(controller.SelectTarget(VerticalSliceController.TargetId), Is.True);
            Assert.That(controller.TryPlaceSelected(0, 0), Is.True);

            var snapshot = controller.ResolveTimeline();

            Assert.That(controller.CurrentTargetHp, Is.Zero);
            Assert.That(controller.EnemyIntentResolved, Is.False);
            Assert.That(snapshot.TargetHpBefore, Is.EqualTo(100));
            Assert.That(snapshot.TargetHpAfter, Is.Zero);
            Assert.That(snapshot.ResolutionOrder.Select(item => item.CardId),
                Is.EqualTo(new[] { "lighting", "enemy-intent" }));
        }

        [UnityTest]
        public IEnumerator Recover_PublicScenePathRestoresTargetAndReportsOccupantResult()
        {
            yield return LoadSlice();
            var controller = GetController();

            Assert.That(controller.CurrentTargetHp, Is.EqualTo(10));
            Assert.That(controller.SelectCard("recover"), Is.True);
            Assert.That(controller.SelectTarget(VerticalSliceController.TargetId), Is.True);
            Assert.That(controller.TryPlaceSelected(0, 0), Is.True);

            var snapshot = controller.ResolveTimeline();

            Assert.That(controller.CurrentTargetHp, Is.EqualTo(100));
            Assert.That(snapshot.OccupantEffectResults.Count, Is.EqualTo(1));
            Assert.That(snapshot.OccupantEffectResults[0].EffectKind, Is.EqualTo(CardEffectKind.Recover));
            Assert.That(snapshot.OccupantEffectResults[0].Before.Hp, Is.EqualTo(10));
            Assert.That(snapshot.OccupantEffectResults[0].After.Hp, Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator Built_PublicScenePathCreatesTowerPrefabViewOnEmptyTile()
        {
            yield return LoadSlice();
            var controller = GetController();
            var coordinate = new HexCoord(0, 0);
            AdvanceToSecondHand(controller);

            Assert.That(controller.SelectCard("tower"), Is.True);
            Assert.That(controller.SelectEarthquakeTarget(coordinate), Is.True);
            Assert.That(controller.PreviewTimelineSelected(4, 0), Is.True);
            Assert.That(controller.TryPlaceSelected(4, 0), Is.True);

            var snapshot = controller.ResolveTimeline();

            Assert.That(snapshot.OccupantEffectResults, Has.Count.EqualTo(1));
            var tower = snapshot.OccupantEffectResults[0].After;
            Assert.That(tower.CreationId, Is.EqualTo("tower"));
            Assert.That(tower.Coordinate, Is.EqualTo(coordinate));
            Assert.That(tower.Hp, Is.EqualTo(100));
            var towerObject = GameObject.Find("Occupant-" + tower.RuntimeId);
            Assert.That(towerObject, Is.Not.Null);
            Assert.That(towerObject.GetComponent<CombatOccupantView>(), Is.Not.Null);
            Assert.That(towerObject.transform.Find("OriginalArt-tower"), Is.Not.Null);
            Assert.That(towerObject.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(towerObject.GetComponent<CombatOccupantView>().Hp, Is.EqualTo(50));
            Assert.That(
                GameObject.Find("TargetStatus").GetComponent<Text>().text,
                Is.EqualTo("高塔 | 生命 50"));
        }

        [UnityTest]
        public IEnumerator Poison_PublicScenePathShowsOriginalIconAndStackCount()
        {
            yield return LoadSlice();
            var controller = GetController();

            Assert.That(controller.SelectCard("poison"), Is.True);
            Assert.That(controller.SelectTarget(VerticalSliceController.TargetId), Is.True);
            Assert.That(controller.PreviewTimelineSelected(4, 0), Is.True);
            Assert.That(controller.TryPlaceSelected(4, 0), Is.True);

            var snapshot = controller.ResolveTimeline();

            Assert.That(snapshot.OccupantEffectResults, Has.Count.EqualTo(1));
            Assert.That(snapshot.OccupantEffectResults[0].Before.PoisonStacks, Is.Zero);
            Assert.That(snapshot.OccupantEffectResults[0].After.PoisonStacks, Is.EqualTo(2));
            var statusObject = GameObject.Find("PoisonStatus-" + VerticalSliceController.TargetId);
            Assert.That(statusObject, Is.Null);
            Assert.That(controller.CurrentTargetHp, Is.Zero);
            Assert.That(
                GameObject.Find("TargetStatus").GetComponent<Text>().text,
                Is.EqualTo("目标 01 | 生命 0"));
        }

        [UnityTest]
        public IEnumerator Wind_PublicScenePathClearsCompleteEnemyActionWithoutMapTarget()
        {
            yield return LoadSlice();
            var controller = GetController();

            Assert.That(controller.TimelineOccupiedCellCount, Is.EqualTo(2));
            Assert.That(controller.SelectCard("wind"), Is.True);
            Assert.That(controller.SelectedCardId, Is.EqualTo("wind"));
            var intentCell = controller.TimelineActions.Single().OccupiedCells[0];
            var clearOrigin = new TimelineCell(
                Mathf.Max(0, intentCell.X - 1),
                Mathf.Max(0, intentCell.Y - 1));
            Assert.That(
                controller.PreviewTimelineSelected(clearOrigin.X, clearOrigin.Y),
                Is.True);
            Assert.That(
                GameObject.Find("TargetStatus").GetComponent<Text>().text,
                Is.EqualTo(CombatChineseText.ClearHits(1)));
            Assert.That(GameObject.Find(
                    string.Format("Slot-{0}-{1}", intentCell.X, intentCell.Y))
                .GetComponent<TimelineCellView>().DisplayText,
                Is.EqualTo(CombatChineseText.ClearHit));

            Assert.That(controller.TryPlaceSelected(clearOrigin.X, clearOrigin.Y), Is.True);

            Assert.That(controller.TimelineOccupiedCellCount, Is.Zero);
            Assert.That(GameObject.Find(
                    string.Format("Slot-{0}-{1}", intentCell.X, intentCell.Y))
                .GetComponent<TimelineCellView>().DisplayText,
                Is.Not.EqualTo(CombatChineseText.ClearHit));
            Assert.That(
                GameObject.Find("TargetStatus").GetComponent<Text>().text,
                Is.EqualTo(CombatChineseText.ClearRemoved(1)));
            Assert.That(GameObject.Find("Resolve").GetComponent<Button>().interactable, Is.False);
        }

        [UnityTest]
        public IEnumerator Tornado_IsExcludedFromFrozenStarterDeck()
        {
            yield return LoadSlice();
            var controller = GetController();

            Assert.That(controller.SelectCard("tornado"), Is.False);
            Assert.That(controller.CardHandHost.Cards.All(card => card.StableId != "tornado"),
                Is.True);
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

            Assert.That(controller.SelectCard("poison"), Is.True);
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
        public IEnumerator InitialFiveCardHand_UsesFrozenInstancesAndRegisteredArtwork()
        {
            yield return LoadSlice();
            var controller = GetController();
            var host = controller.CardHandHost;

            Assert.That(host, Is.Not.Null);
            Assert.That(host.CardCount, Is.EqualTo(5));
            Assert.That(host.Cards.Select(card => card.StableId),
                Is.EqualTo(new[]
                {
                    "poison", "poison", "recover", "wind", "wind"
                }));
            Assert.That(host.Cards.Select(card => card.ViewId).Distinct().Count(), Is.EqualTo(5));
            foreach (var card in host.Cards)
            {
                Assert.That(card.Artwork.sprite, Is.Not.Null, card.StableId);
                Assert.That(card.Artwork.sprite.texture.width, Is.GreaterThan(0), card.StableId);
                Assert.That(card.Artwork.sprite.texture.height, Is.GreaterThan(0), card.StableId);
                Assert.That(card.Artwork.preserveAspect, Is.True);
            }

            var poisonCards = host.Cards.Where(card => card.StableId == "poison").ToArray();
            poisonCards[0].OnPointerClick(new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left
            });
            poisonCards[1].OnPointerClick(new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left
            });

            Assert.That(controller.SelectedCardId, Is.EqualTo("poison"));
            Assert.That(host.SelectedStableId, Is.EqualTo("poison"));
            Assert.That(host.SelectedViewId, Is.EqualTo(poisonCards[1].ViewId));
            Assert.That(poisonCards[0].InteractionState,
                Is.EqualTo(CardHandInteractionState.Idle));
            Assert.That(poisonCards[1].InteractionState,
                Is.EqualTo(CardHandInteractionState.Selected));
        }

        [UnityTest]
        public IEnumerator Earthquake_RaisesSevenRealColumnsAndKeepsCardinalSelection()
        {
            yield return LoadSlice();
            var controller = GetController();
            AdvanceToSecondHand(controller);
            var center = new HexCoord(0, 0);
            var range = new[]
            {
                new HexCoord(0, 0), new HexCoord(1, 0), new HexCoord(1, -1),
                new HexCoord(0, -1), new HexCoord(-1, 0), new HexCoord(-1, 1),
                new HexCoord(0, 1)
            };
            var beforeLayers = range.ToDictionary(coordinate => coordinate,
                coordinate => controller.GetTileColumn(coordinate).LayerCount);
            var centerTopBefore = controller.GetTileColumn(center).TopBounds.max.y;
            var targetYBefore = controller.TargetWorldPosition.y;

            Assert.That(controller.SelectCard(VerticalSliceController.EarthquakeCardId), Is.True);
            Assert.That(controller.SelectEarthquakeTarget(center), Is.True);
            Assert.That(controller.BoardRangePreview.ActiveCoordinates, Is.EquivalentTo(range));
            Assert.That(controller.BoardRangePreview.MissingCoordinates, Is.Empty);

            Assert.That(controller.PreviewTimelineSelected(11, 0), Is.False);
            var invalidOutline = GameObject.Find("Slot-11-0").GetComponent<Outline>();
            Assert.That(invalidOutline, Is.Not.Null);
            Assert.That(invalidOutline.enabled, Is.True);
            Assert.That(controller.TimelineOccupiedCellCount, Is.EqualTo(2));

            Assert.That(controller.PreviewTimelineSelected(0, 0), Is.True);
            Assert.That(controller.TimelinePreview.ActiveCoordinates,
                Is.EqualTo(new[] { new TimelineCell(0, 0), new TimelineCell(1, 0) }));
            Assert.That(controller.TryPlaceSelected(0, 0), Is.True);
            Assert.That(controller.TimelineOccupiedCellCount, Is.EqualTo(4));

            var snapshot = controller.ResolveTimeline();

            Assert.That(snapshot.EffectResults.Count, Is.EqualTo(7));
            foreach (var coordinate in range)
            {
                var column = controller.GetTileColumn(coordinate);
                Assert.That(column.LayerCount, Is.EqualTo(beforeLayers[coordinate] + 2));
                Assert.That(column.Blocks.Count, Is.EqualTo(column.LayerCount));
                Assert.That(column.Colliders.Count, Is.EqualTo(column.LayerCount));
                for (var layer = 0; layer < column.Blocks.Count; layer++)
                {
                    var block = column.Blocks[layer];
                    Assert.That(block.GetComponentInChildren<MeshFilter>(true), Is.Not.Null);
                    Assert.That(block.GetComponentInChildren<Renderer>(true), Is.Not.Null);
                    Assert.That(block.GetComponentInChildren<Collider>(true), Is.Not.Null);
                    Assert.That(block.transform.localPosition.y,
                        Is.EqualTo(layer * VerticalSliceController.HexBlockHeight).Within(0.001f));
                }
            }

            Assert.That(controller.GetTileColumn(center).TopBounds.max.y - centerTopBefore,
                Is.EqualTo(0.64f).Within(0.01f));
            Assert.That(controller.TargetWorldPosition.y - targetYBefore,
                Is.EqualTo(0.64f).Within(0.01f));

            yield return null;
            foreach (var yaw in new[] { 0f, 90f, 180f, 270f })
            {
                controller.SetBoardView(yaw);
                Physics.SyncTransforms();
                var bounds = controller.GetTileColumn(center).TopBounds;
                var selectionPoint = new Vector3(bounds.center.x, bounds.max.y - 0.02f, bounds.center.z);
                var screenPoint = controller.SceneCamera.WorldToScreenPoint(selectionPoint);
                Assert.That(controller.IsScreenPointOverInterface(screenPoint), Is.False);
                Assert.That(controller.TrySelectWorldAtScreenPoint(screenPoint), Is.True, "yaw=" + yaw);
                Assert.That(controller.SelectedTile.HasValue, Is.True);
                var selected = controller.SelectedTile.Value;
                Assert.That(selected.Q, Is.EqualTo(center.Q),
                    string.Format("yaw={0}, selected={1},{2}", yaw, selected.Q, selected.R));
                Assert.That(selected.R, Is.EqualTo(center.R),
                    string.Format("yaw={0}, selected={1},{2}", yaw, selected.Q, selected.R));
            }
        }

        [UnityTest]
        public IEnumerator TargetAndTimelinePreview_DoNotMutateUntilCommitAndSurviveOrbitChanges()
        {
            yield return LoadSlice();
            var controller = GetController();
            AdvanceToSecondHand(controller);

            Assert.That(controller.TimelineOccupiedCellCount, Is.EqualTo(2), "Enemy intent is the only initial action.");
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

            var occupied = controller.TimelineActions.Single().OccupiedCells[0];
            Assert.That(controller.PreviewTimelineSelected(occupied.X, occupied.Y), Is.False);
            Assert.That(controller.TimelinePreview.IsValid, Is.False);
            Assert.That(controller.TimelineOccupiedCellCount, Is.EqualTo(2));

            Assert.That(controller.PreviewTimelineSelected(0, 0), Is.True);
            Assert.That(controller.TimelinePreview.IsValid, Is.True);
            Assert.That(controller.TimelineOccupiedCellCount, Is.EqualTo(2));
            Assert.That(controller.CardHand.InteractionState, Is.EqualTo(CardHandInteractionState.Scheduling));

            Assert.That(controller.TryPlaceSelected(0, 0), Is.True);
            Assert.That(controller.TimelineOccupiedCellCount, Is.EqualTo(3));
            Assert.That(controller.BoardRangePreview.ActiveCoordinates, Is.Empty);
            Assert.That(controller.TimelinePreview.ActiveCoordinates, Is.Empty);
            Assert.That(controller.CardHand.gameObject.activeSelf, Is.True);
            Assert.That(
                controller.CardHand.InteractionState,
                Is.EqualTo(CardHandInteractionState.Disabled));
            Assert.That(controller.BoardCamera.InputEnabled, Is.True);

            var snapshot = controller.ResolveTimeline();
            Assert.That(snapshot.TargetHpAfter, Is.Zero);
            Assert.That(snapshot.EnemyIntentResolved, Is.False);
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

        private static void AdvanceToSecondHand(VerticalSliceController controller)
        {
            Assert.That(controller.SelectCard("recover"), Is.True);
            Assert.That(controller.SelectTarget(VerticalSliceController.TargetId), Is.True);
            Assert.That(controller.TryPlaceSelected(0, 0), Is.True);
            Assert.That(controller.ResolveTimeline(), Is.Not.Null);
            Assert.That(controller.BattleFlow.Phase, Is.EqualTo(2));
            Assert.That(controller.BattleFlow.Hand, Has.Count.EqualTo(5));
        }
    }
}
