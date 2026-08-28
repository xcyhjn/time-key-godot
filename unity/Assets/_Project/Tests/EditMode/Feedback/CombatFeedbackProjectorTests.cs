using NUnit.Framework;
using TimeKey.Application;
using TimeKey.Domain;
using TimeKey.Domain.BattleFlow;
using TimeKey.Presentation.Feedback;

namespace TimeKey.Tests.EditMode.Feedback
{
    public sealed class CombatFeedbackProjectorTests
    {
        [Test]
        public void Trace_DamageProjectsTypedEventAtRealCoordinate()
        {
            var projector = new CombatFeedbackProjector(40);
            var trace = new CombatTraceEntry(
                "resolve-effect",
                "Resolved",
                "Resolved",
                "lighting",
                "enemy-01",
                new HexCoord(1, 0),
                new TimelineCell(2, 1),
                effectKind: CardEffectKind.Damage,
                beforeValue: 10,
                afterValue: 0);

            var events = projector.ProjectTrace(trace);

            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Sequence, Is.EqualTo(41));
            Assert.That(events[0].Kind, Is.EqualTo(CombatFeedbackKind.Damage));
            Assert.That(events[0].SourceId, Is.EqualTo("lighting"));
            Assert.That(events[0].TargetId, Is.EqualTo("enemy-01"));
            Assert.That(events[0].WorldAnchor.Coordinate, Is.EqualTo(new HexCoord(1, 0)));
            Assert.That(events[0].IsNoEffect, Is.False);
        }

        [Test]
        public void Trace_OrdinaryUnknownCommandDoesNotInventFeedback()
        {
            var projector = new CombatFeedbackProjector();
            var trace = new CombatTraceEntry("preview-timeline", "TargetSelected", "TimelinePreview");

            Assert.That(projector.ProjectTrace(trace), Is.Empty);
            Assert.That(projector.NextSequence, Is.EqualTo(1));
        }

        [Test]
        public void Trace_ExplicitUnsupportedSourceCommandIsNoEffect()
        {
            var projector = new CombatFeedbackProjector();
            var trace = new CombatTraceEntry(
                "unsupported-source-command",
                "ResolvingTimeline",
                "ResolvingTimeline",
                "enemy-intent",
                "enemy-01",
                new HexCoord(-1, 1));

            var value = projector.ProjectTrace(trace)[0];

            Assert.That(value.Kind, Is.EqualTo(CombatFeedbackKind.UnsupportedSourceCommand));
            Assert.That(value.IsNoEffect, Is.True);
            Assert.That(value.Duration, Is.Zero);
            Assert.That(value.Phase, Is.EqualTo(CombatFeedbackPhase.Completed));
        }

        [TestCase(CardEffectKind.Damage, CombatFeedbackKind.Damage)]
        [TestCase(CardEffectKind.Elevation, CombatFeedbackKind.ElevationPulse)]
        [TestCase(CardEffectKind.Recover, CombatFeedbackKind.Recover)]
        [TestCase(CardEffectKind.Built, CombatFeedbackKind.Built)]
        [TestCase(CardEffectKind.Poison, CombatFeedbackKind.PoisonApply)]
        [TestCase(CardEffectKind.Clear, CombatFeedbackKind.Clear)]
        public void Trace_MapsEveryCardEffect(
            CardEffectKind effectKind,
            CombatFeedbackKind expectedKind)
        {
            var projector = new CombatFeedbackProjector();
            var trace = new CombatTraceEntry(
                "resolve-effect",
                "Resolved",
                "Resolved",
                "card",
                effectKind: effectKind);

            Assert.That(projector.ProjectTrace(trace)[0].Kind, Is.EqualTo(expectedKind));
        }

        [Test]
        public void Lifecycle_MapsTowerAndPoisonChangesWithoutChangingDomainValues()
        {
            var lifecycle = new TurnLifecycleRunner().Run(TurnLifecycleRequest.InitialStart());
            var towerMutation = new LifecycleOccupantMutation(
                "tower-01",
                new HexCoord(0, 0),
                TurnLifecyclePhase.RunningBuildingBehaviors,
                LifecycleMutationReason.TowerDecay,
                expectedHp: 100,
                expectedPoisonStacks: 0,
                afterHp: 50,
                afterPoisonStacks: 0,
                remove: false);
            var poisonMutation = new LifecycleOccupantMutation(
                "enemy-01",
                new HexCoord(1, 0),
                TurnLifecyclePhase.ProcessingTurnStartStatuses,
                LifecycleMutationReason.PoisonTurnStart,
                expectedHp: 10,
                expectedPoisonStacks: 2,
                afterHp: 0,
                afterPoisonStacks: 0,
                remove: false);
            var changes = new[]
            {
                new LifecycleOccupantChangeResult(1, towerMutation),
                new LifecycleOccupantChangeResult(1, poisonMutation)
            };

            var events = new CombatFeedbackProjector().ProjectLifecycle(lifecycle, changes);

            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events[0].Kind, Is.EqualTo(CombatFeedbackKind.TowerDecay));
            Assert.That(events[1].Kind, Is.EqualTo(CombatFeedbackKind.PoisonTick));
            Assert.That(changes[0].BeforeHp, Is.EqualTo(100));
            Assert.That(changes[0].AfterHp, Is.EqualTo(50));
            Assert.That(changes[1].BeforePoisonStacks, Is.EqualTo(2));
        }

        [TestCase(BattleOutcome.VictorySettlement, CombatFeedbackKind.Victory)]
        [TestCase(BattleOutcome.Defeat, CombatFeedbackKind.Defeat)]
        public void BattleSettlement_ProjectsTerminalKind(
            BattleOutcome outcome,
            CombatFeedbackKind expectedKind)
        {
            var settlement = new BattleSettlementState(
                "battle-01",
                31,
                new BattleRewardEntry("reward-01", BattleRewardKind.Acquire, "获得卡牌"));

            var events = new CombatFeedbackProjector().ProjectBattleSettlement(
                settlement.TryResolve(1, outcome));

            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Kind, Is.EqualTo(expectedKind));
            Assert.That(events[0].SourceId, Is.EqualTo("battle-01"));
        }

        [Test]
        public void Projector_AssignsStrictlyIncreasingSequenceAcrossSources()
        {
            var projector = new CombatFeedbackProjector(7);
            var draw = projector.ProjectCardDraw("lighting");
            var confirm = projector.ProjectCardConfirm(
                "lighting",
                "enemy-01",
                CombatFeedbackWorldAnchor.FromCoordinate(new HexCoord(1, 0)));
            var transition = projector.ProjectSceneTransition("Combat", "OutOfBattle");

            Assert.That(draw.Sequence, Is.EqualTo(8));
            Assert.That(confirm.Sequence, Is.EqualTo(9));
            Assert.That(transition.Sequence, Is.EqualTo(10));
            Assert.That(projector.NextSequence, Is.EqualTo(11));
        }
    }
}
