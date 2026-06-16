using UnityEngine;

namespace Monopoly.Core
{
    public class OrbitCameraController : MonoBehaviour
    {
        [SerializeField] private Vector3 targetPoint = Vector3.zero;
        [SerializeField] private float distance = 19f;
        [SerializeField] private float pitchAngle = 70f;
        [SerializeField] private float yawAngle;
        [SerializeField] private float dragSensitivity = 0.2f;
        [SerializeField] private int rotateMouseButton = 1;
        [SerializeField] private bool syncFromCurrentTransformOnStart;
        [Header("Mouse Sway")]
        [SerializeField] private bool enableMouseSway = true;
        [SerializeField] private float maxMouseYawOffset = 3f;
        [SerializeField] private float maxMousePitchOffset = 1.5f;
        [SerializeField] private float mouseSwaySmoothTime = 0.28f;
        private bool isDragging;
        private Vector3 lastMousePosition;
        private float currentMouseYawOffset;
        private float currentMousePitchOffset;
        private float mouseYawVelocity;
        private float mousePitchVelocity;

        public void Configure(Vector3 centerPoint, float orbitDistance, float fixedPitchAngle, float initialYawAngle = 0f)
        {
            targetPoint = centerPoint;
            distance = orbitDistance;
            pitchAngle = fixedPitchAngle;
            yawAngle = initialYawAngle;
            UpdateCameraTransform();
        }

        private void Start()
        {
            if (syncFromCurrentTransformOnStart)
            {
                SyncOrbitFromCurrentTransform();
            }
            else
            {
                UpdateCameraTransform();
            }

            lastMousePosition = Input.mousePosition;
        }

        private void Update()
        {
            UpdateMouseSway();

            if (Input.GetMouseButtonDown(rotateMouseButton))
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
                return;
            }

            if (Input.GetMouseButtonUp(rotateMouseButton))
            {
                isDragging = false;
                return;
            }

            if (isDragging && Input.GetMouseButton(rotateMouseButton))
            {
                Vector3 currentMousePosition = Input.mousePosition;
                Vector3 mouseDelta = currentMousePosition - lastMousePosition;
                lastMousePosition = currentMousePosition;

                yawAngle += mouseDelta.x * dragSensitivity;
            }

            UpdateCameraTransform();
        }

        private void UpdateCameraTransform()
        {
            Quaternion orbitRotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            Vector3 offset = orbitRotation * new Vector3(0f, 0f, -distance);
            transform.position = targetPoint + offset;

            Quaternion swayRotation = Quaternion.Euler(
                currentMousePitchOffset,
                currentMouseYawOffset,
                0f);

            transform.rotation = orbitRotation * swayRotation;
        }

        private void UpdateMouseSway()
        {
            if (!enableMouseSway || Screen.width <= 0 || Screen.height <= 0)
            {
                currentMouseYawOffset = 0f;
                currentMousePitchOffset = 0f;
                return;
            }

            Vector2 normalizedMouse = new Vector2(
                Mathf.Clamp01(Input.mousePosition.x / Screen.width),
                Mathf.Clamp01(Input.mousePosition.y / Screen.height));

            Vector2 centeredMouse = (normalizedMouse - new Vector2(0.5f, 0.5f)) * 2f;
            float targetYawOffset = centeredMouse.x * maxMouseYawOffset;
            float targetPitchOffset = -centeredMouse.y * maxMousePitchOffset;

            currentMouseYawOffset = Mathf.SmoothDamp(
                currentMouseYawOffset,
                targetYawOffset,
                ref mouseYawVelocity,
                mouseSwaySmoothTime);

            currentMousePitchOffset = Mathf.SmoothDamp(
                currentMousePitchOffset,
                targetPitchOffset,
                ref mousePitchVelocity,
                mouseSwaySmoothTime);
        }

        private void SyncOrbitFromCurrentTransform()
        {
            Vector3 offset = transform.position - targetPoint;
            if (offset.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            distance = offset.magnitude;

            float verticalRatio = Mathf.Clamp(offset.y / distance, -1f, 1f);
            pitchAngle = Mathf.Asin(verticalRatio) * Mathf.Rad2Deg;
            yawAngle = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
        }

        [ContextMenu("Sync Orbit From Current Transform")]
        private void SyncOrbitFromCurrentTransformContextMenu()
        {
            SyncOrbitFromCurrentTransform();
        }
    }
}
