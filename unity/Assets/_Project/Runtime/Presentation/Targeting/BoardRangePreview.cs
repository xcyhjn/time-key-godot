using System;
using System.Collections.Generic;
using TimeKey.Domain;
using UnityEngine;

namespace TimeKey.Presentation.Targeting
{
    public sealed class BoardRangePreview : MonoBehaviour
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private readonly Dictionary<HexCoord, TileRegistration> _tiles =
            new Dictionary<HexCoord, TileRegistration>();
        private readonly List<HexCoord> _activeCoordinates = new List<HexCoord>();
        private readonly List<HexCoord> _missingCoordinates = new List<HexCoord>();

        public event Action<HexCoord> MissingCoordinate;

        public IReadOnlyList<HexCoord> ActiveCoordinates => _activeCoordinates;

        public IReadOnlyList<HexCoord> MissingCoordinates => _missingCoordinates;

        public int RegisteredCount => _tiles.Count;

        public void Register(HexCoord coordinate, Component tileView)
        {
            if (tileView == null)
            {
                throw new ArgumentNullException(nameof(tileView));
            }

            var renderers = tileView.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new ArgumentException("A registered board tile must contain a Renderer.", nameof(tileView));
            }

            Clear();
            _tiles[coordinate] = new TileRegistration(renderers);
        }

        public void Show(HexCoord center, IEnumerable<HexCoord> relativeOffsets)
        {
            if (relativeOffsets == null)
            {
                throw new ArgumentNullException(nameof(relativeOffsets));
            }

            Clear();
            var projected = new HashSet<HexCoord>();
            foreach (var offset in relativeOffsets)
            {
                projected.Add(new HexCoord(center.Q + offset.Q, center.R + offset.R));
            }

            foreach (var coordinate in projected)
            {
                if (_tiles.TryGetValue(coordinate, out var tile) && tile.IsAvailable)
                {
                    tile.Show(TargetPreviewPalette.RangeValid);
                    _activeCoordinates.Add(coordinate);
                }
                else
                {
                    _missingCoordinates.Add(coordinate);
                }
            }

            _activeCoordinates.Sort(CompareCoordinates);
            _missingCoordinates.Sort(CompareCoordinates);
            for (var index = 0; index < _missingCoordinates.Count; index++)
            {
                MissingCoordinate?.Invoke(_missingCoordinates[index]);
            }
        }

        public void Clear()
        {
            for (var index = 0; index < _activeCoordinates.Count; index++)
            {
                if (_tiles.TryGetValue(_activeCoordinates[index], out var tile))
                {
                    tile.Clear();
                }
            }

            _activeCoordinates.Clear();
            _missingCoordinates.Clear();
        }

        private void OnDisable()
        {
            Clear();
        }

        private static int CompareCoordinates(HexCoord left, HexCoord right)
        {
            var qComparison = left.Q.CompareTo(right.Q);
            return qComparison != 0 ? qComparison : left.R.CompareTo(right.R);
        }

        private sealed class TileRegistration
        {
            private readonly Renderer[] _renderers;
            private readonly MaterialPropertyBlock[] _savedBlocks;
            private readonly MaterialPropertyBlock _workingBlock = new MaterialPropertyBlock();
            private bool _isShowing;

            public TileRegistration(Renderer[] renderers)
            {
                _renderers = renderers;
                _savedBlocks = new MaterialPropertyBlock[renderers.Length];
                for (var index = 0; index < renderers.Length; index++)
                {
                    _savedBlocks[index] = new MaterialPropertyBlock();
                }
            }

            public bool IsAvailable
            {
                get
                {
                    for (var index = 0; index < _renderers.Length; index++)
                    {
                        if (_renderers[index] != null)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            public void Show(Color color)
            {
                if (_isShowing)
                {
                    Clear();
                }

                for (var index = 0; index < _renderers.Length; index++)
                {
                    var renderer = _renderers[index];
                    if (renderer == null)
                    {
                        continue;
                    }

                    _savedBlocks[index].Clear();
                    renderer.GetPropertyBlock(_savedBlocks[index]);
                    _workingBlock.Clear();
                    renderer.GetPropertyBlock(_workingBlock);
                    _workingBlock.SetColor(BaseColorProperty, color);
                    _workingBlock.SetColor(ColorProperty, color);
                    renderer.SetPropertyBlock(_workingBlock);
                }

                _isShowing = true;
            }

            public void Clear()
            {
                if (!_isShowing)
                {
                    return;
                }

                for (var index = 0; index < _renderers.Length; index++)
                {
                    if (_renderers[index] != null)
                    {
                        _renderers[index].SetPropertyBlock(_savedBlocks[index]);
                    }
                }

                _isShowing = false;
            }
        }
    }
}
