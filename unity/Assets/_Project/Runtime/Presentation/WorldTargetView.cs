using UnityEngine;

namespace TimeKey.Presentation
{
    internal sealed class WorldTargetView : MonoBehaviour
    {
        public string TargetId { get; private set; }

        public void Initialize(string targetId)
        {
            TargetId = targetId;
        }
    }
}
