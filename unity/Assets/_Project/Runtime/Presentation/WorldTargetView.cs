using UnityEngine;

namespace TimeKey.Presentation
{
    internal sealed class WorldTargetView : MonoBehaviour
    {
        private VerticalSliceController _controller;
        private string _targetId;

        public void Initialize(VerticalSliceController controller, string targetId)
        {
            _controller = controller;
            _targetId = targetId;
        }

        private void OnMouseDown()
        {
            if (_controller != null)
            {
                _controller.SelectTarget(_targetId);
            }
        }
    }
}
