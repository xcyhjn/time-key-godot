using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Domain.Intents;

namespace TimeKey.Tests.EditMode.Application.Lifecycle
{
    public sealed class CombatTurnLifecycleCoordinatorTests
    {
        [Test]
        public void InitialStart_GeneratesUnsupportedIntentFromLiveOccupant()
        {
            var state = CreateState();
            var timeline = new TimelineGrid();
            var coordinator = CreateCoordinator(state, timeline);

            var result = coordinator.RunInitialStart();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.PhaseHistory, Is.EqualTo(new[]
            {
                TurnLifecyclePhase.ProcessingTurnStartStatuses,
                TurnLifecyclePhase.RefreshingEnemyIntents,
                TurnLifecyclePhase.PlayerReady
            }));
            var intent = timeline.ScheduledActions.Single();
            Assert.That(intent.SourceId, Is.EqualTo("target-01"));
            Assert.That(intent.SourceCoord, Is.EqualTo(new HexCoord(1, 0)));
            Assert.That(intent.ActionId, Is.EqualTo(TimelineActionIdentity.FromSequence(1, 0)));
            Assert.That(
                coordinator.TryGetScheduledIntentSnapshot(intent.ActionId, out var snapshot),
                Is.True);
            Assert.That(snapshot.Validity, Is.EqualTo(TimelineActionValidity.Unsupported));
            Assert.That(
                snapshot.InvalidReason,
                Is.EqualTo(TimelineActionInvalidReason.UnsupportedSourceCommand));
        }

        [Test]
        public void TwoEndTurns_DecayAndRemoveTowerThroughSameRunner()
        {
            var state = CreateState();
            Assert.That(state.TryAddOccupant(new CombatOccupantState(
                "tower-01",
                new HexCoord(0, 0),
                "building",
                CombatAttitude.Neutral,
                100,
                100,
                0,
                supportsHealth: true,
                supportsStatus: true,
                creationId: "tower")), Is.True);
            var timeline = new TimelineGrid();
            var coordinator = CreateCoordinator(state, timeline);
            Assert.That(coordinator.RunInitialStart().Succeeded, Is.True);

            var first = coordinator.RunEndTurn();

            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.PhaseHistory, Is.EqualTo(EndTurnPhases));
            Assert.That(state.TryGetOccupant("tower-01", out var tower), Is.True);
            Assert.That(tower.Hp, Is.EqualTo(50));
            Assert.That(coordinator.LifecycleChanges.Single(
                value => value.RuntimeId == "tower-01").AfterHp, Is.EqualTo(50));
            Assert.That(timeline.ScheduledActions, Has.Count.EqualTo(1));

            var second = coordinator.RunEndTurn();

