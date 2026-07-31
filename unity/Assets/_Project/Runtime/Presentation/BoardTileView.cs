using TimeKey.Domain;
using UnityEngine;

namespace TimeKey.Presentation
{
    internal sealed class BoardTileView : MonoBehaviour
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock _propertyBlock;
        private Renderer[] _renderers;
        private bool _isSelected;

        public HexCoord Coordinate { get; private set; }

        public void Initialize(HexCoord coordinate, Renderer[] tileRenderers)
        {
            Coordinate = coordinate;
            _propertyBlock = new MaterialPropertyBlock();
            RefreshRenderers(tileRenderers);
        }

        public void RefreshRenderers(Renderer[] tileRenderers)
        {
            _renderers = tileRenderers ?? System.Array.Empty<Renderer>();
            ApplySelection();
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            ApplySelection();
        }

        private void ApplySelection()
        {
            if (_renderers == null || _propertyBlock == null)
            {
                return;
            }

            for (var index = 0; index < _renderers.Length; index++)
            {
                _propertyBlock.Clear();
                if (_renderers[index] == null)
                {
                    continue;
                }

                if (_isSelected)
                {
                    var color = new Color(0.24f, 0.78f, 0.82f, 1f);
                    _propertyBlock.SetColor(BaseColorProperty, color);
                    _propertyBlock.SetColor(ColorProperty, color);
                }

                _renderers[index].SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
