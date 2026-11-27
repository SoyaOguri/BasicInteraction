using UnityEngine;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// JSONデータ構造
[System.Serializable]
public class ZigSimData { public SensorData sensordata; } // ← OK!

[System.Serializable]
public class SensorData
{
    public QuaternionData quaternion;
    public TouchData[] touch;
}
[System.Serializable]
public class QuaternionData { public double x; public double y; public double z; public double w; }
[System.Serializable]
public class TouchData { public double x; public double y; }

// ZIG SIM（iPhone）の向きを受け取り、このオブジェクトの回転にする（ヘルメットの上の iPhone で首振りを測る）
//
// 0 の合わせ方は 3 通り：
//   ・iPhone の画面をタッチした瞬間に、そのときの向きを 0 にする（向き全体。ヘルメットをかぶった直後に 1 回やる）
//   ・試行ごとの自動ゼロ合わせ：試行が始まって自転車が走り出したら、最初の数秒の首の向き（左右）の中央値を 0 にする
//     （Exp2bTrialManager が試行の始まりに BeginAutoZero を呼ぶ。走り出しの直線では前を見ているはずなので、
//       前の試行の終わりに横を向いていても、次の試行の 0 がずれない。ちらっと横を見ても中央値なので影響は小さい）
//   ・実験者のキーボードの Zero Key（H）を押した瞬間に、そのときの左右の向きを 0 にする
//   自動とキーで直すのは左右（yaw）だけ。上下・傾きはタッチで合わせたままにする
public class HeadRotationReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    public int port = 50005;

    [Header("Settings")]
    [Tooltip("頭頂部平置きの場合は基本的に(0,0,0)でOK。軸が違う場合のみ変更")]
    public Vector3 rotationOffset = new Vector3(0, 0, 0);

    [Header("試行ごとの自動ゼロ合わせ（左右の向き）")]
    [Tooltip("試行が始まったら、自転車が走り出してからの数秒の向きで、左右の 0 を合わせ直す")]
    public bool autoZero = true;
    [Tooltip("この速さ [m/s] を超えたら、走り出したとみなして向きを集め始める")]
    public float zeroStartSpeed = 1.5f;
    [Tooltip("走り出してから何秒分の向きを集めるか（その中央値を 0 にする）")]
    public float zeroWindowSeconds = 2.0f;
    [Tooltip("このキーを押すと、そのときの左右の向きをすぐ 0 にする（実験者用）。" +
             "Z は自転車のセンサのオフセット、Space・Tab・R・G・P・矢印・Enter はほかで使っているので避ける")]
    public KeyCode zeroKey = KeyCode.H;

    [Header("届いていないときの警告")]
    [Tooltip("iPhone から首の向きが届かなくなったら、画面の上に赤い警告を出す")]
    public bool showWarning = true;
    [Tooltip("この秒数より長く届かなかったら警告を出す")]
    public float warnAfterSeconds = 1.0f;
    [Tooltip("警告の文字の大きさ [px]（画面の高さ 1080 px のとき）")]
    public int warnFontSize = 18;

    [Header("確認用（読み取り専用）")]
    [Tooltip("今の左右の 0 の位置 [deg]（タッチで合わせた向きから見て）。記録の headYawZero")]
    [SerializeField] float yawZero;
    [Tooltip("0 を合わせる前の左右の向き [deg]。記録の headYawRaw")]
    [SerializeField] float yawRaw;
    [Tooltip("自動ゼロ合わせの状態")]
    [SerializeField] string zeroState = "";
    [SerializeField] int packets;

    private UdpClient udpClient;
    private Thread receiveThread;

    // データ保持用
    private Quaternion receivedRotation = Quaternion.identity;
    private volatile bool isTouching = false;
    private bool wasTouching = false;
    private int packetCount;          // 受信スレッドが数える
    private int lastPacketCount;

    // ゼロ点補正用
    private Quaternion initialRotation = Quaternion.identity;

    // 自動ゼロ合わせ
    private bool zeroing, collecting;
    private float collectEnd;
    private readonly List<float> samples = new List<float>();
    private System.Func<float> speedSource;

    public float YawZero { get { return yawZero; } }
    public float YawRaw { get { return yawRaw; } }
    public bool Receiving { get; private set; }
    // 警告を出しているか（最後に届いてから warnAfterSeconds より長くたった、または一度も届いていない）
    public bool Lost { get; private set; }

    private float lastReceiveTime = -1f;   // 最後に届いた時刻（-1 = 起動してから一度も届いていない）
    private GUIStyle warnStyle;
    private Texture2D warnBg;

    void Start()
    {
        // 古い既定値の Z がシーンに保存されていても、自転車のセンサのオフセット（Z）とぶつからないよう H に直す
        if (zeroKey == KeyCode.Z) zeroKey = KeyCode.H;
        try
        {
            udpClient = new UdpClient(port);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Head] ポート " + port + " を開けませんでした（ほかのプログラムが使っていませんか）: " + e.Message);
            return;
        }
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void Update()
    {
        // 前のフレームから受信があったか
        int n = packetCount;
        Receiving = n != lastPacketCount;
        lastPacketCount = n;
        packets = n;
        UpdateLost();

        // 1. スマホのタッチを検知した「瞬間」にリセット
        if (isTouching && !wasTouching)
        {
            ResetHeadPosition();
        }
        wasTouching = isTouching;

        // 2. 回転の適用
        // ZIGSIM(Right-handed) -> Unity(Left-handed) 変換
        Quaternion rawRot = new Quaternion((float)-receivedRotation.x, (float)-receivedRotation.z, (float)-receivedRotation.y, (float)receivedRotation.w);

        // 3. ゼロ点補正（初期値との差分を計算）
        Quaternion relativeRot = Quaternion.Inverse(initialRotation) * rawRot;
        yawRaw = Signed(relativeRot.eulerAngles.y);

        // 4. 左右の 0 合わせ（キー・試行ごとの自動）
        if (Input.GetKeyDown(zeroKey)) SetYawZero(yawRaw, "キーで 0 合わせ");
        UpdateAutoZero();

        // 5. このGameObjectの回転に適用（左右の 0 だけ戻す）
        transform.localRotation = Quaternion.Euler(0f, -yawZero, 0f) * relativeRot * Quaternion.Euler(rotationOffset);
    }

    // ゼロ点合わせ処理（タッチ。向き全体を 0 にする）
    public void ResetHeadPosition()
    {
        Quaternion rawRot = new Quaternion((float)-receivedRotation.x, (float)-receivedRotation.z, (float)-receivedRotation.y, (float)receivedRotation.w);
        initialRotation = rawRot;
        yawZero = 0f;
        zeroState = "タッチで 0 合わせ";
        //Debug.Log("Head Position Reset (Zeroed)");
    }

    // 試行の始まりに呼ぶ。speed は自転車の速さ [m/s] を返す関数（null なら、すぐに集め始める）
    public void BeginAutoZero(System.Func<float> speed)
    {
        if (!autoZero) return;
        speedSource = speed;
        zeroing = true;
        collecting = false;
        samples.Clear();
        zeroState = "走り出し待ち";
    }

    void UpdateAutoZero()
    {
        if (!zeroing) return;
        if (!collecting)
        {
            float v = speedSource != null ? speedSource() : float.MaxValue;
            if (v < zeroStartSpeed) return;
            collecting = true;
            collectEnd = Time.time + zeroWindowSeconds;
            zeroState = "向きを集めています";
        }
        // iPhone から届いていないフレームの値は使わない
        if (Receiving) samples.Add(yawRaw);
        if (Time.time < collectEnd) return;
        zeroing = false;
        if (samples.Count < 5)
        {
            zeroState = "自動 0 合わせできず（iPhone から届いていません）";
            Debug.LogWarning("[Head] " + zeroState);
            return;
        }
        SetYawZero(MedianAngle(samples), "自動で 0 合わせ");
    }

    void SetYawZero(float yaw, string why)
    {
        yawZero = yaw;
        zeroState = string.Format("{0}（{1:F1}°）", why, yaw);
        Debug.Log("[Head] " + zeroState);
    }

    // 届いているかを見て、途切れたとき・戻ったときに記録に残す
    void UpdateLost()
    {
        if (Receiving) lastReceiveTime = Time.unscaledTime;
        bool lost = lastReceiveTime < 0f || Time.unscaledTime - lastReceiveTime > warnAfterSeconds;
        // 起動直後（最初の数秒）は、まだ iPhone がつながる前なので記録には残さない（画面の警告は出す）
        if (lost != Lost && Time.unscaledTime > 3f)
        {
            if (lost) Debug.LogWarning("[Head] iPhone から首の向きが届かなくなりました");
            else Debug.Log("[Head] iPhone から首の向きがまた届くようになりました");
        }
        Lost = lost;
    }

    // 警告：画面の上の真ん中に赤い帯で出す（実験者が気づけるように。届けば自動で消える）
    void OnGUI()
    {
        if (!showWarning || !Lost) return;
        if (warnStyle == null)
        {
            warnBg = new Texture2D(1, 1);
            warnBg.SetPixel(0, 0, new Color(0.8f, 0f, 0f, 0.9f));
            warnBg.Apply();
            warnStyle = new GUIStyle(GUI.skin.box);
            warnStyle.normal.background = warnBg;
            warnStyle.normal.textColor = Color.white;
            warnStyle.alignment = TextAnchor.MiddleCenter;
            warnStyle.fontStyle = FontStyle.Bold;
            warnStyle.wordWrap = false;
        }
        float s = Mathf.Max(0.5f, Screen.height / 1080f);
        warnStyle.fontSize = Mathf.RoundToInt(warnFontSize * s);
        string msg = lastReceiveTime < 0f
            ? "首の向きが届いていません（iPhone の ZIG SIM を確認）"
            : string.Format("首の向きが届いていません（{0:F0} 秒前から。iPhone の ZIG SIM を確認）",
                            Time.unscaledTime - lastReceiveTime);
        // 帯の幅は文字に合わせる（左右に少し余白）
        Vector2 size = warnStyle.CalcSize(new GUIContent(msg));
        float w = Mathf.Min(Screen.width, size.x + 30f * s), h = size.y + 12f * s;
        GUI.Box(new Rect((Screen.width - w) * 0.5f, 10f * s, w, h), msg, warnStyle);
    }

    // 角度の中央値（-180/180 をまたいでも崩れないよう、最初の値からの差で並べる）
    static float MedianAngle(List<float> a)
    {
        float b = a[0];
        var d = new List<float>(a.Count);
        foreach (float x in a) d.Add(Mathf.DeltaAngle(b, x));
        d.Sort();
        float m = d.Count % 2 == 1 ? d[d.Count / 2] : 0.5f * (d[d.Count / 2 - 1] + d[d.Count / 2]);
        return Signed(b + m);
    }

    static float Signed(float deg)
    {
        deg = Mathf.Repeat(deg + 180f, 360f) - 180f;
        return deg;
    }

    private void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
        while (true)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string text = Encoding.UTF8.GetString(data);

                // ★実験本番ではログが邪魔ならこの下をコメントアウトしてもOK
                // Debug.Log("受信データ: " + text);

                ZigSimData json = JsonUtility.FromJson<ZigSimData>(text);

                if (json != null && json.sensordata != null)
                {
                    if (json.sensordata.quaternion != null)
                    {
                        var q = json.sensordata.quaternion;
                        receivedRotation = new Quaternion((float)q.x, (float)q.y, (float)q.z, (float)q.w);
                        Interlocked.Increment(ref packetCount);
                    }

                    // 配列に中身があればTrue、空っぽならFalseにする（これで指を離した判定ができる）
                    isTouching = (json.sensordata.touch != null && json.sensordata.touch.Length > 0);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("エラー: " + e.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null) receiveThread.Abort();
        if (udpClient != null) udpClient.Close();
    }
}
