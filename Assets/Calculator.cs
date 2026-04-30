using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Calculator : MonoBehaviour
{

    GameObject pm;
    Vector3 lastPos_w;
    Vector3 lastPos_p;
    Vector3 vel_w;
    Vector3 vel_p;
    [Header("値を入力")]
    //テストで一時消してる[Space(5), Tooltip("何人目の歩行者の値であるか")] public int pedIndex;

    [Header("値を観測")]
    [Space(5), Tooltip("電動車椅子との距離")] public float distance;
    [Space(5), Tooltip("電動車椅子との距離")] public float RelativeAngle;
    [Space(5), Tooltip("すれ違い発生点")] public Vector3 CrossingPoint;
    [Space(5), Tooltip("電動車椅子の到達時間")] public float t_w;
    [Space(5), Tooltip("歩行者の到達時間")] public float t_p;



    // Start is called before the first frame update
    void Start()
    {
        pm = GameObject.Find("Wheelchair_High");//車椅子
    }

    void Update()
    {
        float x_w = pm.transform.position.x;
        float z_w = pm.transform.position.z;
        float x_p = this.transform.position.x;
        float z_p = this.transform.position.z;

        vel_w = ((pm.transform.position - lastPos_w) / Time.deltaTime);
        lastPos_w = pm.transform.position;
        //(フレーム間の移動距離)÷(フレーム間の時間)で車椅子の速度求めた

        vel_p = ((this.transform.position - lastPos_p) / Time.deltaTime);
        lastPos_p = this.transform.position;
        //(フレーム間の移動距離)÷(フレーム間の時間)で歩行者の速度求めた

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


        Vector3 pos_rel = lastPos_w - lastPos_p;
        RelativeAngle = Vector3.Angle(vel_w, vel_p);
        distance = pos_rel.magnitude;


        if (t_w > 0 && t_p > 0) //すれ違い前
        {
            CrossingPoint.x = (f * s_p - g * s_w) / e;      
            CrossingPoint.z = (f * u_p - g * u_w) / e;
            //ベクトルの計算で求めた衝突想定点の座標
        }

    }


}