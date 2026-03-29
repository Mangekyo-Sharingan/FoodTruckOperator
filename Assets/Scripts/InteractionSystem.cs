using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attached to the Player. Detects nearby IInteractables, shows a prompt,
/// and calls Interact() when the player presses E.
/// </summary>
public class InteractionSystem : MonoBehaviour
{
    [Header("Detection")]
    public float interactRadius = 2.5f;
    public LayerMask interactLayers = ~0;
    public Transform eyePoint;           // optional — falls back to transform

    IInteractable _focused;
    readonly Collider[] _hits = new Collider[8];

    // Cached GUI style
    GUIStyle _promptStyle;

    void Update()
    {
        DetectNearest();

        if (_focused != null && Keyboard.current != null
            && Keyboard.current.eKey.wasPressedThisFrame)
        {
            _focused.Interact(gameObject);
        }
    }

    void DetectNearest()
    {
        Vector3 origin = eyePoint != null ? eyePoint.position : transform.position;
        int count = Physics.OverlapSphereNonAlloc(origin, interactRadius, _hits,
            interactLayers, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var ia = _hits[i].GetComponentInParent<IInteractable>();
            if (ia == null || string.IsNullOrEmpty(ia.GetPrompt())) continue;

            // Don't interact with self
            if (_hits[i].transform.IsChildOf(transform)) continue;

            float d = Vector3.Distance(origin, _hits[i].transform.position);
            if (d < bestDist) { bestDist = d; best = ia; }
        }

        _focused = best;
    }

    void OnGUI()
    {
        if (_focused == null) return;

        if (_promptStyle == null)
        {
            _promptStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
            _promptStyle.normal.textColor = Color.white;
        }

        string label = $"[E]  {_focused.GetPrompt()}";
        Vector2 size = _promptStyle.CalcSize(new GUIContent(label));
        size.x += 24; size.y += 12;

        float x = (Screen.width - size.x) * 0.5f;
        float y = Screen.height * 0.72f;
        GUI.Box(new Rect(x, y, size.x, size.y), label, _promptStyle);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
