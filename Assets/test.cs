using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DSUnityViz;

public class test : MonoBehaviour, IDSCarUpdater
{

    private GameObject light_R, light_L;
    private AudioSource sound01;
    private int count = 0;
    private int first = 0;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}


    public void UpdateByDSInfo(int car_id, CAR_UNIT_INFO car_info)
    {
        //transform.localPosition = new Vector3 (car_info.x, car_info.z, car_info.y);

        //transform.localRotation = DSParam.TransformCoordinateYPR(
        //    car_info.yaw,
        //    car_info.pitch,
        //    car_info.roll
        //);

        if (first == 0)
        {
            first = 1;
            light_R = this.transform.Find("Right").gameObject;
            light_L = this.transform.Find("Left").gameObject;
            light_R.SetActive(false);
            light_L.SetActive(false);
        }

        if (car_info.winkers == 1)
        {
            if (count == 0)
                sound01.PlayOneShot(sound01.clip);
            count++;

            if (count < 30)
                light_R.SetActive(true);
            else
                light_R.SetActive(false);
            if (count > 60) count = 0;
        }
        if (car_info.winkers == 2)
        {
            if (count == 0)
                sound01.PlayOneShot(sound01.clip);
            count++;
            if (count < 30)
                light_L.SetActive(true);
            else
                light_L.SetActive(false);
            if (count > 60) count = 0;
        }
        if (car_info.winkers == 0)
        {
            count = 0;
            light_R.SetActive(false);
            light_L.SetActive(false);
        }
        // something special here
    }
}
