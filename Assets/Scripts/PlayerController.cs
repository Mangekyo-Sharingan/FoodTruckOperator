using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Thin input feeder — mirrors CookedUp's PlayerInputHandler pattern.
/// Reads Unity Input System callbacks and forwards them to PlayerMovement.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    PlayerMovement _movement;

    void Awake() => _movement = GetComponent<PlayerMovement>();

    // Input System callbacks (PlayerInput component must use SendMessages mode)
    public void OnMove(InputValue value)   => _movement.MovementInput = value.Get<Vector2>();
    public void OnJump(InputValue value)   => _movement.JumpPressed   = value.isPressed;
    public void OnSprint(InputValue value) => _movement.SprintHeld    = value.isPressed;
}
