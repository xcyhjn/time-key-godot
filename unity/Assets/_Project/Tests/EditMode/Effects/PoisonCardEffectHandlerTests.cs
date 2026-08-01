using System;
using System.Collections.Generic;
using NUnit.Framework;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode.Effects
{
    public sealed class PoisonCardEffectHandlerTests
    {
        private static readonly HexCoord TargetCoordinate = new HexCoord(1, 0);

        [Test]
        public void Resolve_LivingStatusOccupantAddsTwoStacksAndCapturesSnapshots()
        {
            var state = CreateState(CreateOccupant("target-01", hp: 10, poisonStacks: 0));
            var resolution = Resolve(state);

            Assert.That(state.TryGetOccupant("target-01", TargetCoordinate, out var target), Is.True);
            Assert.That(target.PoisonStacks, Is.EqualTo(2));
            Assert.That(target.Hp, Is.EqualTo(10));
            Assert.That(resolution.OccupantEffectResults, Has.Count.EqualTo(1));
            var result = resolution.OccupantEffectResults[0];
            Assert.That(result.EffectKind, Is.EqualTo(CardEffectKind.Poison));
            Assert.That(result.Before.PoisonStacks, Is.Zero);
            Assert.That(result.After.PoisonStacks, Is.EqualTo(2));
        }

        [Test]
        public void Resolve_RepeatedApplicationsAccumulateAndEarlierSnapshotStaysStable()
        {
            var state = CreateState(CreateOccupant("target-01", hp: 10, poisonStacks: 0));
            var first = Resolve(state).OccupantEffectResults[0];
            var second = Resolve(state).OccupantEffectResults[0];

            Assert.That(first.Before.PoisonStacks, Is.Zero);
            Assert.That(first.After.PoisonStacks, Is.EqualTo(2));
            Assert.That(second.Before.PoisonStacks, Is.EqualTo(2));
            Assert.That(second.After.PoisonStacks, Is.EqualTo(4));
            Assert.That(state.TryGetOccupant("target-01", TargetCoordinate, out var target), Is.True);
            Assert.That(target.PoisonStacks, Is.EqualTo(4));
        }

        [Test]
        public void Resolve_EmptyTileIsPureNoOp()
        {
            var state = CreateState();
            Assert.That(Resolve(state).OccupantEffectResults, Is.Empty);
            Assert.That(state.OccupantCount, Is.Zero);
        }

        [TestCase(0, true)]
        [TestCase(10, false)]
        public void Resolve_DeadOrStatusUnsupportedOccupantIsPureNoOp(
            int hp,
            bool supportsStatus)
        {
            var state = CreateState(CreateOccupant(
                "target-01",
                hp,
                poisonStacks: 0,
                supportsStatus));

            Assert.That(Resolve(state).OccupantEffectResults, Is.Empty);
            Assert.That(state.TryGetOccupant("target-01", TargetCoordinate, out var target), Is.True);
            Assert.That(target.PoisonStacks, Is.Zero);
        }

        [Test]
        public void Resolve_IdOrCoordinateReplacementIsPureNoOp()
        {
            var replacement = CreateOccupant("replacement", hp: 10, poisonStacks: 1);
            var state = CreateState(replacement);

            Assert.That(Resolve(state, targetId: "target-01").OccupantEffectResults, Is.Empty);
            Assert.That(state.TryGetOccupant("replacement", TargetCoordinate, out var target), Is.True);
            Assert.That(target.PoisonStacks, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_MissingCoordinateOrRangeIsPureNoOp()
        {
            var state = CreateState(CreateOccupant("target-01", hp: 10, poisonStacks: 0));

            Assert.That(Resolve(state, includeCoordinate: false).OccupantEffectResults, Is.Empty);
            Assert.That(Resolve(state, effectRange: Array.Empty<HexCoord>()).OccupantEffectResults, Is.Empty);
            Assert.That(state.TryGetOccupant("target-01", TargetCoordinate, out var target), Is.True);
            Assert.That(target.PoisonStacks, Is.Zero);
        }

        [TestCase(0)]
        [TestCase(3)]
        public void TryPlace_UnsupportedValueFailsBeforeOccupyingTimeline(int value)
        {
            var grid = CreateGrid();
            var exception = Assert.Throws<UnsupportedCardEffectException>(
                () => grid.TryPlace(CreateAction(
                    effect: new CardEffect(CardEffectKind.Poison, value))));

            Assert.That(exception.Kind, Is.EqualTo(CardEffectKind.Poison));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void Resolve_IntegerOverflowFailsExplicitlyWithoutMutation()
        {
            var state = CreateState(CreateOccupant(
                "target-01",
                hp: 10,
                poisonStacks: int.MaxValue - 1));
            var grid = CreateGrid();
            Assert.That(grid.TryPlace(CreateAction()), Is.True);

            Assert.Throws<OverflowException>(() => grid.Resolve(state));
            Assert.That(state.TryGetOccupant("target-01", TargetCoordinate, out var target), Is.True);
            Assert.That(target.PoisonStacks, Is.EqualTo(int.MaxValue - 1));
        }

        private static ResolutionSnapshot Resolve(
            CombatSliceState state,
            string targetId = "target-01",
            bool includeCoordinate = true,
            IReadOnlyList<HexCoord> effectRange = null)
        {
            var grid = CreateGrid();
            Assert.That(
                grid.TryPlace(CreateAction(targetId, includeCoordinate, effectRange)),
                Is.True);
            return grid.Resolve(state);
        }

        private static TimelineGrid CreateGrid()
        {
            return new TimelineGrid(
                effectHandlers: new ICardEffectHandler[] { new PoisonCardEffectHandler() });
        }

        private static TimelineAction CreateAction(
            string targetId = "target-01",
            bool includeCoordinate = true,
            IReadOnlyList<HexCoord> effectRange = null,
            CardEffect? effect = null)
        {
            return new TimelineAction(
                TimelineActorKind.Player,
                "poison",
                targetId,
                new TimelineCell(0, 0),
                new[] { new TimelineCell(0, 0) },
                includeCoordinate ? TargetCoordinate : (HexCoord?)null,
                new[] { effect ?? new CardEffect(CardEffectKind.Poison, 2) },
                effectRange ?? new[] { new HexCoord(0, 0) });
        }

        private static CombatSliceState CreateState(params CombatOccupantState[] occupants)
        {
            var board = new CombatBoardState();
            board.AddTile(TargetCoordinate, 2);
            return new CombatSliceState("target-01", 731, board, occupants);
        }

        private static CombatOccupantState CreateOccupant(
            string runtimeId,
            int hp,
            int poisonStacks,
            bool supportsStatus = true)
        {
            return new CombatOccupantState(
                runtimeId,
                TargetCoordinate,
                "entity",
                CombatAttitude.Enemy,
                hp,
                maxHp: 100,
                poisonStacks,
                supportsHealth: true,
                supportsStatus);
        }
    }
}
