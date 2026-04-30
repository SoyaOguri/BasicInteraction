using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class VIVE_Controller : MonoBehaviour
{
    [SerializeField]
    SteamVR_Input_Sources hand;
    [SerializeField]
    private SteamVR_Action_Vector2 tpad;

    public Vector2 vec;

    NetworkCharacterVelocity vel;

    // Start is called before the first frame update
    void Start()
    {
        vel = (NetworkCharacterVelocity)this.GetComponent(typeof(NetworkCharacterVelocity));
    }

    // Update is called once per frame
    void Update()
    {
        vec = tpad.GetAxis(hand);
        vel.velocity.x = vec.x;
        vel.velocity.z = vec.y;
    }
}
