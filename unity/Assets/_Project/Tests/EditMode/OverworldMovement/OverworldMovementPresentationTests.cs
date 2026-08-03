using System.Collections.Generic;
using NUnit.Framework;
using TimeKey.Application.Overworld;
using TimeKey.Application.SceneFlow;
using TimeKey.Domain.Overworld;
using TimeKey.Domain.OverworldMovement;
using TimeKey.Presentation.OverworldMovement;
using TimeKey.Presentation.Theming;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimeKey.Tests.EditMode.OverworldMovement
{
    public sealed class OverworldMovementPresentationTests
    {
        private const string PrefabPath =
            "Assets/_Project/Prefabs/Shell/OutOfBattleShell.prefab";

        [Test]
        public void FormalPrefab_ContainsDynamicMapHostsAndThemeScope()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<UiThemeScope>(), Is.Not.Null);
            var presenter = prefab.GetComponent<OverworldMovementPresenter>();
            Assert.That(presenter, Is.Not.Null);
            Assert.That(prefab.transform.Find("GateDCanvas/RoomRevealLayer/MapViewport/MapHost/EdgeHost"),
                Is.Not.Null);
            Assert.That(prefab.transform.Find("GateDCanvas/RoomRevealLayer/MapViewport/MapHost/PlayerMarker"),
                Is.Not.Null);
            var presenterData = new SerializedObject(presenter);
            var nodeViews = presenterData.FindProperty("nodeViews");
            Assert.That(nodeViews.arraySize, Is.EqualTo(0));
            Assert.That(
                presenterData.FindProperty("nodePrefab").objectReferenceValue,
                Is.Not.Null);
            Assert.That(
                presenterData.FindProperty("edgeHost").objectReferenceValue,
                Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<UiThemeBinder>(true).Length, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void AuthoritativeBind_InstantiatesExactGeneratedMapAndNodeStates()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var presenter = instance.GetComponent<OverworldMovementPresenter>();
                var start = new RunStartPayload(
                    RunStartKind.SeedGame,
                    "gate-c-map-bind",
                    731,
                    "731",
                    1,
                    1,
                    1,
                    100,
                    "silver-character",
                    new[] { "lighting", "recover" });
                var run = new OverworldRunApplication(
                    start,
                    new OverworldMapGenerationConfig(1, 4, 3, 3),
                    finalChapter: 3);

                presenter.BindAuthoritativeMap(run.Map, run.ChapterSnapshot);

                var views = presenter.GetComponentsInChildren<OverworldNodeView>(true);
                var identities = new HashSet<string>();
                var currentCount = 0;
                var availableCount = 0;
                foreach (var view in views)
                {
                    identities.Add(view.Id.Value);
                    currentCount += view.State == OverworldNodeVisualState.Current ? 1 : 0;
                    availableCount += view.State == OverworldNodeVisualState.Available ? 1 : 0;
                }

                var expected = new HashSet<string>();
                foreach (var node in run.Map.Nodes)
                {
                    expected.Add(node.Id.Value);
                }

                Assert.That(views.Length, Is.EqualTo(run.Map.Nodes.Count));
                Assert.That(identities, Is.EquivalentTo(expected));
                Assert.That(currentCount, Is.EqualTo(1));
                Assert.That(availableCount, Is.GreaterThan(0));
                Assert.That(
                    instance.transform.Find(
                        "GateDCanvas/RoomRevealLayer/MapViewport/MapHost/EdgeHost")
                        .childCount,
                    Is.EqualTo(run.Map.Edges.Count));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void InputController_UsesThresholdAndLockForPanAndZoom()
        {
            var viewport = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(OverworldMapInputController));
            var map = new GameObject("Map", typeof(RectTransform));
            map.transform.SetParent(viewport.transform, false);
            var controller = viewport.GetComponent<OverworldMapInputController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("mapHost").objectReferenceValue = map.GetComponent<RectTransform>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            try
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
                try
                {
                    var pointer = new PointerEventData(eventSystem.GetComponent<EventSystem>())
                    {
                        button = PointerEventData.InputButton.Left,
                        position = Vector2.zero
                    };
                    controller.OnBeginDrag(pointer);
                    pointer.position = new Vector2(3f, 4f);
                    controller.OnDrag(pointer);
                    Assert.That(controller.ShouldSuppressClick, Is.False);
                    pointer.position = new Vector2(8f, 0f);
                    controller.OnDrag(pointer);
                    Assert.That(controller.ShouldSuppressClick, Is.True);
                    Assert.That(map.GetComponent<RectTransform>().anchoredPosition.x, Is.EqualTo(8f));
                    controller.OnEndDrag(pointer);

                    controller.OnScroll(new PointerEventData(eventSystem.GetComponent<EventSystem>()));
                    controller.SetInteractionLocked(true);
                    var before = map.GetComponent<RectTransform>().anchoredPosition;
                    controller.OnBeginDrag(pointer);
                    pointer.position = new Vector2(90f, 90f);
                    controller.OnDrag(pointer);
                    Assert.That(map.GetComponent<RectTransform>().anchoredPosition, Is.EqualTo(before));
                }
                finally
                {
                    Object.DestroyImmediate(eventSystem);
                }
            }
            finally
            {
                Object.DestroyImmediate(viewport);
            }
        }

        [Test]
        public void ThemeAsset_UsesSilverAndNineSliceSprite()
        {
            var theme = AssetDatabase.LoadAssetAtPath<TimeKeyUiTheme>(
                "Assets/_Project/Resources/UIThemes/TimeKeyDefaultUiTheme.asset");
            Assert.That(theme, Is.Not.Null);
            Assert.That(theme.Styles[0].frame.backgroundSprite, Is.Not.Null);
            Assert.That(theme.Styles[0].frame.backgroundSprite.border, Is.Not.EqualTo(Vector4.zero));
        }
    }
}
