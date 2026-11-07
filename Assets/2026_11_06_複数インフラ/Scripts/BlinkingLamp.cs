using UnityEngine;

// 赤点滅信号などの灯器を点滅させる（再生中のみ。エディタでは点灯したまま表示）
public class BlinkingLamp : MonoBehaviour
{
    public Renderer lamp;
    public Material onMaterial;
    public Material offMaterial;
    [Tooltip("点滅の周期 [s]（1秒に1回）")]
    public float period = 1.0f;
    [Tooltip("1周期のうち点灯している割合")]
    [Range(0f, 1f)] public float duty = 0.5f;

    float startTime;
    bool lastOn = true;

    void OnEnable()
    {
        startTime = Time.time;
        SetOn(true);
    }

    void Update()
    {
        if (lamp == null || period <= 0f) return;
        float phase = Mathf.Repeat(Time.time - startTime, period) / period;
        bool on = phase < duty;
        if (on != lastOn) SetOn(on);
    }

    void SetOn(bool on)
    {
        lastOn = on;
        if (lamp == null) return;
        Material m = on ? onMaterial : offMaterial;
        if (m != null) lamp.sharedMaterial = m;
    }
}
