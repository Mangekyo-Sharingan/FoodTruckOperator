using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle Settings")]
    public float cycleDuration = 120f;
    public float startTime = 0.25f;

    [Header("Sun")]
    public Light sun;
    public float dayIntensity = 1.2f;
    public float nightIntensity = 0.1f;
    public Color sunriseColor = new Color(1f, 0.6f, 0.3f);
    public Color noonColor = new Color(1f, 0.98f, 0.9f);
    public Color sunsetColor = new Color(1f, 0.4f, 0.2f);
    public Color nightColor = new Color(0.1f, 0.15f, 0.35f);

    [Header("Ambient")]
    public Color dayAmbient = new Color(0.4f, 0.45f, 0.55f);
    public Color nightAmbient = new Color(0.05f, 0.08f, 0.15f);

    [Header("Fog")]
    public Color dayFog = new Color(0.72f, 0.75f, 0.80f);
    public Color nightFog = new Color(0.15f, 0.18f, 0.25f);
    public float dayFogDensity = 0.005f;
    public float nightFogDensity = 0.015f;

    public float CurrentTime { get; private set; }
    public bool IsDaytime { get; private set; }

    float _timer;

    void Start()
    {
        _timer = startTime * cycleDuration;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= cycleDuration) _timer -= cycleDuration;

        CurrentTime = _timer / cycleDuration;
        IsDaytime = CurrentTime > 0.2f && CurrentTime < 0.8f;

        UpdateSun();
        UpdateAmbient();
        UpdateFog();
    }

    void UpdateSun()
    {
        if (sun == null) return;

        float sunAngle = CurrentTime * 360f - 90f;
        sun.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);

        float dayAmount = Mathf.Sin(CurrentTime * Mathf.PI);
        dayAmount = Mathf.Clamp01(dayAmount * 2f);

        sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, dayAmount);
        sun.color = GetSunColor(CurrentTime);
        sun.shadows = dayAmount > 0.1f ? LightShadows.Soft : LightShadows.None;
    }

    Color GetSunColor(float time)
    {
        if (time < 0.2f) return nightColor;
        if (time < 0.3f) return Color.Lerp(nightColor, sunriseColor, (time - 0.2f) / 0.1f);
        if (time < 0.4f) return Color.Lerp(sunriseColor, noonColor, (time - 0.3f) / 0.1f);
        if (time < 0.6f) return noonColor;
        if (time < 0.7f) return Color.Lerp(noonColor, sunsetColor, (time - 0.6f) / 0.1f);
        if (time < 0.8f) return Color.Lerp(sunsetColor, nightColor, (time - 0.7f) / 0.1f);
        return nightColor;
    }

    void UpdateAmbient()
    {
        float dayAmount = Mathf.Sin(CurrentTime * Mathf.PI);
        dayAmount = Mathf.Clamp01(dayAmount * 2f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, dayAmount);
    }

    void UpdateFog()
    {
        float dayAmount = Mathf.Sin(CurrentTime * Mathf.PI);
        dayAmount = Mathf.Clamp01(dayAmount * 2f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = Color.Lerp(nightFog, dayFog, dayAmount);
        RenderSettings.fogDensity = Mathf.Lerp(nightFogDensity, dayFogDensity, dayAmount);
    }

    public float GetDayProgress()
    {
        return CurrentTime;
    }
}
