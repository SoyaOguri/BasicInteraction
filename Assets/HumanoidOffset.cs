using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanoidOffset : MonoBehaviour
{

    public GameObject HeadAnchor, LeftHandAnchor, RightHandAnchor, LeftFootAnchor, RightFootAnchor;
    public float HeadPosition = 1.7f, LeftHandPosition = 0.8f, RightHandPosition = 0.8f, LeftFootPosition = 0, RightFootPosition = 0;
    private bool controlFlag = true;
    public GameObject HumanoidObject;
    public GameObject HeadBox;
    public GameObject ParentObject;
    private const float offset = -0.01f, zeroOffset = 60f;

    public bool isDS;

    public int HMDHeadID;

    DSUnityViz.HMD_DS_Imitator imitator;
    MasterObject masObj;
    RootMotion.FinalIK.VRIK vr;

    // Use this for initialization
    void Start()
    {
        GameObject obj = GameObject.Find("MasterObject");
        imitator = obj.GetComponent<DSUnityViz.HMD_DS_Imitator>();
        masObj = obj.GetComponent<MasterObject>();
        vr = HumanoidObject.GetComponent<RootMotion.FinalIK.VRIK>();
        HeadBox.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (isDS)
        {
            if (masObj.updater.receiver.GetLast().car_states[HMDHeadID].speed == 1)
            {
                if (controlFlag == false)
                {
                    SetControl();
                    SetPosition(zeroOffset);
                    controlFlag = true;
                }

            }
            else
            {
                if (controlFlag == true)
                {
                    ReleaseControl();
                    controlFlag = false;
                }

            }
            if (!controlFlag)
            {
                SetOffset(offset);
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                SetPosition();
            }

            try
            {
                if (GameObject.Find("Car01").transform.position.x < 22.7)
                {
                    if (controlFlag)
                    {
                        ReleaseControl();
                        controlFlag = !controlFlag;
                    }
                    else
                    {
                        SetOffset(offset);
                    }

                }
                else
                {
                    if (!controlFlag)
                    {
                        SetControl();
                        SetPosition(zeroOffset);
                        controlFlag = !controlFlag;
                    }
                }
            }
            catch { }

            try
            {

                imitator.sender.list[HMDHeadID].m_speed = controlFlag == true ? 1 : 0;
            }
            catch { }

            if (!controlFlag)
            {
                SetOffset(offset);
            }
        }

    }

    void ReleaseControl()
    {
        vr.enabled = false;
        if (HeadBox != null)
            HeadBox.SetActive(true);
    }

    void SetControl()
    {
        vr.enabled = true;
        if (HeadBox != null)
            HeadBox.SetActive(false);
    }

    void SetOffset(float offset)
    {
        Vector3 vec = ParentObject.transform.position;
        vec.x += offset;
        ParentObject.transform.position = vec;
    }

    void SetPosition(float offset)
    {
        Vector3 vec = ParentObject.transform.position;
        vec.x = offset;
        ParentObject.transform.position = vec;
    }

    void SetPosition()
    {
        Vector3 vec = HeadAnchor.transform.position;
        vec.y = HeadPosition;
        HeadAnchor.transform.position = vec;

        vec = LeftHandAnchor.transform.position;
        vec.y = LeftHandPosition;
        LeftHandAnchor.transform.position = vec;

        vec = RightHandAnchor.transform.position;
        vec.y = RightHandPosition;
        RightHandAnchor.transform.position = vec;

        vec = LeftFootAnchor.transform.position;
        vec.y = LeftFootPosition;
        LeftFootAnchor.transform.position = vec;

        vec = RightFootAnchor.transform.position;
        vec.y = RightFootPosition;
        RightFootAnchor.transform.position = vec;
    }
}