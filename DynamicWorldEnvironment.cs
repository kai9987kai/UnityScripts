using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Standalone runtime day/night, weather, wind and precipitation controller.
/// Compatible with Unity 2022.3 LTS and Unity 6; no third-party packages.
/// Assign a sun Light; all other scene references are optional.
/// </summary>
[DisallowMultipleComponent]
public sealed class DynamicWorldEnvironment : MonoBehaviour
{
    public enum WeatherType { Clear, Overcast, Rain, Storm, Snow }

    [Header("Optional Scene References")]
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;
    [SerializeField] private Light lightningLight;
    [SerializeField] private WindZone windZone;
    [SerializeField] private ParticleSystem rainParticles;
    [SerializeField] private ParticleSystem snowParticles;

    [Header("Clock")]
    [SerializeField, Range(0f, 24f)] private float startingHour = 9f;
    [SerializeField, Min(0.1f)] private float fullDayMinutes = 20f;
    [SerializeField] private bool clockRunning = true;
    [SerializeField, Range(0f, 360f)] private float solarAzimuth = 170f;

    [Header("Weather")]
    [SerializeField] private WeatherType startingWeather = WeatherType.Clear;
    [SerializeField, Min(0f)] private float transitionSeconds = 10f;
    [SerializeField, Min(0f)] private float rainParticlesPerSecond = 450f;
    [SerializeField, Min(0f)] private float snowParticlesPerSecond = 160f;

    [Header("Lighting")]
    [SerializeField, Min(0f)] private float sunIntensity = 1.25f;
    [SerializeField, Min(0f)] private float moonIntensity = 0.18f;
    [SerializeField] private Color daySun = new Color(1f, 0.95f, 0.83f);
    [SerializeField] private Color sunsetSun = new Color(1f, 0.52f, 0.27f);
    [SerializeField] private Color moonColour = new Color(0.64f, 0.74f, 1f);
    [SerializeField] private bool controlAmbient = true;
    [SerializeField] private Color dayAmbient = new Color(0.59f, 0.68f, 0.79f);
    [SerializeField] private Color nightAmbient = new Color(0.045f, 0.065f, 0.13f);

    [Header("Fog / Wind")]
    [SerializeField] private bool controlFog = true;
    [SerializeField, Min(0f)] private float clearFogDensity = 0.0015f;
    [SerializeField, Min(0f)] private float thickFogDensity = 0.022f;
    [SerializeField] private Color dayFog = new Color(0.66f, 0.76f, 0.85f);
    [SerializeField] private Color nightFog = new Color(0.05f, 0.07f, 0.13f);
    [SerializeField, Min(0f)] private float calmWind = 0.15f;
    [SerializeField, Min(0f)] private float strongWind = 2f;
    [SerializeField, Min(0f)] private float lightningIntensity = 4f;

    [Header("Demo / Cleanup")]
    [SerializeField] private bool legacyKeyboardShortcuts = true;
    [SerializeField] private bool drawStatus = true;
    [SerializeField] private bool restoreGlobalSettingsOnDisable = true;

    private struct WeatherProfile
    {
        public float Clouds, Fog, Wind, Rain, Snow, Lightning;

        public WeatherProfile(float c, float f, float w, float r, float s, float l)
        {
            Clouds = c; Fog = f; Wind = w;
            Rain = r; Snow = s; Lightning = l;
        }

        public static WeatherProfile Blend(WeatherProfile a, WeatherProfile b, float t)
        {
            return new WeatherProfile(
                Mathf.Lerp(a.Clouds, b.Clouds, t),
                Mathf.Lerp(a.Fog, b.Fog, t),
                Mathf.Lerp(a.Wind, b.Wind, t),
                Mathf.Lerp(a.Rain, b.Rain, t),
                Mathf.Lerp(a.Snow, b.Snow, t),
                Mathf.Lerp(a.Lightning, b.Lightning, t));
        }
    }

    private float hour, daylight, elapsedTransition, nextLightning, flashRemaining;
    private WeatherType desiredWeather;
    private WeatherProfile current, transitionFrom, transitionTo;

    private bool saved, originalFog;
    private FogMode originalFogMode;
    private float originalFogDensity;
    private Color originalFogColour, originalAmbientColour;
    private AmbientMode originalAmbientMode;

