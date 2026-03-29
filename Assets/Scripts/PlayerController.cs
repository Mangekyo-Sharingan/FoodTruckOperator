using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float jumpHeight = 1.5f;
    public float gravity = -100f;
    public float rotationSpeed = 10f;

    [Header("Ground Check")]
    public float groundCheckRadius = 0.28f;
    public float groundCheckOffset = -0.01f;
    public LayerMask groundLayers = ~0;

    CharacterController _cc;
    Transform _camera;
    Vector2 _moveInput;
    bool _jumpPressed;
    bool _runHeld;
    Vector3 _velocity;
    bool _grounded;
    Animator _animator;
    Vector3 _spherePos;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
        _camera = Camera.main?.transform;

        if (_camera == null)
        {
            Debug.LogWarning("PlayerController: Main Camera not found. Camera-relative movement disabled.");
        }
    }

    // Input System callbacks (called by PlayerInput component)
    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();
    public void OnJump(InputValue value) => _jumpPressed = value.isPressed;
    public void OnSprint(InputValue value) => _runHeld = value.isPressed;

    void Update()
    {
        GroundCheck();
        ApplyGravity();
        Move();
    }

    void GroundCheck()
    {
        _spherePos.x = transform.position.x;
        _spherePos.y = transform.position.y - groundCheckOffset;
        _spherePos.z = transform.position.z;
        _grounded = Physics.CheckSphere(_spherePos, groundCheckRadius, groundLayers,
            QueryTriggerInteraction.Ignore);

        if (_grounded && _velocity.y < 0f)
            _velocity.y = -2f;
    }

    void ApplyGravity()
    {
        if (_jumpPressed && _grounded)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        _jumpPressed = false;
        _velocity.y += gravity * Time.deltaTime;
    }

    void Move()
    {
        float speed = _runHeld ? runSpeed : walkSpeed;
        Vector3 inputDir = Vector3.zero;

        if (_moveInput.sqrMagnitude > 0.01f && _camera != null)
        {
            // Camera-relative movement direction
            float targetAngle = Mathf.Atan2(_moveInput.x, _moveInput.y) * Mathf.Rad2Deg
                + _camera.eulerAngles.y;
            inputDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

            // Smoothly rotate player toward movement direction
            Quaternion targetRot = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                rotationSpeed * Time.deltaTime);
        }

        Vector3 move = inputDir.normalized * speed;
        move.y = _velocity.y;
        _cc.Move(move * Time.deltaTime);

        // Animator
        if (_animator != null)
        {
            float speedNorm = (inputDir.magnitude * speed) / runSpeed;
            _animator.SetFloat("Speed", speedNorm, 0.1f, Time.deltaTime);
            _animator.SetBool("Grounded", _grounded);
        }
    }

    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        Gizmos.color = _grounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(new Vector3(transform.position.x,
            transform.position.y - groundCheckOffset, transform.position.z), groundCheckRadius);
#endif
    }
}
