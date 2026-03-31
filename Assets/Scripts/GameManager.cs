using UnityEngine;

/// <summary>
/// Central game state singleton. Tracks money, day, and game phase.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Economy")]
    public int money = 1000;

    [Header("Day Tracking")]
    public int currentDay = 1;
    public float dayDurationMinutes = 15f; // minutes per selling day

    float _dayTimer; // seconds elapsed this day
    bool _dayActive;

    float DayDurationSeconds => dayDurationMinutes * 60f;

    public bool DayActive => _dayActive;
    /// <summary>Minutes elapsed so far this day.</summary>
    public float DayTimeElapsed => _dayTimer / 60f;
    /// <summary>0→1 progress through the day.</summary>
    public float DayProgress => Mathf.Clamp01(_dayTimer / DayDurationSeconds);

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
        if (_dayTimer >= DayDurationSeconds) EndDay();
    }

    public void StartDay()
    {
        _dayTimer = 0f;
        _dayActive = true;
        if (currentDay >= 2)
        {
            Debug.Log($"[GameManager] Adding daily government benefits for day {currentDay}... +100");
            AddMoney(100);
        }
        Debug.Log($"[GameManager] Day {currentDay} started. You have ${money}.");
    }

    public void EndDay()
    {
        _dayActive = false;
        currentDay += 1;
        Debug.Log($"[GameManager] Day ended. Money: ${money}. Starting day {currentDay}...");
        // TODO: Show end-of-day summary, then call StartDay()
        Debug.Log($"[GameManager] Simulating end-of-day summary... (TODO: Implement UI summary screen)");
        Debug.Log($"Starting next day...");
        StartDay();
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
