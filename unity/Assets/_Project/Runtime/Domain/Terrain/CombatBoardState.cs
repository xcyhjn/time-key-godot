using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TimeKey.Domain
{
    public sealed class BoardTileState
    {
        internal BoardTileState(int logicalLayerCount)
        {
            LogicalLayerCount = logicalLayerCount;
        }

        public int LogicalLayerCount { get; internal set; }
    }

    public sealed class CombatBoardState
    {
        public const int MinimumLogicalLayerCount = 1;
        public const int MaximumLogicalLayerCount = 6;

        private readonly Dictionary<HexCoord, BoardTileState> _tiles =
            new Dictionary<HexCoord, BoardTileState>();

        public int TileCount => _tiles.Count;

        public IReadOnlyList<HexCoord> Coordinates
        {
            get
            {
                var coordinates = new List<HexCoord>(_tiles.Keys);
                coordinates.Sort(CompareCoordinates);
                return new ReadOnlyCollection<HexCoord>(coordinates);
            }
        }

        public void AddTile(HexCoord coordinate, int logicalLayerCount)
        {
            ValidateLayerCount(logicalLayerCount);
            _tiles.Add(coordinate, new BoardTileState(logicalLayerCount));
        }

        public bool TryGetTile(HexCoord coordinate, out BoardTileState tile)
        {
            return _tiles.TryGetValue(coordinate, out tile);
        }

        public bool TryApplyElevation(
            HexCoord coordinate,
            int amount,
            out TileEffectResult result)
        {
            result = null;
            if (!_tiles.TryGetValue(coordinate, out var tile))
            {
                return false;
            }

            var before = tile.LogicalLayerCount;
            var requested = before + amount;
            if (requested < MinimumLogicalLayerCount || requested > MaximumLogicalLayerCount)
            {
                _tiles.Remove(coordinate);
                result = new TileEffectResult(coordinate, before, 0, true);
                return true;
            }

            tile.LogicalLayerCount = requested;
            result = new TileEffectResult(coordinate, before, requested, false);
            return true;
        }

        private static void ValidateLayerCount(int logicalLayerCount)
        {
            if (logicalLayerCount < MinimumLogicalLayerCount ||
                logicalLayerCount > MaximumLogicalLayerCount)
            {
                throw new ArgumentOutOfRangeException(nameof(logicalLayerCount));
            }
        }

        private static int CompareCoordinates(HexCoord left, HexCoord right)
        {
            var qComparison = left.Q.CompareTo(right.Q);
            return qComparison != 0 ? qComparison : left.R.CompareTo(right.R);
        }
    }
}
