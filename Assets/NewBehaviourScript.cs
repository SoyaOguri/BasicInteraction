using UnityEngine;
using UnityEngine.XR;
using System.Collections;

public class CameraController : MonoBehaviour
{
    void Start()
    {

    }
    void Update()
    {
        // TODO: ここで固定したい位置があれば指定しておく
        Vector3 basePos = Vector3.zero;

        // VR.InputTracking から hmd の位置を取得
        Vector3 trackingPos =
                InputTracking.GetLocalPosition(XRNode.CenterEye);

        // CameraController 自体の rotation が
        // zero でなければ rotation を掛ける
        // trackingPosition = trackingPos * transform.rotation;

        // 固定したい位置から hmd の位置を
        // 差し引いて実質 hmd の移動を無効化する
        transform.position = basePos - trackingPos;
    }

}