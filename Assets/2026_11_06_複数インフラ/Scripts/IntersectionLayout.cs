using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// 実験2-b：交差点のインフラを TrialCondition ひとつで切り替える
//
// ■ ローカル座標の約束（このオブジェクトの Transform で決まる）
//   原点 = 自転車の進路と、交差道路の中心線（2本の白線の中央）の交点
//   +Z   = 自転車の進行方向、+X = 自転車から見て右
//   → 今のシーンなら Position (1349.6, 0, 2.5)、Rotation Y = 90
//     （交差道路の白線 x = 1347.1 / 1352.1 の中央。
//       自転車は z = 2.5 = 左の建物線 z = 4.5 から 2 m を走る）
//
// ■ 見通し距離 D の定義（スライド「見通し距離の定義」）
//   基準点 P から塀の角 C を通る視線が、車の走行線と交わる点までの、
//   「自転車の進路と走行線の交点」からの距離。
//   塀は L 字で、角 C は (塀の線 x, 交差道路の端から setback 手前)。
public class IntersectionLayout : MonoBehaviour
{
    [Header("インフラ部品（子オブジェクトを割り当てる）")]
    public GameObject stopLine;
    public GameObject stopSign;
    public GameObject stopMark;      // 「止まれ」路面表示
    public GameObject redFlash;      // 赤点滅信号（ランダム条件のみ）
    public GameObject colorPavement;
    public GameObject cautionSign;   // 「注意 この先交差点あり」看板（ランダム条件のみ）
    public GameObject mirror;
    public GameObject ledBoard;      // 車両接近LED表示板（ランダム条件のみ）

    [Header("交差点の路面（規範ありのとき「止まれ」入りのマテリアルに替える）")]
    [Tooltip("towndata > 交差点 > Quad3 の MeshRenderer")]
    public Renderer intersectionSurface;
    [Tooltip("規範なしのときの路面（白線だけ）")]
    public Material surfaceDefault;
    [Tooltip("規範あり（停止線＋標識＋路面表示）のときの路面（停止線と「止まれ」入り）")]
    public Material surfaceStop;
    [Tooltip("カラー舗装あり・規範なしの路面（停止線の内側をベンガラ色に塗ったもの）。空ならカラー舗装は Color Pavement のオブジェクトで表す")]
    public Material surfaceDefaultColor;
    [Tooltip("カラー舗装あり・規範ありの路面")]
    public Material surfaceStopColor;

    bool ColorOnSurface
    {
        get { return intersectionSurface != null && surfaceDefaultColor != null && surfaceStopColor != null; }
    }

    [Header("塀（L字：道路沿いの辺 Along ＋ 交差道路と平行な辺 Across）")]
    public Transform sightRoot;      // 空なら子の "Sight" を探す
    public Transform leftWallAlong;
    public Transform leftWallAcross;
    public Transform rightWallAlong;
    public Transform rightWallAcross;
    public float wallHeight = 2.2f;
    public float wallThickness = 0.15f;
    public float wallAlongLength = 40f;   // 角から手前側へ伸ばす長さ
    public float wallAcrossLength = 40f;  // 角から外側（交差道路と平行）へ伸ばす長さ
    [Tooltip("ブロック塀テクスチャ 1 枚が表す大きさ [m]（BlockWall.png は 1.6 m 四方）")]
    public float wallTextureSize = 1.6f;

    [Header("塀の角と交差道路のあいだの空き地（砂利の板）")]
    public Transform leftLot;
    public Transform rightLot;
    [Tooltip("砂利テクスチャ 1 枚が表す大きさ [m]")]
    public float lotTextureSize = 2f;
    [Tooltip("空き地の板の高さ（地面に埋もれる場合は少し上げる）")]
    public float lotHeight = 0.02f;

    [Header("幾何（ローカル座標, m）— 既定値は今のシーンの実測値")]
    [Tooltip("交差道路の中心線（白線の中央）から、手前側の建物線までの距離。塀の角の後退距離はこの線から測る")]
    public float crossRoadNearEdge = 6.1f;
    [Tooltip("交差道路の中心線（白線の中央）から、向こう側の建物線までの距離（Gizmo の表示用）")]
    public float crossRoadFarEdge = 4.3f;
    [Tooltip("自転車の進路から左の塀（建物線）までの距離（左の建物線 z=4.5 から 2 m を走る）")]
    public float leftWallOffset = 2f;
    [Tooltip("自転車の進路から右の塀（建物線）までの距離（z=2.5 から右の建物線 z=-3.9 まで）")]
    public float rightWallOffset = 6.4f;
    [Tooltip("基準点：交差道路の手前側の建物線から、さらに手前へ何 m か（スライドの「手前10m」は幅6mの道路の中心からなので、端からは7m）")]
    public float referenceBeforeNearEdge = 7f;
    [Tooltip("左から来る車の走行線の z（遠い車線なので +）")]
    public float leftCarLaneZ = 1.4f;
    [Tooltip("右から来る車の走行線の z（近い車線なので −）")]
    public float rightCarLaneZ = -1.1f;

