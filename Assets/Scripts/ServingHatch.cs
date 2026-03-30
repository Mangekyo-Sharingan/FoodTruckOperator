using UnityEngine;

/// <summary>
/// Sits on the HatchPivot (the hinge transform at the top of the serving window).
/// The hatch panel is a child of this GameObject and rotates with it.
/// Rotation is around the local Z axis (along the window's length), so the panel
/// swings outward like an awning when opened.
/// </summary>
public class ServingHatch : MonoBehaviour
{
    public bool isOpen;
    public float openAngle = -82f;   // local Z rotation when fully open (swings outward)
    public float animSpeed = 4f;

    float _targetAngle;

    void Update()
    {
        float current = transform.localEulerAngles.z;
        if (current > 180f) current -= 360f;   // unwrap so Lerp goes the short way
        float next = Mathf.Lerp(current, _targetAngle, animSpeed * Time.deltaTime);
        transform.localEulerAngles = new Vector3(0f, 0f, next);
    }

    public void Toggle()
    {
        isOpen      = !isOpen;
        _targetAngle = isOpen ? openAngle : 0f;
    }

    public string Prompt => isOpen ? "Close Hatch" : "Open Hatch";
}

// ── Interactable wrapper on the trigger GO (doesn't rotate with the hatch) ──
public class HatchInteract : MonoBehaviour, IInteractable
{
    public ServingHatch hatch;

    public string GetPrompt() => hatch != null ? hatch.Prompt : null;
    public void Interact(GameObject interactor) => hatch?.Toggle();
}
