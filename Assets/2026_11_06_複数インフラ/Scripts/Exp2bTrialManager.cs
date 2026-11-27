using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

// 実験2-b（学習用・第1段階）の試行の進行
//
//  ・被験者番号とフェーズ（練習 / セット1〜3）は Setting/exp2b.txt から読む（無ければ Inspector の値）
//    → ビルドしたあとも、テキストを書き換えて起動し直すだけで切り替えられる
//  ・試行が変わるたびに、条件を UDP で左右の画面のビルド（Exp2bLayoutReceiver）へ送る
//  ・Enter で次の試行へ（自転車の位置は BicycleModelCalculator が Enter で初期位置に戻す）
//  ・試行ごとに IntersectionLayout へ条件を適用し、車ありの試行だけ車を出す
//  ・車は、自転車が交差点に着く時刻に合わせて発進させる（自転車の速度から毎フレーム到着時刻を予測）
//  ・記録は保存先（Setting/exp2b.txt の dataDir）の中の被験者フォルダ（P03 など）にまとめる
//      - 試行ごとの時系列：DataRecorder が 1 試行 1 ファイルで書く。各行に試行の条件の列も入る
//          例）P03_Set1_T05_F12_Stop_S19_Mirror_Color_CarL.csv
//      - 試行の一覧（1 試行 1 行）：P03_Set1_summary.csv
//
//  ※ シーンにある従来の TrialManager は Enter を取り合うので、このコンポーネントを使うときは無効にする
public class Exp2bTrialManager : MonoBehaviour
{
    [Header("連携するオブジェクト")]
    public IntersectionLayout layout;
    public CarControllManagement rightCar;
    public CarControllManagement leftCar;
    public BicycleModelCalculator bicycle;
    public GoalLine goalLine;
    public DataRecorder dataRecorder;
    [Tooltip("首振り（ZIG SIM）の受信。空ならシーンから探す。試行の始まりに左右の 0 を自動で合わせ直す")]
    public HeadRotationReceiver headTracker;

    [Header("走行後の評定（テンキー）")]
    [Tooltip("走行のあとに評定 4 問を出すフェーズ（「,」区切り。Practice / Set1 / Set2 / Set3）。" +
             "空なら出さない。Setting/exp2b.txt の ratePhases が優先")]
    public string ratePhases = "Practice,Set1";
    [Tooltip("車が出る試行（ダミー・練習の車あり）でも評定を出すか")]
    public bool rateCarTrials = false;
    [Tooltip("空なら自動で付ける")]
    public Exp2bRatingPanel ratingPanel;

    [Header("実験の設定")]
    [Range(1, Exp2bSchedule.Participants)] public int participant = 1;
    public Exp2bPhase phase = Exp2bPhase.Practice;
    [Tooltip("途中から再開するとき：このフェーズの何試行目から始めるか（1 始まり）")]
    public int startTrial = 1;
    public Exp2bBaseOrder baseOrder = Exp2bBaseOrder.FixedShuffle;
    [Tooltip("練習 3 試行とダミー 4 試行（入れる位置・レイアウト・車の向き）を決める乱数の種。" +
             "0 なら起動のたびに新しく決める（おすすめ）。途中から再開（startTrial > 1）するときは、" +
             "被験者のフォルダの P01_Set1_seed.txt に残した前回の種を自動で使う。Setting/exp2b.txt の seed が優先")]
    public int randomSeed = 0;
    [Tooltip("確認用：この起動で使っている種")]
    [SerializeField] int usedSeed;

    [Header("車ありの試行（ダミー・練習）")]
    [Tooltip("車が自転車より何秒早く交差点（自転車の進路との交点）に着くようにするか。0 = 同時。" +
             "0.5 秒以下だと、車が自転車に近づきすぎて CarControllManagement のブレーキ（Safe Distance）で交差点内に止まりやすい")]
    public float carLeadSeconds = 1.0f;
    [Tooltip("車が自転車の何 m 手前でブレーキをかけて止まるか（CarControllManagement の Safe Distance）。" +
             "負の値なら車の Inspector の値のまま。Setting/exp2b.txt の carSafeDistance が優先")]
    public float carSafeDistance = -1f;

    [Header("衝突の判定（Exp2bCollisionMonitor。空なら自動で付ける）")]
    public Exp2bCollisionMonitor collisionMonitor;

    [Header("校門までの制限時間（GoalLine のカウントダウン）")]
    [Tooltip("自転車が動き出してから校門（GoalZone）を通過するまでの制限時間 [s]。Setting/exp2b.txt の timeLimit が優先。" +
             "止まらずに 5〜5.5 m/s で走れば間に合い、交差点で一時停止すると間に合わない長さにする")]
    public float timeLimit = 25f;

