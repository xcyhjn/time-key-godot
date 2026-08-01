using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Presentation;
using TimeKey.Presentation.Localization;
using TimeKey.Presentation.Presenters;
using TimeKey.Presentation.Targeting;
using TimeKey.Tests.PlayMode.Bindings;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.Targeting
{
    public sealed class ClearTimelinePresentationTests
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
        public IEnumerator Preview_ShowsEmptyHitAndOutOfBoundsMarkersThenRestoresAppearance()
        {
            var rig = CreateRig(12, 2);
            var grid = new TimelineGrid();
            var action = new TimelineAction(
                TimelineActorKind.Enemy,
                "enemy-intent",
                "target-01",
                new TimelineCell(0, 0),
                new[] { new TimelineCell(0, 0), new TimelineCell(1, 0) },
                0);
            Assert.That(grid.TryPlace(action), Is.True);
            rig.Presenter.RenderAction(
                action.Origin,
                action.Shape,
                CombatChineseText.EnemyIntentTimelineLabel,
                new Color(0.78f, 0.27f, 0.25f, 1f));

            var wind = CreateClearCard("wind", CreateRectangle(2, 2));
            var session = new CombatApplicationSession(
                new TestCatalog(new[] { wind }),
                CreateState(),
                grid);
            Assert.That(session.SelectCard("wind").Succeeded, Is.True);
            Assert.That(session.PreviewClear(new TimelineCell(0, 0)).Succeeded, Is.True);

            rig.Presenter.Refresh(session.Current);
            Assert.That(rig.Cells[new TimelineCell(0, 0)].DisplayText, Is.EqualTo(CombatChineseText.ClearHit));
            Assert.That(rig.Cells[new TimelineCell(1, 0)].DisplayText, Is.EqualTo(CombatChineseText.ClearHit));
            Assert.That(rig.Cells[new TimelineCell(0, 1)].DisplayText, Is.EqualTo("○"));
            Assert.That(
                rig.Cells[new TimelineCell(0, 0)].Graphic.GetComponent<Outline>().effectDistance,
                Is.EqualTo(new Vector2(4f, 4f)));

            Assert.That(session.PreviewClear(new TimelineCell(11, 0)).Succeeded, Is.False);
            rig.Presenter.Refresh(session.Current);
            Assert.That(rig.Cells[new TimelineCell(11, 0)].DisplayText, Is.EqualTo("!"));
            Assert.That(rig.ClearPreview.IsInBounds, Is.False);

            Assert.That(session.PreviewClear(new TimelineCell(0, 0)).Succeeded, Is.True);
            rig.Presenter.Refresh(session.Current);
            rig.Presenter.ClearPreview();
            rig.Presenter.ClearPreview();
            Assert.That(
                rig.Cells[new TimelineCell(0, 0)].DisplayText,
                Is.EqualTo(CombatChineseText.EnemyIntentTimelineLabel));
            Assert.That(rig.Cells[new TimelineCell(0, 1)].DisplayText, Is.EqualTo("01"));
            Assert.That(
                rig.Cells[new TimelineCell(0, 0)].Graphic.GetComponents<Outline>().Length,
                Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Commit_RemovesCompleteActionUiAndTornadoPreviewRestoresAllCells()
        {
            var rig = CreateRig(12, 1);
            var grid = new TimelineGrid();
            var action = new TimelineAction(
                TimelineActorKind.Player,
                "lighting",
                "target-01",
                new TimelineCell(4, 0),
                new[]
                {
                    new TimelineCell(0, 0),
                    new TimelineCell(1, 0),
                    new TimelineCell(2, 0)
                },
                100);
            Assert.That(grid.TryPlace(action), Is.True);
            rig.Presenter.RenderAction(
                action.Origin,
                action.Shape,
                CombatChineseText.GetTimelineLabel("lighting"),
                new Color(0.16f, 0.74f, 0.82f, 1f));

            var tornado = CreateClearCard("tornado", CreateRectangle(12, 1));
            var session = new CombatApplicationSession(
                new TestCatalog(new[] { tornado }),
                CreateState(),
                grid);
            Assert.That(session.SelectCard("tornado").Succeeded, Is.True);
            Assert.That(session.PreviewClear(new TimelineCell(0, 0)).Succeeded, Is.True);
            rig.Presenter.Refresh(session.Current);
            Assert.That(rig.ClearPreview.ActiveCoordinates.Count, Is.EqualTo(12));
            Assert.That(rig.Cells[new TimelineCell(4, 0)].DisplayText, Is.EqualTo(CombatChineseText.ClearHit));

            var commit = session.CommitClear();
            Assert.That(commit.Succeeded, Is.True);
            Assert.That(commit.ClearResult.RemovedActions.Count, Is.EqualTo(1));
            rig.Presenter.ApplyClearResult(commit.ClearResult);

            Assert.That(rig.Cells[new TimelineCell(4, 0)].DisplayText, Is.EqualTo("05"));
            Assert.That(rig.Cells[new TimelineCell(5, 0)].DisplayText, Is.EqualTo("06"));
            Assert.That(rig.Cells[new TimelineCell(6, 0)].DisplayText, Is.EqualTo("07"));
            Assert.That(rig.ClearPreview.IsShowing, Is.False);
            yield return null;
        }

        private PresentationRig CreateRig(int width, int height)
        {
            _root = new GameObject("ClearTimelinePresentationRig");
            _root.SetActive(false);
            var placement = _root.AddComponent<TimelinePlacementPreview>();
            var clear = _root.AddComponent<ClearTimelinePreview>();
            var presenter = _root.AddComponent<TimelinePresenter>();
            var cells = new List<TimelineCellView>(width * height);
            var cellsByCoordinate = new Dictionary<TimelineCell, TimelineCellView>();
            for (var row = 0; row < height; row++)
            {
                for (var column = 0; column < width; column++)
                {
                    var cellObject = new GameObject(
                        "Cell-" + column + "-" + row,
                        typeof(RectTransform),
                        typeof(Image),
                        typeof(Button));
                    cellObject.transform.SetParent(_root.transform, false);
                    var cell = cellObject.AddComponent<TimelineCellView>();
                    var labelObject = new GameObject("Label", typeof(RectTransform));
                    labelObject.transform.SetParent(cellObject.transform, false);
                    var label = labelObject.AddComponent<Text>();
                    label.text = (column + 1).ToString("00");
                    cellObject.GetComponent<Image>().color = new Color(0.16f, 0.19f, 0.20f, 1f);
                    CombatPresentationBindingTests.SetField(cell, "column", column);
                    CombatPresentationBindingTests.SetField(cell, "row", row);
                    CombatPresentationBindingTests.SetField(cell, "button", cellObject.GetComponent<Button>());
                    CombatPresentationBindingTests.SetField(cell, "label", label);
                    cell.CaptureCurrentAsDefault();
                    cells.Add(cell);
                    cellsByCoordinate.Add(cell.Coordinate, cell);
                }
            }

            CombatPresentationBindingTests.SetField(presenter, "timelinePreview", placement);
            CombatPresentationBindingTests.SetField(presenter, "clearTimelinePreview", clear);
            CombatPresentationBindingTests.SetField(presenter, "timelineCells", cells);
            _root.SetActive(true);
            presenter.Bind();
            return new PresentationRig(presenter, clear, cellsByCoordinate);
        }

        private static CardDefinition CreateClearCard(
            string stableId,
            IReadOnlyList<TimelineCell> mask)
        {
            return new CardDefinition(
                stableId,
                1,
                stableId + ".png",
                new[] { new CardEffect(CardEffectKind.Clear, mask) },
                new[] { new HexCoord(0, 0) },
                Array.Empty<TimelineCell>());
        }

        private static IReadOnlyList<TimelineCell> CreateRectangle(int width, int height)
        {
            var result = new List<TimelineCell>(width * height);
            for (var row = 0; row < height; row++)
            {
                for (var column = 0; column < width; column++)
                {
                    result.Add(new TimelineCell(column, row));
                }
            }

            return result;
        }

        private static CombatSliceState CreateState()
        {
            var board = new CombatBoardState();
            board.AddTile(new HexCoord(0, 0), 1);
            return new CombatSliceState("target-01", 10, 731, board);
        }

        private sealed class TestCatalog : ICardCatalog
        {
            private readonly Dictionary<string, CardDefinition> _cards;

            public TestCatalog(IReadOnlyList<CardDefinition> cards)
            {
                Cards = cards;
                _cards = cards.ToDictionary(card => card.StableId, StringComparer.Ordinal);
            }

            public IReadOnlyList<CardDefinition> Cards { get; }

            public bool TryGet(string stableId, out CardDefinition card)
            {
                return _cards.TryGetValue(stableId, out card);
            }
        }

        private sealed class PresentationRig
        {
            public PresentationRig(
                TimelinePresenter presenter,
                ClearTimelinePreview clearPreview,
                IReadOnlyDictionary<TimelineCell, TimelineCellView> cells)
            {
                Presenter = presenter;
                ClearPreview = clearPreview;
                Cells = cells;
            }

            public TimelinePresenter Presenter { get; }

            public ClearTimelinePreview ClearPreview { get; }

            public IReadOnlyDictionary<TimelineCell, TimelineCellView> Cells { get; }
        }
    }
}
