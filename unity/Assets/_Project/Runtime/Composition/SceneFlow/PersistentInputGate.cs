using UnityEngine;
using UnityEngine.EventSystems;
using TimeKey.Application.SceneFlow;

namespace TimeKey.Composition.SceneFlow
{
    [DisallowMultipleComponent]
    public sealed class PersistentInputGate : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlayGroup = null;
        [SerializeField] private EventSystem eventSystem = null;

        public bool IsLocked { get; private set; }

        public void SetLocked(bool value)
        {
            IsLocked = value;
            SceneInputLockState.SetLocked(value);
            if (overlayGroup != null)
            {
                overlayGroup.blocksRaycasts = value;
                overlayGroup.interactable = value;
            }

            if (eventSystem != null)
            {
                eventSystem.sendNavigationEvents = !value;
                if (value)
                {
                    eventSystem.SetSelectedGameObject(null);
                }
            }
        }

        private void OnDisable()
        {
            SceneInputLockState.SetLocked(false);
        }

        private void OnDestroy()
        {
            SceneInputLockState.SetLocked(false);
        }
    }
}
