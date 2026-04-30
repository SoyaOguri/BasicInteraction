using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ByMTC: MonoBehaviour
{

    GameObject pm;
    Vector3 lastPos_w;
    Vector3 lastPos_p;
    Vector3 vel_w;
    Vector3 vel_p;
    [Header("値を入力")]
    [Space(5), Tooltip("何人目の歩行者の値であるか")] public int pedIndex;
    [Space(5), Tooltip("MTCの計算を開始するための閾値")] public float thresholdOfTTC;
    [Space(5), Tooltip("HMIを起動する閾値")] public float thresholdOfMTC1;
    [Space(5), Tooltip("HMIを起動する閾値")] public float thresholdOfMTC2;
    [Header("値を観測")]
    [Space(5), Tooltip("電動車椅子との距離")] public float Distance;
    [Space(5), Tooltip("歩行者の存在角度")] public float RelativeAngle1;
    [Space(5), Tooltip("左右を考慮した歩行者の存在角度")] public float RelativeAngle2;
    [Space(5), Tooltip("交差時の進入角度")] public float EntryAngle;
    [Space(5), Tooltip("交差点座標")] public Vector3 CrossingPoint;
    public float t_w;
    public  float t_p;
    [Space(5), Tooltip("すれ違い余裕時間")] public float MTC;
    [Space(5), Tooltip("重み")] public float s;
    [Space(5), Tooltip("優先度")] public float score;


    GameObject refObj;
    ControlSphere startScript;


    // Start is called before the first frame update
    void Start()
    {
        pm = GameObject.Find("Wheelchair_High");
        refObj = GameObject.Find("Sphere");
        startScript = refObj.GetComponent<ControlSphere>();

    }

    void Update()
    {
        float x_w = pm.transform.position.x;
        float z_w = pm.transform.position.z;
        float x_p = this.transform.position.x;
        float z_p = this.transform.position.z;

        vel_w = ((pm.transform.position - lastPos_w) / Time.deltaTime);
        //(フレーム間の移動距離)÷(フレーム間の時間)
        lastPos_w = pm.transform.position;

        vel_p = ((this.transform.position - lastPos_p) / Time.deltaTime);
        lastPos_p = this.transform.position;

        float s_w = vel_w.x;
        float u_w = vel_w.z;
        float s_p = vel_p.x;
        float u_p = vel_p.z;

        float a = x_w - x_p;
        float b = z_w - z_p;
        float c = s_w - s_p;
        float d = u_w - u_p;
        float e = s_p * u_w - s_w * u_p;
        float f = x_w * u_w - z_w * s_w;
        float g = x_p * u_p - z_p * s_p;

            t_w = (a * u_p - b * s_p) / e;
            t_p = (a * u_w - b * s_w) / e;



        Vector3 pos_rel = this.transform.position - pm.transform.position;
        Vector3 dir_pos_rel = pos_rel.normalized;
        float angle_w = (pm.transform.eulerAngles.y + 90.0f) * (Mathf.PI / 180.0f) % (2 * Mathf.PI);
        //車椅子の向いている角度(rad)
        //車椅子の角度はx軸正方向が始線
        Vector3 dir_w = new Vector3(Mathf.Sin(angle_w), 0f, Mathf.Cos(angle_w));//車椅子の向いている方向の単位ベクトル
        RelativeAngle1 = Vector3.Angle(dir_pos_rel, dir_w);

        Vector3 up = new Vector3(0, 1, 0);
        var cosTheta = Vector3.Dot(dir_pos_rel, dir_w);
        var cross = Vector3.Cross(dir_pos_rel, dir_w - dir_pos_rel);
        var isClockwise = Vector3.Dot(cross, up) > 0f;
        RelativeAngle2 = isClockwise ?
        Mathf.Acos(cosTheta) * Mathf.Rad2Deg :
        -Mathf.Acos(cosTheta) * Mathf.Rad2Deg;

        Distance = pos_rel.magnitude;

        //score = MTC;
        //score = RelativeAngle1;
        //score = EntryAngle;
        score = s * (-2 * MTC) + (-3.5f * (Mathf.Cos(RelativeAngle1)));



        if (t_w >= 0 && t_p >= 0) //すれ違い前
        {
            CrossingPoint.x = (f * s_p - g * s_w) / e;      //衝突想定点のx座標
            CrossingPoint.z = (f * u_p - g * u_w) / e;      //衝突想定点のz座標
            EntryAngle = 180 - Vector3.Angle(vel_w, vel_p); //進入角度

            if (t_w < thresholdOfTTC && t_p < thresholdOfTTC)//TTCがこのスクリプトで決めた閾値以下になれば
            {
                MTC = Mathf.Abs(t_w - t_p);                  //MTCの計算を開始  
            }

            else
            {
                MTC = thresholdOfTTC;                        //MTCの(初期)値はMTCの計算を開始する閾値と同じにする
            }
        }

        else                    //すれ違い後
        {
            MTC = thresholdOfTTC;                           //MTCの値はMTCの計算を開始する閾値と同じにする
            RelativeAngle1 = 0f;                            //進入角度の値は0にする
            EntryAngle = 0f;
        }

        //ここまでは値の調整
        //ここから先でHMI起動

        startScript.pedestrianLightState[pedIndex].isVisible = true;//MTCの初期値を考慮

        if (MTC < thresholdOfMTC1)
        {
            startScript.pedestrianLightState[pedIndex].isVisible = true;
            startScript.pedestrianLightState[pedIndex].color = Color.yellow;
            startScript.pedestrianLightState[pedIndex].direction = RelativeAngle2 * Mathf.PI / 180;
            startScript.pedestrianLightState[pedIndex].priority = score;

            if (MTC < thresholdOfMTC2)
            {
                startScript.pedestrianLightState[pedIndex].isVisible = true;
                startScript.pedestrianLightState[pedIndex].color = Color.red;
                startScript.pedestrianLightState[pedIndex].direction = RelativeAngle2 * Mathf.PI / 180;
                startScript.pedestrianLightState[pedIndex].priority = score;
            }
        }


        else
        {
            startScript.pedestrianLightState[pedIndex].isVisible = false;
        }

    }


}