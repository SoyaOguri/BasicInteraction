using UnityEngine;
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

public class HeadRotationReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    public int port = 50005;

    [Header("Settings")]
    [Tooltip("頭頂部平置きの場合は基本的に(0,0,0)でOK。軸が違う場合のみ変更")]
    public Vector3 rotationOffset = new Vector3(0, 0, 0);

    private UdpClient udpClient;
    private Thread receiveThread;

    // データ保持用
    private Quaternion receivedRotation = Quaternion.identity;
    private bool isTouching = false;
    private bool wasTouching = false;

    // ゼロ点補正用
    private Quaternion initialRotation = Quaternion.identity;

    void Start()
    {
        udpClient = new UdpClient(port);
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void Update()
    {
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

        // 4. このGameObjectの回転に適用
        transform.localRotation = relativeRot * Quaternion.Euler(rotationOffset);
    }

    // ゼロ点合わせ処理
    public void ResetHeadPosition()
    {
        Quaternion rawRot = new Quaternion((float)-receivedRotation.x, (float)-receivedRotation.z, (float)-receivedRotation.y, (float)receivedRotation.w);
        initialRotation = rawRot;
        //Debug.Log("Head Position Reset (Zeroed)");
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
                    }

                    // ▼▼▼ ここを修正しました！ ▼▼▼
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