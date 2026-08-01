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
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.Actions
{
    public sealed class TimelineActionFramePresentationTests
    {
        private GameObject _root;
        private EventSystem _eventSystem;

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
        public IEnumerator Refresh_MultiCellActionCreatesOneFrameAndEveryCellMapsSameSnapshot()
        {
            var rig = CreateRig(4);
            var action = CreatePlayerAction(
                "cycle:7/action:1",
                new TimelineCell(0, 0),
                new[] { new TimelineCell(0, 0), new TimelineCell(1, 0) });
            using (var session = CreateSession(Array.Empty<CardDefinition>(), action))
            {
                var state = session.Current;
                rig.Presenter.Refresh(state);
                yield return null;

                var frames = rig.ActionLayer.GetComponentsInChildren<TimelineActionFrame>(true);
                Assert.That(frames, Has.Length.EqualTo(1));
                Assert.That(frames[0].ActionId, Is.EqualTo(action.ActionId));
                Assert.That(frames[0].Snapshot, Is.SameAs(state.TimelineActions[0]));

                var entered = new List<TimelineActionPresentationSnapshot>();
                var exited = new List<TimelineActionPresentationSnapshot>();
                rig.Presenter.ActionHovered += (snapshot, isEntering) =>
                {
                    (isEntering ? entered : exited).Add(snapshot);
                };

                foreach (var coordinate in new[]
                         {
                             new TimelineCell(0, 0),
                             new TimelineCell(1, 0)
                         })
                {
                    rig.Cells[coordinate].OnPointerEnter(Pointer());
                    Assert.That(entered.Last(), Is.SameAs(state.TimelineActions[0]));
                    Assert.That(frames[0].GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
                    rig.Cells[coordinate].OnPointerExit(Pointer());
                    Assert.That(exited.Last(), Is.SameAs(state.TimelineActions[0]));
                }

                Assert.That(entered, Has.Count.EqualTo(2));
                Assert.That(exited, Has.Count.EqualTo(2));
            }
        }

        [UnityTest]
        public IEnumerator Refresh_PlayerAndUnsupportedEnemyHaveDistinctLabelsBadgesAndStyles()
        {
            var rig = CreateRig(5);
            var player = CreatePlayerAction(
                "cycle:8/action:0",
                new TimelineCell(0, 0),
                new[] { new TimelineCell(0, 0), new TimelineCell(1, 0) });
            var enemy = CreateUnsupportedEnemyAction(
                "cycle:8/action:1",
                new TimelineCell(3, 0));
            using (var session = CreateSession(Array.Empty<CardDefinition>(), player, enemy))
            {
                rig.Presenter.Refresh(session.Current);
                yield return null;

                var frames = rig.ActionLayer.GetComponentsInChildren<TimelineActionFrame>(true);
                Assert.That(frames, Has.Length.EqualTo(2));
                var playerFrame = frames.Single(
                    frame => frame.Snapshot.ActorKind == TimelineActorKind.Player);
                var enemyFrame = frames.Single(
                    frame => frame.Snapshot.ActorKind == TimelineActorKind.Enemy);

                Assert.That(TextChild(playerFrame, "Label").text, Is.EqualTo("雷击"));
                Assert.That(TextChild(playerFrame, "Badge").text, Is.EqualTo("玩家"));
                Assert.That(ImageChild(playerFrame, "Stripe").gameObject.activeSelf, Is.False);

                Assert.That(enemyFrame.Snapshot.Validity, Is.EqualTo(TimelineActionValidity.Unsupported));
                Assert.That(
                    enemyFrame.Snapshot.InvalidReason,
                    Is.EqualTo(TimelineActionInvalidReason.UnsupportedSourceCommand));
                Assert.That(TextChild(enemyFrame, "Label").text, Is.EqualTo("意图"));
                Assert.That(TextChild(enemyFrame, "Badge").text, Is.EqualTo("敌方 / 无效果"));
                Assert.That(ImageChild(enemyFrame, "Stripe").gameObject.activeSelf, Is.True);
                Assert.That(
                    enemyFrame.GetComponent<Outline>().effectColor,
                    Is.Not.EqualTo(playerFrame.GetComponent<Outline>().effectColor));
                Assert.That(
                    enemyFrame.GetComponent<Image>().color,
                    Is.Not.EqualTo(playerFrame.GetComponent<Image>().color));
            }
        }

        [UnityTest]
        public IEnumerator ApplyClearResult_RemovesFrameAndCellIdentityMappingWithoutHoverResidue()
        {
            var rig = CreateRig(4);
            var action = CreatePlayerAction(
                "cycle:9/action:0",
                new TimelineCell(0, 0),
                new[] { new TimelineCell(0, 0), new TimelineCell(1, 0) });
            var clear = new CardDefinition(
                "wind",
                3,
                new[]
                {
                    new CardEffect(
                        CardEffectKind.Clear,
                        new[] { new TimelineCell(0, 0), new TimelineCell(1, 0) })
                },
                new[] { new HexCoord(0, 0) },
                Array.Empty<TimelineCell>());
            using (var session = CreateSession(new[] { clear }, action))
            {
                rig.Presenter.Refresh(session.Current);
                Assert.That(
                    rig.ActionLayer.GetComponentsInChildren<TimelineActionFrame>(true),
                    Has.Length.EqualTo(1));

                Assert.That(session.SelectCard("wind").Succeeded, Is.True);
                Assert.That(session.PreviewClear(new TimelineCell(0, 0)).Succeeded, Is.True);
                var commit = session.CommitClear();
                Assert.That(commit.Succeeded, Is.True);
                Assert.That(commit.ClearResult.RemovedActions, Has.Count.EqualTo(1));
                rig.Presenter.ApplyClearResult(commit.ClearResult);

                var actionHoverCount = 0;
                var emptyCellHoverCount = 0;
                rig.Presenter.ActionHovered += (_, __) => actionHoverCount++;
                rig.Presenter.TimelinePreviewRequested += _ => emptyCellHoverCount++;
                rig.Cells[new TimelineCell(0, 0)].OnPointerEnter(Pointer());
                Assert.That(actionHoverCount, Is.Zero);
                Assert.That(emptyCellHoverCount, Is.EqualTo(1));
                Assert.That(rig.Cells[new TimelineCell(0, 0)].DisplayText, Is.EqualTo("01"));
                Assert.That(rig.Cells[new TimelineCell(1, 0)].DisplayText, Is.EqualTo("02"));

                yield return null;
                Assert.That(
                    rig.ActionLayer.GetComponentsInChildren<TimelineActionFrame>(true),
                    Is.Empty);
            }
        }

        private TimelineRig CreateRig(int width)
        {
            _root = new GameObject("TimelineActionFrameTestRig", typeof(RectTransform), typeof(Canvas));
            _root.SetActive(false);
            _root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            if (EventSystem.current == null)
            {
                var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystemObject.transform.SetParent(_root.transform, false);
                _eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }
            else
            {
                _eventSystem = EventSystem.current;
            }

            var timelineObject = CreateRect("Timeline", _root.transform).gameObject;
            var placement = timelineObject.AddComponent<TimelinePlacementPreview>();
            var clear = timelineObject.AddComponent<ClearTimelinePreview>();
            var presenter = timelineObject.AddComponent<TimelinePresenter>();
            var cells = new List<TimelineCellView>(width);
            var cellsByCoordinate = new Dictionary<TimelineCell, TimelineCellView>();
            for (var column = 0; column < width; column++)
            {
                var cellObject = CreateRect("Cell-" + column, timelineObject.transform).gameObject;
                var cellRect = cellObject.GetComponent<RectTransform>();
                cellRect.sizeDelta = new Vector2(72f, 46f);
                cellRect.anchoredPosition = new Vector2((column * 76f) + 36f, 24f);
                var image = cellObject.AddComponent<Image>();
                image.color = new Color(0.16f, 0.19f, 0.20f, 1f);
                var button = cellObject.AddComponent<Button>();
                button.targetGraphic = image;
                var label = CreateRect("Label", cellObject.transform).gameObject.AddComponent<Text>();
                Stretch(label.rectTransform);
                label.text = (column + 1).ToString("00");
                var cell = cellObject.AddComponent<TimelineCellView>();
                SetField(cell, "column", column);
                SetField(cell, "row", 0);
                SetField(cell, "button", button);
                SetField(cell, "label", label);
                cell.CaptureCurrentAsDefault();
                cells.Add(cell);
                cellsByCoordinate.Add(cell.Coordinate, cell);
            }

            var actionLayer = CreateRect("ActionLayer", timelineObject.transform);
            Stretch(actionLayer);
            var frameTemplate = CreateFrameTemplate(_root.transform);
            SetField(presenter, "timelinePreview", placement);
            SetField(presenter, "clearTimelinePreview", clear);
            SetField(presenter, "timelineCells", cells);
            SetField(presenter, "actionLayer", actionLayer);
            SetField(presenter, "actionFramePrefab", frameTemplate);
            _root.SetActive(true);
            presenter.Bind();
            Canvas.ForceUpdateCanvases();
            return new TimelineRig(presenter, actionLayer, cellsByCoordinate);
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
            SetField(frame, "background", background);
            SetField(frame, "stripe", stripe);
            SetField(frame, "outline", outline);
            SetField(frame, "label", label);
            SetField(frame, "badge", badge);
            SetField(frame, "canvasGroup", canvasGroup);
            return frame;
        }

        private static TimelineAction CreatePlayerAction(
            string actionId,
            TimelineCell origin,
            IReadOnlyList<TimelineCell> shape)
        {
            return new TimelineAction(
                new TimelineActionIdentity(actionId),
                TimelineActorKind.Player,
                0,
                null,
                null,
                "lighting",
                "target-01",
                origin,
                shape,
                new HexCoord(0, 0),
                new[] { new CardEffect(CardEffectKind.Damage, 10) },
                new[] { new HexCoord(0, 0) });
        }

        private static TimelineAction CreateUnsupportedEnemyAction(
            string actionId,
            TimelineCell origin)
        {
            return new TimelineAction(
                new TimelineActionIdentity(actionId),
                TimelineActorKind.Enemy,
                10,
                "enemy-village-01",
                new HexCoord(1, 0),
                "enemy-intent",
                "target-01",
                origin,
                new[] { new TimelineCell(0, 0) },
                new HexCoord(0, 0),
                Array.Empty<CardEffect>(),
                new[] { new HexCoord(0, 0) });
        }

        private static CombatApplicationSession CreateSession(
            IReadOnlyList<CardDefinition> cards,
            params TimelineAction[] actions)
        {
            var board = new CombatBoardState();
            board.AddTile(new HexCoord(0, 0), 1);
            board.AddTile(new HexCoord(1, 0), 1);
            return new CombatApplicationSession(
                new TestCatalog(cards),
                new CombatSliceState("target-01", 10, 731, board),
                new TimelineGrid(),
                actions,
                actionDisplayCatalog: CombatChineseActionDisplayCatalog.Instance);
        }

        private PointerEventData Pointer()
        {
            return new PointerEventData(_eventSystem);
        }

        private static Text TextChild(Component root, string name)
        {
            return root.GetComponentsInChildren<Text>(true).Single(text => text.name == name);
        }

        private static Image ImageChild(Component root, string name)
        {
            return root.GetComponentsInChildren<Image>(true).Single(image => image.name == name);
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

        private sealed class TimelineRig
        {
            public TimelineRig(
                TimelinePresenter presenter,
                RectTransform actionLayer,
                IReadOnlyDictionary<TimelineCell, TimelineCellView> cells)
            {
                Presenter = presenter;
                ActionLayer = actionLayer;
                Cells = cells;
            }

            public TimelinePresenter Presenter { get; }

            public RectTransform ActionLayer { get; }

            public IReadOnlyDictionary<TimelineCell, TimelineCellView> Cells { get; }
        }

        private sealed class TestCatalog : ICardCatalog
        {
            private readonly Dictionary<string, CardDefinition> _cardsById;

            public TestCatalog(IReadOnlyList<CardDefinition> cards)
            {
                Cards = cards;
                _cardsById = cards.ToDictionary(card => card.StableId, StringComparer.Ordinal);
            }

            public IReadOnlyList<CardDefinition> Cards { get; }

            public bool TryGet(string stableId, out CardDefinition card)
            {
                return _cardsById.TryGetValue(stableId, out card);
            }
        }
    }
}
