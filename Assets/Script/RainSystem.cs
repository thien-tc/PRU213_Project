using DigitalRuby.RainMaker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.UI;

public class RainSystem : MonoBehaviour
{
    [Header("References")]
    public ParticleSystem rainParticle;// particle mưa
    public ParticleSystem rainSplashParticle; // particle bắn tung tóe khi mưa chạm đất
    //public Transform waterPlane; // mặt phẳng nước để tạo splash
    public TimeManager timeManager; // quản lý thời gian trong game

    [Header("Rain Maker Asset")]
    public RainScript2D rainMaker; // THÊM: Kéo thả RainScript2D từ RainPrefab2D vào đây
    public bool useRainMaker = true; // Bật/tắt sử dụng Rain Maker

    [Header("Rain settings")]
    [Range(0, 1)] public float rainIntensity = 0.5f;
    public float maxIntensity = 1f;
    public Gradient rainColorOverTime;

    [Header("Wind Effect")]
    [HideInInspector] public Vector2 windDirection = Vector2.right;
    [HideInInspector] public float windStrength = 0.5f;

    [Header("=== TEST CONTROLS (EDITOR ONLY) ===")]

    [Header("--- RAIN CONTROL ---")]
    [Range(0f, 1f)] public float testRainIntensity = 0.5f;
    public bool autoUpdateRain = true;

    [Header("--- WIND CONTROL ---")]
    [Range(0f, 2f)] public float testWindStrength = 0.5f;
    private float testWindAngle = 0f; // Ẩn khỏi Inspector
    public bool autoUpdateWind = true;

    [Header("--- TIME CONTROL ---")]
    [Range(0f, 24f)] public float testHour = 12f;
    public bool autoUpdateTime = true;

    [Header("--- PRESET WEATHER SCENES (Click to test) ---")]
    public bool clearSky = false;
    public bool lightRain = false;
    public bool heavyRain = false;
    public bool storm = false;
    public bool foggyMorning = false;
    public bool sunsetRain = false;

    [Header("=== ENVIRONMENT EFFECTS ===")]
    [Range(1f, 2f)] public float cloudWindMultiplier = 1.5f;
    public WeatherEffect weatherEffect;

    [Header("Rain physics")]
    public float rainGravity = 9.8f;
    public float rainSize = 0.15f;
    public float rainSpeed = 12f;

    // singleton pattern để các script khác dễ dàng truy cập
    public static RainSystem Instance { get; private set; }

    // Trackers
    private float lastRainIntensity;
    private float lastWindStrength;
    private float lastWindAngle;
    private float lastTestHour;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        FindReferences();
        ConfigureRainparticle();

        lastRainIntensity = rainIntensity;
        lastWindStrength = testWindStrength;
        lastWindAngle = testWindAngle;
        lastTestHour = testHour;

