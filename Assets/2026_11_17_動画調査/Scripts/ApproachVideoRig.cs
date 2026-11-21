using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// 動画調査（第2段階）：交差点に一定速度で近づく動画を、インフラ条件ごとに自動で書き出す
//
//  使い方
//   1. Exp2b＿Intersection シーンを開き、空の GameObject にこのコンポーネントを付ける
//   2. Layout に IntersectionLayout を割り当てる（Disable Behaviours に Exp2bTrialManager などを入れる）
//   3. 右クリックメニュー「既定の12本を入れる」で動画の一覧を作る
//   4. 再生すると、一覧の動画を順に録画して、終わると再生を止める
//      （録画そのものは Editor/ApproachVideoRecorder.cs が Unity Recorder で行う）
//
//  カメラは自転車のモデルとは別に、このコンポーネントが作る。
//  交差点のローカル座標（原点 = 自転車の進路と交差道路の中心線の交点、+Z = 進行方向）で、
//  x = 0（左の建物線から 2 m）、z = -startDistance から z = -endDistance まで一定速度で進み、止まって終わる。
public class ApproachVideoRig : MonoBehaviour
{
    [Serializable]
    public class Clip
    {
        public string id = "V01";
        [Tooltip("回答ページに出す名前ではなく、記録用の説明")]
        public string label = "何もなし";
        public bool include = true;

        public NormType norm = NormType.None;
        public AttentionType attention = AttentionType.None;
        public InfoType info = InfoType.None;
        [Tooltip("左の塀の角の後退距離 [m]（交差道路の建物線から手前へ）。悪い 0・中 5・良い 10")]
        public float wallSetbackLeft = TrialCondition.SetbackGood;
        [Tooltip("右の塀の角の後退距離 [m]")]
        public float wallSetbackRight = TrialCondition.SetbackGood;

        [Tooltip("交差点の路面を差し替えるマテリアル（停止線のみ・「止まれ」のみ・イメージハンプなど）。空なら IntersectionLayout のとおり")]
        public Material surfaceOverride;
        [Tooltip("この動画のときだけ表示するオブジェクト（新しく作ったインフラなど）")]
        public GameObject[] extraObjects = new GameObject[0];
        [Tooltip("新しい素材が必要な動画。路面の差し替えも追加オブジェクトも無いときは録画を飛ばす")]
        public bool needsCustomAsset;
        [Tooltip("LED表示板の点灯（新しい LED表示板 VehicleApproachDisplay のときだけ効く）。調査では車が出ないので、Auto は常に消灯")]
        public VehicleApproachDisplay.Force led = VehicleApproachDisplay.Force.Off;
        [Tooltip("練習用の動画（clips.csv の practice 列が 1 になる。回答の解析には使わない）")]
        public bool practice;

        [Tooltip("車が出てくるデモ（シミュレータの車ありダミーと同じ役割）。回答ページでは評定を聞かず、通り方だけを聞く")]
        public CarSpawn car = CarSpawn.None;
        [Tooltip("車が交差点の中心を通る時刻 [s]（動画の始まりから）。自転車は 13.9 s で交差点の 5.5 m 手前に着く")]
        public float carArriveTime = 12f;

        [Tooltip("規範が「なし」でも、路面の差し替えや追加オブジェクトで見せているもの（clips.csv の Line / Sign / Mark 列に反映する）")]
        public bool showsLine;
        public bool showsSign;
        public bool showsMark;

        // clips.csv の z の列（TrialCondition.ZHeader と同じ並び）。停止線セットか、差し替えで見せているものを 1 にする
        public string ZValues()
        {
            bool set = norm == NormType.StopSet;
            var c = ToCondition();
            string[] z = c.ZValues().Split(',');
            z[0] = (set || showsLine) ? "1" : "0";
            z[1] = (set || showsSign) ? "1" : "0";
            z[2] = (set || showsMark) ? "1" : "0";
            return string.Join(",", z);
        }

        public TrialCondition ToCondition()
        {
            var c = new TrialCondition
            {
                id = id,
                kind = TrialKind.Practice,
                norm = norm,
                attention = attention,
                info = info,
            };
            c.SetWalls(wallSetbackLeft, wallSetbackRight);
            return c;
        }

        public bool MissingAsset()
        {
            if (!needsCustomAsset) return false;
            if (surfaceOverride != null) return false;
            foreach (var go in extraObjects) if (go != null) return false;
            return true;
        }
    }

    // 録画の開始・終了（Editor/ApproachVideoRecorder.cs が登録する。ビルドや Recorder が無いときは null）
    public static Action<ApproachVideoRig, Clip, string> RecordingStarted;
    public static Action<ApproachVideoRig> RecordingStopped;
    public static Action<ApproachVideoRig> AllFinished;

    [Header("交差点")]
    public IntersectionLayout layout;

