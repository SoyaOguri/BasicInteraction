using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DSUnityViz;
using UnityEngine;

public class DsActRInfomationSender : MonoBehaviour {

    // sender;
    public UdpSender<DataFromUnityToActR> sender;

    // Use this for initialization
    void Start()
    {
        // create data sender
        sender = new UdpSender<DataFromUnityToActR>();
        sender.Open("127.0.0.1", 10002, 10003);
    }

    private int i = 0;

    // Update is called once per frame
    void Update()
    {
        try
        {

            //DSGlobal.current_car_states;
            DataFromUnityToActR toActR = new DataFromUnityToActR();

            toActR.FrameTime =
                DSGlobal.current_car_states.time_info.frame_cnt;
            toActR.simulationTime =
                DSGlobal.current_car_states.time_info.simulated_time;

            float egoCarX = DSGlobal.current_car_states.car_states[0].x;
            float egoCarY = DSGlobal.current_car_states.car_states[0].z;
            float egoCarZ = DSGlobal.current_car_states.car_states[0].y;

            float lCarX = DSGlobal.current_car_states.car_states[1].x;
            float lCarY = DSGlobal.current_car_states.car_states[1].z;
            float lCarZ = DSGlobal.current_car_states.car_states[1].y;


            float ScreenOffsetX = 0.5f;
            float ScreenOffsetY = 0.5f;

            // 10m先のレーン中心のスクリーン座標
            Vector3 nearLaneSp = Camera.main.WorldToViewportPoint(
                new Vector3(egoCarX + 10, 0, 0)
            );
            toActR.nearLaneCenterX = nearLaneSp.x - ScreenOffsetX;
            toActR.nearLaneCenterY = nearLaneSp.y - ScreenOffsetY;

            // 消失点のスクリーン座標(およそ無限遠の位置)
            Vector3 farVanPtSp = Camera.main.WorldToViewportPoint(
                new Vector3(100000, 0, 0)
            );
            toActR.farLaneCenterX = farVanPtSp.x - ScreenOffsetX;
            toActR.farLaneCenterY = farVanPtSp.y - ScreenOffsetY;

            // タンジェントポイント…はて？？
            toActR.tangentPointX = 0.5f - ScreenOffsetX;
            toActR.tangentPointY = 0.5f - ScreenOffsetY;
            
            // 先行車両の中心のスクリーン座標
            Vector3 lcarSp = Camera.main.WorldToViewportPoint(
                new Vector3(lCarX, lCarY, lCarZ)
            );

            toActR.leadCarTailX = lcarSp.x - ScreenOffsetX;
            toActR.leadCarTailY = lcarSp.y - ScreenOffsetY;

            // 先行車までのユークリッド距離
            toActR.leadCarTailD =
                (float)Math.Sqrt(
                    (lCarX - egoCarX) * (lCarX - egoCarX)
                    + (lCarY - egoCarY) * (lCarY - egoCarY)
                );

            toActR.egoX = egoCarX;
            toActR.egoY = egoCarY;
            toActR.egoZ = egoCarZ;


            i++;
            sender.Send(toActR);
        }
        catch
        {
            
        }

    }
}

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
public struct DataFromUnityToActR
{
    public int FrameTime;
    public float simulationTime;

    public float nearLaneCenterX;  //10m先のレーンセンターのスクリーン座標
    public float nearLaneCenterY;

    public float farLaneCenterX;
    public float farLaneCenterY;

    public float tangentPointX;
    public float tangentPointY;

    public float leadCarTailX;  // 先行者の一番後ろScr位置
    public float leadCarTailY;  // 先行者の一番後ろScr位置

    public float leadCarTailD;  // 奥行き
    
    public float egoX;
    public float egoY;
    public float egoZ;


}
