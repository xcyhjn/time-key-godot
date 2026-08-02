using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TimeKey.Application;
using TimeKey.Application.BattleFlow;
using TimeKey.Diagnostics;
using TimeKey.Domain;
using TimeKey.Domain.BattleFlow;
using TimeKey.Domain.Deck;
using TimeKey.Domain.Intents;

namespace TimeKey.Tests.EditMode.Application
{
    public sealed class CombatApplicationSessionTests
    {
        [Test]
        public void LightingPath_SelectsTargetsCommitsAndResolvesThroughApplication()
        {
            var session = CreateSession(out var grid, out _);

            Assert.That(session.Current.TimelineActions, Has.Count.EqualTo(1));
            Assert.That(
                session.Current.TimelineActions[0].Validity,
                Is.EqualTo(TimelineActionValidity.Unsupported));
            Assert.That(
                session.Current.TimelineActions[0].InvalidReason,
                Is.EqualTo(TimelineActionInvalidReason.UnsupportedSourceCommand));

            var selected = session.SelectCard("lighting");
            var pendingActionId = session.Current.PendingActionId;
            var targeted = session.SelectTarget(CombatTarget.ForEntity("target-01", new HexCoord(0, 0)));
            var previewed = session.PreviewTimeline(new TimelineCell(0, 0));

            Assert.That(selected.Succeeded, Is.True);
            Assert.That(pendingActionId.HasValue, Is.True);
            Assert.That(pendingActionId.Value.Value, Is.EqualTo("cycle:1/action:0"));
            Assert.That(selected.RequiredTargetKind, Is.EqualTo(CombatTargetKind.Entity));
            Assert.That(targeted.Succeeded, Is.True);
            Assert.That(previewed.Succeeded, Is.True);
            Assert.That(previewed.IsPlacementValid, Is.True);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1), "Preview must not place the player action.");

            var committed = session.CommitTimeline();

