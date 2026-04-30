using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;
public class Look_At_Operator : MonoBehaviour
{
    LookAtIK amimCont;
    GameObject pm;

    // Start is called before the first frame update
    void Start()
    {
        amimCont = GetComponentInChildren<LookAtIK>();
        pm = GameObject.Find("Wheelchair_High");
    }

    // Update is called once per frame
    void Update()
    {                
        float theta_w = pm.transform.eulerAngles.y;                        //車椅子の向いている角度(dec)
        float angle_w = (pm.transform.eulerAngles.y) * (Mathf.PI / 180.0f);//車椅子の向いている角度(rad)
                                                                           //車椅子の角度はx軸正方向が始線
        Vector3 w_dir = new Vector3(Mathf.Cos(angle_w), 0f, Mathf.Sin(angle_w));//車椅子の向いている方向の単位ベクトル
                                                                                //velocityを使わなくても方向が分かる

        float theta_p = (90 - this.transform.eulerAngles.y);//歩行者の向いている角度(dec)
        float angle_p = (90 - this.transform.eulerAngles.y) * (Mathf.PI / 180.0f);//歩行者の向いている角度(rad)
                                                                                 //歩行者の角度はz軸正方向が始線
        Vector3 p_dir = new Vector3(Mathf.Cos(angle_p), 0f, Mathf.Sin(angle_p));//歩行者の向いている方向の単位ベクトル
                                                                                //歩行者はvelocityを使っても方向が分かる

        Vector3 pw = pm.transform.position - this.transform.position;//歩行者から見た車椅子のベクトル
        float d_r = Vector3.Distance(pm.transform.position , this.transform.position); 

        float theta_pw = Vector3.Angle(p_dir, pw);//歩行者の向きと歩行者から見た車椅子の向きの角度差

        float theta_c = 180 - (theta_p - theta_w);//衝突時の進入角度


        //Debug.Log("車椅子の向いている角度θw： " + theta_w);
        //Debug.Log("歩行者の向いている角度θp： " + theta_p);
        //Debug.Log("車椅子から見た歩行者の角度θpw： " + theta_pw);
        //Debug.Log("衝突時の進入角度θc： " + theta_c);


        if (d_r < 10 && theta_pw < 30 )
        {
            amimCont.solver.IKPositionWeight = 1f;
        }
        else
        {
            amimCont.solver.IKPositionWeight = 0f;
        }
    }
}
