using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Faithful port of CookedUp/Farfi55 PlayerMovement — Rigidbody-based,
/// same field names, same API, same events.
/// Additions for this 3D project: camera-relative direction, Y-velocity
/// preservation (Unity physics gravity), jump + ground check, sprint.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private NavMeshAgent agent;

    [Header("Speed")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float sprintSpeed   = 9f;
    [SerializeField] private float rotationSpeed = 540f;

    [Header("Jump / Ground")]
    [SerializeField] private float jumpForce          = 6f;
    [SerializeField] private float groundCheckRadius  = 0.28f;
    [SerializeField] private float groundCheckOffset  = 0.01f;
    [SerializeField] private LayerMask groundLayers   = ~0;

    [Header("Behaviour")]
    [SerializeField] private bool stopAllWhenUsingInput = true;

    // ── Public state (CookedUp-identical) ────────────────────────────────────
    public float MovementSpeed => movementSpeed;
    public float RotationSpeed => rotationSpeed;

    public bool IsMoving               => IsMovingUsingInput || IsMovingUsingNavigation;
    public bool IsMovingUsingInput     { get; private set; } = false;
    public bool IsMovingUsingNavigation{ get; private set; } = false;
    public bool HasAgent               => agent != null;

    /// <summary>Set by PlayerController every frame from Input System.</summary>
    public Vector2 MovementInput { get; set; } = Vector2.zero;
    public bool    SprintHeld    { get; set; }
    public bool    JumpPressed   { get; set; }

    // ── Events (CookedUp-identical) ───────────────────────────────────────────
    public event EventHandler OnMoveToStarted;
    public event EventHandler OnMoveToCompleted;
    public event EventHandler OnMoveToCanceled;
    public event EventHandler OnLookAtTargetCompleted;

    // ── Private ───────────────────────────────────────────────────────────────
    Transform _camera;
    Transform _lookAtTarget;
    bool      _lookAtTargetUntilArrived;
    bool      HasLookAtTarget => _lookAtTarget != null;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        _camera = Camera.main?.transform;
    }

    void Update()
    {
        // ── NavMesh destination tracking (CookedUp-identical) ─────────────
        if (HasAgent)
        {
            if (HasReachedDestination())
                DestinationReached();
            else if (IsMovingUsingNavigation && agent.pathStatus == NavMeshPathStatus.PathInvalid)
                StopMoving();
        }

        IsMovingUsingInput = (MovementInput != Vector2.zero);

        if (IsMovingUsingInput && stopAllWhenUsingInput && (IsMovingUsingNavigation || HasLookAtTarget))
            StopAll();

        // ── Jump ──────────────────────────────────────────────────────────
        if (JumpPressed && IsGrounded())
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        JumpPressed = false;

        // ── Direction ─────────────────────────────────────────────────────
        var moveDirection = GetMoveDirection();

        HandleMovement(moveDirection);

        if (IsMovingUsingInput)
            HandleRotation(moveDirection);
        else if (!IsMovingUsingNavigation && HasLookAtTarget)
            HandleRotation(_lookAtTarget.position - transform.position);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// Camera-relative horizontal direction (replaces CookedUp's flat input→world).
    Vector3 GetMoveDirection()
    {
        if (!IsMovingUsingInput) return Vector3.zero;

        if (_camera != null)
        {
            float angle = Mathf.Atan2(MovementInput.x, MovementInput.y) * Mathf.Rad2Deg
                          + _camera.eulerAngles.y;
            return Quaternion.Euler(0, angle, 0) * Vector3.forward;
        }

        return new Vector3(MovementInput.x, 0, MovementInput.y).normalized;
    }

    bool IsGrounded()
    {
        Vector3 p = transform.position;
        p.y += groundCheckOffset;
        return Physics.CheckSphere(p, groundCheckRadius, groundLayers,
            QueryTriggerInteraction.Ignore);
    }

    bool HasReachedDestination()
    {
        return !agent.pathPending
            && !float.IsPositiveInfinity(agent.remainingDistance)
            && agent.remainingDistance <= agent.stoppingDistance;
    }

    void DestinationReached()
    {
        IsMovingUsingNavigation = false;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
        OnMoveToCompleted?.Invoke(this, EventArgs.Empty);
    }

    // ── Movement / Rotation (CookedUp logic, Y-velocity preserved) ───────────

    void HandleMovement(Vector3 direction)
    {
        float speed = SprintHeld ? sprintSpeed : movementSpeed;

        if (HasAgent && IsMovingUsingNavigation && IsMovingUsingInput)
        {
            // CookedUp: agent.velocity = direction * movementSpeed
            agent.velocity = direction * speed;
            return;
        }

        if (!rb.isKinematic)
        {
            // CookedUp: rb.velocity = direction * movementSpeed
            // Y preserved so Unity gravity is unaffected
            rb.linearVelocity = new Vector3(
                direction.x * speed,
                rb.linearVelocity.y,
                direction.z * speed);
        }
        else
        {
            // CookedUp: transform.position += direction * (movementSpeed * Time.deltaTime)
            transform.position += direction * (speed * Time.deltaTime);
        }
    }

    void HandleRotation(Vector3 direction)
    {
        // CookedUp-identical
        if (IsMovingUsingNavigation || direction == Vector3.zero) return;

        direction.y = 0;
        var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    // ── Public API (CookedUp-identical) ───────────────────────────────────────

    public bool TryMoveTo(Vector3 position)
    {
        StopMoving();
        if (HasAgent && agent.SetDestination(position))
        {
            IsMovingUsingNavigation = true;
            OnMoveToStarted?.Invoke(this, EventArgs.Empty);
            return true;
        }
        return false;
    }

    public bool TryMoveToAndLookAt(Transform target)
    {
        if (TryMoveTo(target.position)) { LookAt(target); return true; }
        StopLookingAt();
        return false;
    }

    public void StopAll()
    {
        StopMoving();
        StopLookingAt();
    }

    public void StopMoving()
    {
        if (HasAgent) agent.ResetPath();
        bool had = IsMovingUsingNavigation;
        IsMovingUsingNavigation = false;
        if (had) OnMoveToCanceled?.Invoke(this, EventArgs.Empty);
    }

    public void LookAt(Transform target)
    {
        _lookAtTarget = target;
        _lookAtTargetUntilArrived = false;
    }

    public void LookAtUntilSelected(Transform target)
    {
        _lookAtTarget = target;
        _lookAtTargetUntilArrived = true;
    }

    public void StopLookingAt() => _lookAtTarget = null;

    // ── Gizmos ────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        Gizmos.color = IsGrounded() ? Color.green : Color.red;
        Vector3 p = transform.position; p.y += groundCheckOffset;
        Gizmos.DrawWireSphere(p, groundCheckRadius);
#endif
    }
}