    [Header("走り方")]
    [Tooltip("自転車の速度 [m/s]")]
    public float speed = 5f;
    [Tooltip("スタート位置：交差点の中心（自転車の進路と交差道路の中心線の交点）から手前へ何 m か。今の実験と同じ 75 m")]
    public float startDistance = 75f;
    [Tooltip("止まる位置：交差点の中心から手前へ何 m か。5.5 m で停止線（中心から約 4.5 m）の 1 m 手前、交差道路の端（6.1 m）を少し越えたところ。Scene ビューの緑の球で確認する")]
    public float endDistance = 5.5f;
    [Tooltip("止まったあと、最後の画面を何秒映すか")]
    public float holdSeconds = 1.0f;
    [Tooltip("録画を始める前に条件を切り替えてから待つ秒数（塀やマテリアルの切り替えを落ち着かせる）")]
    public float settleSeconds = 0.5f;

    [Header("カメラ")]
    [Tooltip("目の高さ [m]（地面から）")]
    public float eyeHeight = 1.5f;
    [Tooltip("縦の視野角 [deg]。正面スクリーンのカメラと同じ 60 が既定")]
    public float verticalFov = 60f;
    [Tooltip("指定すると、そのカメラの高さと視野角をコピーする（自転車の正面カメラなど）")]
    public Camera copyFrom;
    [Tooltip("録画中はほかのカメラを止める（左右のスクリーン用カメラなどが映り込まないように）。ミラーなど RenderTexture に描くカメラは止めない")]
    public bool disableOtherCameras = true;

    [Header("パノラマ（つなぎ目のない横長の動画。既定）")]
    [Tooltip("目の位置から全方向を撮り、横 Panorama H Fov°の円筒に展開した 1 枚の横長の動画にする。左右の画面のつなぎ目が出ない。オンのときは下の 3 画面の設定は使わない")]
    public bool panorama = true;
    [Tooltip("パノラマの横の視野角 [deg]。シミュレータの 3 画面（正面と ±45°）が映す範囲は約 180°")]
    public float panoramaHFov = 180f;
    [Tooltip("パノラマの動画の幅 [px]。高さは縦の視野角（Vertical Fov）から自動で決まる（180°・60° なら 2560 × 940）")]
    public int panoramaWidth = 2560;

    [Header("ミラー・LED表示板の拡大（動画の右上に小窓で出す）")]
    [Tooltip("カーブミラーか LED表示板がある動画では、それを目で追って拡大した小窓を右上に出す（動画では小さくて見えないため）")]
    public bool insetForInfo = true;
    [Tooltip("小窓の位置と大きさ（画面に対する割合。x・y は左下から）。回答ページの枠（INSET_RECT）と同じにする")]
    public Rect insetRect = new Rect(0.755f, 0.60f, 0.235f, 0.37f);
    [Tooltip("対象の大きさの何倍の範囲を小窓に映すか（小さいほど大きく映る）")]
    public float insetFill = 1.4f;
    [Tooltip("小窓を本体の動画に焼き込まず、別の動画（<id>_inset.mp4）として書き出す。回答ページは 1 回目のボタンを押してから小窓を重ねて出す" +
             "（最初から小窓が出ていると、ミラーや表示板があることがすぐ分かってしまうため）")]
    public bool insetSeparateVideo = true;
    [Tooltip("小窓の動画の高さ [px]。幅は小窓の横縦比から自動で決まる")]
    public int insetVideoHeight = 360;

    [Header("3 画面（シミュレータの左・正面・右のスクリーンを並べる。Panorama がオフのときだけ）")]
    [Tooltip("シミュレータと同じく、正面と左右 ±45° のカメラで撮って横に並べる。オフなら正面 1 画面だけ")]
    public bool threeScreens = true;
    [Tooltip("左右のスクリーンのカメラの向き [deg]（シーンの Camera 1 / Camera 2 と同じ 45）")]
    public float sideYaw = 45f;
    [Tooltip("スクリーン 1 枚の横縦比（シミュレータのモニタ 16:9）")]
    public float screenAspect = 16f / 9f;
    [Tooltip("左右のスクリーンのうち、正面と重なっていない外側の半分だけを並べる（動画が 32:9 になり、縦が大きく見える）。オフなら 3 枚をそのまま並べる（48:9）")]
    public bool cropSideOverlap = true;
    [Tooltip("3 画面のときの動画の高さ [px]。幅は横縦比から自動で決まる（外側の半分だけなら 32:9 → 720 で 2560）")]
    public int wideHeight = 720;
    [Tooltip("録画中は画面上の UI（タイマーなど）を隠す")]
    public bool hideCanvases = true;
    [Tooltip("自転車のモデル（BicycleModelCalculator の付いたオブジェクト以下）の見た目を隠す。スタート地点でカメラに映り込まないように")]
    public bool hideBicycle = true;

    [Header("止めておくもの")]
    [Tooltip("シーンの Exp2bTrialManager・BicycleModelCalculator・GoalLine・TrialManager を自動で探して止める（下の欄に入れなくてよい）")]
    public bool autoDisableExperimentScripts = true;
    [Tooltip("ほかに録画のあいだ止めたいコンポーネントがあれば入れる（空でよい）")]
    public Behaviour[] disableBehaviours = new Behaviour[0];
    [Tooltip("録画のあいだ隠すオブジェクト。空なら layout の左右の車を隠す")]
    public GameObject[] hideObjects = new GameObject[0];

