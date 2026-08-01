using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Presentation;
using TimeKey.Tests.PlayMode.Bindings;
using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Tests.PlayMode.Presenters
{
    public sealed class CombatPresenterRefreshTests
    {
        [Test]
        public void Refresh_UsesTypedTargetAndShape_WhenStableIdsAreArbitrary()
        {
            var rig = CombatPresentationBindingTests.CreateRig();
            var session = CreateSession();
            try
            {
                RegisterBoardTiles(rig);
                RegisterTimelineCells(rig);

                Assert.That(session.SelectCard("arbitrary-entity-card").Succeeded, Is.True);
                Assert.That(
                    session.SelectTarget(CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Succeeded,
                    Is.True);
                rig.Binding.Refresh(session.Current);
                Assert.That(rig.RangePreview.ActiveCoordinates.Count, Is.EqualTo(1));

                Assert.That(session.PreviewTimeline(new TimelineCell(0, 0)).Succeeded, Is.True);
                rig.Binding.Refresh(session.Current);
                rig.Binding.Refresh(session.Current);
                Assert.That(rig.TimelinePreview.ActiveCoordinates.Count, Is.EqualTo(1));
                Assert.That(rig.TimelinePreview.IsValid, Is.True);

                Assert.That(session.SelectCard("arbitrary-tile-card").Succeeded, Is.True);
                Assert.That(
                    session.SelectTarget(CombatTarget.ForTile(new HexCoord(0, 0))).Succeeded,
                    Is.True);
                rig.Binding.Refresh(session.Current);
                Assert.That(rig.RangePreview.ActiveCoordinates.Count, Is.EqualTo(7));

                Assert.That(session.PreviewTimeline(new TimelineCell(0, 0)).Succeeded, Is.True);
                rig.Binding.Refresh(session.Current);
                rig.Binding.Refresh(session.Current);
                Assert.That(rig.TimelinePreview.ActiveCoordinates.Count, Is.EqualTo(2));
                Assert.That(rig.TimelinePreview.IsValid, Is.True);
            }
            finally
            {
                session.Dispose();
                UnityEngine.Object.DestroyImmediate(rig.Root);
            }
        }

        [Test]
        public void Refresh_AfterResolutionUpdatesHudAndDisablesResolve()
        {
            var rig = CombatPresentationBindingTests.CreateRig();
            var session = CreateSession();
            try
            {
                RegisterBoardTiles(rig);
                RegisterTimelineCells(rig);

                Assert.That(session.SelectCard("arbitrary-entity-card").Succeeded, Is.True);
                Assert.That(
                    session.SelectTarget(CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Succeeded,
                    Is.True);
                Assert.That(session.PreviewTimeline(new TimelineCell(0, 0)).Succeeded, Is.True);
                Assert.That(session.CommitTimeline().Succeeded, Is.True);
                rig.Binding.Refresh(session.Current);
                Assert.That(rig.ResolveButton.interactable, Is.True);
                Assert.That(rig.RangePreview.ActiveCoordinates, Is.Empty);

                var result = session.ResolveTimeline();
                Assert.That(result.Succeeded, Is.True);
                rig.Binding.Refresh(session.Current);

                Assert.That(rig.ResolveButton.interactable, Is.False);
                StringAssert.Contains("HP 0", rig.TargetText.text);
                StringAssert.Contains("RESOLVED", rig.StatusText.text);
            }
            finally
            {
                session.Dispose();
                UnityEngine.Object.DestroyImmediate(rig.Root);
            }
        }

        private static CombatApplicationSession CreateSession()
        {
            var entityCard = new CardDefinition(
                "arbitrary-entity-card",
                1,
                new[] { new CardEffect(CardEffectKind.Damage, 10) },
                new[] { new HexCoord(0, 0) },
                new[] { new TimelineCell(0, 0) });
            var tileCard = new CardDefinition(
                "arbitrary-tile-card",
                2,
                new[] { new CardEffect(CardEffectKind.Elevation, 2) },
                SevenHexRange,
                new[] { new TimelineCell(0, 0), new TimelineCell(1, 0) });
            var board = new CombatBoardState();
            foreach (var coordinate in SevenHexRange)
            {
                board.AddTile(coordinate, 1);
            }

            return new CombatApplicationSession(
                new TestCatalog(new[] { entityCard, tileCard }),
                new CombatSliceState("target-01", 10, 731, board),
                new TimelineGrid());
        }

        private static void RegisterBoardTiles(CombatPresentationBindingTests.BindingRig rig)
        {
            foreach (var coordinate in SevenHexRange)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = string.Format("Tile-{0}-{1}", coordinate.Q, coordinate.R);
                tile.transform.SetParent(rig.Root.transform, false);
                rig.RangePreview.Register(coordinate, tile.transform);
            }
        }

        private static void RegisterTimelineCells(CombatPresentationBindingTests.BindingRig rig)
        {
            var cells = new List<TimelineCellView>();
            for (var column = 0; column < 2; column++)
            {
                var cellObject = CombatPresentationBindingTests.CreateChild(
                    rig.Root,
                    "Cell-" + column,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));
                cellObject.SetActive(false);
                var cell = cellObject.AddComponent<TimelineCellView>();
                var label = CombatPresentationBindingTests.CreateChild(
                    cellObject,
                    "Label",
                    typeof(RectTransform)).AddComponent<Text>();
                CombatPresentationBindingTests.SetField(cell, "column", column);
                CombatPresentationBindingTests.SetField(cell, "row", 0);
                CombatPresentationBindingTests.SetField(cell, "button", cellObject.GetComponent<Button>());
                CombatPresentationBindingTests.SetField(cell, "label", label);
                cellObject.SetActive(true);
                cells.Add(cell);
                rig.TimelinePreview.Register(cell.Coordinate, cell.Graphic);
            }

            rig.TimelinePresenter.Unbind();
            CombatPresentationBindingTests.SetField(rig.TimelinePresenter, "timelineCells", cells);
            rig.TimelinePresenter.Bind();
        }

        private static readonly IReadOnlyList<HexCoord> SevenHexRange = new[]
        {
            new HexCoord(0, 0),
            new HexCoord(1, 0),
            new HexCoord(-1, 0),
            new HexCoord(0, 1),
            new HexCoord(0, -1),
            new HexCoord(1, -1),
            new HexCoord(-1, 1)
        };

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
