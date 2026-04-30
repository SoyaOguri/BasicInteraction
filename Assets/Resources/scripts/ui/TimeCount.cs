using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using DSUnityViz;

public class TimeCount : MonoBehaviour
{
    private float time = 0.0f;
    private float vel = 0.0f;
    private Vector3 prePosition;
    public GameObject parentObj;
    public GameObject TargetObj;
    public Text timeText;
    public Text xText;
    public Text velText;
    public Text modeText;
    public Text goText;
    public Text switchText;
    public Text ParamText;
    public Text GainText;
    const float countStartX = 297;
    const float countEndX = 347;
    private bool startFlag = false;
    private bool waitFlag = true;
    float go_disp_time = 0;
    private bool firstProc = true;
    const int averageSize = 25;
    private float[] velBuf = new float[averageSize];
    private int bufIdx = 0;

    BicycleModelCalculator bmc;
    DataRecorder dr;


    /*    float[] car_init_pos = new float[] { 205, 200, 195, 190, 185,
                                         155, 150, 145, 140, 135,
                                         138.33f, 133.33f, 128.33f, 123.33f, 118.33f,
                                         105, 100, 95, 90, 85,
                                         80, 75, 70, 65, 60,
                                         45, 40};*/
    float[] car_init_pos = new float[] { 142, 147, 152, 157, 162,
                                     192, 197, 202, 207, 212,
                                     208.67f, 213.67f, 218.67f, 223.67f, 228.67f,
                                     242, 247, 252, 257, 262,
                                     267, 272, 277, 282, 287,
                                     302, 307};

    string[] mode_text = new string[] {"NoneCorrect", "CnstGain", "EqOfMotion", "DivVel"};

    // Start is called before the first frame update
    void Start()
    {
        prePosition = TargetObj.transform.position;
        ResetFlag();
        modeText.text = mode_text[0];
        dr = parentObj.GetComponent<DataRecorder>();

    }

    // Update is called once per frame
    void Update()
    {
        /* velocity */
        Vector3 position = TargetObj.transform.position;
        float x = TargetObj.transform.position.x;

        if (!Mathf.Approximately(Time.deltaTime, 0))
        {
            Vector3 vel = (position - prePosition) / Time.deltaTime;
            float v = (float)(vel.magnitude * 3.6);
            // average
            float velAverage = 0;
            velBuf[bufIdx] = v;
            bufIdx = bufIdx + 1;
            if (bufIdx >= averageSize)
            {
                bufIdx = 0;
            }
            for (int i = 0; i< averageSize; i++)
            {
                velAverage = velAverage + velBuf[i];
            }
            velAverage = (float)(velAverage / averageSize);
            float roundV = Mathf.Round(velAverage);
            velText.text = roundV.ToString("F1");
        }
        prePosition = position;

        /* time */
        xText.text = x.ToString("F1");
        if ((countStartX <= x) && (x <= countEndX))
        {
            time += Time.deltaTime;
        }
        else if(x < countStartX)
        {
            time = 0;
        }

        if (x > countEndX)
        {
            timeText.text = "Time:" + time.ToString("F1") + "[sec]";
        }
        else
        {
            timeText.text = "";
        }

        /* start timing */
        // get DS car position
        ObjectManager objman = MasterObject.FindObjectOfType<MasterObject>().updater.objman;
        Vector3 car_pos = objman.cars[0].transform.position; // ID0番が車両(暫定的にServerCommunicatorの設定から決め打ち)
        float car_x = car_pos.x;
        if(startFlag)
        {
            go_disp_time += Time.deltaTime;
            if(go_disp_time >= 2)
            {
                goText.text = "";
            }
            else
            {

            }
        }
        else
        {
            for(int i = 0; i < car_init_pos.Length; i++)
            {
                if ((car_init_pos[i] + 0.6 >= car_x) && (car_x >= car_init_pos[i]))
                {
                    startFlag = true;
                    waitFlag = false;
                    goText.color = new Color(0.18f, 0.84f, 0.21f, 1.0f);
                    goText.text = "開始";
                    break;
                }
            }
        }

        if(waitFlag)
        {
            goText.color = new Color(0.85f, 0.18f, 0.20f, 1.0f);
            goText.text = "待機";
        }

        /* Switch */
        BicycleModelCalculator bmc = TargetObj.GetComponent<BicycleModelCalculator>();
        // 最初の1回だけスイッチを押してなくても値が0になるため初回判定のフラグを利用
        if (bmc.judgeSwitchData == 0 && firstProc == false)
        {
            switchText.text = "Switch: On";
        }

        /* Reset */
        if (Input.GetMouseButtonDown(0))
        {
            ResetFlag();
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ResetFlag();
        }

        /* SteerAngleCorrectMode */
        if (Input.GetKeyDown(KeyCode.Space))
        {
            int current_mode = bmc.getSteerCorrectMode();
            int next_mode = (current_mode + 1) % mode_text.Length;
            bmc.setSteerCorrectMode(next_mode);
            modeText.text = mode_text[next_mode];

            dr.NewFile();
        }

        /* Tuning Parameter Set */
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bmc.LoadNextParams();
            string param_file = bmc.getCurrentParam();
            string[] path_split = param_file.Split('\\');
            string file_name = path_split[path_split.Length - 1];
            string[] file_name_split = file_name.Split('.');
            ParamText.text = file_name_split[0];

            dr.NewFile();

        }

        /* Gain Parameter Set */
        if (Input.GetKeyDown(KeyCode.G))
        {
            bmc.LoadNextGains();
            string gain_file = bmc.getCurrentGain();
            string[] path_split = gain_file.Split('\\');
            string file_name = path_split[path_split.Length - 1];
            string[] file_name_split = file_name.Split('.');
            GainText.text = file_name_split[0];

            dr.NewFile();
        }

        /* Parameter Reload */
        if (Input.GetKeyDown(KeyCode.R))
        {
            bmc.ReloadParams();

            string param_file = bmc.getCurrentParam();
            string[] param_path_split = param_file.Split('\\');
            string param_file_name = param_path_split[param_path_split.Length - 1];
            string[] param_file_name_split = param_file_name.Split('.');
            ParamText.text = param_file_name_split[0];

            string gain_file = bmc.getCurrentGain();
            string[] gain_path_split = gain_file.Split('\\');
            string gain_file_name = gain_path_split[gain_path_split.Length - 1];
            string[] gain_file_name_split = gain_file_name.Split('.');
            GainText.text = gain_file_name_split[0];

            dr.NewFile();
        }

        // 初回判定
        if (firstProc == true)
        {
            firstProc = false;
        }
    }
    public void ResetFlag()
    {
        waitFlag = true;
        startFlag = false;
        goText.text = "待機";
        switchText.text = "";
        go_disp_time = 0;
    }

    public int GetGoFlag()
    {
        int flag = 0;
        if(startFlag)
        {
            flag = 1;
        }

        return flag;
    }
}
