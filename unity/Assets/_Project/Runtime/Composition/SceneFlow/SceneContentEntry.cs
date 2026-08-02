using System;
using TimeKey.Application.SceneFlow;
using UnityEngine;

namespace TimeKey.Composition.SceneFlow
{
    [DisallowMultipleComponent]
    public sealed class SceneContentEntry : MonoBehaviour
    {
        [SerializeField] private SceneId sceneId = SceneId.None;
        [SerializeField] private GameObject contentRoot = null;
        [SerializeField] private Camera contentCamera = null;
        [SerializeField] private CanvasGroup interactionGroup = null;
        [SerializeField] private string defaultFocusId = "default";

        public SceneId SceneId => sceneId;

        public bool IsBound { get; private set; }

        public bool IsInteractive { get; private set; }

        public string DefaultFocusId => defaultFocusId;

        public ISceneTransitionPayload Payload { get; private set; }

        public void Bind(ISceneTransitionPayload payload)
        {
            if (payload == null || payload.TargetScene != sceneId)
            {
                throw new InvalidOperationException(
                    "The scene content entry received an incompatible payload.");
            }

            Payload = payload;
            IsBound = true;
            if (contentRoot != null)
            {
                contentRoot.SetActive(true);
            }

            SetInteractive(false);
        }

        public void SetInteractive(bool value)
        {
            IsInteractive = value;
            if (interactionGroup == null)
            {
                return;
            }

            interactionGroup.interactable = value;
            interactionGroup.blocksRaycasts = value;
        }

        public void SetCameraEnabled(bool value)
        {
            if (contentCamera != null)
            {
                contentCamera.enabled = value;
            }
        }

        public void EnsureRenderable()
        {
            if (contentCamera == null || !contentCamera.enabled)
            {
                throw new InvalidOperationException(
                    "The content scene cannot signal a renderable frame without an enabled camera.");
            }
        }

        public void PlayRevealPresentation()
        {
            foreach (var behaviour in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is ISceneRevealPresentation presentation)
                {
                    presentation.PlayReveal();
                }
            }
        }

        public bool IsRevealPresentationComplete()
        {
            foreach (var behaviour in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is ISceneRevealPresentation presentation &&
                    !presentation.IsComplete)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
