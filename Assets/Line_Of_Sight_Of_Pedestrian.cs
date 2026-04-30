using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;
public class Line_Of_Sight_Of_Pedestrian : MonoBehaviour
{
    LookAtIK amimCont;
    GameObject sight_dir;

    // Start is called before the first frame update
    void Start()
    {
        amimCont = GetComponentInChildren<LookAtIK>();
        sight_dir = GameObject.Find("line_of_sight_ef");
    }

    // Update is called once per frame
    void Update()
    {
        amimCont.solver.IKPositionWeight = 1f;
    }
}
