using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HMD_receiver : MonoBehaviour {

    public GameObject HMDCameraAnchor;
    public GameObject CenterEyeAnchor;

    public int port;
    private DSUnityViz.UdpReceiver<DSUnityViz.ShrCarState> receiver = new DSUnityViz.UdpReceiver<DSUnityViz.ShrCarState>();
    private DSUnityViz.ShrCarState shrCarState;
    // Use this for initialization
    void Start () {
        receiver.Open(port);

    }

    private Vector3 tempVec;

    // Update is called once per frame
    void Update () {

        shrCarState = receiver.Get();

        if (shrCarState.m_flg == -225)
        {

//            HMDCameraAnchor.transform.position = new Vector3((float)shrCarState.m_x, (float)shrCarState.m_z, (float)shrCarState.m_y);

            CenterEyeAnchor.transform.rotation = new Quaternion((float)shrCarState.m_brakepdl, (float)shrCarState.m_throtpdl, (float)shrCarState.m_steering, 1);

            CenterEyeAnchor.transform.position = new Vector3((float)shrCarState.m_brake_input, (float)shrCarState.m_throttle_input, (float)shrCarState.m_steering_input);
            HMD_sender.velocity = shrCarState.m_speed;
        }

    }
}
