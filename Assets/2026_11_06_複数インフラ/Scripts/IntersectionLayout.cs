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

    [Header("車（走行線を読み込むときだけ使う）")]
    public Transform leftCar;
    public Transform rightCar;

    [Header("エディタでのプレビュー")]
    public bool livePreview = true;
    [Tooltip("F1〜F10 を選ぶと下の preview に要因条件が入る（0 = 使わない）")]
    [Range(0, 10)] public int factorialPreset = 0;
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

    float NearEdgeZ { get { return -crossRoadNearEdge; } }
    float RefZ { get { return NearEdgeZ - referenceBeforeNearEdge; } }

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

        SetActive(colorPavement, c.attention == AttentionType.ColorPavement);
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
    }

    static void SetActive(GameObject go, bool on)
    {
        if (go != null && go.activeSelf != on) go.SetActive(on);
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
