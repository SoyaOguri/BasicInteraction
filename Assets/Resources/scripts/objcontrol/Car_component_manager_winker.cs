using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DSUnityViz;
using System;

public class Car_component_manager_winker : MonoBehaviour
{
    private GameObject parentObj;

    CharacterController characterController;
    Animator animator;
    SharedMemoryManager<PacketToDSViz> shrMem;
    GameObject ChildObject_FL;
    GameObject ChildObject_FR;
    GameObject ChildObject_RT;
    GameObject car_position;
    WinkerState ws;

    private float R_i = 1 / 0.3f;
    private float last_x;
    private float last_y;
    private float last_z;

    // Use this for initialization
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        ChildObject_FL = gameObject.transform.Find("FTIREL").gameObject;
        ChildObject_FR = gameObject.transform.Find("FTIRER").gameObject;
        ChildObject_RT = gameObject.transform.Find("RTIRE").gameObject;
        car_position = gameObject.transform.parent.gameObject;
        ws = gameObject.GetComponentInParent<WinkerState>();
    }

    // Update is called once per frame
    void Update()
    {
        //rotate car wheel
        Vector3 rot_FL = ChildObject_FL.transform.localEulerAngles;
        Vector3 rot_FR = ChildObject_FR.transform.localEulerAngles;
        Vector3 rot_R = ChildObject_RT.transform.localEulerAngles;
        Vector3 pos = car_position.transform.position;

        float delta = (pos.x - last_x) * (pos.x - last_x) + (pos.y - last_y) * (pos.y - last_y) + (pos.z - last_z) * (pos.z - last_z);
        float FL = (float)Math.Sqrt(delta * 400) * R_i;
        float FR = FL;
        float RT = FL;

        ChildObject_FL.transform.localEulerAngles = new Vector3(rot_FL.x, rot_FL.y, rot_FL.z - FL);
        ChildObject_FR.transform.localEulerAngles = new Vector3(rot_FR.x, rot_FR.y, rot_FR.z - FR);
        ChildObject_RT.transform.localEulerAngles = new Vector3(rot_R.x, rot_R.y, rot_R.z - RT);

        last_x = pos.x;
        last_y = pos.y;
        last_z = pos.z;

        //manage winker animation
        //if (shrMem.data.car_states[0].winkers == 1 || shrMem.data.car_states[0].winkers == -1)
        ////if (R_i > 0)
        //{
        //    animator.SetBool("winker", true);
        //}
        //else
        //{
        //    animator.SetBool("winker", false);
        //}

        if (ws.winkers == 1)
        {
            animator.SetBool("winkerR", true);
            animator.SetBool("winkerL", false);
        }
        else if (ws.winkers == 2)
        {
            animator.SetBool("winkerR", false);
            animator.SetBool("winkerL", true);
        }
        else
        {
            animator.SetBool("winkerR", false);
            animator.SetBool("winkerL", false);
        }
    }
}
