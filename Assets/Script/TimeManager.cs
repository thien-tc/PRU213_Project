using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TimeManager : MonoBehaviour
{
    [Header("References")]
    public Light2D globalLight;   // ánh sáng chung
    public Transform sun;
    public Transform moon;
    public Light2D moonLight;     // ánh trăng (Point/Spot/Global 2D)

    [Header("Day/Night")]
    public Gradient lightColor;
    [Range(5, 240)] public float dayDuration = 30f;

    [Header("Global Intensity")]
    public float minIntensity = 0.08f;  // đêm tối hẳn
    public float maxIntensity = 1.2f;   // ngày sáng

    [Header("Orbit")]
    public Vector2 center = new Vector2(0f, 0f);
    public float radiusX = 12f;
    public float radiusY = 6f;
    public float angleOffset = -90f;   // mọc bên trái

    [Header("Moon Light")]
    public float moonLightMin = 0.0f;  // ban ngày
    public float moonLightMax = 0.45f; // ban đêm
    public bool moonLightFollowsMoon = true;

    private float t;

    void Start()
    {
        if (globalLight == null) globalLight = FindObjectOfType<Light2D>();
    }

    void Update()
    {
        if (globalLight == null || lightColor == null) return;

        // time 0..1
        t += Time.deltaTime / dayDuration;
        if (t >= 1f) t = 0f;

        // Global light color
        globalLight.color = lightColor.Evaluate(t);

        // Global intensity (sáng giữa ngày, tối về đêm)
        float dayWave = Mathf.Sin(t * Mathf.PI); // 0->1->0
        globalLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, dayWave);

        // Orbit angles
        float aSun = (t * 360f + angleOffset) * Mathf.Deg2Rad;
        float aMoon = aSun + Mathf.PI;

        // Sun pos + show
        if (sun != null)
        {
            sun.position = new Vector3(
                center.x + Mathf.Cos(aSun) * radiusX,
                center.y + Mathf.Sin(aSun) * radiusY,
                sun.position.z
            );
            sun.gameObject.SetActive(Mathf.Sin(aSun) > 0f);
        }

        // Moon pos + show
        bool moonUp = false;
        if (moon != null)
        {
            moon.position = new Vector3(
                center.x + Mathf.Cos(aMoon) * radiusX,
                center.y + Mathf.Sin(aMoon) * radiusY,
                moon.position.z
            );
            moonUp = Mathf.Sin(aMoon) > 0f;
            moon.gameObject.SetActive(moonUp);
        }
        else
        {
            // nếu không có object moon thì vẫn tính "đêm" theo aMoon
            moonUp = Mathf.Sin(aMoon) > 0f;
        }

        // MoonLight: chỉ sáng khi đêm
        if (moonLight != null)
        {
            // tăng/giảm mượt (không bật tắt cái bụp)
            float target = moonUp ? moonLightMax : moonLightMin;
            moonLight.intensity = Mathf.MoveTowards(moonLight.intensity, target, Time.deltaTime * 1.5f);

            if (moonLightFollowsMoon && moon != null)
                moonLight.transform.position = new Vector3(moon.position.x, moon.position.y, moonLight.transform.position.z);

            // nếu muốn tắt hẳn gameObject khi ngày:
            // moonLight.gameObject.SetActive(moonUp);
        }
    }
}