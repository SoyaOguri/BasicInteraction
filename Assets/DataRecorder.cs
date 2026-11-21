using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class DataRecorder : MonoBehaviour
{

    public string saveDir;
    public List<EditorTargetList> trackingTarget;

    bool isOpen = false;

    // いま書き込み中の時系列ファイルのパス（実験2-b の試行記録から参照する。読み取り専用）
    public string CurrentFilePath { get; private set; }

    [System.Serializable]
    public class EditorTargetList
    {
        public GameObject obj;
        public bool isWritePosition = true;
        public bool isWriteRotate = true;
    }

    System.IO.StreamWriter sw;

    [System.Serializable]
    public class EditorDataList
    {
        public string label;
        public string name;
        public int number;
        public FieldInfo info;
        public object obj;

        public EditorDataList()
        {
            name = "";
            number = 0;
            info = null;
            obj = null;
        }
    }

    [HideInInspector, SerializeField]
    public List<EditorDataList> list = new List<EditorDataList>();

    bool isEnableDir = true;
    public int saveDataSize;
    string dir;
    float time;

    const float deg2rad = Mathf.PI / 180;

    public GameObject parentObj;
    TimeCount tc;
    ShowExpConditions sec;
    public GoalLine goalLine;
    public Text modeText;
    public Text paarmText;
    public Text gainText;

    int testNo = 0;

    // ---- 実験2-b 用：試行ごとのファイルを外（Exp2bTrialManager）から開く ----
    //  externallyControlled = true のとき
    //   ・起動時に自分でファイルを開かない（試行が始まるたびに StartTrialFile で開く）
    //   ・マウスの左クリックで新しいファイルにしない
    //   ・各行の time の直後に、試行の条件の列（conditionHeader / conditionValues）を書く
    //   ・行の最後に、毎フレーム変わる列（dynamicHeader / dynamicValues）を書く（例：車が発進したか）
    [HideInInspector] public bool externallyControlled = false;
    string conditionHeader = "";
    string conditionValues = "";
    string dynamicHeader = "";
    System.Func<string> dynamicValues;

    void Start()
    {
        sec = parentObj.GetComponent<ShowExpConditions>();
        if (sec != null) testNo = sec.GetNo();
        if (!externallyControlled) OpenFile();
        tc = parentObj.GetComponent<TimeCount>();
    }

    public void OpenFile(string dir)
    {
        if (!System.IO.Directory.Exists(dir))
        {
            Debug.LogWarning("DataRecoderのデータ保存場所が見つかりません\n今回のデータは保存されません！");
            isEnableDir = false;
            return;
        }

        testNo = sec.GetNo();
        dir += @"\data_TestNo" + testNo.ToString("D3") + "_" + System.DateTime.Now.Month.ToString("00") +
            System.DateTime.Now.Day.ToString("00") + "_" + System.DateTime.Now.Hour.ToString("00") +
            System.DateTime.Now.Minute.ToString("00") + System.DateTime.Now.Second.ToString("00") + "_" +
            modeText.text + "_" + paarmText.text + "_" + gainText.text + @".csv";

        OpenPath(dir);
    }

    // 実験2-b：試行が始まるたびに呼ぶ。dir が無ければ作る。同じ名前のファイルがあれば _2, _3 … を付ける
    //   condHeader / condValues : 「,」区切り（先頭の「,」なし）。試行中ずっと同じ値を毎行に書く
    //   dynHeader / dynValues   : 「,」区切り。毎フレーム dynValues() の結果を行の最後に書く
    public string StartTrialFile(string dirPath, string fileNameWithoutExt,
                                 string condHeader, string condValues,
                                 string dynHeader, System.Func<string> dynValues)
    {
        CloseFile();
        externallyControlled = true;
        conditionHeader = string.IsNullOrEmpty(condHeader) ? "" : "," + condHeader;
        conditionValues = string.IsNullOrEmpty(condValues) ? "" : "," + condValues;
        dynamicHeader = string.IsNullOrEmpty(dynHeader) ? "" : "," + dynHeader;
        dynamicValues = dynValues;
        try
        {
            System.IO.Directory.CreateDirectory(dirPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("DataRecorder: 保存フォルダを作れませんでした（" + dirPath + "）: " + e.Message + "\n今回のデータは保存されません！");
            isEnableDir = false;
            return "";
        }
        string path = System.IO.Path.Combine(dirPath, fileNameWithoutExt + ".csv");
        for (int k = 2; System.IO.File.Exists(path); k++)
            path = System.IO.Path.Combine(dirPath, fileNameWithoutExt + "_" + k + ".csv");
        isEnableDir = true;
        OpenPath(path);
        return path;
    }

    void OpenPath(string path)
    {
        isOpen = true;
        CurrentFilePath = path;
        sw = new System.IO.StreamWriter(path, false, System.Text.Encoding.Default);
        time = 0;

        try
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].name != "None")
                {
                    MonoBehaviour[] components = trackingTarget[list[i].number].obj.GetComponents<MonoBehaviour>();
                    FieldInfo f;
                    foreach (var component in components)
                    {
                        f = component.GetType().GetField(list[i].name);
                        if (f != null)
                        {
                            list[i].info = f;
                            list[i].obj = component;
                            break;
                        }
                    }
                }
                else
                {
                    list[i].info = null;
                }
            }
        }
        catch { }

        try
        {
            sw.Write("time");
            sw.Write(conditionHeader);
            sw.Write(",StartFlag");
            sw.Write(",GoalSuccess");

            for (int i = 0; i < trackingTarget.Count; i++)
            {
                if (trackingTarget[i].isWritePosition)
                {
                    sw.Write("," + i + "_x_pos_" + trackingTarget[i].obj.name);
                    sw.Write("," + i + "_y_pos_" + trackingTarget[i].obj.name);
                    sw.Write("," + i + "_z_pos_" + trackingTarget[i].obj.name);
                }
                // ★修正箇所1：isWritePosition を isWriteRotate に直しました
                if (trackingTarget[i].isWriteRotate)
                {
                    sw.Write("," + i + "_roll_" + trackingTarget[i].obj.name);
                    sw.Write("," + i + "_yaw_" + trackingTarget[i].obj.name);
                    sw.Write("," + i + "_pitch_" + trackingTarget[i].obj.name);
                }
            }
        }
        catch { }

        try
        {
            for (int i = 0; i < list.Count; i++)
            {
                sw.Write("," + list[i].label + "_" + ((MonoBehaviour)list[i].obj).name);
            }
        }
        catch { }

        // ★修正箇所2：昔のシミュレータ用の不要なヘッダー(id_0_x_pos等)を書き出す処理を丸ごと削除しました

        sw.Write(dynamicHeader);
        sw.WriteLine();
    }

    public void OpenFile()
    {
        if (saveDir == "") dir = @"C:";
        else dir = saveDir;
        OpenFile(dir);
    }

    public void CloseFile()
    {
        if (isOpen)
        {
            isOpen = false;
            if (sw != null) sw.Close();
        }
    }

    public void NewFile()
    {
        CloseFile();
        OpenFile();
    }

    private void OnApplicationQuit()
    {
        CloseFile();
    }

    void Update()
    {
        if (!externallyControlled && Input.GetMouseButtonDown(0))
        {
            sec.NextCondition();
            NewFile();
            time = (float)0;
        }
    }

    public void StartNewTrialLog()
    {
        NewFile();
        time = (float)0;
    }

    float ChangeDomain(float degree)
    {
        return degree > 180 ? degree * deg2rad - Mathf.PI * 2 : degree * deg2rad;
    }

    private void FixedUpdate()
    {
        if (!isEnableDir) return;
        if (!isOpen) return;

        sw.Write(time);
        sw.Write(conditionValues);

        try
        {
            string startFlagStr = (tc != null) ? tc.GetGoFlag().ToString() : "0";
            sw.Write("," + startFlagStr);

            int successValue = (goalLine != null && goalLine.isSuccess) ? 1 : 0;
            sw.Write("," + successValue);
        }
        catch { sw.Write(",0,0"); }

        try
        {
            for (int i = 0; i < trackingTarget.Count; i++)
            {
                if (trackingTarget[i].isWritePosition)
                {
                    sw.Write("," + trackingTarget[i].obj.transform.position.x);
                    sw.Write("," + trackingTarget[i].obj.transform.position.y);
                    sw.Write("," + trackingTarget[i].obj.transform.position.z);
                }
                // ★修正箇所3：データ書き込み側も isWriteRotate に直しました
                if (trackingTarget[i].isWriteRotate)
                {
                    sw.Write("," + ChangeDomain(trackingTarget[i].obj.transform.rotation.eulerAngles.x));
                    sw.Write("," + ChangeDomain(trackingTarget[i].obj.transform.rotation.eulerAngles.y));
                    sw.Write("," + ChangeDomain(trackingTarget[i].obj.transform.rotation.eulerAngles.z));
                }
            }
        }
        catch { }

        try
        {
            for (int i = 0; i < list.Count; i++)
            {
                sw.Write("," + list[i].info.GetValue(list[i].obj).ToString());
            }
        }
        catch { }

        // ★修正箇所4：昔のシミュレータ用の不要なデータ(DSGlobal)を書き出す処理を丸ごと削除しました

        if (dynamicValues != null)
        {
            string d;
            try { d = dynamicValues(); } catch { d = ""; }
            sw.Write("," + d);
        }
        sw.WriteLine();
        time += Time.fixedDeltaTime;
    }
}