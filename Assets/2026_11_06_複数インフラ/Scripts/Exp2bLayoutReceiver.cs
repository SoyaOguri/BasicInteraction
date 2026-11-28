using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

// 左・右の画面用ビルドで使う：正面のビルド（Exp2bTrialManager）が UDP で送ってくる交差点の条件を受け取り、
// 自分の IntersectionLayout に同じ条件を適用する。
//
//  ・正面は試行が変わるたびと、その後も Resend Interval ごとに同じ条件を送り続ける
//    → 左右のビルドを後から起動しても、すぐ同じ交差点になる
//  ・車は正面から位置が届き、ModelBank の車（MasterObject の CarMeshes）として表示される。
//    このビルドの中のローカルの車（RightCar / LeftCar）は動かないので、地下に隠しておく
//  ・BicycleSideUpdater が有効なとき（＝左右のビルド）だけ動く。正面とエディタでは自分で止まる
public class Exp2bLayoutReceiver : MonoBehaviour
{
    public IntersectionLayout layout;
    [Tooltip("正面のビルドの Exp2bTrialManager の Sync Port と同じにする")]
    public int port = 30101;
    [Tooltip("左右のビルドで隠すローカルの車。空ならシーン中の CarControllManagement をすべて隠す")]
    public CarControllManagement[] localCarsToHide;

    [Header("ボタンの回数の表示（Setting/exp2b_side.txt の showButtonCount = 1 のビルドだけ）")]
    [Tooltip("このビルドの Setting フォルダの中のこのファイルを読む（右の画面の exe のフォルダにだけ置く）")]
    public string sideConfigFileName = "exp2b_side.txt";
    public bool showButtonCount = false;
    [Tooltip("文字の大きさ（画面の高さ 1080 px のとき）")]
    public int buttonFontSize = 18;
    [Tooltip("出す画面の番号（1 始まり。この exe がつないでいる何番目の画面か）。showDisplayNumbers = 1 にすると各画面に番号が出るので、それで確かめる")]
    public int buttonDisplay = 2;
    [Tooltip("画面の中の位置（0〜1）。x：0 = 左端、1 = 右端。y：0 = 下端、1 = 上端")]
    public float buttonPosX = 0.97f;
    public float buttonPosY = 0.25f;
    [Tooltip("この回数を超えたらオレンジ（0〜この回数は白）")]
    public int expectedPresses = 2;
    [Tooltip("確認用：つないでいる各画面の真ん中に「画面 1」「画面 2」… を大きく出す（どれが何番か確かめるとき）")]
    public bool showDisplayNumbers = false;

    [Header("確認用（読み取り専用）")]
    [SerializeField] string lastId = "（まだ受信していない）";
    [SerializeField] int lastSeq = -1;
    [SerializeField] float secondsSinceLastPacket = -1f;

    UdpClient udp;
    Thread thread;
    volatile bool running;
    readonly object gate = new object();
    string pending, pendingButton;
    string buttonTrial = "";
    int buttonCount = -1;
    UnityEngine.UI.Text buttonText;
    float lastPacketTime = -1f;
    string lastMessage;

    // Start で判定する（シーンの読み込み直後の OnEnable では、他のコンポーネントの有効状態がまだ確定していないことがある）
    void Start()
    {
        // 正面のビルド（とエディタ）では受信しない。正面は自分で条件を決めて送る側
        if (!Exp2bRole.IsSideBuild())
        {
            enabled = false;
            return;
        }
        LoadSideConfig();
        try
        {
            udp = new UdpClient();
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            running = true;
            thread = new Thread(ReceiveLoop) { IsBackground = true };
            thread.Start();
            Debug.Log("[Exp2bLayoutReceiver] UDP " + port + " で条件を待っています");
        }
        catch (Exception e)
        {
            Debug.LogError("[Exp2bLayoutReceiver] UDP " + port + " を開けませんでした: " + e.Message);
        }
        HideLocalCars();
    }

    void OnDisable()
    {
        running = false;
        if (udp != null) { udp.Close(); udp = null; }
        thread = null;
    }

    void ReceiveLoop()
    {
        var ep = new IPEndPoint(IPAddress.Any, 0);
        while (running)
        {
            try
            {
                byte[] data = udp.Receive(ref ep);
                string s = System.Text.Encoding.UTF8.GetString(data);
                if (s.StartsWith(Exp2bButtonMessage.Header)) { lock (gate) { pendingButton = s; } continue; }
                if (!s.StartsWith(Exp2bLayoutMessage.Header)) continue;
                lock (gate) { pending = s; }
            }
            catch (SocketException) { if (!running) return; }
            catch (ObjectDisposedException) { return; }
        }
    }

