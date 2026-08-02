using TimeKey.Application.SceneFlow;
using UnityEngine;

namespace TimeKey.Composition.SceneFlow
{
    [DisallowMultipleComponent]
    public sealed class SceneFlowStateStore : MonoBehaviour
    {
        public SceneTransitionRequest LastRequest { get; private set; }

        public ISceneTransitionPayload LastPayload { get; private set; }

        public void Record(SceneTransitionRequest request)
        {
            LastRequest = request;
            LastPayload = request == null ? null : request.Payload;
        }
    }
}