        // Reset preset triggers
        ClearPresetTriggers();
    }

    void ClearPresetTriggers()
    {
        clearSky = false;
        lightRain = false;
        heavyRain = false;
        storm = false;
        foggyMorning = false;
        sunsetRain = false;
    }

    void FindReferences()
    {
        if (rainMaker == null)
        {
            rainMaker = GetComponentInChildren<RainScript2D>();
        }

        if (timeManager == null)
        {
            timeManager = FindObjectOfType<TimeManager>();
        }

        if (weatherEffect == null)
        {
            weatherEffect = FindObjectOfType<WeatherEffect>();
        }

        if (rainMaker == null && useRainMaker)
        {
            Debug.LogError("RainSystem: Không tìm thấy RainScript2D!");
        }
    }

    void ConfigureRainparticle()
    {
        if (!useRainMaker && rainParticle != null)
        {
            var main = rainParticle.main;
            main.gravityModifier = rainGravity;
            main.startSpeed = rainSpeed;
            main.startSize = rainSize;
            main.prewarm = true;
        }
    }

    void Update()
    {
        HandlePresetScenes();
        UpdateTestControls();

        if (useRainMaker && rainMaker != null)
        {
            UpdateRainMaker();
        }
        else if (!useRainMaker && rainParticle != null)
        {
            UpdateRainIntensity();
        }

        UpdateWindEffect();
        UpdateStarsBasedOnRain(); // Giữ logic mưa thì không có sao

        lastRainIntensity = rainIntensity;
        lastWindStrength = testWindStrength;
        lastWindAngle = testWindAngle;
        lastTestHour = testHour;
    }

    void HandlePresetScenes()
    {
        if (clearSky)
        {
            SetClearSky();
            ClearPresetTriggers();
        }
        else if (lightRain)
        {
            SetLightRain();
            ClearPresetTriggers();
        }
        else if (heavyRain)
        {
            SetHeavyRain();
            ClearPresetTriggers();
        }
        else if (storm)
        {
            SetStorm();
            ClearPresetTriggers();
        }
        else if (foggyMorning)
        {
            SetFoggyMorning();
            ClearPresetTriggers();
        }
        else if (sunsetRain)
        {
            SetSunsetRain();
            ClearPresetTriggers();
        }
    }

    void UpdateTestControls()
    {
        if (autoUpdateRain && Mathf.Abs(lastRainIntensity - testRainIntensity) > 0.01f)
        {
            rainIntensity = testRainIntensity;
        }

        if (autoUpdateWind)
        {
            if (Mathf.Abs(lastWindStrength - testWindStrength) > 0.01f)
            {
                SetWind(testWindAngle, testWindStrength);
            }
        }

        if (autoUpdateTime && timeManager != null && Mathf.Abs(lastTestHour - testHour) > 0.1f)
        {
            float t = testHour / 24f;

            System.Reflection.FieldInfo field = typeof(TimeManager).GetField("t",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(timeManager, t);
            }
        }
    }

    void UpdateRainMaker()
    {
        if (rainMaker == null) return;

        rainMaker.RainIntensity = rainIntensity;

        if (rainColorOverTime != null)
        {
            UpdateRainColor();
        }

        if (rainMaker.EnableWind && rainMaker.WindZone != null)
        {
            rainMaker.WindZone.windMain = windStrength * 20f;
            float windAngle = Mathf.Atan2(windDirection.y, windDirection.x) * Mathf.Rad2Deg;
            rainMaker.WindZone.transform.rotation = Quaternion.Euler(0, 0, windAngle);
        }

        if (rainMaker.RainFallParticleSystem != null)
        {
            var velocity = rainMaker.RainFallParticleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = windDirection.x * windStrength * 8f;
            velocity.y = -rainGravity;
        }
    }

    void UpdateRainColor()
    {
        if (rainMaker.RainFallParticleSystem != null)
        {
            var main = rainMaker.RainFallParticleSystem.main;
            main.startColor = rainColorOverTime.Evaluate(Time.time % 10f / 10f);
        }

        if (rainMaker.RainMistParticleSystem != null)
        {
            var mistMain = rainMaker.RainMistParticleSystem.main;
            mistMain.startColor = rainColorOverTime.Evaluate(Time.time % 10f / 10f);
        }

        if (rainMaker.RainExplosionParticleSystem != null)
        {
            var explosionMain = rainMaker.RainExplosionParticleSystem.main;
            explosionMain.startColor = rainColorOverTime.Evaluate(Time.time % 10f / 10f);
        }
    }

    void UpdateRainIntensity()
    {
        if (rainParticle == null) return;

        var emission = rainParticle.emission;
        emission.rateOverTime = rainIntensity * 100;

        if (rainColorOverTime != null)
        {
            var main = rainParticle.main;
            main.startColor = rainColorOverTime.Evaluate(Time.time % 10f / 10f);
        }
    }

    void UpdateWindEffect()
    {
        if (rainParticle != null)
        {
            var velocity = rainParticle.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = windDirection.x * windStrength * 5f;
            velocity.y = -rainGravity;
        }

        if (rainSplashParticle != null)
        {
            var splashMain = rainSplashParticle.main;
            splashMain.startSpeed = rainIntensity * 5f;
        }
    }

    void UpdateStarsBasedOnRain()
    {
        if (weatherEffect == null || weatherEffect.starsParticle == null) return;

        var emission = weatherEffect.starsParticle.emission;

        // Nếu có mưa, tắt sao hoàn toàn
        if (rainIntensity > 0.01f)
        {
            emission.rateOverTime = 0f;
        }
        // Nếu không mưa, KHÔNG làm gì - để WeatherEffect tự xử lý
        // Chỉ cần return, không ghi đè logic
    }

    // PRESET WEATHER SCENES
    public void SetClearSky()
    {
        Debug.Log("Setting Clear Sky...");
        testRainIntensity = 0f;
        rainIntensity = 0f;
        testWindStrength = 0f;
        testWindAngle = 0f;
        SetWind(0f, 0f);
        testHour = 12f;
    }

    public void SetLightRain()
    {
        Debug.Log("Setting Light Rain...");
        testRainIntensity = 0.3f;
        rainIntensity = 0.3f;
        testWindStrength = 0.3f;
        testWindAngle = 45f;
        SetWind(45f, 0.3f);
        testHour = 14f;
    }

    public void SetHeavyRain()
    {
        Debug.Log("Setting Heavy Rain...");
        testRainIntensity = 0.8f;
        rainIntensity = 0.8f;
        testWindStrength = 1.2f;
        testWindAngle = 60f;
        SetWind(60f, 1.2f);
        testHour = 15f;
    }

    public void SetStorm()
    {
        Debug.Log("Setting Storm...");
        testRainIntensity = 1f;
        rainIntensity = 1f;
        testWindStrength = 2f;
        testWindAngle = 80f;
        SetWind(80f, 2f);
        testHour = 16f;
    }

    public void SetFoggyMorning()
    {
        Debug.Log("Setting Foggy Morning...");
        testRainIntensity = 0.1f;
        rainIntensity = 0.1f;
        testWindStrength = 0.1f;
        testWindAngle = 0f;
        SetWind(0f, 0.1f);
        testHour = 6f;
        RenderSettings.fogDensity = 0.05f;
    }

    public void SetSunsetRain()
    {
        Debug.Log("Setting Sunset Rain...");
        testRainIntensity = 0.4f;
        rainIntensity = 0.4f;
        testWindStrength = 0.5f;
        testWindAngle = 30f;
        SetWind(30f, 0.5f);
        testHour = 18f;
    }

    // public methods de UI dieu khien
    public void SetRainIntensity(float value)
    {
        rainIntensity = Mathf.Clamp01(value);
        testRainIntensity = rainIntensity;
    }

    public void SetWind(float directionAngle, float strength)
    {
        windDirection.x = Mathf.Cos(directionAngle * Mathf.Deg2Rad);
        windDirection.y = Mathf.Sin(directionAngle * Mathf.Deg2Rad);
        windStrength = strength;

        testWindAngle = directionAngle;
        testWindStrength = strength;
    }

    public void TestHeavyRain()
    {
        SetRainIntensity(0.9f);
        SetWind(45f, 2f);
    }

    public void TestLightRain()
    {
        SetRainIntensity(0.3f);
        SetWind(0f, 0.2f);
    }

    public void TestNoRain()
    {
        SetRainIntensity(0f);
        SetWind(0f, 0f);
    }
}