    [Header("画面の試行番号の表示（左下、タイマーの上）")]
    [Tooltip("空なら、GoalLine のタイマーの文字をコピーして自動で作る")]
    public Text progressText;
    public bool showProgress = true;
    [Tooltip("画面左下のタイマー（校門通過制限時間まで）の文字の大きさ。画面の高さ 1080 px のときの大きさ")]
    public int timerFontSize = 20;
    [Tooltip("タイマーの上の試行番号（セット1　5 / 28 試行目）の文字の大きさ。画面の高さ 1080 px のときの大きさ")]
    public int progressFontSize = 14;
    [Tooltip("画面の左端・下端からの余白 [px]（画面の高さ 1080 px のとき）")]
    public float hudMargin = 40f;
    [Tooltip("自転車の到着時刻を予測するときの最低速度 [m/s]（止まっているときに予測が無限大にならないように）")]
    public float minBikeSpeedForEta = 1.0f;

    [Header("記録")]
    [Tooltip("保存先。この中に被験者ごとのフォルダ（P01〜P08）を作る。Setting/exp2b.txt の dataDir が優先。" +
             "フォルダを作れない（D ドライブが無いなど）ときは exe の横の Log/Exp2b に保存する")]
    public string dataDir = @"D:\work\M1_Experiment\BasicInteraction_Experiment\Log\2-b";
    [Tooltip("Unity の編集画面で再生したときは、本番のフォルダ（dataDir）ではなく、下の試し用フォルダに保存する" +
             "（本番の被験者フォルダに試しの走行・評定・種が混ざらないように）。ビルドした exe には関係ない")]
    public bool editorSavesToTestFolder = true;
    [Tooltip("編集画面で再生したときの保存先（プロジェクト直下からの場所）")]
    public string editorTestDir = @"Log\2-b_editor_test";
    [Tooltip("確認用：実際に保存しているフォルダ")]
    [SerializeField] string savingTo = "";

    [Header("設定ファイル（ビルドで被験者・フェーズを切り替える）")]
    [Tooltip("プロジェクト直下（ビルドでは exe の横）の Setting フォルダの中のこのファイルを、再生開始時に読む。" +
             "ファイルが無ければ Inspector の値を使う")]
    public string configFileName = "exp2b.txt";
    [Tooltip("確認用：実際に読み込んだ設定ファイル")]
    [SerializeField] string loadedConfig = "";

    [Header("左右の画面への条件の送信（UDP）")]
    public bool sendLayoutToSideScreens = true;
    [Tooltip("左右のビルドの PC に届く宛先。ブロードキャスト（例 192.168.11.255）か、各 PC の IP")]
    public string syncAddress = "192.168.11.255";
    [Tooltip("左右のビルドの Exp2bLayoutReceiver の Port と同じにする")]
    public int syncPort = 30101;
    [Tooltip("同じ条件を送り直す間隔 [s]（左右のビルドを後から起動しても追いつくように）")]
    public float resendInterval = 0.5f;

    [Header("確認用（読み取り専用）")]
    [SerializeField] int trialNumber;
    [SerializeField] int trialCount;
    [SerializeField] string currentId = "";
    [SerializeField] float bikeEta;
    [SerializeField] float carEta;
    [SerializeField] bool carLaunched;

    List<TrialCondition> trials;
    List<TrialCondition> fullSequence;   // 練習＋セット1〜3（同じレイアウトを何回目に見たかの計算用）
    int fullOffset;                       // このフェーズの 1 試行目が fullSequence の何番目か
    int index = -1;
    bool finished;
    bool trialLogged;
    int ratingEndedFrame = -1;   // 評定が終わったフレーム（同じテンキーの Enter で次の試行を終えないように）
    TimeCount timeCount;   // この試行を summary に書いたか（評定中に終了したとき二重に書かないように）
    CarControllManagement pendingCar;
    float trialStartTime;
    float carLaunchTime = -1f;
    string trialStartClock;
    StreamWriter log;
    System.Net.Sockets.UdpClient udp;
    System.Net.IPEndPoint syncEndPoint;
    int syncSeq;
    float nextResend;

    void Awake()
    {
        // 正面のビルドでは、DataRecorder のファイルをこのコンポーネントが試行ごとに開く
        // （DataRecorder が起動時に自分で開いたり、マウスのクリックで切り替えたりしないようにする）
        if (dataRecorder != null && !Exp2bRole.IsSideBuild()) dataRecorder.externallyControlled = true;
    }