    [Header("白線（インフラの仮部品を置くときに使う, ローカル座標）")]
    [Tooltip("自転車側の道路の左の白線の x（ワールド z = 2.9）")]
    public float bikeRoadLeftLineX = -0.4f;
    [Tooltip("自転車側の道路の右の白線の x（ワールド z = -2.3）")]
    public float bikeRoadRightLineX = 4.8f;
    [Tooltip("交差道路の手前側の白線の z（ワールド x = 1347.1）")]
    public float crossRoadNearLineZ = -2.5f;
    [Tooltip("交差道路の向こう側の白線の z（ワールド x = 1352.1）")]
    public float crossRoadFarLineZ = 2.5f;

    [Header("車（走行線の読み込みと、LED表示板の点灯判定に使う）")]
    public Transform leftCar;
    public Transform rightCar;

    [Header("エディタでのプレビュー")]
    public bool livePreview = true;
    [Tooltip("F1〜F10 を選ぶと下の preview に要因条件が入る（0 = 使わない）")]
    [Range(0, 10)] public int factorialPreset = 0;
    [Tooltip("ランダム条件の CSV（Conditions/random_conditions.csv）")]
    public TextAsset randomConditionsCsv;
    [Tooltip("1〜48 を選ぶとランダム条件 R01〜R48 を preview に入れる（Factorial Preset が 0 のときだけ）")]
    [Range(0, 48)] public int randomPreset = 0;
    public TrialCondition preview = new TrialCondition();
    [Tooltip("再生中に F1〜F10 キーで要因条件を切り替える（テスト用）")]
    public bool debugKeys = true;

    [Header("結果（読み取り専用）")]
    [SerializeField] float leftSetback;
    [SerializeField] float rightSetback;
    [SerializeField] float actualSightLeft;
    [SerializeField] float actualSightRight;
    [SerializeField] string warning = "";

    public TrialCondition Current { get; private set; }

    // 交差道路の手前側の建物線の z（ローカル）
    public float NearEdgeZ { get { return -crossRoadNearEdge; } }
    // 基準点の z（ローカル、自転車の進路上 x = 0）
    public float RefZ { get { return NearEdgeZ - referenceBeforeNearEdge; } }

    // ---------------- 公開 API ----------------

