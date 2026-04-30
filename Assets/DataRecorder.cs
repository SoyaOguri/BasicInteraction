using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;


public class DataRecorder : MonoBehaviour {

    public string saveDir;
    public List<EditorTargetList> trackingTarget;

    bool isOpen = false;

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

    // 開始フラグ等のデータ取得用の親オブジェクト(解析時のデータ切り出しに使用 2023.06)
    public GameObject parentObj;
    TimeCount tc; // 開始フラグ取得用
    ShowExpConditions sec; // 試験No取得用

    public Text modeText;
    public Text paarmText;
    public Text gainText;

    int testNo = 0;

    // Use this for initialization
    void Start()
    {
        sec = parentObj.GetComponent<ShowExpConditions>();
        testNo = sec.GetNo();
        OpenFile();
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

        isOpen = true;

        testNo = sec.GetNo();
        dir += @"\data_TestNo" +
            testNo.ToString("D3") +
            "_" +
            System.DateTime.Now.Month.ToString("00") +
            System.DateTime.Now.Day.ToString("00") +
            "_" +
            System.DateTime.Now.Hour.ToString("00") +
            System.DateTime.Now.Minute.ToString("00") +
            System.DateTime.Now.Second.ToString("00") +
            "_" +
            modeText.text +
            "_" +
            paarmText.text +
            "_" +
            gainText.text +
            @".csv";
        sw = new System.IO.StreamWriter(dir, false, System.Text.Encoding.Default);
        time = 0;
        try
        {
            //search FieldInfo
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
        catch
        { }

        try
        {
            //write header
            sw.Write("time");
            for (int i = 0; i < trackingTarget.Count; i++)
            {
                if (trackingTarget[i].isWritePosition)
                {
                    sw.Write("," + i + "_x_pos_" + trackingTarget[i].obj.name);
                    sw.Write("," + i + "_y_pos_" + trackingTarget[i].obj.name);
                    sw.Write("," + i + "_z_pos_" + trackingTarget[i].obj.name);
                }
                if (trackingTarget[i].isWritePosition)
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

        try
        {
            //for(int i = 0; i < DSGlobal.current_car_states.num_of_car_data; i++)
            for (int i = 0; i < 2; i++) // 試験用暫定対策
                {
                    sw.Write(",id_" + i + "_x_pos");
                sw.Write(",id_" + i + "_y_pos");
                sw.Write(",id_" + i + "_z_pos");
                sw.Write(",id_" + i + "_roll");
                sw.Write(",id_" + i + "_pitch");
                sw.Write(",id_" + i + "_yaw");
                sw.Write(",id_" + i + "_steer");
                sw.Write(",id_" + i + "_accel");
                sw.Write(",id_" + i + "_brake");
                sw.Write(",id_" + i + "_winker");
                sw.Write(",id_" + i + "_speed");
            }
        }
        catch { }

        try
        {
            sw.Write("," + "StartFlag");
        }
        catch
        { }

        sw.WriteLine();
    }

    public void OpenFile()
    {
        //open save file
        if (saveDir == "")
        {
            dir = @"C:";
        }
        else
        {
            dir = saveDir;
        }
        OpenFile(dir);
    }

    public void CloseFile()
    {
        if (isOpen)
        {
            isOpen = false;
            if (sw != null)
                sw.Close();
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

    //Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            sec.NextCondition();
            NewFile();
            time = (float)0;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            sec.NextCondition();
            NewFile();
            time = (float)0;
        }
    }

    float ChangeDomain(float degree)
    {
        return degree > 180 ? degree * deg2rad - Mathf.PI * 2 : degree * deg2rad;
    }

    private void FixedUpdate()
    {
        if (!isEnableDir)
            return;

        if (!isOpen)
            return;

        sw.Write(time);
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
                if (trackingTarget[i].isWritePosition)
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

        try
        {
            for (int i = 0; i < DSGlobal.current_car_states.num_of_car_data; i++)
            {
                sw.Write("," + DSGlobal.current_car_states.car_states[i].x.ToString());
                sw.Write("," + DSGlobal.current_car_states.car_states[i].y.ToString());
                sw.Write("," + DSGlobal.current_car_states.car_states[i].z.ToString());
                sw.Write("," + DSGlobal.current_car_states.car_states[i].roll.ToString());
                sw.Write("," + DSGlobal.current_car_states.car_states[i].pitch.ToString());
                sw.Write("," + DSGlobal.current_car_states.car_states[i].yaw.ToString());
                sw.Write("," + DSGlobal.current_car_states.car_states[i].steering.ToString());
                sw.Write("," + DSGlobal.current_car_states.car_states[i].accelpedal.ToString());
                sw.Write("," + DSGlobal.current_car_states.car_states[i].brakepedal.ToString());
                sw.Write("," + DSGlobal.current_car_states.car_states[i].winkers.ToString());
                sw.Write("," + DSGlobal.current_car_states.car_states[i].speed.ToString());
            }
        }
        catch { }

        try
        {
            sw.Write("," + tc.GetGoFlag().ToString());
        }
        catch { }

        sw.WriteLine();

        time += Time.fixedDeltaTime;

    }
}
