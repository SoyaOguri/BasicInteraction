using UnityEngine;
using UnityEngine.UI;

// 実験2-b：自転車と車の衝突を判定して記録する（正面のビルドだけで動く）
//
//  ・車の見た目（Renderer）の外形を、自転車の大きさのぶんだけ広げた箱に、
//    自転車の位置が入ったら「衝突」とする（高さは見ない）
//  ・1 試行で一度でも衝突したら、その試行は collision = 1（Exp2bTrialManager が試行ごとに ResetTrial で戻す）
//  ・衝突した瞬間に、画面の中央に「衝突！」を出し、音（任意）を鳴らす
//  ・あわせて、車と自転車の最も近づいた距離（すき間）を記録する（ニアミスの指標）
//
//  ※ シーンに置かなくても、Exp2bTrialManager が自分の GameObject に自動で付ける
public class Exp2bCollisionMonitor : MonoBehaviour
{
    [Header("対象（空なら Exp2bTrialManager が入れる）")]
    public Transform bicycle;
    public CarControllManagement[] cars;

    [Header("自転車の大きさ（半分の長さ, m）")]
    [Tooltip("自転車の進行方向（ワールド x）の半分の長さ。全長 1.8 m なら 0.9")]
    public float bikeHalfLength = 0.9f;
    [Tooltip("自転車の幅方向（ワールド z）の半分の幅。ハンドル幅 0.6 m なら 0.3")]
    public float bikeHalfWidth = 0.3f;

    [Header("画面の表示と音")]
    [Tooltip("空なら、GoalLine のタイマーの文字をコピーして画面中央に作る")]
    public Text collisionText;
    public string collisionMessage = "衝突！";
    public AudioClip collisionClip;
    public float collisionVolume = 1.0f;

    [Header("確認用（読み取り専用）")]
    [SerializeField] bool collided;
    [SerializeField] float collisionTime = -1f;
    [SerializeField] string collidedCar = "";
    [SerializeField] float currentGap = float.NaN;
    [SerializeField] float minGap = float.NaN;

    public bool Collided { get { return collided; } }
    // 試行の始まり（ResetTrial）から衝突までの秒数。衝突していなければ -1
    public float CollisionTime { get { return collisionTime; } }
    public string CollidedCar { get { return collidedCar; } }
    // 今のフレームの、地上にいる車と自転車のすき間 [m]（0 = 接触）。車が地下なら NaN
    public float CurrentGap { get { return currentGap; } }
    // この試行で最も近づいたときのすき間 [m]。車が一度も地上に出ていなければ NaN
    public float MinGap { get { return minGap; } }

    float trialStart;
    AudioSource audioSource;
    Renderer[][] carRenderers;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void Init(Transform bike, CarControllManagement[] carList, GoalLine goalLine)
    {
        if (bicycle == null) bicycle = bike;
        if (cars == null || cars.Length == 0) cars = carList;
        CacheRenderers();
        if (collisionText == null && goalLine != null && goalLine.timerText != null)
            collisionText = MakeCenterText(goalLine.timerText);
        if (collisionText != null) collisionText.gameObject.SetActive(false);
    }

    void CacheRenderers()
    {
        if (cars == null) { carRenderers = new Renderer[0][]; return; }
        carRenderers = new Renderer[cars.Length][];
        for (int i = 0; i < cars.Length; i++)
            carRenderers[i] = cars[i] != null ? cars[i].GetComponentsInChildren<Renderer>(true) : new Renderer[0];
    }

    // 試行の始まりに呼ぶ
    public void ResetTrial()
    {
        collided = false;
        collisionTime = -1f;
        collidedCar = "";
        currentGap = float.NaN;
        minGap = float.NaN;
        trialStart = Time.time;
        if (collisionText != null) collisionText.gameObject.SetActive(false);
    }

    void FixedUpdate()
    {
        if (bicycle == null || cars == null) return;
        if (carRenderers == null || carRenderers.Length != cars.Length) CacheRenderers();

        Vector3 p = bicycle.position;
        float gapNow = float.NaN;
        for (int i = 0; i < cars.Length; i++)
        {
            var car = cars[i];
            if (car == null || !car.gameObject.activeInHierarchy) continue;
            Bounds b;
            if (!CarBounds(i, out b)) continue;
            if (b.max.y < -50f) continue;   // 地下で待っている車は見ない

            // x・z の平面で、車の箱（自転車の大きさだけ広げる）と自転車の位置のすき間
            float dx = Mathf.Max(0f, Mathf.Abs(p.x - b.center.x) - (b.extents.x + bikeHalfLength));
            float dz = Mathf.Max(0f, Mathf.Abs(p.z - b.center.z) - (b.extents.z + bikeHalfWidth));
            float gap = Mathf.Sqrt(dx * dx + dz * dz);
            if (float.IsNaN(gapNow) || gap < gapNow) gapNow = gap;

            if (gap <= 0f && !collided) OnCollision(car);
        }
        currentGap = gapNow;
        if (!float.IsNaN(gapNow) && (float.IsNaN(minGap) || gapNow < minGap)) minGap = gapNow;
    }

    bool CarBounds(int i, out Bounds b)
    {
        b = new Bounds();
        bool any = false;
        foreach (var r in carRenderers[i])
        {
            if (r == null || !r.enabled || r is ParticleSystemRenderer) continue;
            if (!any) { b = r.bounds; any = true; }
            else b.Encapsulate(r.bounds);
        }
        return any;
    }

    void OnCollision(CarControllManagement car)
    {
        collided = true;
        collisionTime = Time.time - trialStart;
        collidedCar = car.name;
        Debug.Log(string.Format("[Exp2b] 衝突：{0}（試行開始から {1:F2} s）", car.name, collisionTime));
        if (collisionText != null)
        {
            collisionText.text = collisionMessage;
            collisionText.gameObject.SetActive(true);
        }
        if (collisionClip != null)
        {
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.PlayOneShot(collisionClip, collisionVolume);
        }
    }

    // タイマーの文字をコピーして、画面中央に大きな赤い文字を作る
    static Text MakeCenterText(Text src)
    {
        var go = Instantiate(src.gameObject, src.transform.parent);
        go.name = "Exp2bCollisionText";
        foreach (var mb in go.GetComponents<MonoBehaviour>())
            if (!(mb is Text) && !(mb is Shadow)) Destroy(mb);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1600f, 400f);
        var t = go.GetComponent<Text>();
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.fontSize = Mathf.Max(t.fontSize * 3, 120);
        t.color = Color.red;
        t.text = "";
        return t;
    }
}
