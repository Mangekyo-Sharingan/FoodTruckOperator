using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);

    [Header("Distance")]
    public float distance = 6f;
    public float minDistance = 2f;
    public float maxDistance = 12f;
    public float zoomSpeed = 4f;

    [Header("Orbit")]
    public float sensitivityX = 0.15f;
    public float sensitivityY = 0.15f;
    public float minPitch = -20f;
    public float maxPitch = 75f;

    [Header("Collision")]
    public float collisionRadius = 0.3f;
    public LayerMask collisionLayers = ~0;

    float _yaw;
    float _pitch = 20f;
    float _currentDist;

    void Awake()
    {
        _currentDist = distance;
        _yaw = transform.eulerAngles.y;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        // Toggle cursor lock
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        if (target == null) return;

        // Poll mouse delta directly — works regardless of PlayerInput location
        if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            _yaw   += mouseDelta.x * sensitivityX;
            _pitch -= mouseDelta.y * sensitivityY;
            _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);

            float scroll = Mouse.current.scroll.ReadValue().y;
            distance = Mathf.Clamp(distance - scroll * zoomSpeed * Time.deltaTime * 100f,
                minDistance, maxDistance);
        }

        _currentDist = Mathf.Lerp(_currentDist, distance, 12f * Time.deltaTime);

        // Compute desired position with collision avoidance
        Vector3 pivot      = target.position + targetOffset;
        Quaternion rot     = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 desiredPos = pivot - rot * Vector3.forward * _currentDist;

        if (Physics.SphereCast(pivot, collisionRadius, (desiredPos - pivot).normalized,
            out RaycastHit hit, _currentDist, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            desiredPos = pivot + (desiredPos - pivot).normalized * (hit.distance - 0.05f);
        }

        transform.position = desiredPos;
        transform.LookAt(pivot);
    }
}
