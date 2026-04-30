using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossSitu : MonoBehaviour
{

    GameObject pm;
    Vector3 lastPos_w;
    Vector3 lastPos_p;
    Vector3 vel_w;
    Vector3 vel_p;
    [Header("値を入力")]
    [Space(5), Tooltip("MTCの計算を開始するための閾値")] public float thresholdOfTTC;
    [Header("値を観測")]
    [Space(5), Tooltip("電動車椅子との距離")] public float d_r;
    [Space(5), Tooltip("電動車椅子との距離")] public float RelativeAngle;
    [Space(5), Tooltip("すれ違い進入角度")] public float EntryAngle;
    [Space(5), Tooltip("すれ違い発生点")] public Vector3 CrossingPoint;
    [Space(5), Tooltip("電動車椅子の到達時間")] public float t_w;
    [Space(5), Tooltip("歩行者の到達時間")] public float t_p;
    [Space(5), Tooltip("すれ違い余裕時間")] public float MTC;


    // Start is called before the first frame update
    void Start()
    {
        pm = GameObject.Find("Wheelchair_High");
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
        
        Vector3 pos_rel = lastPos_w - lastPos_p;
        RelativeAngle = Vector3.Angle(vel_w, vel_p);
        d_r = pos_rel.magnitude;

        if (t_w > 0 && t_p > 0) //すれ違い前
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
            EntryAngle = 0f;                                //進入角度の値は0にする
        }


                
    }


}