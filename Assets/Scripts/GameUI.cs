using UnityEngine;

public class GameUI : MonoBehaviour
{
    [Header("Position")]
    public float xOffset = 20f;
    public float yOffset = 20f;
    public TextAnchor alignment = TextAnchor.UpperLeft;

    [Header("Style")]
    public Color textColor = Color.white;
    public float fontSize = 18;
    public float shadowOffset = 2f;

    GUIStyle _style;
    Color _shadowColor;

    void Start()
    {
        Debug.Log("[GameUI] Start - GameUI object: " + gameObject.name + ", active: " + gameObject.activeSelf);
        Debug.Log("[GameUI] Component enabled: " + enabled);
    }

    void Update()
    {
        Debug.Log("[GameUI] Update running - UI should display!");
    }

    void Awake()
    {
        Debug.Log("[GameUI] Awake called - GameUI initializing");
        DontDestroyOnLoad(gameObject);
        _shadowColor = new Color(0, 0, 0, 0.5f);
        _style = new GUIStyle
        {
            alignment = alignment,
            fontSize = Mathf.RoundToInt(fontSize),
            normal = new GUIStyleState
            {
                textColor = textColor
            }
        };
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, Screen.height - 30, 200, 20), "GameUI Test");

        if (GameManager.Instance == null)
        {
            DrawDebugText(10, 30, "GameManager not found!", Color.red);
            return;
        }

        float timeRemaining = GameManager.Instance.DayTimeRemaining;
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        string timeStr = $"{minutes:0}:{seconds:00}";

        string label = $"Day {GameManager.Instance.currentDay}  |  ${GameManager.Instance.money}  |  Time: {timeStr}";

        float labelHeight = fontSize * 1.5f;
        float labelWidth = 350f;

        float x = xOffset;
        float y = yOffset;

        if (alignment == TextAnchor.UpperRight)
            x = Screen.width - labelWidth - xOffset;
        else if (alignment == TextAnchor.UpperCenter)
            x = (Screen.width - labelWidth) / 2;

        _style.normal.textColor = _shadowColor;
        GUI.Label(new Rect(x + shadowOffset, y + shadowOffset, labelWidth, labelHeight), label, _style);

        _style.normal.textColor = textColor;
        GUI.Label(new Rect(x, y, labelWidth, labelHeight), label, _style);
    }

    void DrawShadowText(float x, float y, string text)
    {
        _style.normal.textColor = _shadowColor;
        GUI.Label(new Rect(x + shadowOffset, y + shadowOffset, 300, 30), text, _style);

        _style.normal.textColor = textColor;
        GUI.Label(new Rect(x, y, 300, 30), text, _style);
    }

    void DrawDebugText(float x, float y, string text, Color color)
    {
        GUIStyle debugStyle = new GUIStyle
        {
            fontSize = 14,
            normal = new GUIStyleState { textColor = color }
        };
        GUI.Label(new Rect(x, y, 400, 30), text, debugStyle);
    }
}
