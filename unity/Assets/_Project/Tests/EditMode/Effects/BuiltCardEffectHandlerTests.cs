using System;
using System.Collections.Generic;
using NUnit.Framework;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode.Effects
{
    public sealed class BuiltCardEffectHandlerTests
    {
        private static readonly HexCoord TargetCoordinate = new HexCoord(2, -1);

        [Test]
        public void Resolve_EmptyExistingTileCreatesOneNeutralTowerAndCapturesResult()
        {
            var state = CreateState();
            var grid = CreateGrid();
            Assert.That(grid.TryPlace(CreateBuiltAction()), Is.True);

            var resolution = grid.Resolve(state);

            Assert.That(state.OccupantCount, Is.EqualTo(1));
            Assert.That(state.TryGetOccupant(TargetCoordinate, out var tower), Is.True);
            Assert.That(tower.RuntimeId, Is.Not.Empty);
            Assert.That(tower.Coordinate, Is.EqualTo(TargetCoordinate));
            Assert.That(tower.Kind, Is.EqualTo("tower"));
            Assert.That(tower.CreationId, Is.EqualTo("tower"));
            Assert.That(tower.Attitude, Is.EqualTo(CombatAttitude.Neutral));
            Assert.That(tower.Hp, Is.EqualTo(100));
            Assert.That(tower.MaxHp, Is.EqualTo(100));
            Assert.That(tower.PoisonStacks, Is.Zero);
            Assert.That(tower.SupportsHealth, Is.True);
            Assert.That(tower.SupportsStatus, Is.True);
            Assert.That(resolution.EffectResults, Is.Empty);
            Assert.That(resolution.OccupantEffectResults, Has.Count.EqualTo(1));
            var result = resolution.OccupantEffectResults[0];
            Assert.That(result.EffectKind, Is.EqualTo(CardEffectKind.Built));
            Assert.That(result.Before, Is.Null);
            Assert.That(result.After.RuntimeId, Is.EqualTo(tower.RuntimeId));
            Assert.That(result.After.Coordinate, Is.EqualTo(TargetCoordinate));
            Assert.That(result.After.CreationId, Is.EqualTo("tower"));
            Assert.That(result.After.Hp, Is.EqualTo(100));
        }

        [Test]
        public void Resolve_MissingTileIsPureNoOp()
        {
            var board = new CombatBoardState();
            var state = CreateState(board);
            var grid = CreateGrid();
            Assert.That(grid.TryPlace(CreateBuiltAction()), Is.True);

            var resolution = grid.Resolve(state);

            Assert.That(state.OccupantCount, Is.Zero);
            Assert.That(state.TryGetOccupant(TargetCoordinate, out _), Is.False);
            Assert.That(resolution.OccupantEffectResults, Is.Empty);
        }

        [Test]
        public void Resolve_AlreadyOccupiedTileIsPureNoOp()
        {
            var incumbent = CreateOccupant("incumbent", TargetCoordinate);
            var state = CreateState(CreateBoard(), incumbent);
            var grid = CreateGrid();
            Assert.That(grid.TryPlace(CreateBuiltAction()), Is.True);

            var resolution = grid.Resolve(state);

            AssertIncumbentUnchanged(state);
            Assert.That(resolution.OccupantEffectResults, Is.Empty);
        }

        [Test]
        public void Resolve_TileOccupiedAfterPlacementIsPureNoOp()
        {
            var state = CreateState();
            var grid = CreateGrid();
            Assert.That(grid.TryPlace(CreateBuiltAction()), Is.True);
            Assert.That(state.TryAddOccupant(CreateOccupant("late-incumbent", TargetCoordinate)), Is.True);

            var resolution = grid.Resolve(state);

            Assert.That(state.OccupantCount, Is.EqualTo(1));
            Assert.That(state.TryGetOccupant(TargetCoordinate, out var incumbent), Is.True);
            Assert.That(incumbent.RuntimeId, Is.EqualTo("late-incumbent"));
            Assert.That(incumbent.Hp, Is.EqualTo(35));
            Assert.That(incumbent.PoisonStacks, Is.EqualTo(3));
            Assert.That(resolution.OccupantEffectResults, Is.Empty);
        }

        [Test]
        public void Resolve_MissingEffectRangeIsPureNoOp()
        {
            var state = CreateState();
            var grid = CreateGrid();
            Assert.That(
                grid.TryPlace(CreateBuiltAction(effectRange: Array.Empty<HexCoord>())),
                Is.True);

            var resolution = grid.Resolve(state);

            Assert.That(state.OccupantCount, Is.Zero);
            Assert.That(resolution.OccupantEffectResults, Is.Empty);
        }

        [Test]
        public void Resolve_MissingTargetCoordinateIsPureNoOp()
        {
            var state = CreateState();
            var grid = CreateGrid();
            var action = new TimelineAction(
                TimelineActorKind.Player,
                "tower",
                "tile-2--1",
                new TimelineCell(0, 0),
                new[] { new TimelineCell(0, 0) },
                null,
                new[] { new CardEffect(CardEffectKind.Built, 1, "tower") },
                new[] { new HexCoord(0, 0) });
            Assert.That(grid.TryPlace(action), Is.True);

            var resolution = grid.Resolve(state);

            Assert.That(state.OccupantCount, Is.Zero);
            Assert.That(resolution.OccupantEffectResults, Is.Empty);
        }

        [TestCase("wall", 1)]
        [TestCase("tower", 0)]
        [TestCase("tower", 2)]
        public void TryPlace_UnsupportedCreationOrValueFailsBeforeOccupyingTimeline(
            string creationId,
            int value)
        {
            var grid = CreateGrid();
            var action = CreateBuiltAction(new CardEffect(CardEffectKind.Built, value, creationId));

            var exception = Assert.Throws<UnsupportedCardEffectException>(
                () => grid.TryPlace(action));

            Assert.That(exception.Kind, Is.EqualTo(CardEffectKind.Built));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void Resolve_ValueOneCreatesAtMostOneTower()
        {
            var board = CreateBoard();
            var secondCoordinate = new HexCoord(3, -1);
            board.AddTile(secondCoordinate, 1);
            var state = CreateState(board);
            var grid = CreateGrid();
            Assert.That(grid.TryPlace(CreateBuiltAction()), Is.True);

            var resolution = grid.Resolve(state);

            Assert.That(state.OccupantCount, Is.EqualTo(1));
            Assert.That(state.TryGetOccupant(TargetCoordinate, out _), Is.True);
            Assert.That(state.TryGetOccupant(secondCoordinate, out _), Is.False);
            Assert.That(resolution.OccupantEffectResults, Has.Count.EqualTo(1));
        }

        private static TimelineGrid CreateGrid()
        {
            return new TimelineGrid(
                effectHandlers: new ICardEffectHandler[] { new BuiltCardEffectHandler() });
        }

        private static TimelineAction CreateBuiltAction(
            CardEffect? effect = null,
            IReadOnlyList<HexCoord> effectRange = null)
        {
            return new TimelineAction(
                TimelineActorKind.Player,
                "tower",
                "tile-2--1",
                new TimelineCell(0, 0),
                new[]
                {
                    new TimelineCell(1, 0),
                    new TimelineCell(0, 1),
                    new TimelineCell(1, 1),
                    new TimelineCell(2, 1)
                },
                TargetCoordinate,
                new[] { effect ?? new CardEffect(CardEffectKind.Built, 1, "tower") },
                effectRange ?? new[] { new HexCoord(0, 0) });
        }

        private static CombatSliceState CreateState()
        {
            return CreateState(CreateBoard());
        }

        private static CombatSliceState CreateState(
            CombatBoardState board,
            params CombatOccupantState[] occupants)
        {
            return new CombatSliceState("target-01", 731, board, occupants);
        }

        private static CombatBoardState CreateBoard()
        {
            var board = new CombatBoardState();
            board.AddTile(TargetCoordinate, 3);
            return board;
        }

        private static CombatOccupantState CreateOccupant(
            string runtimeId,
            HexCoord coordinate)
        {
            return new CombatOccupantState(
                runtimeId,
                coordinate,
                "entity",
                CombatAttitude.Enemy,
                hp: 35,
                maxHp: 80,
                poisonStacks: 3,
                supportsHealth: true,
                supportsStatus: true);
        }

        private static void AssertIncumbentUnchanged(CombatSliceState state)
        {
            Assert.That(state.OccupantCount, Is.EqualTo(1));
            Assert.That(state.TryGetOccupant(TargetCoordinate, out var incumbent), Is.True);
            Assert.That(incumbent.RuntimeId, Is.EqualTo("incumbent"));
            Assert.That(incumbent.Kind, Is.EqualTo("entity"));
            Assert.That(incumbent.Hp, Is.EqualTo(35));
            Assert.That(incumbent.MaxHp, Is.EqualTo(80));
            Assert.That(incumbent.PoisonStacks, Is.EqualTo(3));
        }
    }
}
