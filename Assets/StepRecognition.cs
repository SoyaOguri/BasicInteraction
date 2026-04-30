using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StepRecognition : MonoBehaviour {

    public GameObject rightLeg, leftLeg;

    public float groundHight, floatingHight;


    public enum LegState
    {
        Grounding, //接地
        Moving, //足を動かし中
        Floating, //遊脚
    }

    class Leg
    {
        public GameObject obj;
        public LegState state;
        public System.Diagnostics.Stopwatch floatTime = new System.Diagnostics.Stopwatch(),movingTime = new System.Diagnostics.Stopwatch();

    }

    Leg right = new Leg(), left = new Leg();

    // Use this for initialization
    void Start () {
        right.state = LegState.Grounding;
        right.obj = rightLeg;

        left.state = LegState.Grounding;
        left.obj = leftLeg;

    }
	
    public float data;
    public LegState ls, rs;

    int groundingCount = 0;
    float staticVelocity = 0.9f, velocity;
    public GameObject targetObj;
    float stepLength = 0.6f;

    System.Diagnostics.Stopwatch rightSW = new System.Diagnostics.Stopwatch(), leftSW = new System.Diagnostics.Stopwatch();

    // Update is called once per frame
    void Update () {
        LegUpdate(ref right);
        LegUpdate(ref left);
        //data = left.movingTime.ElapsedMilliseconds;
        ls = left.state;
        rs = right.state;
        if(left.state == LegState.Grounding && right.state == LegState.Grounding)
        {
            groundingCount++;
        }
        else
        {
            groundingCount = 0;
            if(left.state != LegState.Grounding)
            {
                if(leftSW.ElapsedMilliseconds != 0)
                {
                    velocity = stepLength*1000/ leftSW.ElapsedMilliseconds;
                }
                else
                {
                    velocity = staticVelocity;
                }
            }
            else
            {
                if (rightSW.ElapsedMilliseconds != 0)
                {
                    velocity = stepLength * 1000 / rightSW.ElapsedMilliseconds;
                }
                else
                {
                    velocity = staticVelocity;
                }
            }
        }

        if (left.state!= LegState.Grounding && right.state == LegState.Grounding)
        {
            if (rightSW.IsRunning == false)
            {
                rightSW.Reset();
                rightSW.Start();
            }

        }
        else
        {
            if (rightSW.IsRunning == true)
                rightSW.Stop();
        }

        if (left.state == LegState.Grounding && right.state != LegState.Grounding)
        {
            if (leftSW.IsRunning == false)
            {
                leftSW.Reset();
                leftSW.Start();
            }

        }
        else
        {
            if (leftSW.IsRunning == true)
                leftSW.Stop();
        }

        if (groundingCount > 20)
        {
            velocity = 0;
            leftSW.Reset();
            rightSW.Reset();
        }
        targetObj.transform.position = (new Vector3(-velocity*Time.deltaTime, 0, 0) + targetObj.transform.position);
    }

    float y;

    void LegUpdate(ref Leg leg)
    {
        y = leg.obj.transform.position.y;

        switch (leg.state)
        {
            case LegState.Grounding:
                if (y > groundHight)
                {
                    leg.state = LegState.Moving;
                    leg.floatTime.Reset();
                    leg.floatTime.Start();
                    leg.movingTime.Reset();
                    leg.movingTime.Start();
                }
                break;
            case LegState.Moving:
                if (y < groundHight)
                {
                    leg.state = LegState.Grounding;
                    leg.floatTime.Stop();
                }
                else if (y > floatingHight)
                {
                    leg.state = LegState.Floating;
                    leg.movingTime.Stop();
                }
                break;
            case LegState.Floating:
                if (y < floatingHight)
                {
                    leg.state = LegState.Moving;
                }
                break;
        }

    }

}
