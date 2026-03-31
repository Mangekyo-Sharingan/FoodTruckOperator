using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// End-of-Day summary popup.
/// Assign references via the Inspector (panel, texts, button).
/// GameManager.EndDay() calls Show(); the Continue button calls Dismiss().
/// </summary>
public class EndOfDayUI : MonoBehaviour
{
    [Header("Panel")]
    public CanvasGroup canvasGroup;

    [Header("Text Fields")]
    public TMP_Text dayNumberText;
    public TMP_Text moneyEarnedText;
    public TMP_Text moneySpentText;
    public TMP_Text netText;

    [Header("Button")]
    public Button continueButton;

    [Header("Fade")]
    public float fadeDuration = 0.5f;

    bool _isVisible;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponentInChildren<CanvasGroup>();
        continueButton?.onClick.AddListener(Dismiss);
        // Start hidden via CanvasGroup — do NOT deactivate the GameObject
        SetVisible(false);
    }

    void Update()
    {
        if (_isVisible && Input.GetKeyDown(KeyCode.Space))
            Dismiss();
    }

    /// <summary>Populate and fade the panel in.</summary>
    public void Show(int earned, int spent)
    {
        int day = GameManager.Instance != null ? GameManager.Instance.currentDay - 1 : 0;
        int net = earned - spent;

        if (dayNumberText)   dayNumberText.text   = $"Day {day} Complete";
        if (moneyEarnedText) moneyEarnedText.text = $"${earned}";
        if (moneySpentText)  moneySpentText.text  = $"${spent}";
        if (netText)
        {
            netText.text  = net >= 0 ? $"+${net}" : $"-${Mathf.Abs(net)}";
            netText.color = net >= 0 ? new Color(0.2f, 0.85f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        _isVisible                 = true;
        canvasGroup.interactable   = true;
        canvasGroup.blocksRaycasts = true;
        StopAllCoroutines();
        StartCoroutine(Fade(0f, 1f, fadeDuration));
    }

    /// <summary>Fade the panel out, then tell GameManager to proceed.</summary>
    public void Dismiss()
    {
        continueButton.interactable = false;
        StopAllCoroutines();
        StartCoroutine(FadeOutAndContinue());
    }

    IEnumerator FadeOutAndContinue()
    {
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));
        SetVisible(false);
        if (continueButton) continueButton.interactable = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        GameManager.Instance?.ContinueToNextDay();
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        canvasGroup.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    void SetVisible(bool visible)
    {
        _isVisible                 = visible;
        canvasGroup.alpha          = visible ? 1f : 0f;
        canvasGroup.interactable   = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
