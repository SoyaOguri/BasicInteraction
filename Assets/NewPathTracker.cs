using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewPathTracker : MonoBehaviour
{

    NetworkCharacterVelocity vel;
    GameObject obj;
    public Vector3 targetVel;

    // Start is called before the first frame update
    void Start()
    {
        vel = GetComponent<NetworkCharacterVelocity>();
        obj = GameObject.Find("Wheelchair_High");
    }

    // Update is called once per frame
    void Update()
    {
        if (obj.transform.position.z > -76)
        {
            vel.velocity.x = targetVel.x;
            vel.velocity.z = targetVel.z;
        }
    }
}
