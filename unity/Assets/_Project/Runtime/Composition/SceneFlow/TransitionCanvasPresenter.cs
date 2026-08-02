using UnityEngine;

namespace TimeKey.Composition.SceneFlow
{
    [DisallowMultipleComponent]
    public sealed class TransitionCanvasPresenter : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlayGroup = null;
        [SerializeField] private GameObject loadingIndicator = null;

        public bool IsCovered { get; private set; }

        public void SetCovered(bool value)
        {
            IsCovered = value;
            if (overlayGroup != null)
            {
                overlayGroup.alpha = value ? 1f : 0f;
            }

            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(value);
            }
        }
    }
}