    public float TimeOfDayHours { get { return hour; } }
    public float Sunlight01 { get { return daylight; } }
    public bool IsNight { get { return daylight < 0.01f; } }
    public WeatherType TargetWeather { get { return desiredWeather; } }
    public float RainIntensity01 { get { return current.Rain; } }
    public float SnowIntensity01 { get { return current.Snow; } }

    /// <summary>Raised when the selected weather type changes.</summary>
    public event Action<WeatherType> WeatherChanged;

    private static WeatherProfile Preset(WeatherType type)
    {
        switch (type)
        {
            case WeatherType.Overcast: return new WeatherProfile(.75f, .22f, .30f, 0, 0, 0);
            case WeatherType.Rain: return new WeatherProfile(.85f, .50f, .55f, .72f, 0, 0);
            case WeatherType.Storm: return new WeatherProfile(1, .90f, 1, 1, 0, 1);
            case WeatherType.Snow: return new WeatherProfile(.80f, .72f, .42f, 0, .75f, 0);
            default: return new WeatherProfile(.05f, .03f, .10f, 0, 0, 0);
        }
    }

    private void Awake()
    {
        if (sun == null) sun = RenderSettings.sun;
        originalFog = RenderSettings.fog;
        originalFogMode = RenderSettings.fogMode;
        originalFogDensity = RenderSettings.fogDensity;
        originalFogColour = RenderSettings.fogColor;
        originalAmbientMode = RenderSettings.ambientMode;
        originalAmbientColour = RenderSettings.ambientLight;
        saved = true;

        hour = Mathf.Repeat(startingHour, 24f);
        desiredWeather = startingWeather;
        current = transitionFrom = transitionTo = Preset(desiredWeather);
        if (lightningLight != null) lightningLight.enabled = false;
        ApplyEnvironment();
    }

    private void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (legacyKeyboardShortcuts)
        {
            if (Input.GetKeyDown(KeyCode.F6)) clockRunning = !clockRunning;
            if (Input.GetKeyDown(KeyCode.F7))
                SetWeather((WeatherType)(((int)desiredWeather + 1) % 5));
            if (Input.GetKeyDown(KeyCode.F8)) SetTime(hour + 1f);
        }
#endif
        float dt = Time.deltaTime;
        if (clockRunning)
            hour = Mathf.Repeat(hour + dt * 24f / (Mathf.Max(.1f, fullDayMinutes) * 60f), 24f);

