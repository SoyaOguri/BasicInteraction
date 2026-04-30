using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BicycleSideUpdater : MonoBehaviour
{
    
    public int carID;


    DSUnityViz.CAR_UNIT_INFO carU;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        carU = DSGlobal.current_car_states.car_states[carID];
        
        var roll = carU.pitch ;

        if(roll> 3)
        {
            roll = -(2 * Mathf.PI - roll);
        }

        
        //Debug.Log(carU.pitch);
        //Debug.Log(roll);
        this.transform.position = new Vector3(carU.x, carU.z, carU.y);
        this.transform.rotation = Quaternion.Euler((float)(roll * 180 / Mathf.PI), -carU.yaw * 180 / Mathf.PI - 90, 0);
        //this.transform.rotation = Quaternion.Euler((float)(roll * 180 / Mathf.PI), -carU.yaw * 180 / Mathf.PI, 0);

        //if(roll>=0)
        //{
        //    this.transform.rotation = Quaternion.Euler((float)(roll * 180 / Mathf.PI), -carU.yaw * 180 / Mathf.PI - 90, 0);
        //}
        //else
        //{
        //    this.transform.rotation = Quaternion.Euler((float)(360+ roll * 180 / Mathf.PI), -carU.yaw * 180 / Mathf.PI - 90, 0);
        //}
        //var rotX = Quaternion.AngleAxis((float)roll * 180 / Mathf.PI, new Vector3(1, 0, 0));
        //var rotY = Quaternion.AngleAxis(carU.yaw * 180 / Mathf.PI, new Vector3(0, 1, 0));

        //this.transform.rotation = rotX * rotY;

        //if (objman.cars.Count > 0)
        //{



        //for (int i = 0; i < HMDAnchors.Count; i++)
        //{
        //    if (HMDAnchors[i].traceTarget != null)
        //    {
        //        targetObj = objman.cars[HMDAnchors[i].id];
        //        HMDAnchors[i].traceTarget.transform.position = targetObj.transform.position;
        //        HMDAnchors[i].traceTarget.transform.rotation = targetObj.transform.rotation;
        //    }

        //}
        //}
    }
}
