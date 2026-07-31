using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TimeKey.Domain;

namespace TimeKey.Tests.EditMode.Terrain
{
    public sealed class EarthquakeResolutionTests
    {
        private static readonly HexCoord[] SevenHexRange =
        {
            new HexCoord(0, 0),
            new HexCoord(1, 0),
            new HexCoord(1, -1),
            new HexCoord(0, -1),
            new HexCoord(-1, 0),
            new HexCoord(-1, 1),
            new HexCoord(0, 1)
        };

        [Test]
        public void Resolve_EarthquakeRaisesSevenExistingTilesByTwo()
        {
            var board = CreateBoard(SevenHexRange, 1);

            var snapshot = ResolveEarthquake(board);

            Assert.That(snapshot.EffectResults.Count, Is.EqualTo(7));
            foreach (var coordinate in SevenHexRange)
            {
                Assert.That(board.TryGetTile(coordinate, out var tile), Is.True);
                Assert.That(tile.LogicalLayerCount, Is.EqualTo(3));
                var result = snapshot.EffectResults.Single(item => item.Coordinate == coordinate);
                Assert.That(result.BeforeLayers, Is.EqualTo(1));
                Assert.That(result.AfterLayers, Is.EqualTo(3));
                Assert.That(result.Removed, Is.False);
            }
        }

        [Test]
        public void Resolve_EarthquakeSkipsMissingRangeTileWithoutCreatingIt()
        {
            var existing = SevenHexRange.Take(6).ToArray();
            var missing = SevenHexRange[6];
            var board = CreateBoard(existing, 1);

            var snapshot = ResolveEarthquake(board);

            Assert.That(snapshot.EffectResults.Count, Is.EqualTo(6));
            Assert.That(board.TileCount, Is.EqualTo(6));
            Assert.That(board.TryGetTile(missing, out _), Is.False);
        }

        [Test]
        public void Resolve_EarthquakeRemovesTileAboveGodotMaximum()
        {
            var board = new CombatBoardState();
            board.AddTile(new HexCoord(0, 0), 5);

            var snapshot = ResolveEarthquake(board);

            Assert.That(board.TryGetTile(new HexCoord(0, 0), out _), Is.False);
            Assert.That(snapshot.EffectResults.Count, Is.EqualTo(1));
            Assert.That(snapshot.EffectResults[0].BeforeLayers, Is.EqualTo(5));
            Assert.That(snapshot.EffectResults[0].AfterLayers, Is.Zero);
            Assert.That(snapshot.EffectResults[0].Removed, Is.True);
        }

        [Test]
        public void CombatBoardState_ElevationAtGodotMinimumRemovesTileAtZero()
        {
            var coordinate = new HexCoord(0, 0);
            var board = new CombatBoardState();
            board.AddTile(coordinate, 1);

            var applied = board.TryApplyElevation(coordinate, -1, out var result);

            Assert.That(applied, Is.True);
            Assert.That(board.TileCount, Is.Zero);
            Assert.That(board.TryGetTile(coordinate, out _), Is.False);
            Assert.That(result.Coordinate, Is.EqualTo(coordinate));
            Assert.That(result.BeforeLayers, Is.EqualTo(1));
            Assert.That(result.AfterLayers, Is.Zero);
            Assert.That(result.Removed, Is.True);
        }

        [Test]
        public void CardPlaySession_EarthquakeTwoCellShapePreviewsAndCommitsOnce()
        {
            var grid = new TimelineGrid();
            var session = new CardPlaySession(CreateEarthquake());
            Assert.That(session.SelectTarget("hex-0-0", new HexCoord(0, 0)).Succeeded, Is.True);

            var preview = session.PreviewTimeline(grid, new TimelineCell(10, 0));

            Assert.That(preview.Succeeded, Is.True);
            Assert.That(grid.OccupiedCellCount, Is.Zero);
            Assert.That(session.Commit(grid).Succeeded, Is.True);
            Assert.That(grid.OccupiedCellCount, Is.EqualTo(2));
        }

        [Test]
        public void CardPlaySession_EarthquakeCommitPreservesTargetCoordinateForResolve()
        {
            var target = new HexCoord(4, -2);
            var translatedRange = SevenHexRange
                .Select(offset => new HexCoord(target.Q + offset.Q, target.R + offset.R))
                .ToArray();
            var board = CreateBoard(translatedRange, 1);
            var grid = new TimelineGrid();
            var session = new CardPlaySession(CreateEarthquake());
            Assert.That(session.SelectTarget("hex-4--2", target).Succeeded, Is.True);
            Assert.That(session.PreviewTimeline(grid, new TimelineCell(0, 0)).Succeeded, Is.True);
            Assert.That(session.Commit(grid).Succeeded, Is.True);

            var snapshot = grid.Resolve(new CombatSliceState("target-01", 10, 731, board));

            Assert.That(snapshot.EffectResults.Count, Is.EqualTo(7));
            foreach (var coordinate in translatedRange)
            {
                Assert.That(board.TryGetTile(coordinate, out var tile), Is.True);
                Assert.That(tile.LogicalLayerCount, Is.EqualTo(3));
            }
        }