    [Header("出力")]
    [Tooltip("プロジェクト直下からの保存先フォルダ")]
    public string outputFolder = "Recordings/ApproachVideos";
    public int width = 1920;
    public int height = 1080;
    public int frameRate = 30;
    [Tooltip("再生したら自動で全部録画する。オフなら右クリックメニュー「1本だけ再生（確認用）」で動きだけ見る")]
    public bool recordOnPlay = true;
    [Tooltip("ここに id をカンマ区切りで書くと、その動画だけ録る（例：D01,D02）。一覧は clips.csv を上書きせず clips_part.csv に書く。空なら全部")]
    public string onlyIds = "";
    [Tooltip("録画が全部終わったら再生を止める")]
    public bool stopPlayingWhenDone = true;

    [Header("動画の一覧")]
    public List<Clip> clips = new List<Clip>();

    [Header("確認用（読み取り専用）")]
    [SerializeField] string status = "";
    [SerializeField] float currentDistance;

    Transform camRoot;
    Camera insetCam;
    Camera panoCam;
    bool insetPass;   // 小窓の動画を録っている最中
    Vector3 insetCenter;
    float insetRadius;
    Material originalSurface;

    // 録画する動画の大きさ（小窓の動画を録るときは小窓の大きさ）
    public int OutputWidth
    {
        get
        {
            if (!insetPass) return MainWidth;
            int w = Mathf.RoundToInt(insetVideoHeight * InsetAspect);
            return w - (w % 2);
        }
    }
    public int OutputHeight { get { return insetPass ? insetVideoHeight - (insetVideoHeight % 2) : MainHeight; } }
    float InsetAspect { get { return (insetRect.width * MainWidth) / Mathf.Max(1f, insetRect.height * MainHeight); } }

