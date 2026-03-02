using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TimeManager : MonoBehaviour
{
    [Header("References")]
    public Light2D globalLight;
    public Transform sun;

    [Header("Day/Night")]
    public Gradient lightColor;
    [Range(5, 240)] public float dayDuration = 30f;

    [Header("Light Intensity")]
    public float minIntensity = 0.2f;
    public float maxIntensity = 1.2f;

    [Header("Sun Movement")]
    public Vector2 sunCenter = new Vector2(0f, 0f);
    public float sunRadiusX = 7f;
    public float sunRadiusY = 4f;
    public float startAngle = -20f;
    public float endAngle = 200f;

    private float t;

    void Update()
    {
        if (globalLight == null || lightColor == null) return;

        t += Time.deltaTime / dayDuration;
        if (t >= 1f) t = 0f;

        // đổi màu ánh sáng
        globalLight.color = lightColor.Evaluate(t);

        // đổi độ sáng
        float k = Mathf.Sin(t * Mathf.PI);
        globalLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, k);

        // di chuyển mặt trời
        if (sun != null)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
            float x = sunCenter.x + Mathf.Cos(angle) * sunRadiusX;
            float y = sunCenter.y + Mathf.Sin(angle) * sunRadiusY;
            sun.position = new Vector3(x, y, sun.position.z);
        }
    }
}