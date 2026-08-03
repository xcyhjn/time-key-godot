using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Presentation;
using TimeKey.Presentation.Actions;
using TimeKey.Presentation.Localization;
using TimeKey.Presentation.Presenters;
using TimeKey.Presentation.Targeting;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.Actions
{
    public sealed class EffectFrameGeometryStabilityTests
    {
        private GameObject _root;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator TowerShape_DoesNotFillUnoccupiedBoundingBoxCells()
        {
            var rig = CreateRig(3, 2);
            var occupied = new[]
            {
                new TimelineCell(1, 1),
                new TimelineCell(0, 0),
                new TimelineCell(1, 0),
                new TimelineCell(2, 0)
            };
            var action = CreatePlayerAction(
                "cycle:geometry/action:tower",
                new TimelineCell(0, 0),
                occupied,
                "tower");
            using (var session = CreateSession(action))
            {
                rig.Presenter.Refresh(session.Current);
                yield return null;

                var frame = rig.ActionLayer.GetComponentsInChildren<TimelineActionFrame>(true)
                    .Single();
                var missingTopLeft = GetWorldRect(rig.Cells[new TimelineCell(0, 1)]);
                var missingTopRight = GetWorldRect(rig.Cells[new TimelineCell(2, 1)]);
                var missingInteriorLeft = Inset(missingTopLeft, 2f);
                var missingInteriorRight = Inset(missingTopRight, 2f);
                foreach (var image in frame.GetComponentsInChildren<Image>(true))
                {
                    if (!image.enabled || !image.gameObject.activeInHierarchy || image.color.a <= 0.01f)
                    {
                        continue;
                    }

                    var graphicRect = GetWorldRect(image.rectTransform);
                    Assert.That(
                        graphicRect.Overlaps(missingInteriorLeft),
                        Is.False,
                        "A visible action-frame image fills Tower's missing top-left cell.");
                    Assert.That(
                        graphicRect.Overlaps(missingInteriorRight),
                        Is.False,
                        "A visible action-frame image fills Tower's missing top-right cell.");
                }
            }
        }

        [UnityTest]
        public IEnumerator PoisonShape_DoesNotFillUnoccupiedTopRightCell()
        {
            var rig = CreateRig(3, 2);
            var occupied = new[]
            {
                new TimelineCell(0, 1),
                new TimelineCell(1, 1),
                new TimelineCell(0, 0),
                new TimelineCell(1, 0),
                new TimelineCell(2, 0)
            };
            var action = CreatePlayerAction(
                "cycle:geometry/action:poison",
                new TimelineCell(0, 0),
                occupied,
                "poison");
            using (var session = CreateSession(action))
            {
                rig.Presenter.Refresh(session.Current);
                yield return null;

                var frame = rig.ActionLayer.GetComponentsInChildren<TimelineActionFrame>(true)
                    .Single();
                var missingInterior = Inset(
                    GetWorldRect(rig.Cells[new TimelineCell(2, 1)]),
                    2f);
                foreach (var image in frame.GetComponentsInChildren<Image>(true))
                {
                    if (!image.enabled || !image.gameObject.activeInHierarchy || image.color.a <= 0.01f)
                    {
                        continue;
                    }

                    Assert.That(
                        GetWorldRect(image.rectTransform).Overlaps(missingInterior),
                        Is.False,
                        "A visible action-frame image fills Poison's missing top-right cell.");
                }
            }
        }

        [UnityTest]
        public IEnumerator ActionFrame_ReanchorsAfterSameInstanceResizeSequence()
        {
            var rig = CreateRig(4, 1);
            var occupied = new[]
            {
                new TimelineCell(1, 0),
                new TimelineCell(2, 0)
            };
            var action = CreatePlayerAction(
                "cycle:geometry/action:resize",
                new TimelineCell(0, 0),
                occupied,
                "lighting");
            using (var session = CreateSession(action))
            {
                rig.Presenter.Refresh(session.Current);
                yield return null;
                var frame = rig.ActionLayer.GetComponentsInChildren<TimelineActionFrame>(true)
                    .Single();

                foreach (var viewport in new[]
                         {
                             new ResizeStep(1280f, 720f, 0f, 0f),
                             new ResizeStep(2560f, 1080f, 130f, 38f),
                             new ResizeStep(1920f, 1080f, -90f, -24f)
                         })
                {
                    rig.Resize(viewport);
                    Canvas.ForceUpdateCanvases();
                    yield return null;
                    AssertFrameMatchesCells(frame, rig.Cells, occupied, viewport);
                }
            }
        }

        private TimelineRig CreateRig(int width, int height)
        {
            _root = new GameObject("EffectFrameGeometryTestRig", typeof(RectTransform), typeof(Canvas));
            _root.SetActive(false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasRect = _root.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1280f, 720f);

            var timelineRect = CreateRect("Timeline", _root.transform);
            timelineRect.anchorMin = new Vector2(0.5f, 0.5f);
            timelineRect.anchorMax = new Vector2(0.5f, 0.5f);
            timelineRect.sizeDelta = new Vector2(900f, 240f);
            var placement = timelineRect.gameObject.AddComponent<TimelinePlacementPreview>();
            var clear = timelineRect.gameObject.AddComponent<ClearTimelinePreview>();
            var presenter = timelineRect.gameObject.AddComponent<TimelinePresenter>();
            var cells = new List<TimelineCellView>(width * height);
            var byCoordinate = new Dictionary<TimelineCell, TimelineCellView>();
            for (var row = 0; row < height; row++)
            {
                for (var column = 0; column < width; column++)
                {
                    var cellRect = CreateRect("Cell-" + column + "-" + row, timelineRect);
                    cellRect.anchorMin = new Vector2(0.5f, 0.5f);
                    cellRect.anchorMax = new Vector2(0.5f, 0.5f);
                    cellRect.sizeDelta = new Vector2(80f, 42f);
                    cellRect.anchoredPosition = new Vector2(
                        ((column - ((width - 1) * 0.5f)) * 84f),
                        ((row - ((height - 1) * 0.5f)) * 48f));
                    var image = cellRect.gameObject.AddComponent<Image>();
                    image.color = new Color(0.16f, 0.19f, 0.20f, 1f);
                    var button = cellRect.gameObject.AddComponent<Button>();
                    button.targetGraphic = image;
                    var label = CreateRect("Label", cellRect).gameObject.AddComponent<Text>();
                    Stretch(label.rectTransform);
                    label.text = (column + 1).ToString("00");
                    var cell = cellRect.gameObject.AddComponent<TimelineCellView>();
                    SetField(cell, "column", column);
                    SetField(cell, "row", row);
                    SetField(cell, "button", button);
                    SetField(cell, "label", label);
                    cell.CaptureCurrentAsDefault();
                    cells.Add(cell);
                    byCoordinate.Add(cell.Coordinate, cell);
                }
            }

            var actionLayer = CreateRect("ActionLayer", timelineRect);
            Stretch(actionLayer);
            var actionLayout = actionLayer.gameObject.AddComponent<LayoutElement>();
            actionLayout.ignoreLayout = true;
            var frameTemplate = CreateFrameTemplate(_root.transform);
            SetField(presenter, "timelinePreview", placement);
            SetField(presenter, "clearTimelinePreview", clear);
            SetField(presenter, "timelineCells", cells);
            SetField(presenter, "actionLayer", actionLayer);
            SetField(presenter, "actionFramePrefab", frameTemplate);
            _root.SetActive(true);
            presenter.Bind();
            Canvas.ForceUpdateCanvases();
            return new TimelineRig(canvasRect, timelineRect, presenter, actionLayer, byCoordinate);
        }

        private static TimelineActionFrame CreateFrameTemplate(Transform parent)
        {
            var frameObject = CreateRect("TimelineActionFrameTemplate", parent).gameObject;
            var background = frameObject.AddComponent<Image>();
            background.raycastTarget = false;
            var canvasGroup = frameObject.AddComponent<CanvasGroup>();
            var outline = frameObject.AddComponent<Outline>();
            var frame = frameObject.AddComponent<TimelineActionFrame>();
            var stripe = CreateRect("Stripe", frameObject.transform).gameObject.AddComponent<Image>();
            stripe.raycastTarget = false;
            var label = CreateRect("Label", frameObject.transform).gameObject.AddComponent<Text>();
            label.raycastTarget = false;
            var badge = CreateRect("Badge", frameObject.transform).gameObject.AddComponent<Text>();
            badge.raycastTarget = false;
            var cellTemplate = CreateRect("CellBackgroundTemplate", frameObject.transform)
                .gameObject.AddComponent<Image>();
            cellTemplate.raycastTarget = false;
            cellTemplate.gameObject.SetActive(false);
            var edgeTemplate = CreateRect("EdgeTemplate", frameObject.transform)
                .gameObject.AddComponent<Image>();
            edgeTemplate.raycastTarget = false;
            edgeTemplate.gameObject.SetActive(false);
            SetField(frame, "background", background);
            SetField(frame, "stripe", stripe);
            SetField(frame, "outline", outline);
            SetField(frame, "label", label);
            SetField(frame, "badge", badge);
            SetField(frame, "canvasGroup", canvasGroup);
            SetField(frame, "cellBackgroundTemplate", cellTemplate);
            SetField(frame, "edgeTemplate", edgeTemplate);
            return frame;
        }

        private static TimelineAction CreatePlayerAction(
            string actionId,
            TimelineCell origin,
            IReadOnlyList<TimelineCell> shape,
            string stableId)
        {
            return new TimelineAction(
                new TimelineActionIdentity(actionId),
                TimelineActorKind.Player,
                0,
                null,
                null,
                stableId,
                "target-01",
                origin,
                shape,
                new HexCoord(0, 0),
                new[] { new CardEffect(CardEffectKind.Elevation, 2) },
                new[] { new HexCoord(0, 0) });
        }

        private static CombatApplicationSession CreateSession(params TimelineAction[] actions)
        {
            var board = new CombatBoardState();
            board.AddTile(new HexCoord(0, 0), 1);
            return new CombatApplicationSession(
                new EmptyCatalog(),
                new CombatSliceState("target-01", 10, 731, board),
                new TimelineGrid(),
                actions,
                actionDisplayCatalog: CombatChineseActionDisplayCatalog.Instance);
        }

        private static void AssertFrameMatchesCells(
            TimelineActionFrame frame,
            IReadOnlyDictionary<TimelineCell, TimelineCellView> cells,
            IReadOnlyList<TimelineCell> occupied,
            ResizeStep viewport)
        {
            var expected = UnionWorldRect(cells, occupied);
            var actual = GetWorldRect((RectTransform)frame.transform);
            Assert.That(
                Vector2.Distance(actual.center, expected.center),
                Is.LessThan(1f),
                "Action frame did not re-anchor after resize to " + viewport + ".");
            Assert.That(
                Mathf.Abs(actual.width - (expected.width + 4f)),
                Is.LessThan(1f),
                "Action frame width did not follow resized occupied cells.");
            Assert.That(
                Mathf.Abs(actual.height - (expected.height + 4f)),
                Is.LessThan(1f),
                "Action frame height did not follow resized occupied cells.");
        }

        private static Rect UnionWorldRect(
            IReadOnlyDictionary<TimelineCell, TimelineCellView> cells,
            IReadOnlyList<TimelineCell> occupied)
        {
            var result = GetWorldRect(cells[occupied[0]]);
            for (var index = 1; index < occupied.Count; index++)
            {
                result = Union(result, GetWorldRect(cells[occupied[index]]));
            }

            return result;
        }

        private static Rect Union(Rect left, Rect right)
        {
            return Rect.MinMaxRect(
                Mathf.Min(left.xMin, right.xMin),
                Mathf.Min(left.yMin, right.yMin),
                Mathf.Max(left.xMax, right.xMax),
                Mathf.Max(left.yMax, right.yMax));
        }

        private static Rect GetWorldRect(Component component)
        {
            return GetWorldRect((RectTransform)component.transform);
        }

        private static Rect GetWorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(
                corners[0].x,
                corners[0].y,
                corners[2].x,
                corners[2].y);
        }

        private static Rect Inset(Rect rect, float inset)
        {
            return Rect.MinMaxRect(
                rect.xMin + inset,
                rect.yMin + inset,
                rect.xMax - inset,
                rect.yMax - inset);
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing serialized field " + fieldName + ".");
            field.SetValue(target, value);
        }

        private sealed class EmptyCatalog : ICardCatalog
        {
            public IReadOnlyList<CardDefinition> Cards => Array.Empty<CardDefinition>();

            public bool TryGet(string stableId, out CardDefinition card)
            {
                card = null;
                return false;
            }
        }

        private readonly struct ResizeStep
        {
            public ResizeStep(float width, float height, float xOffset, float yOffset)
            {
                Width = width;
                Height = height;
                XOffset = xOffset;
                YOffset = yOffset;
            }

            public float Width { get; }
            public float Height { get; }
            public float XOffset { get; }
            public float YOffset { get; }

            public override string ToString()
            {
                return string.Format("{0:0}x{1:0} ({2:0},{3:0})", Width, Height, XOffset, YOffset);
            }
        }

        private sealed class TimelineRig
        {
            public TimelineRig(
                RectTransform canvas,
                RectTransform timeline,
                TimelinePresenter presenter,
                RectTransform actionLayer,
                IReadOnlyDictionary<TimelineCell, TimelineCellView> cells)
            {
                Canvas = canvas;
                Timeline = timeline;
                Presenter = presenter;
                ActionLayer = actionLayer;
                Cells = cells;
            }

            public RectTransform Canvas { get; }
            public RectTransform Timeline { get; }
            public TimelinePresenter Presenter { get; }
            public RectTransform ActionLayer { get; }
            public IReadOnlyDictionary<TimelineCell, TimelineCellView> Cells { get; }

            public void Resize(ResizeStep step)
            {
                Canvas.sizeDelta = new Vector2(step.Width, step.Height);
                Timeline.sizeDelta = new Vector2(step.Width * 0.70f, step.Height * 0.28f);
                foreach (var pair in Cells)
                {
                    var rect = (RectTransform)pair.Value.transform;
                    rect.anchoredPosition += new Vector2(step.XOffset, step.YOffset);
                }
            }
        }
    }
}