            Assert.That(committed.Succeeded, Is.True);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(2));
            Assert.That(
                grid.ScheduledActions.Single(item => item.ActorKind == TimelineActorKind.Player).ActionId,
                Is.EqualTo(pendingActionId.Value));
            Assert.That(session.Current.TimelineActions, Has.Count.EqualTo(2));
            Assert.That(
                session.Current.TimelineActions
                    .Single(item => item.ActorKind == TimelineActorKind.Player).ActionId,
                Is.EqualTo(pendingActionId.Value));

            var resolved = session.ResolveTimeline();

            Assert.That(resolved.Succeeded, Is.True);
            Assert.That(resolved.StableId, Is.EqualTo("lighting"));
            Assert.That(resolved.Resolution.TargetHpBefore, Is.EqualTo(10));
            Assert.That(resolved.Resolution.TargetHpAfter, Is.Zero);
            Assert.That(resolved.Resolution.EnemyIntentResolved, Is.True);
            Assert.That(
                resolved.Resolution.ResolutionOrder.Count(item => item.CardId == "enemy-intent"),
                Is.EqualTo(1));
            Assert.That(
                resolved.Resolution.ResolutionOrder
                    .Single(item => item.CardId == "lighting").ActionId,
                Is.EqualTo(pendingActionId.Value));
            Assert.That(session.Current.Phase, Is.EqualTo(CombatSessionPhase.Resolved));
            Assert.That(session.Current.SelectedCard, Is.Null);
            Assert.That(session.Current.TimelineActions, Is.Empty);
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void EarthquakePath_UsesTypedTileTargetAndRaisesSevenTilesByTwo()
        {
            var board = CreateSevenTileBoard();
            var state = new CombatSliceState("target-01", 10, 731, board);
            var session = CreateSession(state, out _, out var sink);

            Assert.That(session.SelectCard("earthquake").Succeeded, Is.True);
            Assert.That(
                session.SelectTarget(CombatTarget.ForTile(new HexCoord(0, 0))).Succeeded,
                Is.True);
            Assert.That(session.PreviewTimeline(new TimelineCell(0, 1)).Succeeded, Is.True);
            Assert.That(session.CommitTimeline().Succeeded, Is.True);

            var result = session.ResolveTimeline();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Resolution.EffectResults.Count, Is.EqualTo(7));
            Assert.That(result.Resolution.EffectResults.All(item => item.BeforeLayers == 1), Is.True);
            Assert.That(result.Resolution.EffectResults.All(item => item.AfterLayers == 3), Is.True);
            Assert.That(result.Resolution.EffectResults.All(item => !item.Removed), Is.True);
            var effectTraces = sink.Entries.Where(item => item.Command == "resolve-effect").ToArray();
            Assert.That(effectTraces.Length, Is.EqualTo(7));
            Assert.That(effectTraces.Select(item => item.TargetCoordinate),
                Is.EquivalentTo(result.Resolution.EffectResults.Select(item => (HexCoord?)item.Coordinate)));
            Assert.That(effectTraces.All(item => item.EffectKind == CardEffectKind.Elevation), Is.True);
            Assert.That(effectTraces.All(item => item.BeforeValue == 1 && item.AfterValue == 3), Is.True);
        }

        [Test]
        public void RecoverPath_RequiresExactOccupantAndResolvesClampedBeforeAfterResult()
        {
            var target = CombatTarget.ForEntity("target-01", new HexCoord(0, 0));
            var state = CreateRecoverState(25, 100);
            var session = CreateSession(state, out var grid, out var sink);

            Assert.That(session.SelectCard("recover").Succeeded, Is.True);
            Assert.That(session.SelectTarget(target).Succeeded, Is.True);
            Assert.That(session.PreviewTimeline(new TimelineCell(9, 0)).Succeeded, Is.True);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
            Assert.That(session.CommitTimeline().Succeeded, Is.True);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(4));

            var result = session.ResolveTimeline();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(state.TryGetOccupant("target-01", new HexCoord(0, 0), out var occupant), Is.True);
            Assert.That(occupant.Hp, Is.EqualTo(100));
            Assert.That(result.Resolution.OccupantEffectResults, Has.Count.EqualTo(1));
            Assert.That(result.Resolution.OccupantEffectResults[0].Before.Hp, Is.EqualTo(25));
            Assert.That(result.Resolution.OccupantEffectResults[0].After.Hp, Is.EqualTo(100));
            var trace = sink.Entries.Single(item =>
                item.Command == "resolve-effect" && item.EffectKind == CardEffectKind.Recover);
            Assert.That(trace.TargetId, Is.EqualTo("target-01"));
            Assert.That(trace.TargetCoordinate, Is.EqualTo(new HexCoord(0, 0)));
            Assert.That(trace.BeforeValue, Is.EqualTo(25));
            Assert.That(trace.AfterValue, Is.EqualTo(100));
        }

        [Test]
        public void RecoverSelection_RejectsFullHealthIdMismatchCoordMismatchAndMissingRange()
        {
            var state = CreateRecoverState(100, 100);
            var session = CreateSession(state, out var grid, out _);
            Assert.That(session.SelectCard("recover").Succeeded, Is.True);

            Assert.That(
                session.SelectTarget(CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Failure,
                Is.EqualTo(CombatCommandFailure.InvalidTarget));
            Assert.That(
                session.SelectTarget(CombatTarget.ForEntity("other", new HexCoord(0, 0))).Failure,
                Is.EqualTo(CombatCommandFailure.InvalidTarget));
            Assert.That(
                session.SelectTarget(CombatTarget.ForEntity("target-01", new HexCoord(1, 0))).Failure,
                Is.EqualTo(CombatCommandFailure.InvalidTarget));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));

            var missingRangeCard = new CardDefinition(
                "recover-no-range",
                5,
                new[] { new CardEffect(CardEffectKind.Recover, 100) },
                Array.Empty<HexCoord>(),
                new[] { new TimelineCell(0, 0) });
            var missingRangeSession = new CombatApplicationSession(
                new TestCardCatalog(new[] { missingRangeCard }),
                CreateRecoverState(25, 100),
                new TimelineGrid());
            Assert.That(missingRangeSession.SelectCard("recover-no-range").Succeeded, Is.True);
            Assert.That(
                missingRangeSession.SelectTarget(
                    CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Failure,
                Is.EqualTo(CombatCommandFailure.InvalidTarget));
        }

        [Test]
        public void RecoverSelection_AllowsExistingZeroHpOccupant()
        {
            var state = CreateRecoverState(0, 100);
            var session = CreateSession(state, out _, out _);

            Assert.That(session.SelectCard("recover").Succeeded, Is.True);
            Assert.That(
                session.SelectTarget(CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Succeeded,
                Is.True);
            Assert.That(session.PreviewTimeline(new TimelineCell(0, 0)).Succeeded, Is.True);
            Assert.That(session.CommitTimeline().Succeeded, Is.True);
            Assert.That(session.ResolveTimeline().Succeeded, Is.True);
            Assert.That(state.TryGetOccupant("target-01", new HexCoord(0, 0), out var occupant), Is.True);
            Assert.That(occupant.Hp, Is.EqualTo(100));
        }

        [Test]
        public void RecoverSelection_UsesMatchingOccupantInsteadOfLegacyPrimaryTargetOnly()
        {
            var board = CreateSevenTileBoard();
            var ally = new CombatOccupantState(
                "ally-02",
                new HexCoord(0, 0),
                "entity",
                CombatAttitude.Player,
                hp: 10,
                maxHp: 100,
                poisonStacks: 0,
                supportsHealth: true,
                supportsStatus: true);
            var state = new CombatSliceState(
                "target-01",
                731,
                board,
                new[] { ally });
            var session = CreateSession(state, out _, out _);

            Assert.That(session.SelectCard("recover").Succeeded, Is.True);
            Assert.That(
                session.SelectTarget(CombatTarget.ForEntity("ally-02", new HexCoord(0, 0))).Succeeded,
                Is.True);
            Assert.That(session.PreviewTimeline(new TimelineCell(0, 0)).Succeeded, Is.True);
            Assert.That(session.CommitTimeline().Succeeded, Is.True);
            var result = session.ResolveTimeline();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(state.TryGetOccupant("ally-02", new HexCoord(0, 0), out var recovered), Is.True);
            Assert.That(recovered.Hp, Is.EqualTo(100));
            Assert.That(result.Resolution.OccupantEffectResults[0].After.RuntimeId, Is.EqualTo("ally-02"));
        }

        [Test]
        public void RecoverResolve_RevalidatesDisappearanceAndFullHealthWithoutMutation()
        {
            var removedState = CreateRecoverState(25, 100);
            var removedSession = CreateSession(removedState, out _, out _);
            CommitRecover(removedSession);
            Assert.That(
                removedState.TryRemoveOccupant("target-01", new HexCoord(0, 0)),
                Is.True);

            var removedResult = removedSession.ResolveTimeline();

            Assert.That(removedResult.Succeeded, Is.True);
            Assert.That(removedResult.Resolution.OccupantEffectResults, Is.Empty);
            Assert.That(removedState.OccupantCount, Is.Zero);

            var fullState = CreateRecoverState(25, 100);
            var fullSession = CreateSession(fullState, out _, out _);
            CommitRecover(fullSession);
            Assert.That(
                fullState.TrySetOccupantHealth("target-01", new HexCoord(0, 0), 100),
                Is.True);

            var fullResult = fullSession.ResolveTimeline();

            Assert.That(fullResult.Succeeded, Is.True);
            Assert.That(fullResult.Resolution.OccupantEffectResults, Is.Empty);
            Assert.That(fullState.TryGetOccupant("target-01", new HexCoord(0, 0), out var full), Is.True);
            Assert.That(full.Hp, Is.EqualTo(100));
        }

        [Test]
        public void BuiltPath_RequiresEmptyTileAndCreatesTowerThroughApplication()
        {
            var state = new CombatSliceState("target-01", 10, 731, CreateSevenTileBoard());
            var session = CreateSession(state, out _, out var sink);

            Assert.That(session.SelectCard("tower").Succeeded, Is.True);
            Assert.That(
                session.SelectTarget(CombatTarget.ForTile(new HexCoord(1, 0))).Failure,
                Is.EqualTo(CombatCommandFailure.InvalidTarget));
            Assert.That(
                session.SelectTarget(CombatTarget.ForTile(new HexCoord(0, 0))).Succeeded,
                Is.True);
            Assert.That(session.PreviewTimeline(new TimelineCell(4, 0)).Succeeded, Is.True);
            Assert.That(session.CommitTimeline().Succeeded, Is.True);

            var result = session.ResolveTimeline();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Resolution.OccupantEffectResults, Has.Count.EqualTo(1));
            var tower = result.Resolution.OccupantEffectResults[0].After;
            Assert.That(tower.CreationId, Is.EqualTo("tower"));
            Assert.That(tower.Attitude, Is.EqualTo(CombatAttitude.Neutral));
            Assert.That(tower.Hp, Is.EqualTo(100));
            Assert.That(tower.Coordinate, Is.EqualTo(new HexCoord(0, 0)));
            var trace = sink.Entries.Single(item =>
                item.Command == "resolve-effect" && item.EffectKind == CardEffectKind.Built);
            Assert.That(trace.BeforeValue, Is.Zero);
            Assert.That(trace.AfterValue, Is.EqualTo(100));
        }

        [Test]
        public void PoisonPath_RequiresLivingStatusOccupantAndAccumulatesStacks()
        {
            var state = CreateRecoverState(10, 100);
            var session = CreateSession(state, out _, out var sink);

            Assert.That(session.SelectCard("poison").Succeeded, Is.True);
            Assert.That(
                session.SelectTarget(
                    CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Succeeded,
                Is.True);
            Assert.That(session.PreviewTimeline(new TimelineCell(4, 0)).Succeeded, Is.True);
            Assert.That(session.CommitTimeline().Succeeded, Is.True);

            var result = session.ResolveTimeline();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Resolution.OccupantEffectResults, Has.Count.EqualTo(1));
            Assert.That(result.Resolution.OccupantEffectResults[0].Before.PoisonStacks, Is.Zero);
            Assert.That(result.Resolution.OccupantEffectResults[0].After.PoisonStacks, Is.EqualTo(2));
            var trace = sink.Entries.Single(item =>
                item.Command == "resolve-effect" && item.EffectKind == CardEffectKind.Poison);
            Assert.That(trace.BeforeValue, Is.Zero);
            Assert.That(trace.AfterValue, Is.EqualTo(2));
        }

        [Test]
        public void PoisonSelection_RejectsDeadOrStatusUnsupportedOccupant()
        {
            var deadSession = CreateSession(CreateRecoverState(0, 100), out _, out _);
            Assert.That(deadSession.SelectCard("poison").Succeeded, Is.True);
            Assert.That(
                deadSession.SelectTarget(
                    CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Failure,
                Is.EqualTo(CombatCommandFailure.InvalidTarget));

            var board = CreateSevenTileBoard();
            var unsupported = new CombatOccupantState(
                "target-01",
                new HexCoord(0, 0),
                "entity",
                CombatAttitude.Enemy,
                hp: 10,
                maxHp: 100,
                poisonStacks: 0,
                supportsHealth: true,
                supportsStatus: false);
            var state = new CombatSliceState("target-01", 731, board, new[] { unsupported });
            var unsupportedSession = CreateSession(state, out _, out _);
            Assert.That(unsupportedSession.SelectCard("poison").Succeeded, Is.True);
            Assert.That(
                unsupportedSession.SelectTarget(
                    CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Failure,
                Is.EqualTo(CombatCommandFailure.InvalidTarget));
        }

        [Test]
        public void WindClear_SelectsNoMapTargetPreviewsThreeStatesAndCommitsImmediately()
        {
            var session = CreateSession(out var grid, out _);

            var selected = session.SelectCard("wind");

            Assert.That(selected.Succeeded, Is.True);
            Assert.That(selected.RequiredTargetKind, Is.Null);
            Assert.That(selected.InteractionMode, Is.EqualTo(CombatInteractionMode.TimelineClear));
            Assert.That(session.Current.InteractionMode, Is.EqualTo(CombatInteractionMode.TimelineClear));
            Assert.That(session.Current.Target, Is.Null);
            Assert.That(
                session.SelectTarget(CombatTarget.ForTile(new HexCoord(0, 0))).Failure,
                Is.EqualTo(CombatCommandFailure.InteractionModeMismatch));
            Assert.That(
                session.PreviewTimeline(new TimelineCell(2, 0)).Failure,
                Is.EqualTo(CombatCommandFailure.InteractionModeMismatch));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));

            var preview = session.PreviewClear(new TimelineCell(2, 0));

            Assert.That(preview.Succeeded, Is.True);
            Assert.That(preview.IsPlacementValid, Is.True);
            Assert.That(preview.ClearPreview.IsInBounds, Is.True);
            Assert.That(preview.ClearPreview.Cells, Has.Count.EqualTo(4));
            Assert.That(
                preview.ClearPreview.Cells.Count(cell =>
                    cell.State == TimelineClearCellState.Occupied),
                Is.EqualTo(1));
            Assert.That(preview.ClearPreview.HitActions, Has.Count.EqualTo(1));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));

            var committed = session.CommitClear();

            Assert.That(committed.Succeeded, Is.True);
            Assert.That(committed.PhaseAfter, Is.EqualTo(CombatSessionPhase.Resolved));
            Assert.That(committed.InteractionMode, Is.EqualTo(CombatInteractionMode.TimelineClear));
            Assert.That(committed.ClearResult.Succeeded, Is.True);
            Assert.That(committed.ClearResult.RemovedActions, Has.Count.EqualTo(1));
            Assert.That(committed.ClearResult.RemovedActions[0].CardId, Is.EqualTo("enemy-intent"));
            Assert.That(session.Current.SelectedCard, Is.Null);
            Assert.That(session.Current.InteractionMode, Is.Null);
            Assert.That(session.Current.LastClearResult, Is.SameAs(committed.ClearResult));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
            Assert.That(
                session.ResolveTimeline().Failure,
                Is.EqualTo(CombatCommandFailure.AlreadyResolved));
        }

        [Test]
        public void TornadoClear_UsesTypedTwelveByOneMaskAndClearsEnemyIntent()
        {
            var session = CreateSession(out var grid, out _);
            Assert.That(session.SelectCard("tornado").Succeeded, Is.True);

            var preview = session.PreviewClear(new TimelineCell(0, 0));
            var committed = session.CommitClear();

            Assert.That(preview.Succeeded, Is.True);
            Assert.That(preview.ClearPreview.Cells, Has.Count.EqualTo(12));
            Assert.That(preview.ClearPreview.HitActions, Has.Count.EqualTo(1));
            Assert.That(committed.Succeeded, Is.True);
            Assert.That(committed.ClearResult.RemovedActions, Has.Count.EqualTo(1));
            Assert.That(committed.ClearResult.RemovedCellCount, Is.EqualTo(1));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void ClearOutOfBoundsAndCancelArePureWhileRepeatedPreviewStaysDeterministic()
        {
            var session = CreateSession(out var grid, out _);
            Assert.That(session.SelectCard("wind").Succeeded, Is.True);

            var first = session.PreviewClear(new TimelineCell(11, 1));
            var repeated = session.PreviewClear(new TimelineCell(11, 1));
            var commit = session.CommitClear();

            Assert.That(first.Failure, Is.EqualTo(CombatCommandFailure.InvalidTimelinePlacement));
            Assert.That(repeated.Failure, Is.EqualTo(CombatCommandFailure.InvalidTimelinePlacement));
            Assert.That(
                repeated.ClearPreview.Cells.Select(cell => cell.State),
                Is.EqualTo(first.ClearPreview.Cells.Select(cell => cell.State)));
            Assert.That(commit.Failure, Is.EqualTo(CombatCommandFailure.InvalidTimelinePlacement));
            Assert.That(commit.ClearResult.Succeeded, Is.False);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));

            var cancelled = session.CancelCard();
            var repeatedCancel = session.CancelCard();

            Assert.That(cancelled.Succeeded, Is.True);
            Assert.That(repeatedCancel.Succeeded, Is.True);
            Assert.That(session.Current.Phase, Is.EqualTo(CombatSessionPhase.Cancelled));
            Assert.That(session.Current.ClearPreview, Is.Null);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
        }

        [Test]
        public void EmptyClearSucceedsWithoutOrdinaryCommitOrResolve()
        {
            var grid = new TimelineGrid();
            var session = new CombatApplicationSession(
                new TestCardCatalog(new[] { CreateWind() }),
                new CombatSliceState("target-01", 10, 731, CreateSevenTileBoard()),
                grid);

            Assert.That(session.SelectCard("wind").Succeeded, Is.True);
            Assert.That(session.PreviewClear(new TimelineCell(4, 1)).Succeeded, Is.True);
            var committed = session.CommitClear();

            Assert.That(committed.Succeeded, Is.True);
            Assert.That(committed.ClearResult.RemovedActions, Is.Empty);
            Assert.That(committed.ClearResult.RemovedCellCount, Is.Zero);
            Assert.That(session.Current.Phase, Is.EqualTo(CombatSessionPhase.Resolved));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void OrdinaryCardRejectsClearCommandsWithoutChangingItsSession()
        {
            var session = CreateSession(out var grid, out _);
            Assert.That(session.SelectCard("lighting").Succeeded, Is.True);

            var preview = session.PreviewClear(new TimelineCell(0, 0));
            var commit = session.CommitClear();

            Assert.That(preview.Failure, Is.EqualTo(CombatCommandFailure.InteractionModeMismatch));
            Assert.That(commit.Failure, Is.EqualTo(CombatCommandFailure.InteractionModeMismatch));
            Assert.That(session.Current.Phase, Is.EqualTo(CombatSessionPhase.CardSelected));
            Assert.That(session.Current.InteractionMode, Is.EqualTo(CombatInteractionMode.OrdinaryTimeline));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
        }

        [Test]
        public void InvalidCommandOrderReturnsStructuredFailuresWithoutMutation()
        {
            var session = CreateSession(out var grid, out var sink);

            var targetBeforeCard = session.SelectTarget(
                CombatTarget.ForEntity("target-01", new HexCoord(0, 0)));
            var previewBeforeCard = session.PreviewTimeline(new TimelineCell(0, 0));
            var unknownCard = session.SelectCard("missing");

            Assert.That(targetBeforeCard.Failure, Is.EqualTo(CombatCommandFailure.NoCardSelected));
            Assert.That(previewBeforeCard.Failure, Is.EqualTo(CombatCommandFailure.NoCardSelected));
            Assert.That(unknownCard.Failure, Is.EqualTo(CombatCommandFailure.UnknownCard));
            Assert.That(session.Current.Phase, Is.EqualTo(CombatSessionPhase.Idle));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
            Assert.That(sink.Entries.All(item => !string.IsNullOrWhiteSpace(item.FailureReason)), Is.True);
        }

        [Test]
        public void SelectTargetRejectsWrongKindUnknownEntityAndMissingTile()
        {
            var session = CreateSession(out var grid, out _);
            Assert.That(session.SelectCard("lighting").Succeeded, Is.True);

            var tileForLighting = session.SelectTarget(CombatTarget.ForTile(new HexCoord(0, 0)));
            var unknownEntity = session.SelectTarget(
                CombatTarget.ForEntity("other-target", new HexCoord(0, 0)));

            Assert.That(tileForLighting.Failure, Is.EqualTo(CombatCommandFailure.TargetKindMismatch));
            Assert.That(unknownEntity.Failure, Is.EqualTo(CombatCommandFailure.InvalidTarget));
            Assert.That(session.Current.Phase, Is.EqualTo(CombatSessionPhase.CardSelected));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));

            Assert.That(session.SelectCard("earthquake").Succeeded, Is.True);
            var missingTile = session.SelectTarget(CombatTarget.ForTile(new HexCoord(9, 9)));
            Assert.That(missingTile.Failure, Is.EqualTo(CombatCommandFailure.InvalidTarget));
            Assert.That(session.Current.Phase, Is.EqualTo(CombatSessionPhase.CardSelected));
        }

        [Test]
        public void PreviewRequiresTargetAndInvalidOriginDoesNotOccupyTimeline()
        {
            var session = CreateSession(out var grid, out _);
            Assert.That(session.SelectCard("lighting").Succeeded, Is.True);

            var withoutTarget = session.PreviewTimeline(new TimelineCell(0, 0));

            Assert.That(withoutTarget.Failure, Is.EqualTo(CombatCommandFailure.MissingTarget));
            Assert.That(session.Current.Phase, Is.EqualTo(CombatSessionPhase.CardSelected));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));

            Assert.That(
                session.SelectTarget(CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Succeeded,
                Is.True);
            var outOfBounds = session.PreviewTimeline(new TimelineCell(12, 0));

            Assert.That(
                outOfBounds.Failure,
                Is.EqualTo(CombatCommandFailure.InvalidTimelinePlacement));
            Assert.That(outOfBounds.IsPlacementValid, Is.False);
            Assert.That(session.Current.Phase, Is.EqualTo(CombatSessionPhase.TimelinePreview));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
        }

        [Test]
        public void SelectingSecondCardReplacesUncommittedSelectionWithoutPresentationEvents()
        {
            var session = CreateSession(out var grid, out _);
            Assert.That(session.SelectCard("lighting").Succeeded, Is.True);
            Assert.That(
                session.SelectTarget(CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Succeeded,
                Is.True);
            Assert.That(session.PreviewTimeline(new TimelineCell(0, 0)).Succeeded, Is.True);

            var result = session.SelectCard("earthquake");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(session.Current.Phase, Is.EqualTo(CombatSessionPhase.CardSelected));
            Assert.That(session.Current.SelectedCard.StableId, Is.EqualTo("earthquake"));
            Assert.That(session.Current.Target, Is.Null);
            Assert.That(session.Current.TimelineOrigin, Is.Null);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
        }

        [Test]
        public void CommitDetectsConcurrentTimelineMutationWithoutAdditionalPlacement()
        {
            var session = CreateSession(out var grid, out _);
            Assert.That(session.SelectCard("lighting").Succeeded, Is.True);
            Assert.That(
                session.SelectTarget(CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Succeeded,
                Is.True);
            Assert.That(session.PreviewTimeline(new TimelineCell(0, 0)).Succeeded, Is.True);
            Assert.That(grid.TryPlace(CreateEnemyIntent("concurrent", new TimelineCell(0, 0))), Is.True);

            var result = session.CommitTimeline();

            Assert.That(result.Failure, Is.EqualTo(CombatCommandFailure.InvalidTimelinePlacement));
            Assert.That(result.IsPlacementValid, Is.False);
            Assert.That(session.Current.Phase, Is.EqualTo(CombatSessionPhase.TimelinePreview));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(2));
        }

        [Test]
        public void CancelIsIdempotentUntilASelectionIsCommitted()
        {
            var session = CreateSession(out _, out _);
            Assert.That(session.SelectCard("lighting").Succeeded, Is.True);

            var first = session.CancelCard();
            var repeated = session.CancelCard();

            Assert.That(first.Succeeded, Is.True);
            Assert.That(repeated.Succeeded, Is.True);
            Assert.That(session.Current.Phase, Is.EqualTo(CombatSessionPhase.Cancelled));
            Assert.That(session.Current.SelectedCard, Is.Null);

            Assert.That(session.SelectCard("lighting").Succeeded, Is.True);
            Assert.That(
                session.SelectTarget(CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Succeeded,
                Is.True);
            Assert.That(session.PreviewTimeline(new TimelineCell(0, 0)).Succeeded, Is.True);
            Assert.That(session.CommitTimeline().Succeeded, Is.True);

            var afterCommit = session.CancelCard();

            Assert.That(afterCommit.Failure, Is.EqualTo(CombatCommandFailure.AlreadyCommitted));
        }

        [Test]
        public void InvalidEffectPayloadFailsExplicitlyBeforeOccupyingTimelineAndIsTraced()
        {
            var invalidPoison = new CardDefinition(
                "poison-invalid",
                70,
                new[] { new CardEffect(CardEffectKind.Poison, 3) },
                new[] { new HexCoord(0, 0) },
                new[] { new TimelineCell(0, 0) });
            var grid = new TimelineGrid();
            var sink = new CollectingCombatTraceSink();
            var session = new CombatApplicationSession(
                new TestCardCatalog(new[] { invalidPoison }),
                CreateRecoverState(10, 100),
                grid,
                traceSink: sink);
            Assert.That(session.SelectCard("poison-invalid").Succeeded, Is.True);
            Assert.That(
                session.SelectTarget(CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Succeeded,
                Is.True);

            var result = session.PreviewTimeline(new TimelineCell(0, 0));

            Assert.That(result.Failure, Is.EqualTo(CombatCommandFailure.UnsupportedEffect));
            Assert.That(result.FailureReason, Does.Contain("Poison"));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
            Assert.That(sink.Entries.Last().FailureReason, Does.Contain("Poison"));
        }

        [Test]
        public void RepeatedPreviewCommitAndResolveDoNotDuplicateMutation()
        {
            var session = CreateSession(out var grid, out _);
            Assert.That(session.SelectCard("lighting").Succeeded, Is.True);
            Assert.That(
                session.SelectTarget(CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Succeeded,
                Is.True);
            Assert.That(session.PreviewTimeline(new TimelineCell(1, 1)).Succeeded, Is.True);
            Assert.That(session.PreviewTimeline(new TimelineCell(0, 0)).Succeeded, Is.True);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
            Assert.That(session.CommitTimeline().Succeeded, Is.True);

            Assert.That(
                session.CommitTimeline().Failure,
                Is.EqualTo(CombatCommandFailure.AlreadyCommitted));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(2));
            Assert.That(session.ResolveTimeline().Succeeded, Is.True);
            Assert.That(
                session.ResolveTimeline().Failure,
                Is.EqualTo(CombatCommandFailure.AlreadyResolved));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void TraceSinkCannotChangeGameplayResult()
        {
            var noOpResult = RunLightingScenario(NoOpCombatTraceSink.Instance);
            var collectingSink = new CollectingCombatTraceSink();
            var collectingResult = RunLightingScenario(collectingSink);
            var throwingResult = RunLightingScenario(new ThrowingTraceSink());

            AssertEquivalent(noOpResult, collectingResult);
            AssertEquivalent(noOpResult, throwingResult);
            Assert.That(collectingSink.Entries.Select(item => item.Command), Is.EqualTo(new[]
            {
                "select-card",
                "select-target",
                "preview-timeline",
                "commit-timeline",
                "resolve-effect",
                "resolve-timeline"
            }));
            Assert.That(collectingSink.Entries.Last().PhaseAfter, Is.EqualTo("Resolved"));
            var effectTrace = collectingSink.Entries.Single(item => item.Command == "resolve-effect");
            Assert.That(effectTrace.EffectKind, Is.EqualTo(CardEffectKind.Damage));
            Assert.That(effectTrace.BeforeValue, Is.EqualTo(10));
            Assert.That(effectTrace.AfterValue, Is.EqualTo(0));
        }

        [Test]
        public void DisposeIsIdempotentAndFurtherCommandsFailWithoutMutation()
        {
            var session = CreateSession(out var grid, out _);

            session.Dispose();
            session.Dispose();
            var result = session.SelectCard("lighting");

            Assert.That(session.Current.Phase, Is.EqualTo(CombatSessionPhase.Disposed));
            Assert.That(result.Failure, Is.EqualTo(CombatCommandFailure.Disposed));
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
        }

        [Test]
        public void BattleFlow_UsesCardInstanceAndLocksResolvedOutcome()
        {
            var deck = new DeckState(
                new[]
                {
                    new CardInstance(
                        "lighting",
                        CardInstanceId.FromOrdinal("session-test", 0))
                },
                seed: 731,
                shuffleInitially: false);
            var hook = new BattleFlowNextTurnHook(
                deck,
                new BattleRoundLedger(),
                new BattleSettlementState(
                    "session-test",
                    731,
                    new BattleRewardEntry(
                        "reward-test",
                        BattleRewardKind.Acquire,
                        "获得卡牌")));
            var session = new CombatApplicationSession(
                CreateCatalog(),
                new CombatSliceState(
                    "target-01",
                    10,
                    731,
                    CreateSevenTileBoard()),
                new TimelineGrid(),
                enemyIntentSourceCatalog: EmptyIntentSourceCatalog.Instance,
                battleFlow: hook);
            var card = session.BattleFlowCurrent.Hand.Single();

            Assert.That(session.SelectCard(card.InstanceId.ToString()).Succeeded, Is.True);
            Assert.That(session.Current.SelectedCardInstanceId, Is.EqualTo(card.InstanceId));
            Assert.That(session.SelectTarget(
                CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Succeeded, Is.True);
            Assert.That(session.PreviewTimeline(new TimelineCell(0, 0)).Succeeded, Is.True);
            Assert.That(session.CommitTimeline().Succeeded, Is.True);
            Assert.That(session.BattleFlowCurrent.Hand, Is.Empty);
            Assert.That(session.BattleFlowCurrent.DiscardPile.Single().InstanceId,
                Is.EqualTo(card.InstanceId));
            Assert.That(session.ResolveTimeline().Succeeded, Is.True);

            var outcome = session.TryResolveBattleOutcome(
                100,
                BattleOutcome.VictorySettlement);
            var reward = session.TryClaimBattleReward(101);
            var duplicateReward = session.TryClaimBattleReward(102);
            var boundary = session.TryCreateBattleReturnBoundary();

            Assert.That(outcome.Succeeded, Is.True);
            Assert.That(session.Current.Phase, Is.EqualTo(CombatSessionPhase.Resolved));
            Assert.That(session.SelectCard("lighting").Failure,
                Is.EqualTo(CombatCommandFailure.AlreadyResolved));
            Assert.That(reward.Succeeded, Is.True);
            Assert.That(duplicateReward.Failure,
                Is.EqualTo(BattleSettlementFailure.RewardAlreadyClaimed));
            Assert.That(boundary.Succeeded, Is.True);
            Assert.That(boundary.Payload.IsCompleted, Is.True);
            Assert.That(boundary.Payload.DeckStableIds, Is.EqualTo(new[] { "lighting" }));
        }

        private static CombatApplicationSession CreateSession(
            out TimelineGrid grid,
            out CollectingCombatTraceSink sink)
        {
            return CreateSession(
                new CombatSliceState("target-01", 10, 731, CreateSevenTileBoard()),
                out grid,
                out sink);
        }

        private static CombatApplicationSession CreateSession(
            CombatSliceState state,
            out TimelineGrid grid,
            out CollectingCombatTraceSink sink)
        {
            grid = new TimelineGrid();
            sink = new CollectingCombatTraceSink();
            return new CombatApplicationSession(
                CreateCatalog(),
                state,
                grid,
                new[] { CreateEnemyIntent("enemy-intent", new TimelineCell(3, 0)) },
                sink);
        }

        private static CombatSliceState CreateRecoverState(int hp, int maxHp)
        {
            var board = CreateSevenTileBoard();
            var occupant = new CombatOccupantState(
                "target-01",
                new HexCoord(0, 0),
                "entity",
                CombatAttitude.Enemy,
                hp,
                maxHp,
                poisonStacks: 0,
                supportsHealth: true,
                supportsStatus: true);
            return new CombatSliceState(
                "target-01",
                731,
                board,
                new[] { occupant });
        }

        private static void CommitRecover(CombatApplicationSession session)
        {
            Assert.That(session.SelectCard("recover").Succeeded, Is.True);
            Assert.That(
                session.SelectTarget(CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Succeeded,
                Is.True);
            Assert.That(session.PreviewTimeline(new TimelineCell(0, 0)).Succeeded, Is.True);
            Assert.That(session.CommitTimeline().Succeeded, Is.True);
        }

        private static ResolutionSnapshot RunLightingScenario(ICombatTraceSink sink)
        {
            var grid = new TimelineGrid();
            var session = new CombatApplicationSession(
                CreateCatalog(),
                new CombatSliceState("target-01", 10, 731, CreateSevenTileBoard()),
                grid,
                new[] { CreateEnemyIntent("enemy-intent", new TimelineCell(3, 0)) },
                sink);
            Assert.That(session.SelectCard("lighting").Succeeded, Is.True);
            Assert.That(
                session.SelectTarget(CombatTarget.ForEntity("target-01", new HexCoord(0, 0))).Succeeded,
                Is.True);
            Assert.That(session.PreviewTimeline(new TimelineCell(0, 0)).Succeeded, Is.True);
            Assert.That(session.CommitTimeline().Succeeded, Is.True);
            return session.ResolveTimeline().Resolution;
        }

        private static void AssertEquivalent(ResolutionSnapshot expected, ResolutionSnapshot actual)
        {
            Assert.That(actual.Seed, Is.EqualTo(expected.Seed));
            Assert.That(actual.TargetHpBefore, Is.EqualTo(expected.TargetHpBefore));
            Assert.That(actual.TargetHpAfter, Is.EqualTo(expected.TargetHpAfter));
            Assert.That(actual.EnemyIntentResolved, Is.EqualTo(expected.EnemyIntentResolved));
            Assert.That(
                actual.ResolutionOrder.Select(item => item.CardId),
                Is.EqualTo(expected.ResolutionOrder.Select(item => item.CardId)));
        }

        private static ICardCatalog CreateCatalog()
        {
            return new TestCardCatalog(new[]
            {
                CreateLighting(),
                CreateEarthquake(),
                CreateRecover(),
                CreatePoison(),
                CreateBuilt(),
                CreateWind(),
                CreateTornado()
            });
        }

        private static CardDefinition CreateLighting()
        {
            return new CardDefinition(
                "lighting",
                1,
                new[] { new CardEffect(CardEffectKind.Damage, 100) },
                new[] { new HexCoord(0, 0) },
                new[] { new TimelineCell(0, 0) });
        }

        private static CardDefinition CreateEarthquake()
        {
            return new CardDefinition(
                "earthquake",
                2,
                new[] { new CardEffect(CardEffectKind.Elevation, 2) },
                new[]
                {
                    new HexCoord(0, 0),
                    new HexCoord(1, 0),
                    new HexCoord(-1, 0),
                    new HexCoord(0, 1),
                    new HexCoord(0, -1),
                    new HexCoord(1, -1),
                    new HexCoord(-1, 1)
                },
                new[] { new TimelineCell(0, 0), new TimelineCell(1, 0) });
        }

        private static CardDefinition CreateRecover()
        {
            return new CardDefinition(
                "recover",
                5,
                new[] { new CardEffect(CardEffectKind.Recover, 100) },
                new[]
                {
                    new HexCoord(0, 0),
                    new HexCoord(1, 0),
                    new HexCoord(-1, 1)
                },
                new[]
                {
                    new TimelineCell(0, 0),
                    new TimelineCell(1, 0),
                    new TimelineCell(2, 0)
                });
        }

        private static CardDefinition CreatePoison()
        {
            return new CardDefinition(
                "poison",
                7,
                new[] { new CardEffect(CardEffectKind.Poison, 2) },
                new[] { new HexCoord(0, 0) },
                new[] { new TimelineCell(0, 0) });
        }

        private static CardDefinition CreateBuilt()
        {
            return new CardDefinition(
                "tower",
                6,
                new[] { new CardEffect(CardEffectKind.Built, 1, "tower") },
                new[] { new HexCoord(0, 0) },
                new[]
                {
                    new TimelineCell(1, 0),
                    new TimelineCell(0, 1),
                    new TimelineCell(1, 1),
                    new TimelineCell(2, 1)
                });
        }

        private static CardDefinition CreateWind()
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

        private static CardDefinition CreateTornado()
        {
            return new CardDefinition(
                "tornado",
                8,
                new[]
                {
                    new CardEffect(
                        CardEffectKind.Clear,
                        Enumerable.Range(0, 12)
                            .Select(x => new TimelineCell(x, 0))
                            .ToArray())
                },
                new[] { new HexCoord(0, 0) },
                Array.Empty<TimelineCell>());
        }

        private static CombatBoardState CreateSevenTileBoard()
        {
            var board = new CombatBoardState();
            board.AddTile(new HexCoord(0, 0), 1);
            board.AddTile(new HexCoord(1, 0), 1);
            board.AddTile(new HexCoord(-1, 0), 1);
            board.AddTile(new HexCoord(0, 1), 1);
            board.AddTile(new HexCoord(0, -1), 1);
            board.AddTile(new HexCoord(1, -1), 1);
            board.AddTile(new HexCoord(-1, 1), 1);
            return board;
        }

        private static TimelineAction CreateEnemyIntent(string id, TimelineCell origin)
        {
            return new TimelineAction(
                TimelineActorKind.Enemy,
                id,
                "target-01",
                origin,
                new[] { new TimelineCell(0, 0) },
                0);
        }

        private sealed class EmptyIntentSourceCatalog : IEnemyIntentSourceCatalog
        {
            public static EmptyIntentSourceCatalog Instance { get; } =
                new EmptyIntentSourceCatalog();

            public IReadOnlyList<EnemyIntentSourceSnapshot> CaptureSources(
                CombatSliceState state)
            {
                return Array.Empty<EnemyIntentSourceSnapshot>();
            }
        }

        private sealed class TestCardCatalog : ICardCatalog
        {
            private readonly Dictionary<string, CardDefinition> _cardsById;

            public TestCardCatalog(IReadOnlyList<CardDefinition> cards)
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

        private sealed class ThrowingTraceSink : ICombatTraceSink
        {
            public void Record(CombatTraceEntry entry)
            {
                throw new InvalidOperationException("Diagnostics must be observational.");
            }
        }
    }
}