    void Start()
    {
        // 左右のビルドでは何もしない（条件は正面から受け取る。記録も正面だけ）
        if (Exp2bRole.IsSideBuild())
        {
            Debug.Log("[Exp2b] 左右のビルドなので Exp2bTrialManager は止めます（条件は Exp2bLayoutReceiver が受け取る）");
            // 左右の画面には、校門のタイマーや試行番号を出さない（正面だけに出す）
            if (goalLine != null && goalLine.timerText != null) goalLine.timerText.enabled = false;
            if (progressText != null) progressText.enabled = false;
            enabled = false;
            return;
        }
        LoadConfig();
        if (headTracker == null) headTracker = FindObjectOfType<HeadRotationReceiver>();
        timeCount = FindObjectOfType<TimeCount>();
        OpenSync();
        OpenLog();
        usedSeed = DecideSeed();
        trials = Exp2bSchedule.Phase(phase, participant, baseOrder, usedSeed);
        SaveSeed();
        BuildFullSequence();
        trialCount = trials.Count;
        if (goalLine != null) goalLine.SetTimeLimit(timeLimit);
        ApplyCarSafeDistance();
        SetupCollisionMonitor();
        SetupProgressText();
        Debug.Log(string.Format("[Exp2b] 被験者 {0} / {1} / {2} 試行（{3} 試行目から）", participant, phase, trials.Count, startTrial));
        BeginTrial(Mathf.Clamp(startTrial, 1, trials.Count) - 1, true);
    }

    void Update()
    {
        // 評定の画面が出ている間は、Enter で次の試行に進まない（評定が終わると自動で次へ進む）
        bool rating = ratingPanel != null && ratingPanel.Active;
        // 試行を終えるキー：キーボードの Enter か、テンキーの Enter
        //  ・テンキーの Enter は、評定の最後の問いを決めたのと同じフレームでは使わない（次の試行をすぐ終えてしまうため）
        //  ・Shift / Ctrl ＋ テンキーの Enter はカメラの調整（CameraShifter）なので使わない
        bool mod = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
                   Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool returnKey = Input.GetKeyDown(KeyCode.Return);
        bool keypadEnter = !returnKey && !mod && Input.GetKeyDown(KeyCode.KeypadEnter) && Time.frameCount != ratingEndedFrame;
        if ((returnKey || keypadEnter) && !finished && !rating)
        {
            // キーボードの Enter は、自転車（初期位置へ）と TimeCount（待機へ）が自分で戻す。
            // テンキーの Enter では戻らないので、ここで同じことをする
            if (keypadEnter)
            {
                if (bicycle != null) bicycle.ResetToStart();
                if (timeCount != null) timeCount.ResetFlag();
            }
            EndTrial();
            if (NeedsRating(trials[index]))
            {
                // この試行の時系列はここで閉じる（評定の間の走行は記録しない）
                if (dataRecorder != null) dataRecorder.CloseFile();
                HideCars();
                pendingCar = null;   // 評定の間に車が発進しないように
                StartRating();
            }
            else
            {
                NextTrial();
            }
        }
        UpdateCarTiming();
        if (udp != null && index >= 0 && Time.time >= nextResend) SendLayout();
    }

    void NextTrial()
    {
        if (index + 1 >= trials.Count)
        {
            finished = true;
            HideCars();
            if (dataRecorder != null) dataRecorder.CloseFile();
            Debug.Log("[Exp2b] このフェーズの試行はすべて終わりました（" + phase + "）。");
            if (progressText != null) progressText.text = PhaseLabel() + "　終了";
            if (log != null) log.Flush();
        }
        else
        {
            BeginTrial(index + 1, false);
        }
    }

    // ---------------- 走行後の評定（テンキー） ----------------

