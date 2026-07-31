using System.Linq;
using NUnit.Framework;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode
{
    public sealed class TimelineGridTests
    {
        [TestCase(-1, 0)]
        [TestCase(12, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 3)]
        public void TryPlace_RejectsOutOfBoundsOrigins(int x, int y)
        {
            var grid = new TimelineGrid();
            var action = CreateAction("out-of-bounds", x, y);

            Assert.That(grid.TryPlace(action), Is.False);
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void TryPlace_RejectsShapeThatCrossesBoundary()
        {
            var grid = new TimelineGrid();
            var action = CreateShapedAction(
                "wide",
                11,
                0,
                new TimelineCell(0, 0),
                new TimelineCell(1, 0));

            Assert.That(grid.TryPlace(action), Is.False);
        }

        [Test]
        public void TryPlace_RejectsOverlappingActions()
        {
            var grid = new TimelineGrid();
            var first = CreateShapedAction(
                "first",
                2,
                1,
                new TimelineCell(0, 0),
                new TimelineCell(1, 0));
            var second = CreateAction("second", 3, 1);

            Assert.That(grid.TryPlace(first), Is.True);
            Assert.That(grid.TryPlace(second), Is.False);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(2));
        }

        [Test]
        public void Resolve_OrdersActionsByColumnThenRow()
        {
            var grid = new TimelineGrid();
            var state = new CombatSliceState("target", 10, 731);
            Assert.That(grid.TryPlace(CreateAction("column-two", 2, 0)), Is.True);
            Assert.That(grid.TryPlace(CreateAction("row-two", 1, 2)), Is.True);
            Assert.That(grid.TryPlace(CreateAction("row-zero", 1, 0)), Is.True);

            var snapshot = grid.Resolve(state);

            Assert.That(
                snapshot.ResolutionOrder.Select(item => item.CardId),
                Is.EqualTo(new[] { "row-zero", "row-two", "column-two" }));
        }

        [Test]
        public void Resolve_ExecutesMultiCellActionOnlyOnce()
        {
            var grid = new TimelineGrid();
            var state = new CombatSliceState("target", 10, 731);
            var action = new TimelineAction(
                TimelineActorKind.Player,
                "multi-cell",
                "target",
                new TimelineCell(0, 0),
                new[] { new TimelineCell(0, 0), new TimelineCell(1, 0) },
                3);
            Assert.That(grid.TryPlace(action), Is.True);

            var snapshot = grid.Resolve(state);

            Assert.That(snapshot.TargetHpAfter, Is.EqualTo(7));
            Assert.That(snapshot.ResolutionOrder.Count, Is.EqualTo(1));
            Assert.That(grid.OccupiedCellCount, Is.Zero);
        }

        [Test]
        public void Resolve_ClampsDamageAtZero()
        {
            var grid = new TimelineGrid();
            var state = new CombatSliceState("target", 10, 731);
            Assert.That(grid.TryPlace(CreateAction("lighting", 0, 0, damage: 100)), Is.True);

            var snapshot = grid.Resolve(state);

            Assert.That(snapshot.TargetHpBefore, Is.EqualTo(10));
            Assert.That(snapshot.TargetHpAfter, Is.Zero);
        }

        [Test]
        public void Resolve_ProducesFrozenFixtureSnapshot()
        {
            var snapshot = ResolveFrozenFixture();

            Assert.That(snapshot.Turn, Is.EqualTo(1));
            Assert.That(snapshot.Phase, Is.EqualTo("resolved"));
            Assert.That(snapshot.Timeline.Count, Is.EqualTo(1));
            Assert.That(snapshot.Timeline[0].Origin, Is.EqualTo(new TimelineCell(0, 0)));
            Assert.That(snapshot.Timeline[0].Kind, Is.EqualTo("player"));
            Assert.That(snapshot.Timeline[0].CardId, Is.EqualTo("lighting"));
            Assert.That(snapshot.TargetHpBefore, Is.EqualTo(10));
            Assert.That(snapshot.TargetHpAfter, Is.Zero);
            Assert.That(snapshot.EnemyIntentResolved, Is.True);
            Assert.That(snapshot.Seed, Is.EqualTo(731));
            Assert.That(
                snapshot.ResolutionOrder.Select(item => item.CardId),
                Is.EqualTo(new[] { "lighting", "enemy-intent" }));
        }

        [Test]
        public void Resolve_RepeatedFixtureRunsAreDeterministic()
        {
            var first = ResolveFrozenFixture();
            var second = ResolveFrozenFixture();

            Assert.That(second.Seed, Is.EqualTo(first.Seed));
            Assert.That(second.TargetHpAfter, Is.EqualTo(first.TargetHpAfter));
            Assert.That(
                second.ResolutionOrder.Select(item => item.CardId),
                Is.EqualTo(first.ResolutionOrder.Select(item => item.CardId)));
        }

        private static ResolutionSnapshot ResolveFrozenFixture()
        {
            var lighting = new CardDefinition(
                "lighting",
                1,
                new[] { new CardEffect(CardEffectKind.Damage, 100) },
                new[] { new HexCoord(0, 0), new HexCoord(1, 0), new HexCoord(2, 0) },
                new[] { new TimelineCell(0, 0) });
            var grid = new TimelineGrid();
            var state = new CombatSliceState("target", 10, 731);
            Assert.That(
                grid.TryPlace(TimelineAction.FromCard(lighting, "target", new TimelineCell(0, 0))),
                Is.True);
            Assert.That(
                grid.TryPlace(new TimelineAction(
                    TimelineActorKind.Enemy,
                    "enemy-intent",
                    "target",
                    new TimelineCell(1, 0),
                    new[] { new TimelineCell(0, 0) },
                    0)),
                Is.True);

            return grid.Resolve(state);
        }

        private static TimelineAction CreateAction(
            string cardId,
            int x,
            int y,
            TimelineCell offset = default,
            int damage = 0)
        {
            return new TimelineAction(
                TimelineActorKind.Player,
                cardId,
                "target",
                new TimelineCell(x, y),
                new[] { offset },
                damage);
        }

        private static TimelineAction CreateShapedAction(
            string cardId,
            int x,
            int y,
            params TimelineCell[] shape)
        {
            return new TimelineAction(
                TimelineActorKind.Player,
                cardId,
                "target",
                new TimelineCell(x, y),
                shape,
                0);
        }
    }
}