    void Update()
    {
        string s = null, sb = null;
        lock (gate)
        {
            if (pending != null) { s = pending; pending = null; }
            if (pendingButton != null) { sb = pendingButton; pendingButton = null; }
        }
        if (sb != null)
        {
            string trial; int count;
            if (Exp2bButtonMessage.TryParse(sb, out trial, out count)) { buttonTrial = trial; buttonCount = count; }
        }
        if (s != null)
        {
            lastPacketTime = Time.time;
            int seq;
            TrialCondition c;
            // 通し番号だけで比べると、正面を起動し直したとき（番号が 1 からやり直しになる）に、
            // 前の起動と同じ番号の別の条件（例：セット1の 1 試行目 → セット2の 1 試行目）を無視してしまう。
            // そのため、メッセージ全体（番号＋条件）が前に適用したものと違えば適用する
            string msg = s.Trim();
            if (msg != lastMessage && Exp2bLayoutMessage.TryParse(msg, out seq, out c))
            {
                lastMessage = msg;
                lastSeq = seq;
                lastId = c.id;
                if (layout != null) layout.Apply(c);
                HideLocalCars();
                Debug.Log("[Exp2bLayoutReceiver] 条件を適用: " + c);
            }
        }
        secondsSinceLastPacket = lastPacketTime < 0f ? -1f : Time.time - lastPacketTime;
    }

    // Setting/exp2b_side.txt（このビルドの exe の横の Setting フォルダ）。例）
    //   showButtonCount = 1        # 1 = ボタンの回数を小さく出す（右の画面の exe にだけ置く）
    //   buttonFontSize = 18
    //   buttonCorner = BottomRight
    void LoadSideConfig()
    {
        string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Setting", sideConfigFileName));
        if (!System.IO.File.Exists(path)) return;
        foreach (string raw in System.IO.File.ReadAllLines(path))
        {
            string line = raw;
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line.Substring(0, hash);
            int eq = line.IndexOf('=');
            if (eq < 0) continue;
            string key = line.Substring(0, eq).Trim().ToLowerInvariant(), val = line.Substring(eq + 1).Trim();
            try
            {
                switch (key)
                {
                    case "showbuttoncount": showButtonCount = val == "1" || val.ToLowerInvariant() == "true"; break;
                    case "buttonfontsize": buttonFontSize = int.Parse(val, CultureInfo.InvariantCulture); break;
                    case "buttondisplay": buttonDisplay = int.Parse(val, CultureInfo.InvariantCulture); break;
                    case "buttonposx": buttonPosX = float.Parse(val, CultureInfo.InvariantCulture); break;
                    case "buttonposy": buttonPosY = float.Parse(val, CultureInfo.InvariantCulture); break;
                    case "showdisplaynumbers": showDisplayNumbers = val == "1" || val.ToLowerInvariant() == "true"; break;
                    case "expectedpresses": expectedPresses = int.Parse(val, CultureInfo.InvariantCulture); break;
                }
            }
            catch (Exception e) { Debug.LogWarning("[Exp2bLayoutReceiver] 設定の行を読めませんでした: " + raw + " (" + e.Message + ")"); }
        }
        Debug.Log("[Exp2bLayoutReceiver] 設定を読みました: " + path + "（ボタンの回数の表示 " + (showButtonCount ? "画面 " + buttonDisplay : "なし") + "）");
        if (showButtonCount) buttonText = MakeText(buttonDisplay - 1, buttonFontSize, new Vector2(buttonPosX, buttonPosY), new Vector2(1f, 0.5f));
        if (showDisplayNumbers)
            for (int i = 0; i < Display.displays.Length; i++)
            {
                var t = MakeText(i, 160, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                if (t != null) t.text = "画面 " + (i + 1);
            }
    }

    // 指定した画面（0 始まり）に文字を 1 つ作る。anchor：画面の中の位置（0〜1）、pivot：文字のどこをそこに合わせるか
    UnityEngine.UI.Text MakeText(int display, int size, Vector2 anchor, Vector2 pivot)
    {
        if (display < 0 || display > 7) return null;
        var cgo = new GameObject("Exp2bSideText_Display" + (display + 1));
        DontDestroyOnLoad(cgo);
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = display;
        canvas.sortingOrder = 900;
        var scaler = cgo.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(cgo.transform, false);
        var t = go.AddComponent<UnityEngine.UI.Text>();
        var any = FindObjectOfType<UnityEngine.UI.Text>();
        t.font = any != null && any != t && any.font != null ? any.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var o = go.AddComponent<UnityEngine.UI.Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.85f);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = new Vector2(size * 8f, size * 1.6f);
        rt.anchoredPosition = Vector2.zero;
        t.alignment = pivot.x > 0.75f ? TextAnchor.MiddleRight : pivot.x < 0.25f ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
        t.text = "";
        return t;
    }

