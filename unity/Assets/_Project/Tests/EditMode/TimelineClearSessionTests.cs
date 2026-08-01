using System;
using NUnit.Framework;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode
{
    public sealed class TimelineClearSessionTests
    {
        [Test]
        public void Constructor_RequiresTypedClearCardAndNeverUsesOrdinaryShape()
        {
            var ordinary = new CardDefinition(
                "ordinary",
                1,
                new[] { new CardEffect(CardEffectKind.Damage, 1) },
                Array.Empty<HexCoord>(),
                new[] { new TimelineCell(0, 0) });

            Assert.Throws<ArgumentException>(() => new TimelineClearSession(ordinary));
            Assert.Throws<ArgumentException>(() =>
                TimelineAction.FromCard(CreateWindCard(), "target", new TimelineCell(0, 0)));
        }

        [Test]
        public void RepeatedPreviewIsPureAndCommitRemovesActionWithoutAddingCells()
        {
            var grid = new TimelineGrid();
            Assert.That(grid.TryPlace(CreateAction("victim", new TimelineCell(0, 0))), Is.True);
            var session = new TimelineClearSession(CreateWindCard());

            var firstPreview = session.Preview(grid, new TimelineCell(0, 0));
            var repeatedPreview = session.Preview(grid, new TimelineCell(0, 0));

            Assert.That(firstPreview.Succeeded, Is.True);
            Assert.That(repeatedPreview.Succeeded, Is.True);
            Assert.That(session.State, Is.EqualTo(TimelineClearSessionState.Preview));
            Assert.That(repeatedPreview.Preview.HitActions, Has.Count.EqualTo(1));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));

            var committed = session.Commit(grid);
            var repeatedCommit = session.Commit(grid);

            Assert.That(committed.Succeeded, Is.True);
            Assert.That(committed.Result.RemovedActions, Has.Count.EqualTo(1));
            Assert.That(session.State, Is.EqualTo(TimelineClearSessionState.Committed));
            Assert.That(repeatedCommit.Failure, Is.EqualTo(TimelineClearFailure.AlreadyCommitted));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void EmptyClearCommitsSuccessfullyAndConsumesSession()
        {
            var grid = new TimelineGrid();
            var session = new TimelineClearSession(CreateWindCard());

            Assert.That(session.Preview(grid, new TimelineCell(4, 1)).Succeeded, Is.True);
            var committed = session.Commit(grid);

            Assert.That(committed.Succeeded, Is.True);
            Assert.That(committed.Result.RemovedActions, Is.Empty);
            Assert.That(committed.Result.RemovedCellCount, Is.Zero);
            Assert.That(session.State, Is.EqualTo(TimelineClearSessionState.Committed));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void OutOfBoundsPreviewAndCommitArePure()
        {
            var grid = new TimelineGrid();
            Assert.That(grid.TryPlace(CreateAction("edge", new TimelineCell(11, 1))), Is.True);
            var session = new TimelineClearSession(CreateWindCard());

            var preview = session.Preview(grid, new TimelineCell(11, 1));
            var commit = session.Commit(grid);

            Assert.That(preview.Failure, Is.EqualTo(TimelineClearFailure.InvalidTimelinePlacement));
            Assert.That(commit.Failure, Is.EqualTo(TimelineClearFailure.InvalidTimelinePlacement));
            Assert.That(commit.Result.Succeeded, Is.False);
            Assert.That(session.State, Is.EqualTo(TimelineClearSessionState.Preview));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
        }

        [Test]
        public void CancelIsIdempotentAndHasNoTimelineSideEffect()
        {
            var grid = new TimelineGrid();
            Assert.That(grid.TryPlace(CreateAction("victim", new TimelineCell(0, 0))), Is.True);
            var session = new TimelineClearSession(CreateWindCard());
            Assert.That(session.Preview(grid, new TimelineCell(0, 0)).Succeeded, Is.True);

            var first = session.Cancel();
            var repeated = session.Cancel();
            var commit = session.Commit(grid);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(repeated.Succeeded, Is.True);
            Assert.That(commit.Failure, Is.EqualTo(TimelineClearFailure.Cancelled));
            Assert.That(session.Origin, Is.Null);
            Assert.That(session.LastPreview, Is.Null);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
        }

        private static CardDefinition CreateWindCard()
        {
            return new CardDefinition(
                "wind",
                4,
                new[]
                {
                    new CardEffect(CardEffectKind.Clear, new[]
                    {
                        new TimelineCell(0, 0),
                        new TimelineCell(1, 0),
                        new TimelineCell(0, 1),
                        new TimelineCell(1, 1)
                    })
                },
                new[] { new HexCoord(0, 0) },
                Array.Empty<TimelineCell>());
        }

        private static TimelineAction CreateAction(string cardId, TimelineCell origin)
        {
            return new TimelineAction(
                TimelineActorKind.Player,
                cardId,
                "target",
                origin,
                new[] { new TimelineCell(0, 0) },
                0);
        }
    }
}