    // 本体の動画の大きさ（3 画面のときは横長）
    public int MainWidth
    {
        get
        {
            if (panorama) return panoramaWidth - (panoramaWidth % 2);
            if (!threeScreens) return width;
            float units = cropSideOverlap ? 2f : 3f;   // スクリーン何枚分の幅か
            int w = Mathf.RoundToInt(wideHeight * screenAspect * units);
            return w - (w % 2);
        }
    }
    public int MainHeight
    {
        get
        {
            if (panorama)
            {
                // 円筒の上では、横は角度 [rad]、縦は 2·tan(縦の視野角/2) が同じ縮尺になる
                float h = panoramaWidth * 2f * Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad) / (panoramaHFov * Mathf.Deg2Rad);
                int hi = Mathf.RoundToInt(h);
                return hi - (hi % 2);
            }
            return threeScreens ? wideHeight : height;
        }
    }
    readonly List<Behaviour> stoppedCameras = new List<Behaviour>();
    readonly List<Behaviour> stoppedCanvases = new List<Behaviour>();

    public string OutputDir
    {
        get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputFolder)); }
    }

    float MoveSeconds { get { return (startDistance - endDistance) / Mathf.Max(0.01f, speed); } }

    void Awake()
    {
        // ビルド（実験本番の exe）では何もしない。動画の録画はエディタの再生でだけ行う
        if (!Application.isEditor)
        {
            enabled = false;
            return;
        }
        if (!recordOnPlay)
        {
            Debug.Log("[ApproachVideo] Record On Play がオフなので、実験のスクリプトは止めずに通常どおり動かします。");
            enabled = false;
            return;
        }
        Debug.Log("[ApproachVideo] ===== 動画調査モード：実験のスクリプトを止めて、動画を録画します =====");
        // 他のコンポーネントの Start より前に止める（Awake はすべての Start より先に呼ばれる）
        foreach (var b in disableBehaviours) if (b != null) b.enabled = false;
        if (autoDisableExperimentScripts)
        {
            var stopped = new List<string>();
            foreach (var t in new[] { typeof(Exp2bTrialManager), typeof(BicycleModelCalculator), typeof(GoalLine), typeof(TrialManager) })
            {
                foreach (var o in FindObjectsOfType(t))
                {
                    var b = o as Behaviour;
                    if (b == null || !b.enabled) continue;
                    b.enabled = false;
                    stopped.Add(t.Name + "（" + b.gameObject.name + "）");
                }
            }
            Debug.Log("[ApproachVideo] 録画のあいだ止めたもの：" + (stopped.Count > 0 ? string.Join("、", stopped.ToArray()) : "なし"));
        }
    }

    void Start()
    {
        if (layout == null) layout = FindObjectOfType<IntersectionLayout>();
        if (layout == null)
        {
            Debug.LogError("[ApproachVideo] IntersectionLayout が見つかりません。", this);
            enabled = false;
            return;
        }
        if (layout.intersectionSurface != null) originalSurface = layout.intersectionSurface.sharedMaterial;
        layout.debugKeys = false;
        HideObjects();
        CreateCamera();
        if (recordOnPlay) StartCoroutine(RecordAll());
    }

    // ---------------- 録画の流れ ----------------

    IEnumerator RecordAll()
    {
        Directory.CreateDirectory(OutputDir);
        var rows = new List<string>();
        rows.Add("id,label,practice," + TrialCondition.ZHeader + ",ledLit,surfaceOverride,extraObjects,speed_mps,startDistance_m,endDistance_m,moveSeconds,holdSeconds,totalSeconds,file,car,carArriveSec,inset,insetFile");

        if (RecordingStarted == null)
            Debug.LogWarning("[ApproachVideo] 録画の仕組み（Editor/ApproachVideoRecorder.cs）が見つかりません。動きだけ再生します。");

        foreach (var clip in clips)
        {
            if (!clip.include) continue;
            if (!string.IsNullOrEmpty(onlyIds.Trim()) && Array.IndexOf(onlyIds.Replace(" ", "").Split(','), clip.id) < 0) continue;
            if (clip.MissingAsset())
            {
                Debug.LogWarning(string.Format("[ApproachVideo] {0}（{1}）は新しい素材（路面の差し替えか追加オブジェクト）が未設定なので飛ばします。", clip.id, clip.label), this);
                continue;
            }
            // ファイル名は id だけにする（日本語のファイル名は動画の書き出しで問題が出ることがあるため。説明は clips.csv に入る）
            string file = Path.Combine(OutputDir, SafeName(clip.id));
            yield return PlayClip(clip, file);
            // 小窓を別の動画にするときは、同じ動きをもう一度再生して小窓だけを録る
            bool hasInset = insetForInfo && insetCam != null && (clip.info == InfoType.Mirror || clip.info == InfoType.LedBoard);
            if (hasInset && insetSeparateVideo)
            {
                insetPass = true;
                yield return PlayClip(clip, file + "_inset");
                insetPass = false;
                SetInsetMode();
            }
            rows.Add(string.Join(",", new[]
            {
                clip.id, Csv(clip.label), clip.practice ? "1" : "0", clip.ZValues(),
                clip.info == InfoType.LedBoard && clip.led == VehicleApproachDisplay.Force.On ? "1" : "0",
                clip.surfaceOverride != null ? Csv(clip.surfaceOverride.name) : "",
                Csv(ExtraNames(clip)),
                F(speed), F(startDistance), F(endDistance), F(MoveSeconds), F(holdSeconds), F(MoveSeconds + holdSeconds),
                Csv(Path.GetFileName(file) + ".mp4"),
                clip.car.ToString(), clip.car != CarSpawn.None ? F(clip.carArriveTime) : "",
                hasInset ? "1" : "0",
                hasInset && insetSeparateVideo ? Csv(Path.GetFileName(file) + "_inset.mp4") : "",
            }));
        }

        string listPath = Path.Combine(OutputDir, string.IsNullOrEmpty(onlyIds.Trim()) ? "clips.csv" : "clips_part.csv");
        File.WriteAllText(listPath, string.Join("\n", rows) + "\n", new UTF8Encoding(true));
        status = "完了：" + listPath;
        Debug.Log("[ApproachVideo] すべて終わりました。一覧：" + listPath);
        RestoreAfterClip(null);
        if (AllFinished != null) AllFinished(this);
    }

    IEnumerator PlayClip(Clip clip, string file)
    {
        status = clip.id + " " + clip.label;
        ApplyClip(clip);
        SetDistance(startDistance);
        yield return new WaitForSecondsRealtime(settleSeconds);
        yield return null;

        Transform carT = StartCar(clip);
        if (RecordingStarted != null) RecordingStarted(this, clip, file);
        // 録画中は Time.captureFramerate が固定されるので、deltaTime で進めれば動画の中で正確に speed m/s になる
        yield return null;
        float d = startDistance;
        float te = 0f;
        MoveCar(clip, carT, te);
        while (d > endDistance)
        {
            te += Time.deltaTime;
            d = Mathf.Max(endDistance, d - speed * Time.deltaTime);
            SetDistance(d);
            MoveCar(clip, carT, te);
            yield return null;
        }
        float held = 0f;
        while (held < holdSeconds)
        {
            te += Time.deltaTime;
            held += Time.deltaTime;
            MoveCar(clip, carT, te);
            yield return null;
        }
        if (RecordingStopped != null) RecordingStopped(this);
        yield return null;
        RestoreAfterClip(clip);
    }

    void ApplyClip(Clip clip)
    {
        layout.Apply(clip.ToCondition());
        if (clip.surfaceOverride != null && layout.intersectionSurface != null)
            layout.intersectionSurface.sharedMaterial = clip.surfaceOverride;
        foreach (var go in clip.extraObjects) if (go != null) go.SetActive(true);
        var led = LedDisplay();
        if (led != null) led.force = clip.led;
        bool target = insetCam != null && insetForInfo && FindInsetTarget(clip);
        SetInsetMode();
        // 小窓を本体に焼き込むのは、別の動画にしないときだけ。小窓の動画を録る回は小窓だけを画面いっぱいに映す
        if (insetCam != null) insetCam.enabled = target && (insetPass || !insetSeparateVideo);
        if (led == null && clip.info == InfoType.LedBoard)
            Debug.LogWarning("[ApproachVideo] LED表示板が新しい版（VehicleApproachDisplay）ではありません。IntersectionLayout の ⋮ メニュー「LED表示板を作り直す」を実行してください。", this);
    }

    VehicleApproachDisplay LedDisplay()
    {
        if (layout == null || layout.ledBoard == null) return null;
        return layout.ledBoard.GetComponent<VehicleApproachDisplay>();
    }

    void RestoreAfterClip(Clip clip)
    {
        if (clip != null) foreach (var go in clip.extraObjects) if (go != null) go.SetActive(false);
        if (clip != null && clip.car != CarSpawn.None)
        {
            Transform carT = clip.car == CarSpawn.Left ? layout.leftCar : layout.rightCar;
            if (carT != null) carT.gameObject.SetActive(false);
        }
        var led = LedDisplay();
        if (led != null) led.force = VehicleApproachDisplay.Force.Auto;
        if (insetCam != null) insetCam.enabled = false;
        if (originalSurface != null && layout != null && layout.intersectionSurface != null)
            layout.intersectionSurface.sharedMaterial = originalSurface;
    }

    // ---------------- カメラ ----------------

    void CreateCamera()
    {
        if (copyFrom != null)
        {
            eyeHeight = copyFrom.transform.position.y - layout.transform.position.y;
            verticalFov = copyFrom.fieldOfView;
        }
        if (disableOtherCameras)
        {
            foreach (var other in FindObjectsOfType<Camera>())
            {
                if (!other.enabled) continue;
                if (other.targetTexture != null) continue;   // カーブミラーの映像などはそのまま
                other.enabled = false;
                stoppedCameras.Add(other);
            }
        }
        if (hideCanvases)
        {
            foreach (var canvas in FindObjectsOfType<Canvas>())
            {
                if (!canvas.enabled) continue;
                canvas.enabled = false;
                stoppedCanvases.Add(canvas);
            }
        }
        camRoot = new GameObject("ApproachVideoCamera").transform;
        if (panorama)
        {
            var go = new GameObject("PanoramaOutput");
            go.transform.SetParent(camRoot, false);
            var c = go.AddComponent<Camera>();
            panoCam = c;
            c.cullingMask = 0;                       // 自分では何も描かず、パノラマを貼るだけ
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = Color.black;
            c.depth = 100f;
            c.targetDisplay = 0;
            var pano = go.AddComponent<ApproachPanoramaOutput>();
            pano.eye = camRoot;
            pano.horizontalFov = panoramaHFov;
            pano.verticalFov = verticalFov;
            Debug.Log(string.Format("[ApproachVideo] パノラマで録画します（横 {0:F0}°・縦 {1:F0}°）。動画の大きさ {2}×{3}",
                panoramaHFov, verticalFov, OutputWidth, OutputHeight));
            CreateInsetCamera();
            return;
        }
        if (!threeScreens)
        {
            MakeCamera("Front", 0f, new Rect(0f, 0f, 1f, 1f), 0);
            return;
        }
        // 3 画面：左・正面・右を横に並べる。幅の単位はスクリーンの半分
        float units = cropSideOverlap ? 4f : 6f;
        float side = (cropSideOverlap ? 1f : 2f) / units;
        MakeCamera("Left", -sideYaw, new Rect(0f, 0f, side, 1f), cropSideOverlap ? -1 : 0);
        MakeCamera("Front", 0f, new Rect(side, 0f, 2f / units, 1f), 0);
        MakeCamera("Right", sideYaw, new Rect(side + 2f / units, 0f, side, 1f), cropSideOverlap ? +1 : 0);
        Debug.Log(string.Format("[ApproachVideo] 3 画面で録画します（左右 ±{0:F0}°、{1}）。動画の大きさ {2}×{3}",
            sideYaw, cropSideOverlap ? "左右は正面と重ならない外側の半分" : "3 枚をそのまま", OutputWidth, OutputHeight));
    }

    // 本体を録る回：パノラマを映し、小窓は右上。小窓の動画を録る回：パノラマを止め、小窓を画面いっぱいに
    void SetInsetMode()
    {
        if (panoCam != null) panoCam.enabled = !insetPass;
        if (insetCam != null) insetCam.rect = insetPass ? new Rect(0f, 0f, 1f, 1f) : insetRect;
    }

    void CreateInsetCamera()
    {
        var go = new GameObject("InfoInset");
        go.transform.SetParent(camRoot, false);
        insetCam = go.AddComponent<Camera>();
        insetCam.depth = 101f;                 // パノラマの上に重ねる
        insetCam.rect = insetRect;
        insetCam.nearClipPlane = 0.1f;
        insetCam.farClipPlane = 1000f;
        insetCam.clearFlags = CameraClearFlags.Skybox;
        insetCam.targetDisplay = 0;
        insetCam.enabled = false;
    }

    // 小窓で追う物（ミラーの鏡面・LED表示板の発光面）の中心と大きさ。支柱を除くため、地面から 1.8 m より上の部品だけを使う
    bool FindInsetTarget(Clip clip)
    {
        GameObject target = clip.info == InfoType.Mirror ? layout.mirror : clip.info == InfoType.LedBoard ? layout.ledBoard : null;
        if (target == null) return false;
        bool any = false;
        Bounds b = new Bounds();
        float ground = layout.transform.position.y;
        foreach (var r in target.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (r.bounds.center.y - ground < 1.8f) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        if (!any) return false;
        insetCenter = b.center;
        insetRadius = Mathf.Max(0.2f, b.extents.magnitude);
        return true;
    }

    void UpdateInset()
    {
        if (insetCam == null || !insetCam.enabled) return;
        Vector3 eye = camRoot.position;
        float dist = Mathf.Max(0.5f, Vector3.Distance(eye, insetCenter));
        insetCam.transform.position = eye;
        insetCam.transform.rotation = Quaternion.LookRotation(insetCenter - eye, Vector3.up);
        // 縦の視野角：対象（半径 insetRadius）の insetFill 倍が小窓の高さに入るように
        float fov = 2f * Mathf.Atan(insetRadius * insetFill / dist) * Mathf.Rad2Deg;
        insetCam.fieldOfView = Mathf.Clamp(fov, 1.5f, 40f);
    }

    // half: 0 = スクリーン全体、-1 = 左半分だけ、+1 = 右半分だけ（シミュレータのスクリーンと同じ像を半分に切る）
    void MakeCamera(string name, float yaw, Rect rect, int half)
    {
        var go = new GameObject(name);
        go.transform.SetParent(camRoot, false);
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var c = go.AddComponent<Camera>();
        c.fieldOfView = verticalFov;
        c.nearClipPlane = 0.1f;
        c.farClipPlane = 1000f;
        c.depth = 100f;
        c.targetDisplay = 0;
        c.clearFlags = CameraClearFlags.Skybox;
        c.rect = rect;
        if (threeScreens)
        {
            // スクリーン 1 枚分（縦 verticalFov、横縦比 screenAspect）の視錐台。半分のときは片側だけ使う
            float top = c.nearClipPlane * Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad);
            float right = top * screenAspect;
            float l = half > 0 ? 0f : -right;
            float r = half < 0 ? 0f : right;
            c.projectionMatrix = Matrix4x4.Frustum(l, r, -top, top, c.nearClipPlane, c.farClipPlane);
        }
    }

    void SetDistance(float d)
    {
        currentDistance = d;
        if (camRoot == null || layout == null) return;
        Transform t = layout.transform;
        Vector3 p = t.TransformPoint(new Vector3(0f, 0f, -d));
        p.y = t.position.y + eyeHeight;
        camRoot.position = p;
        camRoot.rotation = Quaternion.LookRotation(t.forward, Vector3.up);
        UpdateInset();
    }

    // 車の位置（交差点のローカル座標）。シミュレータでは地下（-100 m）に待機していることがあるので、高さは地上に直す
    readonly Dictionary<Transform, Vector3> carHome = new Dictionary<Transform, Vector3>();

    void RememberCar(Transform car)
    {
        if (car == null || carHome.ContainsKey(car)) return;
        Vector3 p = layout.transform.InverseTransformPoint(car.position);
        if (p.y < -50f) p.y += 100f;
        carHome[car] = p;
    }

    Transform StartCar(Clip clip)
    {
        if (clip.car == CarSpawn.None) return null;
        Transform carT = clip.car == CarSpawn.Left ? layout.leftCar : layout.rightCar;
        if (carT == null)
        {
            Debug.LogWarning("[ApproachVideo] " + clip.id + "：IntersectionLayout に " + clip.car + " の車が入っていません。車なしで録画します。", this);
            return null;
        }
        // シミュレータ用の車の動き（自転車に合わせて発進・ブレーキ）を止めて、ここから動かす
        foreach (var b in carT.GetComponentsInChildren<Behaviour>(true))
            if (b is CarControllManagement) b.enabled = false;
        carT.gameObject.SetActive(true);
        return carT;
    }

    // 車は自分の走行線（ローカル z は初期位置のまま）を、初期位置の側から交差点に向かって一定速度で進む
    void MoveCar(Clip clip, Transform carT, float t)
    {
        if (carT == null || !carHome.ContainsKey(carT)) return;
        Vector3 home = carHome[carT];
        var ctrl = carT.GetComponent<CarControllManagement>();
        float v = ctrl != null ? ctrl.speed : 8.33f;
        float side = Mathf.Sign(home.x == 0f ? (clip.car == CarSpawn.Left ? -1f : 1f) : home.x);
        float x = side * v * (clip.carArriveTime - t);   // t = carArriveTime で交差点の中心（x = 0）
        carT.position = layout.transform.TransformPoint(new Vector3(x, home.y, home.z));
    }

    void HideObjects()
    {
        RememberCar(layout.leftCar);
        RememberCar(layout.rightCar);
        var list = new List<GameObject>(hideObjects);
        if (list.Count == 0)
        {
            if (layout.leftCar != null) list.Add(layout.leftCar.gameObject);
            if (layout.rightCar != null) list.Add(layout.rightCar.gameObject);
        }
        foreach (var go in list) if (go != null) go.SetActive(false);

        if (hideBicycle)
        {
            foreach (var bike in FindObjectsOfType<BicycleModelCalculator>())
                foreach (var r in bike.GetComponentsInChildren<Renderer>()) r.enabled = false;
        }
    }

    // ---------------- 一覧の既定値 ----------------

    [ContextMenu("既定の一覧を入れる（練習3本＋本番21本＋車ありデモ2本）")]
    void FillDefaultClips()
    {
        clips = DefaultClips();
        AssignDefaultSurfaces(clips);
        AssignStopMarkDecal();
    }

    // 「止まれ」のみ（V06）：印のない路面に、IntersectionLayout の Stop Mark（「止まれ」の板）だけを出す
    void AssignStopMarkDecal()
    {
        if (layout == null) layout = FindObjectOfType<IntersectionLayout>();
        var v06 = clips.Find(c => c.id == "V06");
        if (v06 != null)
        {
            if (layout != null && layout.stopMark != null) v06.extraObjects = new[] { layout.stopMark };
            else Debug.LogWarning("[ApproachVideo] IntersectionLayout の Stop Mark が空です。先に ⋮ メニュー「「止まれ」路面表示を板で置く」を実行してから、もう一度「既定の一覧を入れる」を実行してください。", this);
        }
        // 標識のみ（V21）：IntersectionLayout の Stop Sign（一時停止標識）だけを出す
        var v21 = clips.Find(c => c.id == "V21");
        if (v21 != null)
        {
            if (layout != null && layout.stopSign != null) v21.extraObjects = new[] { layout.stopSign };
            else Debug.LogWarning("[ApproachVideo] IntersectionLayout の Stop Sign が空です。", this);
        }
    }

    // 停止線のみ・「止まれ」のみの路面マテリアル（Materials フォルダ）を V05・V06 に割り当てる
    const string MaterialDir = "Assets/2026_11_17_動画調査/Materials/";

    static void AssignDefaultSurfaces(List<Clip> list)
    {
#if UNITY_EDITOR
        foreach (var c in list)
        {
            string file = (c.id == "V05" || c.id == "V20") ? "Mat_StopLineOnly.mat"
                : (c.id == "V06" || c.id == "V21") ? "Mat_StopMarkOnly.mat" : null;
            if (file == null || c.surfaceOverride != null) continue;
            c.surfaceOverride = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + file);
            if (c.surfaceOverride == null) Debug.LogWarning("[ApproachVideo] " + MaterialDir + file + " が見つかりません。");
        }