    bool NeedsRating(TrialCondition c)
    {
        if (string.IsNullOrEmpty(ratePhases)) return false;
        if (!rateCarTrials && c.carSpawn != CarSpawn.None) return false;
        foreach (string p in ratePhases.Split(','))
            if (string.Equals(p.Trim(), phase.ToString(), System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    void StartRating()
    {
        if (ratingPanel == null) ratingPanel = GetComponent<Exp2bRatingPanel>();
        if (ratingPanel == null) ratingPanel = gameObject.AddComponent<Exp2bRatingPanel>();
        if (ratingPanel.font == null && goalLine != null && goalLine.timerText != null) ratingPanel.font = goalLine.timerText.font;

        var c = trials[index];
        int seen; bool prevCar;
        History(out seen, out prevCar);
        string header = "participant,phase,trialInPhase,trialOverall,id,kind,factorialNo," + TrialCondition.ZHeader + ",car,timesSeenThisLayout";
        string values = string.Join(",", new[]
        {
            participant.ToString(), phase.ToString(), trialNumber.ToString(), (fullOffset + index + 1).ToString(),
            c.id, c.kind.ToString(), c.factorialNo.ToString(), c.ZValues(), c.carSpawn.ToString(), seen.ToString(),
        });
        string path = Path.Combine(savingTo, string.Format("P{0:D2}_ratings.csv", participant));
        string title = PhaseLabel() + "　" + TrialLabel() + "　　いま走った交差点について、当てはまるものを選んでください";
        if (progressText != null) progressText.text = "評定中";
        ratingPanel.Begin(path, header, values, title, () =>
        {
            // 評定の間に自転車が動いていても、初期位置から始める
            if (bicycle != null) bicycle.ResetToStart();
            ratingEndedFrame = Time.frameCount;
            NextTrial();
        });
    }

    // ---------------- 設定ファイル ----------------
    //  Setting/exp2b.txt の例（# 以降はコメント）
    //    participant = 3
    //    phase = Set1          # Practice / Set1 / Set2 / Set3
    //    startTrial = 1
    //    baseOrder = FixedShuffle
    //    carLeadSeconds = 1.0
    //    syncAddress = 192.168.11.255
    //    syncPort = 30101

    void LoadConfig()
    {
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Setting", configFileName));
        if (!File.Exists(path))
        {
            loadedConfig = "（" + path + " が無いので Inspector の値を使用）";
            return;
        }
        var f = CultureInfo.InvariantCulture;
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw;
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line.Substring(0, hash);
            int eq = line.IndexOf('=');
            if (eq < 0) continue;
            string key = line.Substring(0, eq).Trim().ToLowerInvariant();
            string val = line.Substring(eq + 1).Trim();
            try
            {
                switch (key)
                {
                    case "participant": participant = Mathf.Clamp(int.Parse(val, f), 1, Exp2bSchedule.Participants); break;
                    case "phase": phase = (Exp2bPhase)System.Enum.Parse(typeof(Exp2bPhase), val, true); break;
                    case "starttrial": startTrial = Mathf.Max(1, int.Parse(val, f)); break;
                    case "seed": randomSeed = int.Parse(val, f); break;
                    case "baseorder": baseOrder = (Exp2bBaseOrder)System.Enum.Parse(typeof(Exp2bBaseOrder), val, true); break;
                    case "carleadseconds": carLeadSeconds = float.Parse(val, f); break;
                    case "carsafedistance": carSafeDistance = float.Parse(val, f); break;
                    case "syncaddress": syncAddress = val; break;
                    case "syncport": syncPort = int.Parse(val, f); break;
                    case "datadir": dataDir = val; break;
                    case "timelimit": timeLimit = float.Parse(val, f); break;
                    case "showprogress": showProgress = val == "1" || val.ToLowerInvariant() == "true"; break;
                    case "sendlayouttosidescreens": sendLayoutToSideScreens = val == "1" || val.ToLowerInvariant() == "true"; break;
                    case "ratephases": ratePhases = val; break;
                    case "timerfontsize": timerFontSize = int.Parse(val, f); break;
                    case "progressfontsize": progressFontSize = int.Parse(val, f); break;
                    case "ratecartrials": rateCarTrials = val == "1" || val.ToLowerInvariant() == "true"; break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Exp2b] 設定ファイルの行を読めませんでした: \"" + raw + "\" (" + e.Message + ")");
            }
        }
        loadedConfig = path;
        Debug.Log(string.Format("[Exp2b] 設定ファイルを読みました: {0}（被験者 {1} / {2} / {3} 試行目から）", path, participant, phase, startTrial));
    }

    // ---------------- 左右の画面へ条件を送る ----------------

    void OpenSync()
    {
        if (!sendLayoutToSideScreens) return;
        try
        {
            udp = new System.Net.Sockets.UdpClient();
            udp.EnableBroadcast = true;
            syncEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(syncAddress), syncPort);
            Debug.Log("[Exp2b] 左右の画面へ条件を送信: " + syncAddress + ":" + syncPort);
        }
        catch (System.Exception e)
        {
            udp = null;
            Debug.LogError("[Exp2b] 左右の画面への送信を開始できませんでした: " + e.Message);
        }
    }

