/// <summary>
/// Implement on any MonoBehaviour that the player can interact with via the E key.
/// </summary>
public interface IInteractable
{
    /// <summary>Short label shown in the interaction prompt, e.g. "Enter Truck".</summary>
    string GetPrompt();

    /// <summary>Called when the player presses E while this is the focused interactable.</summary>
    void Interact(UnityEngine.GameObject interactor);
}
