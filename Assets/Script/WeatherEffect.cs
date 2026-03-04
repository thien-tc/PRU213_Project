using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public class WeatherEffect : MonoBehaviour
{
    [Header("References to TimeManager")]
    public TimeManager timeManager;
    public Light2D globalLight;

    [Header("Cloud System")]
    public GameObject cloudPrefab;
    public int numberOfClounds = 17;
    public Transform cloudCointainer;
    public float cloudMinY = 0f;
    public float cloudMaxY = 4f;
    public float cloudMinScale = 0.5f;
    public float cloudMaxScale = 1.5f;

    [Header("Cloud Colors by Time")]
    public Color cloudDayColor = Color.white;
    public Color cloudSunsetColor = new Color(1f, 0.7f, 0.4f);
    public Color cloudNightColor = new Color(0.3f, 0.3f, 0.5f);

    [Header("Cloud Movement")]
    public float cloudSpeedDay = 0.5f;
    public float cloudSpeedSunset = 1.2f;
    public float cloudSpeedNight = 0.2f;

    [Header("Sky Background")]
    public SpriteRenderer skyBackground;
    public Gradient skyColorGradient;

    [Header("Sun effect")]
    public SpriteRenderer sunSprite;
    public Color sundayColor = Color.white;
    public Color sunSunsetColor = new Color(1f, 0.4f, 0.1f);
    public float sunSunsetScale = 3.735f;


    [Header("Moon Effects")]
    public SpriteRenderer moonSprite;
    public Gradient moonColorGradient;
    public float moonNormalScale = 3f;
    public float moonSunsetScale = 3.4f;

    [Header("Stars")]
    public ParticleSystem starsParticle;
    public float starsMaxEmission = 30f;

    [Header("Fog")]
    public bool enableFog = true;
    public Gradient fogColorGradient;

    // private variable
    public GameObject[] clouds;
    public SpriteRenderer[] cloudRenderers;
    private float currentTimeOfDay;
    private float currentHour;


    // Start is called before the first frame update
    void Start()
    {
        // tự động tìm timerManager nếu chưa gán
        if(timeManager == null)
        {
            timeManager = FindObjectOfType<TimeManager>();

        }
        if(globalLight == null)
        {
            globalLight = FindObjectOfType<Light2D>();
        }
        // tạo mây
        CreateClouds();

        // khởi tạo stars
        if(starsParticle != null)
        {
            var emission = starsParticle.emission;
            emission.rateOverTime = 0f;
        }

    }

    // Update is called once per frame
    void Update()
    {
        // tính thời gian trong ngày (dựa vào timeManager)
        CaculateTimeFromSun();

        // cập nhật các hiêu ứng
        UpdateClouds();
        UpdateSkyColor();
        UpdateSunEffects();
        UpdateMoonEffect();
        UpdateStars();
        UpdateFog();

    }

    void CaculateTimeFromSun()
    {
        if (timeManager == null || timeManager.sun == null) return;

        // tính giờ dựa vào vị trí mặt trời

        float sunY = timeManager.sun.position.y;
        float sunX = timeManager.sun.position.x;

        // chuyển đôi rhanhf giờ(0-24)
        // mặt trời ở dưới là đêm, ở trên là ngày
        if(sunY > 0)
        {
            // ban ngày là 6h - 18h
            float normalizedX = (sunX + timeManager.radiusX) / (2 * timeManager.radiusX); // -1..1 -> 0..1
            currentHour = 6f + normalizedX * 12f; // 6h -> 18h
        }
        else
        {
            // ban đêm 
            float normalizedX = (sunX + timeManager.radiusX) / (2 * timeManager.radiusX);
            currentHour = 18f + normalizedX * 12f;
            if (currentHour >= 24f) currentHour -= 24f;
        }
        currentTimeOfDay = currentHour / 24f;
    }

    void CreateClouds()
    {
        if (cloudPrefab == null) return;
        clouds = new GameObject[numberOfClounds];
        cloudRenderers = new SpriteRenderer[numberOfClounds];

        for(int i = 0; i< numberOfClounds; i++)
        {
            // tao cloud mới
            GameObject cloud = Instantiate(cloudPrefab, cloudCointainer);

            // vij tri ngau nhieen 
            float x = Random.Range(-15f, 15f);
            float y = Random.Range(cloudMinY, cloudMaxY);
            cloud.transform.position = new Vector3(x, y, 5f);

            // scale ngau nhien
            float scale = Random.Range(cloudMinScale, cloudMaxScale);
            cloud.transform.localScale = new Vector3(scale, scale, 1f);

            // flip ngau nhien
            if(Random.value > 0.5f)
            {
                Vector3 localScale = cloud.transform.localScale;
                localScale.x *= -1;
                cloud.transform.localScale = localScale;

            }
            // luu lai
            clouds[i] = cloud;
            cloudRenderers[i] = cloud.GetComponent<SpriteRenderer>();
        }

    }

    void UpdateClouds()
    {
        if (clouds == null) return;

        // === FIX: KIỂM TRA VÀ CHUẨN HÓA TẤT CẢ SPEED ===
        cloudSpeedDay = NormalizeSpeed(cloudSpeedDay, 0.5f);
        cloudSpeedNight = NormalizeSpeed(cloudSpeedNight, 0.2f);
        cloudSpeedSunset = NormalizeSpeed(cloudSpeedSunset, 1.2f);

        // xac dinh toc do vaf mau sac theo thoi gian
        float speed = cloudSpeedDay;
        Color targetColor = cloudDayColor;

        // hoang hon (5-7h s-t)
        if ((currentHour > 5 && currentHour < 7) || (currentHour > 17 && currentHour < 19))
        {
            speed = cloudSpeedSunset;
            targetColor = cloudSunsetColor;

            // thay doi mau theo tien trinh hoang hon
            float sunsetProgress = 0f;
            if (currentHour > 5 && currentHour < 7)
            {
                sunsetProgress = (currentHour - 5f) / 2f; // 0->1
            }
            else if (currentHour > 17 && currentHour < 19)
            {
                sunsetProgress = (currentHour - 17f) / 2f; // 0->1
            }

            // FIX: Đảm bảo sunsetProgress hợp lệ
            sunsetProgress = Mathf.Clamp01(sunsetProgress);
            targetColor = Color.Lerp(cloudDayColor, cloudSunsetColor, sunsetProgress);
        }
        // ban dem
        else if (currentHour < 6 || currentHour > 18)
        {
            speed = cloudSpeedNight;
            targetColor = cloudNightColor;
        }

        // === FIX: KIỂM TRA speed TRƯỚC KHI DÙNG ===
        speed = NormalizeSpeed(speed, 0.5f);

        // === FIX: GIỚI HẠN speed TRONG KHOẢN AN TOÀN ===
        speed = Mathf.Clamp(speed, -3f, 3f); // Giới hạn tốc độ tối đa

        // di chuyển vaf doi mau may
        for (int i = 0; i < clouds.Length; i++)
        {
            if (clouds[i] == null) continue;

            // di chuyển - TÍNH TOÁN AN TOÀN
            Vector3 pos = clouds[i].transform.position;

            // FIX: Kiểm tra pos hiện tại có hợp lệ không
            if (IsInvalidPosition(pos))
            {
                Debug.LogWarning($"Cloud {i} có vị trí không hợp lệ: {pos}, reset về vị trí mới");
                pos = new Vector3(
                    Random.Range(-15f, 15f),
                    Random.Range(cloudMinY, cloudMaxY),
                    5f
                );
            }

            // Tính toán vị trí mới an toàn
            float deltaMove = speed * Time.deltaTime;

            // FIX: Kiểm tra deltaMove
            if (float.IsNaN(deltaMove) || float.IsInfinity(deltaMove))
            {
                Debug.LogWarning($"deltaMove không hợp lệ: {deltaMove}");
                deltaMove = 0f;
            }

            pos.x += deltaMove;

            // FIX: Kiểm tra pos.x sau khi cộng
            if (float.IsNaN(pos.x) || float.IsInfinity(pos.x))
            {
                Debug.LogError($"pos.x bị lỗi sau khi cộng: {pos.x}");
                pos.x = Random.Range(-15f, 15f);
            }

            // reset khi ra khoi man hinh - VỚI GIỚI HẠN AN TOÀN
            if (pos.x > 20f)
            {
                pos.x = -20f;
                pos.y = Random.Range(cloudMinY, cloudMaxY);
            }
            else if (pos.x < -25f)
            {
                pos.x = 20f;
                pos.y = Random.Range(cloudMinY, cloudMaxY);
            }

            // FIX: Giới hạn Y trong khoảng an toàn
            pos.y = Mathf.Clamp(pos.y, cloudMinY - 2f, cloudMaxY + 2f);

            // FIX: Đảm bảo Z không đổi
            pos.z = 5f;

            // GÁN VỊ TRÍ AN TOÀN
            try
            {
                clouds[i].transform.position = pos;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Không thể gán vị trí cho cloud {i}: {e.Message}");
                // Nếu lỗi, tạo cloud mới ở vị trí an toàn
                clouds[i].transform.position = new Vector3(
                    Random.Range(-10f, 10f),
                    Random.Range(cloudMinY, cloudMaxY),
                    5f
                );
            }

            // doi mau
            if (cloudRenderers[i] != null)
            {
                // FIX: Đảm bảo targetColor hợp lệ
                Color safeTargetColor = targetColor;
                if (IsInvalidColor(safeTargetColor))
                {
                    safeTargetColor = Color.white;
                }

                cloudRenderers[i].color = Color.Lerp(cloudRenderers[i].color, safeTargetColor, Time.deltaTime);
            }
        }
    }

    // === HELPER METHODS ===

    private float NormalizeSpeed(float speed, float defaultValue)
    {
        // Kiểm tra NaN, Infinity
        if (float.IsNaN(speed) || float.IsInfinity(speed))
        {
            Debug.LogWarning($"Speed không hợp lệ: {speed}, dùng giá trị mặc định {defaultValue}");
            return defaultValue;
        }

        // Giới hạn trong khoảng hợp lý (tránh quá lớn)
        return Mathf.Clamp(speed, -10f, 10f);
    }

    private bool IsInvalidPosition(Vector3 pos)
    {
        return float.IsNaN(pos.x) || float.IsInfinity(pos.x) ||
               float.IsNaN(pos.y) || float.IsInfinity(pos.y) ||
               float.IsNaN(pos.z) || float.IsInfinity(pos.z);
    }

    private bool IsInvalidColor(Color color)
    {
        return float.IsNaN(color.r) || float.IsInfinity(color.r) ||
               float.IsNaN(color.g) || float.IsInfinity(color.g) ||
               float.IsNaN(color.b) || float.IsInfinity(color.b) ||
               float.IsNaN(color.a) || float.IsInfinity(color.a);
    }

    void UpdateSkyColor()
    {
        if (skyBackground == null || skyColorGradient == null) return;

        // mau bau troi
        Color skyColor = skyColorGradient.Evaluate(currentTimeOfDay);
        skyBackground.color = skyColor;

        // neu laf hoang hon, tang cuong mau do
        if((currentHour > 5 && currentHour < 7) || (currentHour > 17 && currentHour > 19))
        {
            float factor = 0f;
            if(currentHour > 5 && currentHour < 7)
            {
                factor = Mathf.Abs(currentHour - 6f) / 1f;
            }
            else if (currentHour > 17 && currentHour < 19)
            {
                factor = Mathf.Abs(currentHour - 18f) / 1f;
            }

            skyColor = Color.Lerp(skyColor, new Color(1f, 9.5f, 0.2f), factor * 0.5f);
            skyBackground.color = skyColor;
        }
           
    }

    void UpdateSunEffects()
    {
        if (sunSprite == null || timeManager == null || timeManager.sun == null) return;

        sunSprite.transform.position = timeManager.sun.position;

        float sunriseStart = 5f;
        float sunriseEnd = 7f;
        float sunsetStart = 17f;
        float sunsetEnd = 19f;

        Color targetColor = sundayColor;
        float targetScale = 3f;

        // Sunrise
        if (currentHour >= sunriseStart && currentHour <= sunriseEnd)
        {
            float t = Mathf.InverseLerp(sunriseStart, sunriseEnd, currentHour);
            targetColor = Color.Lerp(sunSunsetColor, sundayColor, t);
            targetScale = Mathf.Lerp(sunSunsetScale, 3f, t);
        }
        // Sunset
        else if (currentHour >= sunsetStart && currentHour <= sunsetEnd)
        {
            float t = Mathf.InverseLerp(sunsetStart, sunsetEnd, currentHour);
            targetColor = Color.Lerp(sundayColor, sunSunsetColor, t);
            targetScale = Mathf.Lerp(3f, sunSunsetScale, t);
        }

        // Lerp mượt theo frame (anti giật)
        sunSprite.color = Color.Lerp(sunSprite.color, targetColor, Time.deltaTime * 3f);
        sunSprite.transform.localScale = Vector3.Lerp(
            sunSprite.transform.localScale,
            Vector3.one * targetScale,
            Time.deltaTime * 3f
        );
    }

    void UpdateMoonEffect()
    {
        if (moonSprite == null || timeManager == null || timeManager.moon == null) return;

        moonSprite.transform.position = timeManager.moon.position;

        // đổi màu theo gradient
        if (moonColorGradient != null)
        {
            moonSprite.color = moonColorGradient.Evaluate(currentTimeOfDay);
        }

        float sunsetStart = 17f;
        float sunsetEnd = 19f;
        float sunriseStart = 5f;
        float sunriseEnd = 7f;

        float targetScale = moonNormalScale;

        // Gần đêm (hoàng hôn)
        if (currentHour >= sunsetStart && currentHour <= sunsetEnd)
        {
            float t = Mathf.InverseLerp(sunsetStart, sunsetEnd, currentHour);
            targetScale = Mathf.Lerp(moonNormalScale, moonSunsetScale, t);
        }
        // Gần sáng (trăng lặn)
        else if (currentHour >= sunriseStart && currentHour <= sunriseEnd)
        {
            float t = Mathf.InverseLerp(sunriseStart, sunriseEnd, currentHour);
            targetScale = Mathf.Lerp(moonSunsetScale, moonNormalScale, t);
        }

        // scale mượt
        moonSprite.transform.localScale = Vector3.Lerp(
            moonSprite.transform.localScale,
            Vector3.one * targetScale,
            Time.deltaTime * 3f
        );
    }

    void UpdateStars()
    {
        if(starsParticle == null) return;
        var emission = starsParticle.emission;
        

        // sao chi hien khi troi toi 
        if(currentHour < 5 || currentHour > 19)
        {
            // dem khuya - nhieu sao
            emission.rateOverTime = starsMaxEmission;
        }
        else if( currentHour < 6 || currentHour > 18)
        {
            // gaanf sang.toi - it sao
            emission.rateOverTime = starsMaxEmission * 0.3f;
        }
        else
        {
            emission.rateOverTime = 0f;

        }
    }

    void UpdateFog()
    {
        if (!enableFog || fogColorGradient == null) return;

        RenderSettings.fogColor = fogColorGradient.Evaluate(currentTimeOfDay);

        // suong mu day hon luc hoang hon
        float density = 0.01f;
        if ((currentHour > 5 && currentHour < 7) || (currentHour > 17 && currentHour < 19))
        {
            density = 0.03f;
        }
        RenderSettings.fogDensity = density;
    }

}