    void SendLayout()
    {
        nextResend = Time.time + resendInterval;
        if (udp == null || index < 0) return;
        try
        {
            byte[] b = System.Text.Encoding.UTF8.GetBytes(Exp2bLayoutMessage.Build(syncSeq, trials[index]));
            udp.Send(b, b.Length, syncEndPoint);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Exp2b] 条件の送信に失敗: " + e.Message);
        }
    }

    void OnDestroy()
    {
        if (udp != null) { udp.Close(); udp = null; }
    }

    // ---------------- 試行の開始と終了 ----------------

    void BeginTrial(int i, bool first)
    {
        index = i;
        var c = trials[index];
        trialNumber = index + 1;
        currentId = c.id;

        if (layout != null) layout.Apply(c);
        syncSeq++;
        SendLayout();
        HideCars();
        pendingCar = c.carSpawn == CarSpawn.Right ? rightCar : c.carSpawn == CarSpawn.Left ? leftCar : null;
        carLaunched = false;
        carLaunchTime = -1f;
        trialStartTime = Time.time;
        trialLogged = false;
        trialStartClock = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        if (!first && goalLine != null) goalLine.ResetGoal();
        if (collisionMonitor != null) collisionMonitor.ResetTrial();
        // 首振りの左右の 0 を、この試行の走り出しの向きで合わせ直す
        if (headTracker != null) headTracker.BeginAutoZero(() => bicycle != null ? bicycle.velocity : float.MaxValue);
        OpenTrialData(c);
        if (progressText != null)
            progressText.text = PhaseLabel() + "　" + TrialLabel();
        Debug.Log(string.Format("[Exp2b] {0} {1}/{2}: {3}", phase, trialNumber, trials.Count, c));
    }

    void EndTrial()
    {
        if (index < 0 || log == null || trialLogged) return;
        trialLogged = true;
        var c = trials[index];
        int fi = fullOffset + index;
        int seen; bool prevCar;
        History(out seen, out prevCar);
        string dataFile = dataRecorder != null ? Path.GetFileName(dataRecorder.CurrentFilePath ?? "") : "";

        var f = CultureInfo.InvariantCulture;
        log.WriteLine(string.Join(",", new[]
        {
            participant.ToString(), phase.ToString(), trialNumber.ToString(), (fi + 1).ToString(),
            c.id, c.kind.ToString(), c.factorialNo.ToString(),
            c.norm.ToString(), c.SightLevel(), c.info.ToString(), c.attention.ToString(),
            c.ZValues(),
            c.carSpawn.ToString(),
            carLaunched ? (carLaunchTime - trialStartTime).ToString("F2", f) : "",
            seen.ToString(), prevCar ? "1" : "0",
            (goalLine != null && goalLine.isSuccess) ? "1" : "0",
            goalLine != null ? goalLine.timeLimit.ToString("F2", f) : "",
            CollisionFlag(),
            (collisionMonitor != null && collisionMonitor.Collided) ? collisionMonitor.CollisionTime.ToString("F2", f) : "",
            collisionMonitor != null ? Meters(collisionMonitor.MinGap) : "",
            (Time.time - trialStartTime).ToString("F2", f),
            trialStartClock, System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            Quote(dataFile),
        }));
        log.Flush();
    }

    void OnApplicationQuit()
    {
        if (!finished) EndTrial();
        if (log != null) { log.Close(); log = null; }
        if (dataRecorder != null) dataRecorder.CloseFile();
    }

    // このレイアウトを（練習から数えて）何回目に見たか、直前の試行に車がいたか
    void History(out int seen, out bool prevCar)
    {
        var c = trials[index];
        int fi = fullOffset + index;
        seen = 0;
        for (int k = 0; k <= fi; k++) if (fullSequence[k].factorialNo == c.factorialNo) seen++;
        prevCar = fi > 0 && fullSequence[fi - 1].carSpawn != CarSpawn.None;
    }

    // 試行の時系列ファイルを開く（DataRecorder に、保存先・ファイル名・条件の列を渡す）
    void OpenTrialData(TrialCondition c)
    {
        if (dataRecorder == null) return;
        int seen; bool prevCar;
        History(out seen, out prevCar);

        string header = "participant,phase,trialInPhase,trialOverall,id,kind,factorialNo," +
                        TrialCondition.ZHeader + ",car,timesSeenThisLayout,prevTrialHadCar";
        string values = string.Join(",", new[]
        {
            participant.ToString(), phase.ToString(), trialNumber.ToString(), (fullOffset + index + 1).ToString(),
            c.id, c.kind.ToString(), c.factorialNo.ToString(),
            c.ZValues(),
            c.carSpawn.ToString(), seen.ToString(), prevCar ? "1" : "0",
        });

        // 例）P03_Set1_T05_F12_Stop_W5_Mirror_Color_CarL
        //   W = 塀の角を建物線から何 m 下げたか（見通しの条件：悪い W0・中 W5・良い W10）。左右で違うときは W0-10 = 左 0 m・右 10 m
        //   塀の位置で決めていない条件（ランダム条件など）は、見通し距離で S11-27 のように書く
        var inv = CultureInfo.InvariantCulture;
        string sight;
        if (c.UsesSetback)
            sight = Mathf.Abs(c.wallSetbackLeft - c.wallSetbackRight) < 0.01f
                ? "W" + c.wallSetbackLeft.ToString("0", inv)
                : "W" + c.wallSetbackLeft.ToString("0", inv) + "-" + c.wallSetbackRight.ToString("0", inv);
        else
            sight = Mathf.Abs(c.sightLeft - c.sightRight) < 0.01f
                ? "S" + c.sightLeft.ToString("0", inv)
                : "S" + c.sightLeft.ToString("0", inv) + "-" + c.sightRight.ToString("0", inv);
        string name = string.Format("P{0:D2}_{1}_T{2:D2}_F{3:D2}_{4}_{5}_{6}_{7}_{8}",
            participant, phase, trialNumber, c.factorialNo,
            c.norm == NormType.None ? "NoStop" : "Stop",
            sight,
            c.info == InfoType.Mirror ? "Mirror" : "NoMirror",
            c.attention == AttentionType.ColorPavement ? "Color" : "NoColor",
            c.carSpawn == CarSpawn.Left ? "CarL" : c.carSpawn == CarSpawn.Right ? "CarR" : "NoCar");

        dataRecorder.StartTrialFile(savingTo, name, header, values,
            "carLaunched,timeLeft,collision,carGap,headYawRaw,headYawZero,headReceiving",
            () => (carLaunched ? "1" : "0") + "," +
                  (goalLine != null ? goalLine.timeLimit.ToString("F2", CultureInfo.InvariantCulture) : "") + "," +
                  CollisionFlag() + "," +
                  (collisionMonitor != null ? Meters(collisionMonitor.CurrentGap) : "") + "," +
                  HeadColumns());
    }

    // ---------------- 衝突の判定と車のブレーキ ----------------

    void SetupCollisionMonitor()
    {
        if (collisionMonitor == null) collisionMonitor = GetComponent<Exp2bCollisionMonitor>();
        if (collisionMonitor == null) collisionMonitor = gameObject.AddComponent<Exp2bCollisionMonitor>();
        collisionMonitor.Init(bicycle != null ? bicycle.transform : null,
                              new[] { rightCar, leftCar }, goalLine);
    }

    void ApplyCarSafeDistance()
    {
        if (carSafeDistance < 0f) return;
        foreach (var car in new[] { rightCar, leftCar })
            if (car != null) car.safeDistance = carSafeDistance;
        Debug.Log("[Exp2b] 車のブレーキ距離（Safe Distance）: " + carSafeDistance + " m");
    }

    // 首振り：0 を合わせる前の左右の向き、そのときの 0 の位置、iPhone から届いているか
    // （解析で 0 を決め直せるように、合わせる前の値も残す。記録の HeadTracker の yaw ≒ headYawRaw − headYawZero）
    string HeadColumns()
    {
        if (headTracker == null) return ",,";
        var f = CultureInfo.InvariantCulture;
        return headTracker.YawRaw.ToString("F2", f) + "," + headTracker.YawZero.ToString("F2", f) + "," +
               (headTracker.Receiving ? "1" : "0");
    }

    string CollisionFlag()
    {
        return (collisionMonitor != null && collisionMonitor.Collided) ? "1" : "0";
    }

    static string Meters(float v)
    {
        return float.IsNaN(v) ? "" : v.ToString("F2", CultureInfo.InvariantCulture);
    }

    // ---------------- 画面の試行番号 ----------------

    string PhaseLabel()
    {
        switch (phase)
        {
            case Exp2bPhase.Practice: return "練習";
            case Exp2bPhase.Set1: return "セット1";
            case Exp2bPhase.Set2: return "セット2";
            default: return "セット3";
        }
    }

    // 練習の試行は「練習 2 / 3」、本番は練習を除いて「5 / 28 試行目」と数える
    string TrialLabel()
    {
        int nPractice = 0;
        foreach (var t in trials) if (t.kind == TrialKind.Practice) nPractice++;
        if (phase != Exp2bPhase.Practice && trials[index].kind == TrialKind.Practice)
            return string.Format("練習 {0} / {1}", trialNumber, nPractice);
        int off = phase == Exp2bPhase.Practice ? 0 : nPractice;
        return string.Format("{0} / {1} 試行目", trialNumber - off, trials.Count - off);
    }

    // 画面左下の文字（タイマーと試行番号）を、画面の大きさに合わせて大きくし、上下に分けて置く
    //   下：校門通過制限時間まで 00:25（GoalLine.timerText）
    //   上：セット1　5 / 28 試行目（タイマーの文字をコピーして作る）
    //   文字の大きさは、画面の高さ 1080 px のときの大きさ（画面が小さければ、その割合で小さくする）
    void SetupProgressText()
    {
        if (goalLine == null || goalLine.timerText == null) return;
        float s = Mathf.Max(0.5f, Screen.height / 1080f);
        Text timer = goalLine.timerText;
        int tSize = Mathf.RoundToInt(timerFontSize * s);
        StyleHudText(timer, tSize);
        var trt = (RectTransform)timer.transform;
        PlaceBottomLeft(trt, hudMargin * s, hudMargin * s, tSize * 2.6f);   // 2 行（「間に合った！」など）まで入る高さ

        if (!showProgress)
        {
            if (progressText != null) progressText.gameObject.SetActive(false);
            progressText = null;
            return;
        }
        if (progressText == null)
        {
            var go = Instantiate(timer.gameObject, timer.transform.parent);
            go.name = "Exp2bProgressText";
            foreach (var mb in go.GetComponents<MonoBehaviour>())
                if (!(mb is Text) && !(mb is UnityEngine.UI.Shadow)) Destroy(mb);
            progressText = go.GetComponent<Text>();
        }
        int pSize = Mathf.RoundToInt(progressFontSize * s);
        StyleHudText(progressText, pSize);
        progressText.color = Color.white;
        progressText.text = "";
        // タイマーの枠のすぐ上（少し間をあける）
        PlaceBottomLeft((RectTransform)progressText.transform, hudMargin * s,
                        hudMargin * s + trt.sizeDelta.y + pSize * 0.4f, pSize * 1.5f);
    }

    static void StyleHudText(Text t, int size)
    {
        t.fontSize = size;
        t.alignment = TextAnchor.LowerLeft;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;   // 折り返さない（折り返すと上の文字に重なる）
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.resizeTextForBestFit = false;
        // 背景が明るくても読めるよう、黒い縁取りを付ける
        var o = t.GetComponent<UnityEngine.UI.Outline>();
        if (o == null) o = t.gameObject.AddComponent<UnityEngine.UI.Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.85f);
        o.effectDistance = new Vector2(2f, -2f);
    }

    static void PlaceBottomLeft(RectTransform rt, float x, float y, float height)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(1600f, height);
    }

    void HideCars()
    {
        if (rightCar != null) rightCar.SendToUnderground();
        if (leftCar != null) leftCar.SendToUnderground();
    }

    // ---------------- 車を自転車の到着に合わせて出す ----------------
    //  車は地下で待たせておき、「自転車が交差点に着くまでの時間」≦「車が初期位置から交差点に着くまでの時間＋先行時間」
    //  になった瞬間に地上の初期位置へ戻す（CarControllManagement は自転車が動いていればすぐ発進する）。
    //  初期位置は交差点から約 110 m 離れているので、出現する瞬間は塀や建物に隠れて見えない。

    void UpdateCarTiming()
    {
        if (pendingCar == null || carLaunched || layout == null || bicycle == null) return;

        Transform L = layout.transform;
        Vector3 car = L.InverseTransformPoint(pendingCar.transform.position);   // 地下にいても x・z は初期位置と同じ
        Vector3 bike = L.InverseTransformPoint(bicycle.transform.position);

        // 交点 = 自転車の進路（ローカル x = 0）と、その車の走行線（ローカル z = 車の z）
        float bikeDist = car.z - bike.z;
        float carDist = Mathf.Abs(car.x);
        float v = Mathf.Max(bicycle.velocity, minBikeSpeedForEta);
        bikeEta = bikeDist / v;
        carEta = carDist / Mathf.Max(pendingCar.speed, 0.1f);

        bool bikeMoving = bicycle.velocity > pendingCar.startThreshold;
        if (bikeMoving && (bikeEta <= carEta + carLeadSeconds || bikeDist <= 0f))
        {
            pendingCar.ResetToSurface();
            carLaunched = true;
            carLaunchTime = Time.time;
            Debug.Log(string.Format("[Exp2b] 車を発進（{0}）：自転車の到着まで {1:F1} s、車の到着まで {2:F1} s",
                pendingCar.name, bikeEta, carEta));
        }
    }

    // ---------------- 記録 ----------------

    void BuildFullSequence()
    {
        fullSequence = new List<TrialCondition>();
        foreach (Exp2bPhase p in new[] { Exp2bPhase.Practice, Exp2bPhase.Set1, Exp2bPhase.Set2, Exp2bPhase.Set3 })
        {
            // 練習は各セットの最初に入っているので、独立の練習フェーズ（Practice）は数えない（それを走っているときだけ数える）
            if (p == Exp2bPhase.Practice && phase != Exp2bPhase.Practice) continue;
            if (p == phase) fullOffset = fullSequence.Count;
            // ほかのセットの並び：そのセットで使った種が残っていればそれ、無ければ既定の種で近似する
            int sd = p == phase ? usedSeed : (ReadSeed(p) ?? Exp2bSchedule.DefaultSeed(participant, p));
            fullSequence.AddRange(Exp2bSchedule.Phase(p, participant, baseOrder, sd));
        }
    }

    // ---------------- 練習とダミーの乱数の種 ----------------
    //  P01_Set1_seed.txt に 1 起動 1 行で追記する：日時,種,試行の id（並び順）
    //  途中から再開（startTrial > 1）するときは、最後の行の種を使って同じ並びを作り直す

    string SeedPath(Exp2bPhase p)
    {
        return Path.Combine(savingTo, string.Format("P{0:D2}_{1}_seed.txt", participant, p));
    }

    int DecideSeed()
    {
        if (phase == Exp2bPhase.Practice) return 0;
        if (randomSeed != 0)
        {
            Debug.Log("[Exp2b] 設定の種を使います: " + randomSeed);
            return randomSeed;
        }
        if (startTrial > 1)
        {
            int? prev = ReadSeed(phase);
            if (prev.HasValue)
            {
                Debug.Log("[Exp2b] 途中から再開するので、前回の種を使います: " + prev.Value);
                return prev.Value;
            }
            Debug.LogWarning("[Exp2b] 途中から再開しますが、前回の種（" + SeedPath(phase) + "）が無いので、新しい並びになります");
        }
        return new System.Random().Next(1, int.MaxValue);
    }

    int? ReadSeed(Exp2bPhase p)
    {
        try
        {
            string path = SeedPath(p);
            if (!File.Exists(path)) return null;
            string[] lines = File.ReadAllLines(path);
            for (int k = lines.Length - 1; k >= 0; k--)
            {
                string[] v = lines[k].Split(',');
                int sd;
                if (v.Length >= 2 && int.TryParse(v[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out sd) && sd != 0)
                    return sd;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Exp2b] 種のファイルを読めませんでした: " + e.Message);
        }
        return null;
    }

    void SaveSeed()
    {
        if (phase == Exp2bPhase.Practice) return;
        try
        {
            string path = SeedPath(phase);
            if (!File.Exists(path))
                File.WriteAllText(path, "clock,seed,startTrial,order\r\n", new System.Text.UTF8Encoding(true));
            var ids = new List<string>();
            foreach (var t in trials) ids.Add(t.id);
            File.AppendAllText(path, string.Format("{0},{1},{2},{3}\r\n",
                System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), usedSeed, startTrial, string.Join(" ", ids.ToArray())),
                new System.Text.UTF8Encoding(false));
            Debug.Log("[Exp2b] 練習とダミーの種 " + usedSeed + "：" + string.Join(" ", ids.ToArray()));
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Exp2b] 種を保存できませんでした: " + e.Message);
        }
    }

    void OpenLog()
    {
        string sub = string.Format("P{0:D2}", participant);
        string root = dataDir;
#if UNITY_EDITOR
        if (editorSavesToTestFolder)
        {
            root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", editorTestDir));
            Debug.LogWarning("[Exp2b] 編集画面での再生なので、試し用のフォルダに保存します: " + root +
                             "（本番の " + dataDir + " には書きません）");
        }
#endif
        string dir = Path.Combine(root, sub);
        try
        {
            Directory.CreateDirectory(dir);
        }
        catch (System.Exception e)
        {
            string fallback = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Log", "Exp2b", sub));
            Debug.LogError("[Exp2b] 保存先 " + dir + " を作れなかったので " + fallback + " に保存します: " + e.Message);
            dir = fallback;
            Directory.CreateDirectory(dir);
        }
        savingTo = dir;
        string baseName = string.Format("P{0:D2}_{1}_summary", participant, phase);
        string path = Path.Combine(dir, baseName + ".csv");
        for (int k = 2; File.Exists(path); k++) path = Path.Combine(dir, baseName + "_" + k + ".csv");
        log = new StreamWriter(path, false, new System.Text.UTF8Encoding(true));
        log.WriteLine("participant,phase,trialInPhase,trialOverall,id,kind,factorialNo,norm,sight,info,attention," +
                      TrialCondition.ZHeader +
                      ",car,carLaunchSec,timesSeenThisLayout,prevTrialHadCar,goalSuccess,timeLeftSec,collision,collisionSec,minCarGap,durationSec,startClock,endClock,dataFile");
        log.Flush();
        Debug.Log("[Exp2b] 試行の記録: " + path);
    }

    static string Quote(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    // ---------------- 全員ぶんの試行順を書き出す（確認・資料用） ----------------

    // ※ 練習とダミーは起動のたびに変わるので、ここで書き出すのは既定の種で作った一例（学習用 24 条件の順番は本番と同じ）
    [ContextMenu("全被験者の試行順を CSV に書き出す")]
    void ExportSchedule()
    {
        string path = Path.Combine(Application.dataPath, "2026_11_06_複数インフラ", "Conditions", "exp2b_schedule.csv");
        using (var w = new StreamWriter(path, false, new System.Text.UTF8Encoding(true)))
        {
            w.WriteLine("participant,phase,order,id,kind,factorialNo,norm,sight,info,attention,car");
            for (int p = 1; p <= Exp2bSchedule.Participants; p++)
            {
                foreach (Exp2bPhase ph in new[] { Exp2bPhase.Practice, Exp2bPhase.Set1, Exp2bPhase.Set2, Exp2bPhase.Set3 })
                {
                    var list = Exp2bSchedule.Phase(ph, p, baseOrder, Exp2bSchedule.DefaultSeed(p, ph));
                    for (int i = 0; i < list.Count; i++)
                    {
                        var c = list[i];
                        w.WriteLine(string.Join(",", new[]
                        {
                            p.ToString(), ph.ToString(), (i + 1).ToString(), c.id, c.kind.ToString(), c.factorialNo.ToString(),
                            c.norm.ToString(), c.SightLevel(), c.info.ToString(), c.attention.ToString(), c.carSpawn.ToString(),
                        }));
                    }
                }
            }
        }
        Debug.Log("[Exp2b] 試行順を書き出しました: " + path);
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}
