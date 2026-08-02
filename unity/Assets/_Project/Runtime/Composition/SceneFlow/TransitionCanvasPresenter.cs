using UnityEngine;
using TimeKey.Presentation.TransitionVisuals;

namespace TimeKey.Composition.SceneFlow
{
    [DisallowMultipleComponent]
    public sealed class TransitionCanvasPresenter : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlayGroup = null;
        [SerializeField] private GameObject loadingIndicator = null;
        [SerializeField] private TransitionVisualPresenter visualPresenter = null;

        public bool IsCovered { get; private set; }

        public bool IsComplete => visualPresenter == null || visualPresenter.IsComplete;

        public void SetCovered(bool value)
        {
            IsCovered = value;
            if (visualPresenter != null)
            {
                if (value)
                {
                    visualPresenter.PlayCover();
                }
                else
                {
                    visualPresenter.PlayReveal();
                }

                return;
            }

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
