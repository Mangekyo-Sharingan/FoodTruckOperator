using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles food truck vehicle movement.
/// Uses Rigidbody velocity + MoveRotation for reliable collision response.
/// Input is polled directly from Keyboard (no PlayerInput dependency).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FoodTruckDriving : MonoBehaviour
{
    [Header("Speed")]
    public float maxForwardSpeed = 10f;
    public float maxReverseSpeed = 5f;
    public float acceleration    = 8f;
    public float deceleration    = 12f;

    [Header("Steering")]
    public float maxSteerAngle = 55f;   // degrees/sec
    public float steerSharpness = 3f;   // how quickly steering builds up

    [Header("Wheel Visuals")]
    public Transform wheelFL;
    public Transform wheelFR;

    Rigidbody _rb;
    float _currentSpeed;
    float _steerInput;
    bool _active;

    public float CurrentSpeed => _currentSpeed;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.mass = 1500f;
        _rb.linearDamping = 1f;
        _rb.angularDamping = 8f;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;
        _rb.isKinematic = true; // start kinematic; enabled when driving
    }

    public void SetActive(bool active)
    {
        _active = active;
        _rb.isKinematic = !active;
        if (!active)
        {
            _currentSpeed = 0f;
            _steerInput = 0f;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    void FixedUpdate()
    {
        if (!_active) return;

        float accel = 0f;
        float rawSteer = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) accel =  1f;
            if (Keyboard.current.sKey.isPressed) accel = -1f;
            if (Keyboard.current.dKey.isPressed) rawSteer =  1f;
            if (Keyboard.current.aKey.isPressed) rawSteer = -1f;
        }

        // Speed
        float targetSpeed = accel > 0 ? accel * maxForwardSpeed
                          : accel < 0 ? accel * maxReverseSpeed
                          : 0f;
        float rate = accel != 0f ? acceleration : deceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed,
            rate * Time.fixedDeltaTime);

        // Apply forward velocity
        _rb.linearVelocity = transform.forward * _currentSpeed;

        // Steering (only meaningful when moving)
        _steerInput = Mathf.MoveTowards(_steerInput, rawSteer,
            steerSharpness * Time.fixedDeltaTime);

        if (Mathf.Abs(_currentSpeed) > 0.3f)
        {
            float speedFactor = Mathf.Clamp01(Mathf.Abs(_currentSpeed) / maxForwardSpeed);
            float turnRate = maxSteerAngle * _steerInput * speedFactor
                           * Mathf.Sign(_currentSpeed) * Time.fixedDeltaTime;
            _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, turnRate, 0f));
        }

        // Front wheel visual steering
        AnimateWheels(rawSteer);
    }

    void AnimateWheels(float steer)
    {
        float angle = steer * 30f;
        if (wheelFL) wheelFL.localRotation = Quaternion.Euler(0f, angle, 0f);
        if (wheelFR) wheelFR.localRotation = Quaternion.Euler(0f, angle, 0f);
    }
}
