using UnityEngine;

// 車両接近LED表示板：交差道路の車が交差点（自転車の進路との交点）に
// threshold 秒以内に着くとき点灯する。消灯＝「車が来ていない」。
// 車の位置は IntersectionLayout のローカル座標で見る（ローカル x = 0 が自転車の進路）。
public class ApproachWarningBoard : MonoBehaviour
{
    public IntersectionLayout layout;
    [Tooltip("空なら layout の Left Car / Right Car を使う")]
    public Transform[] cars;
    public Renderer panel;
    public Material onMaterial;
    public Material offMaterial;
    [Tooltip("交差点までの到着時間がこの秒数以内なら点灯")]
    public float threshold = 5f;
    [Tooltip("これより遅い車は止まっているとみなす [m/s]")]
    public float minSpeed = 0.5f;
    [Tooltip("車がこれ以上上下に離れていたら（地下に退避中）無視する [m]")]
    public float maxHeightGap = 10f;

    [Header("確認用（読み取り専用）")]
    [SerializeField] bool lit;
    [SerializeField] float nearestEta = float.PositiveInfinity;

    Vector3[] lastLocal;
    bool[] hasLast;

    void OnEnable()
    {
        lastLocal = null;
        SetLit(false);
    }

    Transform[] Targets()
    {
        if (cars != null && cars.Length > 0) return cars;
        if (layout == null) return new Transform[0];
        return new[] { layout.leftCar, layout.rightCar };
    }

    void Update()
    {
        if (layout == null) return;
        Transform[] t = Targets();
        if (lastLocal == null || lastLocal.Length != t.Length)
        {
            lastLocal = new Vector3[t.Length];
            hasLast = new bool[t.Length];
        }

        float best = float.PositiveInfinity;
        float dt = Time.deltaTime;
        for (int i = 0; i < t.Length; i++)
        {
            if (t[i] == null) continue;
            Vector3 p = layout.transform.InverseTransformPoint(t[i].position);
            if (Mathf.Abs(p.y) > maxHeightGap)
            {
                hasLast[i] = false;
                continue;
            }
            if (hasLast[i] && dt > 0f)
            {
                float vx = (p.x - lastLocal[i].x) / dt;     // 交差道路に沿った速度
                bool approaching = p.x * vx < 0f;           // x = 0 に向かっているか
                if (approaching && Mathf.Abs(vx) > minSpeed)
                {
                    float eta = Mathf.Abs(p.x) / Mathf.Abs(vx);
                    if (eta < best) best = eta;
                }
            }
            lastLocal[i] = p;
            hasLast[i] = true;
        }

        nearestEta = best;
        bool on = best <= threshold;
        if (on != lit) SetLit(on);
    }

    void SetLit(bool on)
    {
        lit = on;
        if (panel == null) return;
        Material m = on ? onMaterial : offMaterial;
        if (m != null) panel.sharedMaterial = m;
    }
}
