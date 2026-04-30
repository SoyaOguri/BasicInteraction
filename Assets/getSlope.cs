using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class getSlope : MonoBehaviour
{
    public float angle;
    public float degree;
    public int slope;



    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //var direction = this.transform.forward;

        //Debug.Log(direction);

        //float x = direction.x;
        //float z = direction.z;

        //var horizon = new Vector3(x,0,z);
        //horizon /= horizon.magnitude;

        //angle = Mathf.Acos(Vector3.Dot(direction, horizon)) * Mathf.Sign(direction.y);

        //degree = angle * (180 / Mathf.PI);

        //slope = 200f * (angle / Mathf.Atan2(2, 1)); 

        if(Physics.Raycast(transform.position,Vector3.down,out RaycastHit hit))
        {
            angle = Vector3.Angle(Vector3.up, hit.normal);

            degree = angle * (Mathf.PI / 180);

            slope = (int)(100f * (degree / Mathf.Atan2(2, 1)));

        }
        GameObject.Find("FitnessEquipmentDisplay").GetComponent<FitnessEquipmentDisplay>().SetTrainerSlope(slope);
    }
}
