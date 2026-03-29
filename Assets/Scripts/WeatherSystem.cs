using UnityEngine;

public enum WeatherState
{
    Sunny,
    Cloudy,
    Rainy
}

public class WeatherSystem : MonoBehaviour
{
    [Header("Weather Settings")]
    public WeatherState currentWeather = WeatherState.Sunny;
    public float changeInterval = 30f;
    public float transitionDuration = 5f;

    [Header("References")]
    public Light sun;
    public ParticleSystem rainParticles;

    [Header("Sunny")]
    public float sunnyIntensity = 1.2f;
    public Color sunnyFog = new Color(0.72f, 0.75f, 0.80f);
    public Color sunnySky = new Color(0.5f, 0.7f, 1f);

    [Header("Cloudy")]
    public float cloudyIntensity = 0.6f;
    public Color cloudyFog = new Color(0.55f, 0.55f, 0.6f);
    public Color cloudySky = new Color(0.45f, 0.5f, 0.55f);

    [Header("Rainy")]
    public float rainyIntensity = 0.3f;
    public Color rainyFog = new Color(0.35f, 0.38f, 0.42f);
    public Color rainySky = new Color(0.25f, 0.28f, 0.32f);

    WeatherState _targetWeather;
    float _transitionProgress = 1f;
    float _timer;
    float _targetIntensity;
    Color _targetFogColor;
    Color _targetSkyColor;
    float _currentIntensity;
    Color _currentFogColor;
    float _rainIntensity;

    public WeatherState CurrentWeather => currentWeather;
    public bool IsRaining => currentWeather == WeatherState.Rainy;

    void Start()
    {
        _targetWeather = currentWeather;
        _currentIntensity = sunnyIntensity;
        _currentFogColor = sunnyFog;

        SetupRainParticles();
        ApplyWeatherImmediate(currentWeather);
    }

    void SetupRainParticles()
    {
        if (rainParticles == null)
        {
            rainParticles = gameObject.AddComponent<ParticleSystem>();
            var main = rainParticles.main;
            main.loop = true;
            main.startLifetime = 2f;
            main.startSpeed = 15f;
            main.startSize = 0.1f;
            main.maxParticles = 1000;

            var emission = rainParticles.emission;
            emission.rateOverTime = 0;

            var shape = rainParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = new Vector3(0, 20, 0);
            shape.scale = new Vector3(50, 1, 50);

            var renderer = rainParticles.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.color = new Color(0.7f, 0.8f, 1f, 0.5f);
        }
        rainParticles.Stop();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= changeInterval && _transitionProgress >= 1f)
        {
            _timer = 0f;
            PickNewWeather();
        }

        UpdateTransition();
    }

    void PickNewWeather()
    {
        WeatherState[] states = { WeatherState.Sunny, WeatherState.Cloudy, WeatherState.Rainy };
        var weights = GetWeatherWeights();

        float rand = Random.value;
        float cumulative = 0f;

        for (int i = 0; i < states.Length; i++)
        {
            cumulative += weights[i];
            if (rand <= cumulative)
            {
                if (states[i] != currentWeather)
                {
                    _targetWeather = states[i];
                    _transitionProgress = 0f;
                }
                return;
            }
        }
    }

    float[] GetWeatherWeights()
    {
        switch (currentWeather)
        {
            case WeatherState.Sunny: return new float[] { 0.4f, 0.4f, 0.2f };
            case WeatherState.Cloudy: return new float[] { 0.3f, 0.4f, 0.3f };
            case WeatherState.Rainy: return new float[] { 0.3f, 0.4f, 0.3f };
            default: return new float[] { 0.4f, 0.4f, 0.2f };
        }
    }

    void UpdateTransition()
    {
        if (_transitionProgress < 1f)
        {
            _transitionProgress += Time.deltaTime / transitionDuration;
            _transitionProgress = Mathf.Clamp01(_transitionProgress);

            float t = SmoothStep(_transitionProgress);

            _currentIntensity = Mathf.Lerp(_currentIntensity, _targetIntensity, t * 0.1f);
            _currentFogColor = Color.Lerp(_currentFogColor, _targetFogColor, t * 0.1f);

            ApplyWeatherToScene();
            UpdateRainParticles();
        }
    }

    float SmoothStep(float x)
    {
        return x * x * (3f - 2f * x);
    }

    void ApplyWeatherImmediate(WeatherState state)
    {
        switch (state)
        {
            case WeatherState.Sunny:
                _targetIntensity = sunnyIntensity;
                _targetFogColor = sunnyFog;
                break;
            case WeatherState.Cloudy:
                _targetIntensity = cloudyIntensity;
                _targetFogColor = cloudyFog;
                break;
            case WeatherState.Rainy:
                _targetIntensity = rainyIntensity;
                _targetFogColor = rainyFog;
                break;
        }
        _currentIntensity = _targetIntensity;
        _currentFogColor = _targetFogColor;
        ApplyWeatherToScene();
        UpdateRainParticles();
    }

    void ApplyWeatherToScene()
    {
        if (sun != null)
        {
            sun.intensity = _currentIntensity;
        }

        RenderSettings.fogColor = _currentFogColor;

        if (RenderSettings.skybox != null)
        {
            Color skyTint = currentWeather == WeatherState.Rainy ? rainySky 
                : currentWeather == WeatherState.Cloudy ? cloudySky : sunnySky;
            RenderSettings.skybox.SetColor("_Tint", skyTint);
        }
    }

    void UpdateRainParticles()
    {
        if (rainParticles == null) return;

        var emission = rainParticles.emission;

        if (currentWeather == WeatherState.Rainy && _transitionProgress > 0.5f)
        {
            emission.rateOverTime = 500f;
            if (!rainParticles.isPlaying) rainParticles.Play();
        }
        else
        {
            float currentRate = emission.rateOverTime.constant;
            emission.rateOverTime = Mathf.Lerp(currentRate, 0f, Time.deltaTime * 2f);
            if (currentRate < 1f && rainParticles.isPlaying)
                rainParticles.Stop();
        }
    }

    void OnValidate()
    {
        if (currentWeather != _targetWeather)
        {
            _targetWeather = currentWeather;
            _transitionProgress = 0f;
        }
    }
}
