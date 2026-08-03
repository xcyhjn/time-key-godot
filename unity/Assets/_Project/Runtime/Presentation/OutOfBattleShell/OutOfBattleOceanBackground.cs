using UnityEngine;
using UnityEngine.UI;

namespace TimeKey.Presentation.OutOfBattleShell
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class OutOfBattleOceanBackground : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float verticalTileCount = 4.5f;

        public void RefreshUv()
        {
            var image = GetComponent<RawImage>();
            var rect = image.rectTransform.rect;
            if (rect.height <= 0f)
            {
                return;
            }

            image.uvRect = new Rect(
                0f,
                0f,
                verticalTileCount * rect.width / rect.height,
                verticalTileCount);
        }

        private void OnEnable()
        {
            RefreshUv();
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshUv();
        }
    }
}