    public void Apply(TrialCondition c)
    {
        if (c == null) return;
        Current = c.Clone();
        warning = "";

        bool stop = c.norm == NormType.StopSet;
        SetActive(stopLine, stop);
        SetActive(stopSign, stop);
        SetActive(stopMark, stop);
        SetActive(redFlash, c.norm == NormType.RedFlash);
        bool color = c.attention == AttentionType.ColorPavement;
        if (intersectionSurface != null && surfaceDefault != null && surfaceStop != null)
        {
            Material m;
            if (color && ColorOnSurface) m = stop ? surfaceStopColor : surfaceDefaultColor;
            else m = stop ? surfaceStop : surfaceDefault;
            if (intersectionSurface.sharedMaterial != m) intersectionSurface.sharedMaterial = m;
        }

        // 路面のマテリアルでカラー舗装を表すときは、上に重ねる板（Color Pavement）は使わない
        SetActive(colorPavement, color && !ColorOnSurface);
        SetActive(cautionSign, c.attention == AttentionType.CautionSign);

        SetActive(mirror, c.info == InfoType.Mirror);
        SetActive(ledBoard, c.info == InfoType.LedBoard);

        leftSetback = SetbackForSight(leftWallOffset, leftCarLaneZ, c.sightLeft, "左");
        rightSetback = SetbackForSight(rightWallOffset, rightCarLaneZ, c.sightRight, "右");
        PlaceWall(leftWallAlong, leftWallAcross, -leftWallOffset, -1f, leftSetback);
        PlaceWall(rightWallAlong, rightWallAcross, rightWallOffset, +1f, rightSetback);

        actualSightLeft = SightForSetback(leftWallOffset, leftCarLaneZ, leftSetback);
        actualSightRight = SightForSetback(rightWallOffset, rightCarLaneZ, rightSetback);

        if (warning.Length > 0) Debug.LogWarning("[IntersectionLayout] " + warning, this);

#if UNITY_EDITOR
        if (!Application.isPlaying && gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    // 見通し D → 塀の角の後退距離（交差道路の端から手前へ何 m）
    public float SetbackForSight(float wallOffset, float laneZ, float sight, string label)
    {
        float laneDist = laneZ - RefZ;           // 基準点から走行線までの前後距離
        float edgeDist = NearEdgeZ - RefZ;       // 基準点から交差道路の端までの前後距離
        if (sight <= 0.01f)
        {
            warning += label + ": 見通しが 0 以下です。 ";
            return 0f;
        }
        float cornerFromRef = wallOffset * laneDist / sight;
        float setback = edgeDist - cornerFromRef;
        if (setback < 0f)
        {
            float minSight = wallOffset * laneDist / edgeDist;
            warning += string.Format("{0}: 見通し {1:F1} m は塀を交差道路の端まで出しても作れません（最小 {2:F2} m）。 ", label, sight, minSight);
            setback = 0f;
        }
        if (setback > edgeDist - 0.1f)
        {
            warning += string.Format("{0}: 見通し {1:F1} m だと塀の角が基準点より手前になります。 ", label, sight);
            setback = edgeDist - 0.1f;
        }
        return setback;
    }

    // 塀の角の後退距離 → 実際の見通し D（確認用）
    public float SightForSetback(float wallOffset, float laneZ, float setback)
    {
        float cornerFromRef = (NearEdgeZ - RefZ) - setback;
        if (cornerFromRef <= 0.001f) return float.PositiveInfinity;
        return wallOffset * (laneZ - RefZ) / cornerFromRef;
    }

    // ---------------- 内部処理 ----------------

    void PlaceWall(Transform along, Transform across, float wallX, float outward, float setback)
    {
        float cornerZ = NearEdgeZ - setback;
        float t = wallThickness;
        float y = wallHeight * 0.5f;

        // 内側の面が x = wallX、交差道路側の面が z = cornerZ になるように置く
        if (along != null)
        {
            along.localPosition = new Vector3(wallX + outward * t * 0.5f, y, cornerZ - wallAlongLength * 0.5f);
            along.localRotation = Quaternion.identity;
            along.localScale = new Vector3(t, wallHeight, wallAlongLength);
        }
        if (across != null)
        {
            across.localPosition = new Vector3(wallX + outward * wallAcrossLength * 0.5f, y, cornerZ - t * 0.5f);
            across.localRotation = Quaternion.identity;
            across.localScale = new Vector3(wallAcrossLength, wallHeight, t);
        }
        SetTiling(along, wallAlongLength / wallTextureSize, wallHeight / wallTextureSize);
        SetTiling(across, wallAcrossLength / wallTextureSize, wallHeight / wallTextureSize);

        // 塀の角と交差道路の建物線のあいだの空き地（砂利）
        Transform lot = outward < 0f ? leftLot : rightLot;
        if (lot != null)
        {
            float depth = setback;   // 交差道路の建物線から塀までの奥行き
            bool show = depth > 0.05f;
            if (lot.gameObject.activeSelf != show) lot.gameObject.SetActive(show);
            if (show)
            {
                lot.localPosition = new Vector3(wallX + outward * wallAcrossLength * 0.5f, lotHeight, NearEdgeZ - depth * 0.5f);
                lot.localRotation = Quaternion.Euler(90f, 0f, 0f);
                lot.localScale = new Vector3(wallAcrossLength, depth, 1f);
                SetTiling(lot, wallAcrossLength / lotTextureSize, depth / lotTextureSize);
            }
        }
    }

    // 1 枚のマテリアルを共有したまま、オブジェクトごとにテクスチャの繰り返し回数を変える
    static void SetTiling(Transform t, float u, float v)
    {
        if (t == null) return;
        var r = t.GetComponent<Renderer>();
        if (r == null) return;
        var block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        block.SetVector("_MainTex_ST", new Vector4(u, v, 0f, 0f));
        r.SetPropertyBlock(block);
    }

    static void SetActive(GameObject go, bool on)
    {
        if (go != null && go.activeSelf != on) go.SetActive(on);
    }

    void Start()
    {
        // 再生開始時に、エディタで最後に見ていた条件で一度そろえる（塀のテクスチャの繰り返しもここで設定される）
        if (Current == null) Apply(preview);
    }

    void Update()
    {
        if (!debugKeys) return;
        for (int i = 1; i <= 10; i++)
        {
            if (Input.GetKeyDown(KeyCode.F1 + (i - 1)))
            {
                Apply(TrialCondition.Factorial(i));
                Debug.Log("[IntersectionLayout] " + Current);
            }
        }
    }

    // ---------------- エディタ用 ----------------

    [ContextMenu("塀（L字）を自動生成")]
    void CreateWalls()
    {
        if (sightRoot == null) sightRoot = transform.Find("Sight");
        if (sightRoot == null)
        {
            sightRoot = new GameObject("Sight").transform;
            sightRoot.SetParent(transform, false);
        }
        if (leftWallAlong == null) leftWallAlong = MakeWall("Wall_Left_Along");
        if (leftWallAcross == null) leftWallAcross = MakeWall("Wall_Left_Across");
        if (rightWallAlong == null) rightWallAlong = MakeWall("Wall_Right_Along");
        if (rightWallAcross == null) rightWallAcross = MakeWall("Wall_Right_Across");
        Apply(preview);
    }

    Transform MakeWall(string name)
    {
        var existing = sightRoot.Find(name);
        if (existing != null) return existing;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(sightRoot, false);
#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(go, "Create wall");
#endif
        return go.transform;
    }

    [ContextMenu("車の位置から走行線を読み込む")]
    void ReadLanesFromCars()
    {
        if (leftCar != null) leftCarLaneZ = transform.InverseTransformPoint(leftCar.position).z;
        if (rightCar != null) rightCarLaneZ = transform.InverseTransformPoint(rightCar.position).z;
        Apply(preview);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (factorialPreset >= 1 && factorialPreset <= 10)
        {
            preview = TrialCondition.Factorial(factorialPreset);
        }
        else if (randomPreset >= 1 && randomConditionsCsv != null)
        {
            var all = TrialCondition.ParseRandomCsv(randomConditionsCsv.text);
            if (randomPreset <= all.Count) preview = all[randomPreset - 1];
        }
        if (!livePreview || Application.isPlaying) return;
        // OnValidate 中に SetActive すると警告が出るので、1フレーム遅らせる
        EditorApplication.delayCall += () => { if (this != null) Apply(preview); };
    }

    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        float nearZ = NearEdgeZ;
        Vector3 P = new Vector3(0f, 0.05f, RefZ);

        // 自転車の進路
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(0f, 0.05f, RefZ - 20f), new Vector3(0f, 0.05f, crossRoadFarEdge + 3f));
        // 交差道路の両側の建物線
        Gizmos.color = Color.white;
        Gizmos.DrawLine(new Vector3(-40f, 0.05f, nearZ), new Vector3(40f, 0.05f, nearZ));
        Gizmos.DrawLine(new Vector3(-40f, 0.05f, crossRoadFarEdge), new Vector3(40f, 0.05f, crossRoadFarEdge));
        // 自転車側の道路の両側の建物線（＝塀の線）
        Gizmos.DrawLine(new Vector3(-leftWallOffset, 0.05f, RefZ - 20f), new Vector3(-leftWallOffset, 0.05f, nearZ));
        Gizmos.DrawLine(new Vector3(rightWallOffset, 0.05f, RefZ - 20f), new Vector3(rightWallOffset, 0.05f, nearZ));
        // 車の走行線
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(-40f, 0.05f, leftCarLaneZ), new Vector3(0f, 0.05f, leftCarLaneZ));
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawLine(new Vector3(0f, 0.05f, rightCarLaneZ), new Vector3(40f, 0.05f, rightCarLaneZ));
        // 基準点
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(P, 0.25f);

        DrawSightLine(P, -leftWallOffset, leftSetback, leftCarLaneZ, Color.red, "D_L");
        DrawSightLine(P, rightWallOffset, rightSetback, rightCarLaneZ, Color.blue, "D_R");

        Gizmos.matrix = Matrix4x4.identity;
    }

    void DrawSightLine(Vector3 P, float wallX, float setback, float laneZ, Color col, string label)
    {
        Vector3 C = new Vector3(wallX, 0.05f, NearEdgeZ - setback);
        float k = (laneZ - P.z) / (C.z - P.z);
        Vector3 hit = new Vector3(P.x + (C.x - P.x) * k, 0.05f, laneZ);
        Gizmos.color = col;
        Gizmos.DrawLine(P, hit);
        Gizmos.DrawSphere(C, 0.15f);
        Gizmos.DrawSphere(hit, 0.3f);
        Gizmos.DrawLine(new Vector3(0f, 0.05f, laneZ), hit);
        Handles.Label(transform.TransformPoint(hit + Vector3.up * 1.5f),
            string.Format("{0} = {1:F1} m\n角 {2:F2} m 手前", label, Mathf.Abs(hit.x), setback));
    }
#endif
}