        if (transitionSeconds <= 0f)
            current = transitionTo;
        else if (elapsedTransition < transitionSeconds)
        {
            elapsedTransition = Mathf.Min(transitionSeconds, elapsedTransition + dt);
            float t = elapsedTransition / transitionSeconds;
            t = t * t * (3f - 2f * t);
            current = WeatherProfile.Blend(transitionFrom, transitionTo, t);
        }
        ApplyEnvironment();
        TickLightning(dt);
    }

    public void SetWeather(WeatherType weather)
    {
        if (!Enum.IsDefined(typeof(WeatherType), weather) || weather == desiredWeather)
            return;

        transitionFrom = current;
        transitionTo = Preset(weather);
        desiredWeather = weather;
        elapsedTransition = 0f;
        if (transitionSeconds <= 0f) current = transitionTo;
        WeatherChanged?.Invoke(weather);
    }

    public void SetTime(float localHour)
    {
        if (float.IsInfinity(localHour) || float.IsNaN(localHour)) return;
        hour = Mathf.Repeat(localHour, 24f);
        ApplyEnvironment();
    }

    public void PauseClock(bool paused) { clockRunning = !paused; }

    private void ApplyEnvironment()
    {
        // Deliberately simplified: sunrise 06:00; noon 12:00; sunset 18:00.
        float elevation = Mathf.Sin((hour - 6f) * Mathf.PI / 12f);
        daylight = Mathf.Clamp01(elevation);
        float moonlight = Mathf.Clamp01(-elevation);
        float angle = hour * 15f - 90f;

        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(angle, solarAzimuth, 0f);
            sun.color = Color.Lerp(sunsetSun, daySun, Mathf.Pow(daylight, .6f));
            sun.intensity = sunIntensity * daylight * Mathf.Lerp(1f, .26f, current.Clouds);
        }
        if (moon != null)
        {
            moon.transform.rotation = Quaternion.Euler(angle + 180f, solarAzimuth, 0f);
            moon.color = moonColour;
            moon.intensity = moonIntensity * moonlight * Mathf.Lerp(1f, .4f, current.Clouds);
        }
        if (controlAmbient)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            Color tint = Color.Lerp(nightAmbient, dayAmbient, daylight);
            RenderSettings.ambientLight = Color.Lerp(
                tint, new Color(.39f, .42f, .47f), current.Clouds * .55f);
        }
        if (controlFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = Mathf.Lerp(
                clearFogDensity, thickFogDensity, current.Fog);
            RenderSettings.fogColor = Color.Lerp(
                Color.Lerp(nightFog, dayFog, daylight),
                new Color(.39f, .42f, .47f), current.Clouds * .65f);
        }
        if (windZone != null)
        {
            windZone.windMain = Mathf.Lerp(calmWind, strongWind, current.Wind);
            windZone.windTurbulence = Mathf.Lerp(.05f, .65f, current.Wind);
        }
        SetEmission(rainParticles, current.Rain * rainParticlesPerSecond);
        SetEmission(snowParticles, current.Snow * snowParticlesPerSecond);
    }

    private static void SetEmission(ParticleSystem particles, float rate)
    {
        if (particles == null) return;
        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = Mathf.Max(0f, rate);
        if (rate > .1f)
        {
            if (!particles.isPlaying) particles.Play();
        }
        else if (particles.isPlaying)
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void TickLightning(float dt)
    {
        if (lightningLight == null) return;
        if (current.Lightning < .15f)
        {
            flashRemaining = nextLightning = 0f;
            lightningLight.enabled = false;
            return;
        }
        if (flashRemaining > 0f)
        {
            flashRemaining = Mathf.Max(0f, flashRemaining - dt);
            lightningLight.enabled = flashRemaining > 0f;
            lightningLight.intensity = lightningIntensity * current.Lightning *
                Mathf.Clamp01(flashRemaining / .2f);
            return;
        }
        lightningLight.enabled = false;
        nextLightning -= dt;
        if (nextLightning <= 0f)
        {
            flashRemaining = .2f;
            nextLightning = UnityEngine.Random.Range(6f, 17f);
            lightningLight.intensity = lightningIntensity * current.Lightning;
            lightningLight.enabled = true;
        }
    }

    private void OnValidate()
    {
        fullDayMinutes = Mathf.Max(.1f, fullDayMinutes);
        transitionSeconds = Mathf.Max(0f, transitionSeconds);
        thickFogDensity = Mathf.Max(clearFogDensity, thickFogDensity);
        strongWind = Mathf.Max(calmWind, strongWind);
    }

    private void OnDisable()
    {
        if (lightningLight != null) lightningLight.enabled = false;
        if (!saved || !restoreGlobalSettingsOnDisable) return;
        if (controlFog)
        {
            RenderSettings.fog = originalFog;
            RenderSettings.fogMode = originalFogMode;
            RenderSettings.fogDensity = originalFogDensity;
            RenderSettings.fogColor = originalFogColour;
        }
        if (controlAmbient)
        {
            RenderSettings.ambientMode = originalAmbientMode;
            RenderSettings.ambientLight = originalAmbientColour;
        }
    }

    private void OnGUI()
    {
        if (!drawStatus || !Application.isPlaying) return;
        int h = Mathf.FloorToInt(hour);
        int m = Mathf.FloorToInt((hour - h) * 60f);
        GUILayout.BeginArea(new Rect(Mathf.Max(10f, Screen.width - 300f),
            12f, 288f, 110f), GUI.skin.box);
        GUILayout.Label("DYNAMIC WORLD ENVIRONMENT");
        GUILayout.Label("Time " + h.ToString("00") + ":" + m.ToString("00") +
            (clockRunning ? " (running)" : " (paused)"));
        GUILayout.Label("Weather " + desiredWeather + " | Clouds " +
            Mathf.RoundToInt(current.Clouds * 100f) + "%");
#if ENABLE_LEGACY_INPUT_MANAGER
        if (legacyKeyboardShortcuts)
            GUILayout.Label("F6 pause | F7 weather | F8 +1 hour");
#endif
        GUILayout.EndArea();
    }
}
