using UnityEngine;
using UnityEngine.EventSystems;

namespace TimeKey.Presentation
{
    public sealed class BoardOrbitCameraController : MonoBehaviour
    {
        public const float MinimumPitch = 24f;
        public const float MaximumPitch = 72f;
        public const float MinimumDistance = 7f;
        public const float MaximumDistance = 18f;

        [SerializeField] private float orbitSensitivity = 0.22f;
        [SerializeField] private float panSensitivity = 0.0022f;
        [SerializeField] private float zoomStep = 0.9f;
        [SerializeField] private float dampingTime = 0.12f;

        private Camera _camera;
        private Vector3 _pivot;
        private Vector3 _targetPivot;
        private Vector3 _pivotVelocity;
        private Vector3 _lastPointerPosition;
        private float _yaw;
        private float _targetYaw;
        private float _yawVelocity;
        private float _pitch;
        private float _targetPitch;
        private float _pitchVelocity;
        private float _distance;
        private float _targetDistance;
        private float _distanceVelocity;
        private bool _orbiting;
        private bool _panning;

        public float Yaw => _yaw;

        public float Pitch => _pitch;

        public float Distance => _distance;

        public Vector3 Pivot => _pivot;

        public bool InputEnabled { get; set; } = true;

        public bool IsManipulating => _orbiting || _panning;

        public void Initialize(Camera sceneCamera, Vector3 pivot, float yaw, float pitch, float distance)
        {
            _camera = sceneCamera;
            SetView(yaw, pitch, distance, pivot, true);
        }

        public void SetView(float yaw, float pitch, float distance, Vector3 pivot, bool immediate)
        {
            _targetYaw = NormalizeYaw(yaw);
            _targetPitch = Mathf.Clamp(pitch, MinimumPitch, MaximumPitch);
            _targetDistance = Mathf.Clamp(distance, MinimumDistance, MaximumDistance);
            _targetPivot = pivot;

            if (!immediate)
            {
                return;
            }

            _yaw = _targetYaw;
            _pitch = _targetPitch;
            _distance = _targetDistance;
            _pivot = _targetPivot;
            _yawVelocity = 0f;
            _pitchVelocity = 0f;
            _distanceVelocity = 0f;
            _pivotVelocity = Vector3.zero;
            ApplyCameraTransform();
        }

        private void Update()
        {
            if (!InputEnabled || _camera == null)
            {
                EndPointerGestures();
                return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                _orbiting = !IsPointerOverInterface();
                _lastPointerPosition = Input.mousePosition;
            }

            if (Input.GetMouseButtonDown(2))
            {
                _panning = !IsPointerOverInterface();
                _lastPointerPosition = Input.mousePosition;
            }

            if (_orbiting && Input.GetMouseButton(1))
            {
                var current = Input.mousePosition;
                var delta = current - _lastPointerPosition;
                _targetYaw = NormalizeYaw(_targetYaw + (delta.x * orbitSensitivity));
                _targetPitch = Mathf.Clamp(
                    _targetPitch - (delta.y * orbitSensitivity),
                    MinimumPitch,
                    MaximumPitch);
                _lastPointerPosition = current;
            }

            if (_panning && Input.GetMouseButton(2))
            {
                var current = Input.mousePosition;
                var delta = current - _lastPointerPosition;
                Pan(delta);
                _lastPointerPosition = current;
            }

            if (Input.GetMouseButtonUp(1))
            {
                _orbiting = false;
            }

            if (Input.GetMouseButtonUp(2))
            {
                _panning = false;
            }

            if (!IsPointerOverInterface())
            {
                var scroll = Input.mouseScrollDelta.y;
                if (!Mathf.Approximately(scroll, 0f))
                {
                    _targetDistance = Mathf.Clamp(
                        _targetDistance - (scroll * zoomStep),
                        MinimumDistance,
                        MaximumDistance);
                }
            }
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                return;
            }

            var deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            _yaw = Mathf.SmoothDampAngle(_yaw, _targetYaw, ref _yawVelocity, dampingTime, Mathf.Infinity, deltaTime);
            _pitch = Mathf.SmoothDamp(_pitch, _targetPitch, ref _pitchVelocity, dampingTime, Mathf.Infinity, deltaTime);
            _distance = Mathf.SmoothDamp(
                _distance,
                _targetDistance,
                ref _distanceVelocity,
                dampingTime,
                Mathf.Infinity,
                deltaTime);
            _pivot = Vector3.SmoothDamp(
                _pivot,
                _targetPivot,
                ref _pivotVelocity,
                dampingTime,
                Mathf.Infinity,
                deltaTime);
            ApplyCameraTransform();
        }

        private void Pan(Vector3 pointerDelta)
        {
            var right = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up).normalized;
            var forward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
            var scale = _targetDistance * panSensitivity;
            _targetPivot += ((-right * pointerDelta.x) + (-forward * pointerDelta.y)) * scale;
            _targetPivot.x = Mathf.Clamp(_targetPivot.x, -5f, 5f);
            _targetPivot.z = Mathf.Clamp(_targetPivot.z, -5f, 5f);
        }

        private void ApplyCameraTransform()
        {
            var orbit = Quaternion.Euler(_pitch, _yaw, 0f);
            _camera.transform.position = _pivot + (orbit * Vector3.back * _distance);
            _camera.transform.rotation = Quaternion.LookRotation(_pivot - _camera.transform.position, Vector3.up);
        }

        private void EndPointerGestures()
        {
            _orbiting = false;
            _panning = false;
        }

        private static bool IsPointerOverInterface()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static float NormalizeYaw(float yaw)
        {
            yaw %= 360f;
            return yaw < 0f ? yaw + 360f : yaw;
        }
    }
}