            Assert.That(second.Succeeded, Is.True);
            Assert.That(state.TryGetOccupant("tower-01", out _), Is.False);
            var removal = coordinator.LifecycleChanges.Single(
                value => value.RuntimeId == "tower-01");
            Assert.That(removal.BeforeHp, Is.EqualTo(50));
            Assert.That(removal.AfterHp, Is.Zero);
            Assert.That(removal.Removed, Is.True);
            Assert.That(timeline.ScheduledActions, Has.Count.EqualTo(1));
        }

        [Test]
        public void EndTurn_PoisonUsesEntrySnapshotAndIsolatesNewInfection()
        {
            var state = CreateState();
            Assert.That(state.TryAddOccupant(new CombatOccupantState(
                "ally-01",
                new HexCoord(0, 0),
                "entity",
                CombatAttitude.Player,
                100,
                100,
                0,
                supportsHealth: true,
                supportsStatus: true)), Is.True);
            var coordinator = CreateCoordinator(state, new TimelineGrid());
            Assert.That(coordinator.RunInitialStart().Succeeded, Is.True);
            Assert.That(
                state.TryAddOccupantPoisonStacks("target-01", new HexCoord(1, 0), 2),
                Is.True);

            Assert.That(coordinator.RunEndTurn().Succeeded, Is.True);

            Assert.That(state.TryGetOccupant("target-01", out var source), Is.True);
            Assert.That(source.Hp, Is.EqualTo(80));
            Assert.That(source.PoisonStacks, Is.EqualTo(1));
            Assert.That(state.TryGetOccupant("ally-01", out var infected), Is.True);
            Assert.That(infected.Hp, Is.EqualTo(100));
            Assert.That(infected.PoisonStacks, Is.EqualTo(1));
        }

        [Test]
        public void EndTurn_RemovesInvalidIntentWithoutApplyingFakeEffect()
        {
            var state = CreateState();
            var timeline = new TimelineGrid();
            var coordinator = CreateCoordinator(state, timeline);
            Assert.That(coordinator.RunInitialStart().Succeeded, Is.True);
            Assert.That(state.TryRemoveOccupant("target-01", new HexCoord(1, 0)), Is.True);

            var result = coordinator.RunEndTurn();

            Assert.That(result.Succeeded, Is.True);
            var intentResult = coordinator.IntentResolveResults.Single();
            Assert.That(intentResult.Succeeded, Is.False);
            Assert.That(
                intentResult.Outcome,
                Is.EqualTo(EnemyIntentResolveOutcome.RemovedInvalid));
            Assert.That(intentResult.EffectApplied, Is.False);
            Assert.That(coordinator.LastResolution.EnemyIntentResolved, Is.False);
            Assert.That(timeline.ScheduledActions, Is.Empty);
        }

        private static readonly TurnLifecyclePhase[] EndTurnPhases =
        {
            TurnLifecyclePhase.EndTurnRequested,
            TurnLifecyclePhase.ResolvingTimeline,
            TurnLifecyclePhase.RunningBuildingBehaviors,
            TurnLifecyclePhase.ClearingTimeline,
            TurnLifecyclePhase.ProcessingTurnStartStatuses,
            TurnLifecyclePhase.RefreshingEnemyIntents,
            TurnLifecyclePhase.PlayerReady
        };

        private static CombatTurnLifecycleCoordinator CreateCoordinator(
            CombatSliceState state,
            TimelineGrid timeline)
        {
            return new CombatTurnLifecycleCoordinator(
                state,
                timeline,
                new TestIntentSourceCatalog());
        }

        private static CombatSliceState CreateState()
        {
            var board = new CombatBoardState();
            board.AddTile(new HexCoord(0, 0), 1);
            board.AddTile(new HexCoord(1, 0), 1);
            board.AddTile(new HexCoord(2, 0), 1);
            return new CombatSliceState("target-01", 100, 8721, board);
        }

        private sealed class TestIntentSourceCatalog : IEnemyIntentSourceCatalog
        {
            public IReadOnlyList<EnemyIntentSourceSnapshot> CaptureSources(
                CombatSliceState state)
            {
                var result = new List<EnemyIntentSourceSnapshot>();
                var occupants = state.CaptureOccupants();
                for (var index = 0; index < occupants.Count; index++)
                {
                    var occupant = occupants[index];
                    if (occupant.Attitude != CombatAttitude.Enemy)
                    {
                        continue;
                    }

                    result.Add(new EnemyIntentSourceSnapshot(
                        occupant.RuntimeId,
                        occupant.Coordinate,
                        occupant.Kind,
                        occupant.IsAlive,
                        occupant.IsAlive,
                        "enemy-intent",
                        0,
                        new EnemyIntentTargetSnapshot(
                            occupant.RuntimeId,
                            occupant.Coordinate,
                            true,
                            occupant.RuntimeId,
                            CombatAttitude.Enemy),
                        new[] { new TimelineCell(0, 0) },
                        new EnemyIntentEffectSnapshot(
                            "enemy-intent-effect",
                            null,
                            new[] { new HexCoord(0, 0) }),
                        new EnemyIntentDisplayData(
                            "敌方意图",
                            "源命令暂不支持，本轮不产生效果")));
                }

                return result;
            }
        }
    }
}
