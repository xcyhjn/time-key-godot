using NUnit.Framework;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode
{
    public sealed class CardPlaySessionTests
    {
        [Test]
        public void LegalPath_CommitsLightingWithStableTargetAndDamage()
        {
            var grid = new TimelineGrid();
            var session = new CardPlaySession(CreateLighting());

            var targetResult = session.SelectTarget("target-01", new HexCoord(2, -1));
            var previewResult = session.PreviewTimeline(grid, new TimelineCell(4, 1));

            Assert.That(targetResult.Succeeded, Is.True);
            Assert.That(targetResult.State, Is.EqualTo(CardPlaySessionState.TargetSelected));
            Assert.That(session.TargetId, Is.EqualTo("target-01"));
            Assert.That(session.TargetCoord, Is.EqualTo(new HexCoord(2, -1)));
            Assert.That(previewResult.Succeeded, Is.True);
            Assert.That(previewResult.State, Is.EqualTo(CardPlaySessionState.TimelinePreview));
            Assert.That(previewResult.IsPlacementValid, Is.True);
            Assert.That(grid.OccupiedCellCount, Is.Zero);

            var commitResult = session.Commit(grid);

            Assert.That(commitResult.Succeeded, Is.True);
            Assert.That(commitResult.State, Is.EqualTo(CardPlaySessionState.Committed));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));

            var snapshot = grid.Resolve(new CombatSliceState("target-01", 10, 731));
            Assert.That(snapshot.Timeline.Count, Is.EqualTo(1));
            Assert.That(snapshot.Timeline[0].CardId, Is.EqualTo("lighting"));
            Assert.That(snapshot.Timeline[0].Origin, Is.EqualTo(new TimelineCell(4, 1)));
            Assert.That(snapshot.TargetHpBefore, Is.EqualTo(10));
            Assert.That(snapshot.TargetHpAfter, Is.Zero);
        }

        [Test]
        public void PreviewTimeline_WithoutTargetReturnsExplicitFailure()
        {
            var grid = new TimelineGrid();
            var session = new CardPlaySession(CreateLighting());

            var result = session.PreviewTimeline(grid, new TimelineCell(0, 0));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(CardPlayFailure.MissingTarget));
            Assert.That(result.State, Is.EqualTo(CardPlaySessionState.Idle));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [TestCase(-1, 0)]
        [TestCase(12, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 3)]
        public void PreviewTimeline_OutOfBoundsIsInvalidAndDoesNotMutateGrid(int x, int y)
        {
            var grid = new TimelineGrid();
            var session = CreateTargetedSession();

            var result = session.PreviewTimeline(grid, new TimelineCell(x, y));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(CardPlayFailure.InvalidTimelinePlacement));
            Assert.That(result.State, Is.EqualTo(CardPlaySessionState.TimelinePreview));
            Assert.That(result.IsPlacementValid, Is.False);
            Assert.That(grid.OccupiedCellCount, Is.Zero);
            Assert.That(session.Commit(grid).Succeeded, Is.False);
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void PreviewTimeline_ConflictIsInvalidAndDoesNotMutateGrid()
        {
            var grid = new TimelineGrid();
            Assert.That(grid.TryPlace(CreateBlockingAction(new TimelineCell(3, 2))), Is.True);
            var session = CreateTargetedSession();

            var result = session.PreviewTimeline(grid, new TimelineCell(3, 2));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(CardPlayFailure.InvalidTimelinePlacement));
            Assert.That(result.IsPlacementValid, Is.False);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
            Assert.That(session.Commit(grid).Succeeded, Is.False);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
        }

        [Test]
        public void PreviewTimeline_RepeatedQueriesDoNotMutateGrid()
        {
            var grid = new TimelineGrid();
            var session = CreateTargetedSession();

            var first = session.PreviewTimeline(grid, new TimelineCell(0, 0));
            var second = session.PreviewTimeline(grid, new TimelineCell(1, 0));

            Assert.That(first.Succeeded, Is.True);
            Assert.That(second.Succeeded, Is.True);
            Assert.That(session.TimelineOrigin, Is.EqualTo(new TimelineCell(1, 0)));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void PreviewAndCommit_PreserveExplicitActionIdentity()
        {
            var actionId = TimelineActionIdentity.FromSequence(4, 2);
            var grid = new TimelineGrid();
            var session = new CardPlaySession(CreateLighting(), actionId);
            Assert.That(session.SelectTarget("target-01", new HexCoord(0, 0)).Succeeded, Is.True);

            Assert.That(session.PreviewTimeline(grid, new TimelineCell(1, 0)).Succeeded, Is.True);
            Assert.That(session.PreviewTimeline(grid, new TimelineCell(2, 0)).Succeeded, Is.True);
            Assert.That(session.Commit(grid).Succeeded, Is.True);

            Assert.That(session.ActionId, Is.EqualTo(actionId));
            Assert.That(grid.ScheduledActions, Has.Count.EqualTo(1));
            Assert.That(grid.ScheduledActions[0].ActionId, Is.EqualTo(actionId));
            Assert.That(grid.ScheduledActions[0].Origin, Is.EqualTo(new TimelineCell(2, 0)));
        }

        [Test]
        public void Commit_WhenTimelineChangesAfterPreviewFailsWithoutAdditionalMutation()
        {
            var grid = new TimelineGrid();
            var session = CreateTargetedSession();
            Assert.That(session.PreviewTimeline(grid, new TimelineCell(2, 1)).Succeeded, Is.True);
            Assert.That(grid.TryPlace(CreateBlockingAction(new TimelineCell(2, 1))), Is.True);

            var result = session.Commit(grid);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(CardPlayFailure.InvalidTimelinePlacement));
            Assert.That(result.State, Is.EqualTo(CardPlaySessionState.TimelinePreview));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
        }

        [Test]
        public void Commit_RepeatedCallDoesNotPlaceActionTwice()
        {
            var grid = new TimelineGrid();
            var session = CreateTargetedSession();
            Assert.That(session.PreviewTimeline(grid, new TimelineCell(0, 0)).Succeeded, Is.True);
            Assert.That(session.Commit(grid).Succeeded, Is.True);

            var repeated = session.Commit(grid);

            Assert.That(repeated.Succeeded, Is.False);
            Assert.That(repeated.Failure, Is.EqualTo(CardPlayFailure.AlreadyCommitted));
            Assert.That(repeated.State, Is.EqualTo(CardPlaySessionState.Committed));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
        }

        [Test]
        public void Cancel_AfterPreviewIsIdempotentAndPreventsCommit()
        {
            var grid = new TimelineGrid();
            var session = CreateTargetedSession();
            Assert.That(session.PreviewTimeline(grid, new TimelineCell(0, 0)).Succeeded, Is.True);

            var firstCancel = session.Cancel();
            var repeatedCancel = session.Cancel();
            var commit = session.Commit(grid);

            Assert.That(firstCancel.Succeeded, Is.True);
            Assert.That(firstCancel.State, Is.EqualTo(CardPlaySessionState.Cancelled));
            Assert.That(repeatedCancel.Succeeded, Is.True);
            Assert.That(repeatedCancel.State, Is.EqualTo(CardPlaySessionState.Cancelled));
            Assert.That(commit.Succeeded, Is.False);
            Assert.That(commit.Failure, Is.EqualTo(CardPlayFailure.Cancelled));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void TimelineGrid_CanPlaceMatchesTryPlaceWithoutMutation()
        {
            var grid = new TimelineGrid();
            var first = CreateBlockingAction(new TimelineCell(5, 1));
            var conflict = CreateBlockingAction(new TimelineCell(5, 1));
            var outOfBounds = CreateBlockingAction(new TimelineCell(12, 0));

            Assert.That(grid.CanPlace(first), Is.True);
            Assert.That(grid.CanPlace(first), Is.True);
            Assert.That(grid.OccupiedCellCount, Is.Zero);
            Assert.That(grid.TryPlace(first), Is.True);
            Assert.That(grid.CanPlace(first), Is.False);
            Assert.That(grid.CanPlace(conflict), Is.False);
            Assert.That(grid.CanPlace(outOfBounds), Is.False);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
        }

        private static CardPlaySession CreateTargetedSession()
        {
            var session = new CardPlaySession(CreateLighting());
            Assert.That(session.SelectTarget("target-01", new HexCoord(0, 0)).Succeeded, Is.True);
            return session;
        }

        private static CardDefinition CreateLighting()
        {
            return new CardDefinition(
                "lighting",
                1,
                new[] { new CardEffect(CardEffectKind.Damage, 100) },
                new[] { new HexCoord(0, 0), new HexCoord(1, 0), new HexCoord(2, 0) },
                new[] { new TimelineCell(0, 0) });
        }

        private static TimelineAction CreateBlockingAction(TimelineCell origin)
        {
            return new TimelineAction(
                TimelineActorKind.Enemy,
                "enemy-intent",
                "target-01",
                origin,
                new[] { new TimelineCell(0, 0) },
                0);
        }
    }
}
