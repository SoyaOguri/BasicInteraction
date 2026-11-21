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

    [Header("確認用（読み取り専用）")]
    [SerializeField] string lastId = "（まだ受信していない）";
    [SerializeField] int lastSeq = -1;
    [SerializeField] float secondsSinceLastPacket = -1f;

    UdpClient udp;
    Thread thread;
    volatile bool running;
    readonly object gate = new object();
    string pending;
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
                if (!s.StartsWith(Exp2bLayoutMessage.Header)) continue;
                lock (gate) { pending = s; }
            }
            catch (SocketException) { if (!running) return; }
            catch (ObjectDisposedException) { return; }
        }
    }

    void Update()
    {
        string s = null;
        lock (gate) { if (pending != null) { s = pending; pending = null; } }
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