#endif
    }

    // 動画調査の案（2026-11-17）。指定のないものは見通し 良い（塀の角が 10 m 手前）
    public static List<Clip> DefaultClips()
    {
        // 見通しは塀の角の後退距離で指定する（悪い 0 m・中 8 m・良い 16 m、左右同じ）
        float g = TrialCondition.SetbackGood, m = TrialCondition.SetbackMid, b = TrialCondition.SetbackBad;
        var S = NormType.StopSet;
        var R = NormType.RedFlash;
        return new List<Clip>
        {
            // 練習：LED が点灯すると「車が近づいている」ことを見せる（本番の LED は常に消灯）
            new Clip { id = "P01", label = "練習：LED表示板 点灯", practice = true, info = InfoType.LedBoard, led = VehicleApproachDisplay.Force.On, wallSetbackLeft = b, wallSetbackRight = b },
            // 練習 2・3 本目：本番には無い組み合わせにする（本番の動画を先に見せないため）
            new Clip { id = "P02", label = "練習：赤点滅＋カラー舗装 良い", practice = true, norm = R, attention = AttentionType.ColorPavement },
            new Clip { id = "P03", label = "練習：注意看板 悪い", practice = true, attention = AttentionType.CautionSign, wallSetbackLeft = b, wallSetbackRight = b },

            // 見通し（何も置かない）
            new Clip { id = "V01", label = "何もなし 良い" },
            new Clip { id = "V02", label = "何もなし 中", wallSetbackLeft = m, wallSetbackRight = m },
            new Clip { id = "V03", label = "何もなし 悪い", wallSetbackLeft = b, wallSetbackRight = b },
            new Clip { id = "V04", label = "何もなし 左良い 右悪い", wallSetbackLeft = g, wallSetbackRight = b },
            // 規範（強さの違い）
            new Clip { id = "V05", label = "停止線のみ", needsCustomAsset = true, showsLine = true },
            // 「止まれ」のみ：印のない路面（Mat_StopMarkOnly）＋「止まれ」の板（Stop Mark）
            new Clip { id = "V06", label = "止まれ路面表示のみ", needsCustomAsset = true, showsMark = true },
            new Clip { id = "V07", label = "停止線＋標識＋路面表示", norm = S },
            new Clip { id = "V08", label = "赤点滅信号", norm = R },
            // 注意喚起
            new Clip { id = "V09", label = "カラー舗装", attention = AttentionType.ColorPavement },
            new Clip { id = "V10", label = "注意看板", attention = AttentionType.CautionSign },
            // 規範×注意喚起（義務×強調）
            new Clip { id = "V11", label = "停止線セット＋カラー舗装", norm = S, attention = AttentionType.ColorPavement },
            new Clip { id = "V12", label = "停止線セット＋注意看板", norm = S, attention = AttentionType.CautionSign },
            // 規範×見通し
            new Clip { id = "V13", label = "停止線セット 悪い", norm = S, wallSetbackLeft = b, wallSetbackRight = b },
            new Clip { id = "V14", label = "赤点滅信号 悪い", norm = R, wallSetbackLeft = b, wallSetbackRight = b },
            // 情報×見通し
            new Clip { id = "V15", label = "ミラー 良い", info = InfoType.Mirror },
            new Clip { id = "V16", label = "ミラー 悪い", info = InfoType.Mirror, wallSetbackLeft = b, wallSetbackRight = b },
            new Clip { id = "V17", label = "LED表示板 中", info = InfoType.LedBoard, wallSetbackLeft = m, wallSetbackRight = m },
            new Clip { id = "V18", label = "LED表示板 悪い", info = InfoType.LedBoard, wallSetbackLeft = b, wallSetbackRight = b },
            // 情報×規範
            new Clip { id = "V19", label = "停止線セット＋ミラー 悪い", norm = S, info = InfoType.Mirror, wallSetbackLeft = b, wallSetbackRight = b },
            // 現実に近い赤点滅（停止線あり）。V08（赤点滅だけ）と比べて、停止線の上乗せを見る
            new Clip { id = "V20", label = "赤点滅信号＋停止線", norm = R, needsCustomAsset = true, showsLine = true },
            // 一時停止の標識だけ（路面には何も描かない）。印のない路面（Mat_StopMarkOnly）＋標識（Stop Sign）
            new Clip { id = "V21", label = "一時停止標識のみ", needsCustomAsset = true, showsSign = true },

            // デモ：途中で横から車が出てくる（シミュレータの車ありダミーと同じ役割。評定は取らない）
            //   自転車が交差点の約 15 m 手前にいるときに、車が交差点の中心を通る
            new Clip { id = "D01", label = "デモ：左から車 何もなし 中", wallSetbackLeft = m, wallSetbackRight = m, car = CarSpawn.Left, carArriveTime = 12f },
            new Clip { id = "D02", label = "デモ：右から車 停止線セット 悪い", norm = S, wallSetbackLeft = b, wallSetbackRight = b, car = CarSpawn.Right, carArriveTime = 12.5f },
        };
    }

    void Reset()
    {
        layout = FindObjectOfType<IntersectionLayout>();
        clips = DefaultClips();
        AssignDefaultSurfaces(clips);
        AssignStopMarkDecal();
    }

    // ---------------- 小物 ----------------

    static string F(float v) { return v.ToString("F2", CultureInfo.InvariantCulture); }

    static string Csv(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.IndexOfAny(new[] { ',', '"', '\n' }) < 0) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    static string ExtraNames(Clip clip)
    {
        var names = new List<string>();
        foreach (var go in clip.extraObjects) if (go != null) names.Add(go.name);
        return string.Join(" ", names.ToArray());
    }

    static string SafeName(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace('（', '_').Replace('）', '_').Replace('＋', '+').Replace(' ', '_');
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("CONTEXT/ApproachVideoRig/確認：終点（動画の最後の画面）からの眺めを Scene ビューに出す")]
    static void ViewFromEnd(UnityEditor.MenuCommand cmd) { ((ApproachVideoRig)cmd.context).ViewFrom(false); }

    [UnityEditor.MenuItem("CONTEXT/ApproachVideoRig/確認：スタート地点からの眺めを Scene ビューに出す")]
    static void ViewFromStart(UnityEditor.MenuCommand cmd) { ((ApproachVideoRig)cmd.context).ViewFrom(true); }

    // Scene ビューのカメラを、録画用カメラと同じ位置・向き・視野角にする
    void ViewFrom(bool start)
    {
        if (layout == null) layout = FindObjectOfType<IntersectionLayout>();
        var sv = UnityEditor.SceneView.lastActiveSceneView;
        if (layout == null || sv == null)
        {
            Debug.LogWarning("[ApproachVideo] Layout が未設定か、Scene ビューが開いていません。");
            return;
        }
        Transform t = layout.transform;
        float d = start ? startDistance : endDistance;
        Vector3 eye = t.TransformPoint(new Vector3(0f, 0f, -d));
        eye.y = t.position.y + eyeHeight;
        var tmp = new GameObject("tmp_view");
        tmp.hideFlags = HideFlags.HideAndDontSave;
        tmp.transform.position = eye;
        tmp.transform.rotation = Quaternion.LookRotation(t.forward, Vector3.up);
        sv.orthographic = false;
        sv.cameraSettings.fieldOfView = verticalFov;
        sv.AlignViewToObject(tmp.transform);
        DestroyImmediate(tmp);
        sv.Repaint();
        Debug.Log(string.Format("[ApproachVideo] Scene ビューを{0}（交差点の中心の {1:F1} m 手前、目の高さ {2:F2} m）からの眺めにしました。",
            start ? "スタート地点" : "終点", d, eyeHeight));
    }

    void OnDrawGizmos()
    {
        if (layout == null) return;
        Transform t = layout.transform;
        Vector3 a = t.TransformPoint(new Vector3(0f, 0.1f, -startDistance));
        Vector3 b = t.TransformPoint(new Vector3(0f, 0.1f, -endDistance));
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.4f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(b, 0.4f);
        Gizmos.DrawLine(b, b + Vector3.up * eyeHeight);
    }
#endif
}
