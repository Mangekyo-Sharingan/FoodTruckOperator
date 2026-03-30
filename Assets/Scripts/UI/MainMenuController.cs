using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires button callbacks and settings persistence for the Main Menu scene.
/// Attach to the MenuManager GameObject. References are found by name on Awake.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    const string GAME_SCENE       = "CityScene";
    const string PREF_VOL_MASTER  = "vol_master";
    const string PREF_VOL_MUSIC   = "vol_music";
    const string PREF_VOL_SFX     = "vol_sfx";
    const string PREF_QUALITY     = "quality";
    const string PREF_FULLSCREEN  = "fullscreen";

    GameObject _mainPanel;
    GameObject _settingsPanel;

    void Awake()
    {
        var canvas       = FindAnyObjectByType<Canvas>().transform;
        _mainPanel       = canvas.Find("MainPanel")?.gameObject;
        _settingsPanel   = canvas.Find("SettingsPanel")?.gameObject;

        if (_mainPanel == null || _settingsPanel == null)
        {
            Debug.LogError("MainMenuController: MainPanel or SettingsPanel not found in Canvas.");
            return;
        }

        // ── Main panel buttons ─────────────────────────────────────────────
        Wire(_mainPanel, "PlayButton",     OnPlay);
        Wire(_mainPanel, "SettingsButton", OnSettings);
        Wire(_mainPanel, "QuitButton",     OnQuit);

        // ── Settings panel buttons ─────────────────────────────────────────
        Wire(_settingsPanel, "BackButton", OnBack);

        // ── Sliders ────────────────────────────────────────────────────────
        WireSlider(_settingsPanel, "Master VolumeSld", PREF_VOL_MASTER, 1f,
            v => AudioListener.volume = v);
        WireSlider(_settingsPanel, "Music VolumeSld",  PREF_VOL_MUSIC,  0.8f,
            v => PlayerPrefs.SetFloat(PREF_VOL_MUSIC, v));
        WireSlider(_settingsPanel, "SFX VolumeSld",    PREF_VOL_SFX,    1f,
            v => PlayerPrefs.SetFloat(PREF_VOL_SFX, v));

        // ── Toggle ─────────────────────────────────────────────────────────
        var fsTog = _settingsPanel.transform.Find("FSTog")?.GetComponent<Toggle>();
        if (fsTog != null)
        {
            fsTog.isOn = PlayerPrefs.GetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
            fsTog.onValueChanged.AddListener(v => { Screen.fullScreen = v; PlayerPrefs.SetInt(PREF_FULLSCREEN, v ? 1 : 0); });
        }

        // ── Quality dropdown ───────────────────────────────────────────────
        var dd = _settingsPanel.transform.Find("QualDD")?.GetComponent<Dropdown>();
        if (dd != null)
        {
            dd.value = PlayerPrefs.GetInt(PREF_QUALITY, QualitySettings.GetQualityLevel());
            dd.onValueChanged.AddListener(v => { QualitySettings.SetQualityLevel(v); PlayerPrefs.SetInt(PREF_QUALITY, v); });
        }

        // Apply saved settings on startup
        AudioListener.volume = PlayerPrefs.GetFloat(PREF_VOL_MASTER, 1f);
    }

    // ── Callbacks ─────────────────────────────────────────────────────────────
    void OnPlay()     => SceneManager.LoadScene(GAME_SCENE);
    void OnQuit()     { PlayerPrefs.Save(); Application.Quit(); }
    void OnSettings() { _mainPanel.SetActive(false); _settingsPanel.SetActive(true); }
    void OnBack()     { _settingsPanel.SetActive(false); _mainPanel.SetActive(true); PlayerPrefs.Save(); }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static void Wire(GameObject panel, string childName, UnityEngine.Events.UnityAction action)
    {
        var btn = panel.transform.Find(childName)?.GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(action);
        else Debug.LogWarning($"MainMenuController: Button '{childName}' not found in {panel.name}");
    }

    static void WireSlider(GameObject panel, string childName, string prefKey, float defaultVal,
        System.Action<float> onChange)
    {
        var sl = panel.transform.Find(childName)?.GetComponent<Slider>();
        if (sl == null) return;
        sl.value = PlayerPrefs.GetFloat(prefKey, defaultVal);
        sl.onValueChanged.AddListener(v => {
            onChange(v);
            PlayerPrefs.SetFloat(prefKey, v);
            // Update the % label (sibling named childName+"P" without "Sld")
            var pctName = childName.Replace("Sld","P");
            var pctTxt  = panel.transform.Find(pctName)?.GetComponent<Text>();
            if (pctTxt != null) pctTxt.text = Mathf.RoundToInt(v * 100) + "%";
        });
    }
}
