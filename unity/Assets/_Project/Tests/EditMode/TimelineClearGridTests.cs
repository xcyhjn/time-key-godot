using System;
using System.Linq;
using NUnit.Framework;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode
{
    public sealed class TimelineClearGridTests
    {
        [Test]
        public void PreviewClear_WindClassifiesEmptyOccupiedAndOutOfBoundsCells()
        {
            var grid = new TimelineGrid();
            var action = CreateAction(
                TimelineActorKind.Player,
                "wide-player",
                new TimelineCell(10, 1),
                new TimelineCell(0, 0),
                new TimelineCell(1, 0));
            Assert.That(grid.TryPlace(action), Is.True);

            var preview = grid.PreviewClear(new TimelineCell(10, 1), CreateWindEffect());

            Assert.That(preview.IsInBounds, Is.True);
            Assert.That(
                preview.Cells.Select(cell => cell.State),
                Is.EqualTo(new[]
                {
                    TimelineClearCellState.Occupied,
                    TimelineClearCellState.Occupied,
                    TimelineClearCellState.Empty,
                    TimelineClearCellState.Empty
                }));
            Assert.That(preview.HitActions, Has.Count.EqualTo(1));
            Assert.That(preview.Cells[0].HitAction, Is.SameAs(preview.Cells[1].HitAction));
            Assert.That(preview.HitActions[0].ActionId, Is.EqualTo(action.ActionId));
            Assert.That(preview.HitActions[0].CardId, Is.EqualTo("wide-player"));
            Assert.That(preview.HitActions[0].Shape, Has.Count.EqualTo(2));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(2));

            var outOfBounds = grid.PreviewClear(new TimelineCell(11, 1), CreateWindEffect());

            Assert.That(outOfBounds.IsInBounds, Is.False);
            Assert.That(
                outOfBounds.Cells.Count(cell => cell.State == TimelineClearCellState.OutOfBounds),
                Is.EqualTo(2));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(2));
        }

        [TestCase(0, 0, true)]
        [TestCase(0, 2, true)]
        [TestCase(1, 0, false)]
        [TestCase(0, 3, false)]
        public void PreviewClear_TornadoRequiresCompleteTwelveByOneMaskInBounds(
            int x,
            int y,
            bool expected)
        {
            var grid = new TimelineGrid();

            var preview = grid.PreviewClear(
                new TimelineCell(x, y),
                CreateTornadoEffect());

            Assert.That(preview.IsInBounds, Is.EqualTo(expected));
            Assert.That(preview.Cells, Has.Count.EqualTo(12));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void TryClear_EmptyInBoundsMaskSucceedsWithoutCreatingTimelineCells()
        {
            var grid = new TimelineGrid();

            var result = grid.TryClear(new TimelineCell(4, 1), CreateWindEffect());

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RemovedActions, Is.Empty);
            Assert.That(result.RemovedCellCount, Is.Zero);
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void TryClear_MultiCellActionHitMoreThanOnceIsReturnedOnceAndRemovedCompletely()
        {
            var grid = new TimelineGrid();
            var action = CreateAction(
                TimelineActorKind.Player,
                "three-cell-player",
                new TimelineCell(2, 0),
                new TimelineCell(0, 0),
                new TimelineCell(1, 0),
                new TimelineCell(2, 0));
            Assert.That(grid.TryPlace(action), Is.True);

            var result = grid.TryClear(new TimelineCell(2, 0), CreateWindEffect());

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RemovedActions, Has.Count.EqualTo(1));
            Assert.That(result.RemovedActions[0].ActionId, Is.EqualTo(action.ActionId));
            Assert.That(result.RemovedCellCount, Is.EqualTo(3));
            Assert.That(result.RemovedActions[0].Shape, Has.Count.EqualTo(3));
            Assert.That(
                result.RemovedActions[0].OccupiedCells,
                Is.EqualTo(new[]
                {
                    new TimelineCell(2, 0),
                    new TimelineCell(3, 0),
                    new TimelineCell(4, 0)
                }));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void TryClear_RemovesPlayerAndEnemyActionsWithoutActorFiltering()
        {
            var grid = new TimelineGrid();
            Assert.That(
                grid.TryPlace(CreateAction(
                    TimelineActorKind.Player,
                    "player-action",
                    new TimelineCell(0, 0),
                    new TimelineCell(0, 0))),
                Is.True);
            Assert.That(
                grid.TryPlace(CreateAction(
                    TimelineActorKind.Enemy,
                    "enemy-action",
                    new TimelineCell(1, 1),
                    new TimelineCell(0, 0))),
                Is.True);
            Assert.That(
                grid.TryPlace(CreateAction(
                    TimelineActorKind.Enemy,
                    "unrelated",
                    new TimelineCell(5, 2),
                    new TimelineCell(0, 0))),
                Is.True);

            var result = grid.TryClear(new TimelineCell(0, 0), CreateWindEffect());

            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                result.RemovedActions.Select(action => action.CardId),
                Is.EqualTo(new[] { "player-action", "enemy-action" }));
            Assert.That(
                result.RemovedActions.Select(action => action.ActorKind),
                Is.EqualTo(new[] { TimelineActorKind.Player, TimelineActorKind.Enemy }));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
            Assert.That(
                grid.PreviewClear(
                    new TimelineCell(5, 2),
                    CreateSingleCellClearEffect()).Cells[0].State,
                Is.EqualTo(TimelineClearCellState.Occupied));
        }

        [Test]
        public void TryClear_OutOfBoundsValidationOccursBeforeAnyRemoval()
        {
            var grid = new TimelineGrid();
            Assert.That(
                grid.TryPlace(CreateAction(
                    TimelineActorKind.Player,
                    "edge-action",
                    new TimelineCell(11, 1),
                    new TimelineCell(0, 0))),
                Is.True);

            var result = grid.TryClear(new TimelineCell(11, 1), CreateWindEffect());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.RemovedActions, Is.Empty);
            Assert.That(result.RemovedCellCount, Is.Zero);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
            Assert.That(
                grid.PreviewClear(
                    new TimelineCell(11, 1),
                    CreateSingleCellClearEffect()).Cells[0].State,
                Is.EqualTo(TimelineClearCellState.Occupied));
        }

        [Test]
        public void TryClear_RepeatedCallAfterRemovalIsAStableLegalEmptyClear()
        {
            var grid = new TimelineGrid();
            Assert.That(
                grid.TryPlace(CreateAction(
                    TimelineActorKind.Player,
                    "one-shot",
                    new TimelineCell(0, 0),
                    new TimelineCell(0, 0))),
                Is.True);

            var first = grid.TryClear(new TimelineCell(0, 0), CreateWindEffect());
            var second = grid.TryClear(new TimelineCell(0, 0), CreateWindEffect());

            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.RemovedActions, Has.Count.EqualTo(1));
            Assert.That(second.Succeeded, Is.True);
            Assert.That(second.RemovedActions, Is.Empty);
            Assert.That(second.RemovedCellCount, Is.Zero);
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void ClearPreviewAndFailedPlacementDoNotChangeOrdinaryPlacementRules()
        {
            var grid = new TimelineGrid();
            var existing = CreateAction(
                TimelineActorKind.Player,
                "existing",
                new TimelineCell(3, 1),
                new TimelineCell(0, 0));
            var overlapping = CreateAction(
                TimelineActorKind.Player,
                "overlap",
                new TimelineCell(3, 1),
                new TimelineCell(0, 0));
            var free = CreateAction(
                TimelineActorKind.Player,
                "free",
                new TimelineCell(4, 1),
                new TimelineCell(0, 0));
            Assert.That(grid.TryPlace(existing), Is.True);

            Assert.That(grid.PreviewClear(new TimelineCell(3, 1), CreateWindEffect()).IsInBounds, Is.True);
            Assert.That(grid.CanPlace(overlapping), Is.False);
            Assert.That(grid.TryPlace(overlapping), Is.False);
            Assert.That(grid.CanPlace(free), Is.True);
            Assert.That(grid.TryPlace(free), Is.True);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(2));
        }

        [Test]
        public void ClearApiRejectsOrdinaryEffectInsteadOfAcceptingShapeFallback()
        {
            var grid = new TimelineGrid();
            var ordinaryEffect = new CardEffect(CardEffectKind.Damage, 1);

            Assert.Throws<ArgumentException>(() =>
                grid.PreviewClear(new TimelineCell(0, 0), ordinaryEffect));
            Assert.Throws<ArgumentException>(() =>
                grid.TryClear(new TimelineCell(0, 0), ordinaryEffect));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        private static CardEffect CreateWindEffect()
        {
            return new CardEffect(CardEffectKind.Clear, new[]
            {
                new TimelineCell(0, 0),
                new TimelineCell(1, 0),
                new TimelineCell(0, 1),
                new TimelineCell(1, 1)
            });
        }

        private static CardEffect CreateTornadoEffect()
        {
            return new CardEffect(
                CardEffectKind.Clear,
                Enumerable.Range(0, 12).Select(x => new TimelineCell(x, 0)).ToArray());
        }

        private static CardEffect CreateSingleCellClearEffect()
        {
            return new CardEffect(
                CardEffectKind.Clear,
                new[] { new TimelineCell(0, 0) });
        }

        private static TimelineAction CreateAction(
            TimelineActorKind actorKind,
            string cardId,
            TimelineCell origin,
            params TimelineCell[] shape)
        {
            return new TimelineAction(
                actorKind,
                cardId,
                "target",
                origin,
                shape,
                0);
        }
    }
}