        [Test]
        public void CardPlaySession_EarthquakeCrossingRightEdgeIsInvalidAndPure()
        {
            var coordinate = new HexCoord(0, 0);
            var board = new CombatBoardState();
            board.AddTile(coordinate, 1);
            var grid = new TimelineGrid();
            var session = new CardPlaySession(CreateEarthquake());
            Assert.That(session.SelectTarget("hex-0-0", coordinate).Succeeded, Is.True);

            var preview = session.PreviewTimeline(grid, new TimelineCell(11, 0));

            Assert.That(preview.Succeeded, Is.False);
            Assert.That(grid.OccupiedCellCount, Is.Zero);
            Assert.That(session.Commit(grid).Succeeded, Is.False);
            Assert.That(grid.OccupiedCellCount, Is.Zero);
            Assert.That(session.Cancel().Succeeded, Is.True);
            Assert.That(board.TileCount, Is.EqualTo(1));
            Assert.That(board.TryGetTile(coordinate, out var tile), Is.True);
            Assert.That(tile.LogicalLayerCount, Is.EqualTo(1));
        }

        [Test]
        public void TimelineAction_CopiesTargetEffectsAndRange()
        {
            var effects = new List<CardEffect>
            {
                new CardEffect(CardEffectKind.Elevation, 2)
            };
            var range = new List<HexCoord>(SevenHexRange);
            var target = new HexCoord(3, -1);
            var action = new TimelineAction(
                TimelineActorKind.Player,
                "earthquake",
                "hex-3--1",
                new TimelineCell(0, 0),
                new[] { new TimelineCell(0, 0), new TimelineCell(1, 0) },
                target,
                effects,
                range);

            effects[0] = new CardEffect(CardEffectKind.Damage, 100);
            range[0] = new HexCoord(99, 99);

            Assert.That(action.TargetCoord, Is.EqualTo(target));
            Assert.That(action.Effects.Count, Is.EqualTo(1));
            Assert.That(action.Effects[0].Kind, Is.EqualTo(CardEffectKind.Elevation));
            Assert.That(action.Effects[0].NumericAmount, Is.EqualTo(2));
            Assert.That(action.EffectRange.Count, Is.EqualTo(7));
            Assert.That(action.EffectRange[0], Is.EqualTo(new HexCoord(0, 0)));
        }

        [Test]
        public void Resolve_EarthquakeFixtureIsDeterministic()
        {
            var first = ResolveEarthquake(CreateBoard(SevenHexRange, 1));
            var second = ResolveEarthquake(CreateBoard(SevenHexRange, 1));

            Assert.That(second.Seed, Is.EqualTo(first.Seed));
            Assert.That(
                second.EffectResults.Select(ToTuple),
                Is.EqualTo(first.EffectResults.Select(ToTuple)));
        }

        private static ResolutionSnapshot ResolveEarthquake(CombatBoardState board)
        {
            var grid = new TimelineGrid();
            var action = TimelineAction.FromCard(
                CreateEarthquake(),
                "hex-0-0",
                new HexCoord(0, 0),
                new TimelineCell(0, 0));
            Assert.That(grid.TryPlace(action), Is.True);
            return grid.Resolve(new CombatSliceState("target-01", 10, 731, board));
        }

        private static CombatBoardState CreateBoard(HexCoord[] coordinates, int layers)
        {
            var board = new CombatBoardState();
            foreach (var coordinate in coordinates)
            {
                board.AddTile(coordinate, layers);
            }

            return board;
        }

        private static CardDefinition CreateEarthquake()
        {
            return new CardDefinition(
                "earthquake",
                2,
                "earthquake.png",
                new[] { new CardEffect(CardEffectKind.Elevation, 2) },
                SevenHexRange,
                new[] { new TimelineCell(0, 0), new TimelineCell(1, 0) });
        }

        private static string ToTuple(TileEffectResult result)
        {
            return string.Format(
                "{0},{1}:{2}>{3}:{4}",
                result.Coordinate.Q,
                result.Coordinate.R,
                result.BeforeLayers,
                result.AfterLayers,
                result.Removed);
        }
    }
}
