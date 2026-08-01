using UnityEngine;

namespace TimeKey.Presentation
{
    public sealed class WorldTargetView : MonoBehaviour
    {
        public string TargetId { get; private set; }

        public void Initialize(string targetId)
        {
            TargetId = targetId;
        }
    }
}