    // ボタンの回数を小さく出す（実験者が見る用）：まだ押していなければ「ボタン 0」、1 回で「ボタン 1」、2 回で「ボタン 2」
    void LateUpdate()
    {
        if (buttonText == null) return;
        buttonText.text = buttonCount < 0 ? "" : "ボタン " + buttonCount;
        buttonText.color = buttonCount > expectedPresses ? new Color(1f, 0.6f, 0.1f) : Color.white;
    }

    void HideLocalCars()
    {
        // 左右では ServerCommunicator を無効にするので、見える車は正面から届く ModelBank の車だけになる。
        // シーンの中のローカルの車は動かない（自転車の速度が届かない）ので、地下に隠しておく。
        // 欄が空なら、シーン中の CarControllManagement をすべて隠す
        var cars = (localCarsToHide != null && localCarsToHide.Length > 0)
            ? localCarsToHide
            : FindObjectsOfType<CarControllManagement>();
        foreach (var car in cars) if (car != null) car.SendToUnderground();
    }
}

// 正面 → 左右で送る、ボタンの回数のメッセージ
//   EXP2BBTN|通し番号|試行の表示（例：セット2 14 / 28 試行目）|回数
public static class Exp2bButtonMessage
{
    public const string Header = "EXP2BBTN|";

    public static string Build(int seq, string trial, int count)
    {
        return "EXP2BBTN|" + seq.ToString(CultureInfo.InvariantCulture) + "|" + (trial ?? "").Replace("|", " ") + "|" +
               count.ToString(CultureInfo.InvariantCulture);
    }

    public static bool TryParse(string s, out string trial, out int count)
    {
        trial = ""; count = -1;
        string[] v = s.Trim().Split('|');
        if (v.Length < 4 || v[0] != "EXP2BBTN") return false;
        trial = v[2];
        return int.TryParse(v[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out count);
    }
}

// 正面 → 左右で送る 1 行のメッセージ
//   EXP2B|通し番号|id|norm|info|attention|D_L|D_R|W_L|W_R（W は塀の角の後退距離。-1 なら D から決める）
public static class Exp2bLayoutMessage
{
    public const string Header = "EXP2B|";

    public static string Build(int seq, TrialCondition c)
    {
        var f = CultureInfo.InvariantCulture;
        return string.Join("|", new[]
        {
            "EXP2B", seq.ToString(f), c.id, c.norm.ToString(), c.info.ToString(), c.attention.ToString(),
            c.sightLeft.ToString("F2", f), c.sightRight.ToString("F2", f),
            c.wallSetbackLeft.ToString("F2", f), c.wallSetbackRight.ToString("F2", f),
        });
    }

    public static bool TryParse(string s, out int seq, out TrialCondition c)
    {
        seq = -1; c = null;
        try
        {
            string[] v = s.Trim().Split('|');
            if (v.Length < 8 || v[0] != "EXP2B") return false;
            var f = CultureInfo.InvariantCulture;
            seq = int.Parse(v[1], f);
            c = new TrialCondition
            {
                id = v[2],
                norm = (NormType)Enum.Parse(typeof(NormType), v[3]),
                info = (InfoType)Enum.Parse(typeof(InfoType), v[4]),
                attention = (AttentionType)Enum.Parse(typeof(AttentionType), v[5]),
                sightLeft = float.Parse(v[6], f),
                sightRight = float.Parse(v[7], f),
            };
            if (v.Length >= 10)
            {
                c.wallSetbackLeft = float.Parse(v[8], f);
                c.wallSetbackRight = float.Parse(v[9], f);
            }
            return true;
        }
        catch { return false; }
    }
}
