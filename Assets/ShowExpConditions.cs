using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

// 試験条件
class Conditions
{
    public int no = 0;                     // 試験番号
    public string cyclist_state = "";      // サイクリストの心理状態(通常 or 急ぎ)
    public string vehicle_vel = "";        // 指示車速
    public float vehicle_init_pos = 0.0f;  // 車両初期位置
    public string vehicle_path = "";       // 車両通過方法(左折 or 直進)

};


public class ShowExpConditions : MonoBehaviour
{
    public Text condition_text;

    private List<Conditions> conditions = new List<Conditions>();
    public int current_no = 0;

    public GameObject parentObj;
    DataRecorder dr;

    string file;

    // Start is called before the first frame update
    void Start()
    {
        string dir = Directory.GetCurrentDirectory();
//        condition_text.text = dir;
        file = ".\\Conditions\\TestPatternList.csv";
//        file = @".\Conditions\TestPatternList.csv";
        //        file = Application.dataPath + @"\..\Conditions\TestPatternList.csv";
        ReadFile(file);

        dr = parentObj.GetComponent<DataRecorder>();

    }

    // Update is called once per frame
    void Update()
    {

        // 上矢印↑で前に戻る
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            PreviousCondition();
            dr.NewFile(); // 試験条件が変わったらファイル名を変更新しいログファイルを生成
        }
        // 下矢印↓次に進む
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            NextCondition();
            dr.NewFile(); // 試験条件が変わったらファイル名を変更新しいログファイルを生成
        }

        string show_text = "";
        if(conditions.Count > 0)
        {
            // 左折巻き込み試験用
            //show_text = "試験No:" + conditions[current_no].no.ToString() + "\n" + "状態：" + conditions[current_no].cyclist_state;
            // ふらつき再現評価用
            show_text = "試験No:" + (current_no + 1).ToString("d3");
        }
        else
        {
            show_text = "Can't read " + file;
        }
        condition_text.text = show_text;

    }

    /*
     * csvファイルからコース(経路)情報取得
     */
    void ReadFile(string filepath)
    {

        try
        {
            if(File.Exists(filepath))
            {
                //condition_text.text = "Exist " + filepath;
                //読み込むcsvファイルのパスを指定して開く
                //using (StreamReader sr = new StreamReader(filepath, System.Text.Encoding.GetEncoding("shift_jis")))
                using (StreamReader sr = new StreamReader(filepath))
                //using (StreamReader sr = new StreamReader(filepath, System.Text.Encoding.GetEncoding("utf-8")))
                {
                    //condition_text.text = "Create StreamReader Object";
                    // 最初の1行はヘッダとして読み飛ばす
                    string header = sr.ReadLine();
                    while (!sr.EndOfStream)
                    {
                        // 2行目から1行づつ読み取る
                        string line = sr.ReadLine();
                        // カンマで区切る
                        string[] values = line.Split(',');

                        // csvファイルのフォーマット(x,y,z,roll,pitch,yaw,dist)
                        int n = int.Parse(values[0]);
                        string cs = values[1];
                        string vv = values[2];
                        float vip = float.Parse(values[3]);
                        string vp = values[4];

                        Conditions condition = new Conditions();
                        condition.no = n;
                        condition.cyclist_state = cs;
                        condition.vehicle_vel = vv;
                        condition.vehicle_init_pos = vip;
                        condition.vehicle_path = vp;
                        conditions.Add(condition);

                    }
                }
            }
            else
            {
                condition_text.text = "Not exist " + filepath;
            }
        }
        catch (System.Exception e)
        {
        }
    }

    public void NextCondition()
    {
        current_no = current_no + 1;

        /* ふらつき再現評価用にコメントアウト 2024.10.10
        int num = conditions.Count;
        if (current_no > num - 1)
        {
            current_no = num - 1;
        }
        */
    }

    public void PreviousCondition()
    {
        current_no = current_no - 1;
        if (current_no < 0)
        {
            current_no = 0;
        }
    }

    public int GetNo()
    {
        return current_no + 1; // 試験番号(index + 1)を返す
    }

}
