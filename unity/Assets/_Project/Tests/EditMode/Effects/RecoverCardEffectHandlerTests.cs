using System;
using NUnit.Framework;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode.Effects
{
    public sealed class RecoverCardEffectHandlerTests
    {
        private static readonly HexCoord TargetCoordinate = new HexCoord(2, -1);

        [Test]
        public void Resolve_HealsInjuredOccupantAndCapturesClampedBeforeAfterSnapshots()
        {
            var state = CreateState(25, 100);
            var grid = new TimelineGrid();
            Assert.That(grid.TryPlace(CreateRecoverAction("target-01", TargetCoordinate)), Is.True);

            var resolution = grid.Resolve(state);

            Assert.That(state.TryGetOccupant("target-01", TargetCoordinate, out var occupant), Is.True);
            Assert.That(occupant.Hp, Is.EqualTo(100));
            Assert.That(resolution.EffectResults, Is.Empty);
            Assert.That(resolution.OccupantEffectResults, Has.Count.EqualTo(1));
            var result = resolution.OccupantEffectResults[0];
            Assert.That(result.EffectKind, Is.EqualTo(CardEffectKind.Recover));
            Assert.That(result.Before.RuntimeId, Is.EqualTo("target-01"));
            Assert.That(result.Before.Coordinate, Is.EqualTo(TargetCoordinate));
            Assert.That(result.Before.Hp, Is.EqualTo(25));
            Assert.That(result.Before.MaxHp, Is.EqualTo(100));
            Assert.That(result.After.Hp, Is.EqualTo(100));
            Assert.That(result.After.MaxHp, Is.EqualTo(100));
        }

        [Test]
        public void Resolve_ExistingZeroHpOccupantCanRecover()
        {
            var state = CreateState(0, 100);
            var grid = new TimelineGrid();
            Assert.That(grid.TryPlace(CreateRecoverAction("target-01", TargetCoordinate)), Is.True);

            var resolution = grid.Resolve(state);

            Assert.That(state.TryGetOccupant("target-01", TargetCoordinate, out var occupant), Is.True);
            Assert.That(occupant.Hp, Is.EqualTo(100));
            Assert.That(resolution.OccupantEffectResults[0].Before.Hp, Is.Zero);
            Assert.That(resolution.OccupantEffectResults[0].After.Hp, Is.EqualTo(100));
        }

        [Test]
        public void Resolve_FullHealthOccupantIsNoOpWithoutResult()
        {
            var state = CreateState(100, 100);
            var grid = new TimelineGrid();
            Assert.That(grid.TryPlace(CreateRecoverAction("target-01", TargetCoordinate)), Is.True);

            var resolution = grid.Resolve(state);

            Assert.That(state.TryGetOccupant("target-01", TargetCoordinate, out var occupant), Is.True);
            Assert.That(occupant.Hp, Is.EqualTo(100));
            Assert.That(resolution.OccupantEffectResults, Is.Empty);
        }

        [TestCase("missing-id", 2, -1)]
        [TestCase("target-01", 3, -1)]
        public void Resolve_MissingIdOrCoordinateMatchIsPureNoOp(string targetId, int q, int r)
        {
            var state = CreateState(25, 100);
            var grid = new TimelineGrid();
            Assert.That(
                grid.TryPlace(CreateRecoverAction(targetId, new HexCoord(q, r))),
                Is.True);

            var resolution = grid.Resolve(state);

            Assert.That(state.TryGetOccupant("target-01", TargetCoordinate, out var occupant), Is.True);
            Assert.That(occupant.Hp, Is.EqualTo(25));
            Assert.That(resolution.OccupantEffectResults, Is.Empty);
        }

        [Test]
        public void Resolve_ReplacedOccupantWithDifferentRuntimeIdIsNoOp()
        {
            var state = CreateState(25, 100);
            var grid = new TimelineGrid();
            Assert.That(grid.TryPlace(CreateRecoverAction("target-01", TargetCoordinate)), Is.True);
            Assert.That(state.TryRemoveOccupant("target-01", TargetCoordinate), Is.True);
            Assert.That(
                state.TryAddOccupant(CreateOccupant("replacement", TargetCoordinate, 10, 100)),
                Is.True);

            var resolution = grid.Resolve(state);

            Assert.That(state.TryGetOccupant("replacement", TargetCoordinate, out var replacement), Is.True);
            Assert.That(replacement.Hp, Is.EqualTo(10));
            Assert.That(resolution.OccupantEffectResults, Is.Empty);
        }

        [Test]
        public void Resolve_MissingEffectRangeIsPureNoOp()
        {
            var state = CreateState(25, 100);
            var grid = new TimelineGrid();
            var action = new TimelineAction(
                TimelineActorKind.Player,
                "recover",
                "target-01",
                new TimelineCell(0, 0),
                new[] { new TimelineCell(0, 0) },
                TargetCoordinate,
                new[] { new CardEffect(CardEffectKind.Recover, 100) },
                Array.Empty<HexCoord>());
            Assert.That(grid.TryPlace(action), Is.True);

            var resolution = grid.Resolve(state);

            Assert.That(state.TryGetOccupant("target-01", TargetCoordinate, out var occupant), Is.True);
            Assert.That(occupant.Hp, Is.EqualTo(25));
            Assert.That(resolution.OccupantEffectResults, Is.Empty);
        }

        [Test]
        public void Resolve_OccupantWithoutHealthCapabilityIsPureNoOp()
        {
            var occupant = new CombatOccupantState(
                "target-01",
                TargetCoordinate,
                "entity",
                CombatAttitude.Enemy,
                hp: 0,
                maxHp: 0,
                poisonStacks: 0,
                supportsHealth: false,
                supportsStatus: true);
            var state = CreateState(occupant);
            var grid = new TimelineGrid();
            Assert.That(grid.TryPlace(CreateRecoverAction("target-01", TargetCoordinate)), Is.True);

            var resolution = grid.Resolve(state);

            Assert.That(state.TryGetOccupant("target-01", TargetCoordinate, out var after), Is.True);
            Assert.That(after.Hp, Is.Zero);
            Assert.That(resolution.OccupantEffectResults, Is.Empty);
        }

        [Test]
        public void ThreeCellShape_FitsAtLastLegalOriginAndRejectsBoundaryCrossing()
        {
            var legalGrid = new TimelineGrid();
            var illegalGrid = new TimelineGrid();

            Assert.That(
                legalGrid.TryPlace(CreateRecoverAction("target-01", TargetCoordinate, new TimelineCell(9, 0))),
                Is.True);
            Assert.That(legalGrid.OccupiedCellCount, Is.EqualTo(3));
            Assert.That(
                illegalGrid.TryPlace(CreateRecoverAction("target-01", TargetCoordinate, new TimelineCell(10, 0))),
                Is.False);
            Assert.That(illegalGrid.OccupiedCellCount, Is.Zero);
        }

        private static TimelineAction CreateRecoverAction(
            string targetId,
            HexCoord targetCoordinate,
            TimelineCell origin = default)
        {
            return TimelineAction.FromCard(CreateRecoverCard(), targetId, targetCoordinate, origin);
        }

        private static CardDefinition CreateRecoverCard()
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

        private static CombatSliceState CreateState(int hp, int maxHp)
        {
            return CreateState(CreateOccupant("target-01", TargetCoordinate, hp, maxHp));
        }

        private static CombatSliceState CreateState(CombatOccupantState occupant)
        {
            var board = new CombatBoardState();
            board.AddTile(TargetCoordinate, 1);
            return new CombatSliceState(
                "target-01",
                731,
                board,
                new[] { occupant });
        }

        private static CombatOccupantState CreateOccupant(
            string runtimeId,
            HexCoord coordinate,
            int hp,
            int maxHp)
        {
            return new CombatOccupantState(
                runtimeId,
                coordinate,
                "entity",
                CombatAttitude.Enemy,
                hp,
                maxHp,
                poisonStacks: 0,
                supportsHealth: true,
                supportsStatus: true);
        }
    }
}
