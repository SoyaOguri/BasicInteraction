using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HMD_sender : MonoBehaviour {
    public GameObject HMDCameraAnchor;
    public GameObject CenterEyeAnchor;
    public int carTypeID;
    private GameObject parent;
    public static  double velocity;
    private float offset;

    private Vector3 tempVec;

    private DSUnityViz.UdpSender<DSUnityViz.ShrCarState> sender = new DSUnityViz.UdpSender<DSUnityViz.ShrCarState>();
    private DSUnityViz.UdpSender<DSUnityViz.ShrCarState> sender2 = new DSUnityViz.UdpSender<DSUnityViz.ShrCarState>();
    private DSUnityViz.ShrCarState shrCarState = new DSUnityViz.ShrCarState();

    public int portToDS;
    public string ipToDS;
    public int portToUnity;
    public string ipToUnity;

    // Use this for initialization
    void Start () {
        sender.Open(ipToDS, portToDS, 12365);
        sender2.Open(ipToUnity, portToUnity, 12366);
    }

	// Update is called once per frame
	void Update () {
        if (HMDCameraAnchor.transform.parent == null)
            return;

        //if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
        //{
        //    //tempVec = CenterEyeAnchor.transform.forward * 0.01f;
        //    //tempVec.y = 0;
        //    //HMDCameraAnchor.transform.position += tempVec;
        //    offset = 0.05f;
        //    velocity = 1;
        //}
        //else
        //{
        //    velocity = 0;
        //    offset = 0;
        //}



        shrCarState.m_x = HMDCameraAnchor.transform.parent.position.x;
        shrCarState.m_y = HMDCameraAnchor.transform.parent.position.z + offset;
        shrCarState.m_z = HMDCameraAnchor.transform.parent.position.y;

        shrCarState.m_speed = velocity;

        shrCarState.m_brakepdl = CenterEyeAnchor.transform.rotation.x;
        shrCarState.m_steering = CenterEyeAnchor.transform.rotation.z;
        shrCarState.m_throtpdl = CenterEyeAnchor.transform.rotation.y;

        shrCarState.m_brake_input = CenterEyeAnchor.transform.position.x;
        shrCarState.m_steering_input = CenterEyeAnchor.transform.position.z;
        shrCarState.m_throttle_input = CenterEyeAnchor.transform.position.y;

        shrCarState.m_flg = -225;

        shrCarState.m_carID = 1;

        shrCarState.m_carTypeID = carTypeID;

        //if (!OVRInput.Get(OVRInput.Button.PrimaryTouchpad))
        //{
        //    sender.Send(shrCarState);
        //    sender2.Send(shrCarState);
        //}
    }
}
