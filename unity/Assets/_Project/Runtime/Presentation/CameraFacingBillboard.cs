using UnityEngine;

namespace TimeKey.Presentation
{
    public sealed class CameraFacingBillboard : MonoBehaviour
    {
        private Transform _cameraTransform;

        public void Initialize(Camera sceneCamera)
        {
            _cameraTransform = sceneCamera.transform;
            FaceCamera();
        }

        public void FaceCamera()
        {
            if (_cameraTransform == null)
            {
                return;
            }

            var direction = _cameraTransform.position - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                // SpriteRenderer's visible front is the local -Z face.
                transform.rotation = Quaternion.LookRotation(-direction.normalized, Vector3.up);
            }
        }

        private void LateUpdate()
        {
            FaceCamera();
        }
    }
}
