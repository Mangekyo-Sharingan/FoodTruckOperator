using UnityEngine;

/// <summary>
/// Central game state singleton. Tracks money, day, and game phase.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Economy")]
    public int money = 200;

    [Header("Day Tracking")]
    public int currentDay = 1;
    public float dayDuration = 180f; // seconds per trading day

    float _dayTimer;
    bool _dayActive;

    public bool DayActive => _dayActive;
    public float DayTimeRemaining => Mathf.Max(0f, dayDuration - _dayTimer);
    public float DayProgress => Mathf.Clamp01(_dayTimer / dayDuration);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() => StartDay();

    void Update()
    {
        if (!_dayActive) return;
        _dayTimer += Time.deltaTime;
        if (_dayTimer >= dayDuration) EndDay();
    }

    public void StartDay()
    {
        _dayTimer = 0f;
        _dayActive = true;
        Debug.Log($"[GameManager] Day {currentDay} started. You have ${money}.");
    }

    public void EndDay()
    {
        _dayActive = false;
        currentDay++;
        Debug.Log($"[GameManager] Day ended. Money: ${money}. Starting day {currentDay}...");
        // TODO: Show end-of-day summary, then call StartDay()
    }

    public void AddMoney(int amount)
    {
        money += amount;
        Debug.Log($"[GameManager] +${amount} → Total: ${money}");
    }

    public bool SpendMoney(int amount)
    {
        if (money < amount) return false;
        money -= amount;
        return true;
    }
}
