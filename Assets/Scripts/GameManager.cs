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
    public float dayDurationMinutes = 10f; // minutes per selling day

    // Per-day economy tracking
    public int DayMoneyEarned { get; private set; }
    public int DayMoneySpent  { get; private set; }

    float _dayTimer; // seconds elapsed this day
    bool _dayActive;
    bool _dayEndedPending; // waiting for UI to dismiss before starting next day

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
        _dayEndedPending = false;
        DayMoneyEarned = 0;
        DayMoneySpent  = 0;

        if (currentDay >= 2)
        {
            Debug.Log($"[GameManager] Adding daily government benefits for day {currentDay}... +100");
            AddMoney(100);
        }
        Debug.Log($"[GameManager] Day {currentDay} started. You have ${money}.");
    }

    public void EndDay()
    {
        if (_dayEndedPending) return; // guard against double-trigger
        _dayActive = false;
        _dayEndedPending = true;
        currentDay += 1;
        Debug.Log($"[GameManager] Day ended. Earned: ${DayMoneyEarned}  Spent: ${DayMoneySpent}  Balance: ${money}");

        // Show the end-of-day summary UI; it will call ContinueToNextDay() when dismissed.
        EndOfDayUI ui = FindObjectOfType<EndOfDayUI>();
        if (ui != null)
            ui.Show(DayMoneyEarned, DayMoneySpent);
        else
            ContinueToNextDay(); // fallback if no UI present
    }

    /// <summary>Called by EndOfDayUI after the player dismisses the summary.</summary>
    public void ContinueToNextDay()
    {
        StartDay();
    }

    public void AddMoney(int amount)
    {
        money += amount;
        if (_dayActive && amount > 0) DayMoneyEarned += amount;
        Debug.Log($"[GameManager] +${amount} → Total: ${money}");
    }

    public bool SpendMoney(int amount)
    {
        if (money < amount) return false;
        money -= amount;
        if (_dayActive) DayMoneySpent += amount;
        return true;
    }
}
