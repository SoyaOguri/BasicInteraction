using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackerController : MonoBehaviour
{
    public GameObject baseObject, rightLeg, leftLeg;
    public GameObject velocityObject;

    public float velocity;
    public Vector3 direction;

    NetworkCharacterVelocity ncv;

    class LegVelocity
    {
        float length, lastLength;
        Vector3 vec;

        public float velocity;
        MovingAvarage filter = new MovingAvarage(10);

        public void SetObject(GameObject baseObj, GameObject leg, float dt)
        {
            vec = baseObj.transform.position - leg.transform.transform.position;
            lastLength = length;
            length = vec.magnitude;

            filter.SetData((lastLength - length) / dt);
            velocity = filter.data;
        }
    }
    LegVelocity rightLegVel, leftLegVel;

    class MovingAvarage
    {
        List<float> list;
        public float data;
        int listSize;
        int count = 0;

        public MovingAvarage(int size)
        {
            list = new List<float>();
            listSize = size;
            for (int i = 0; i < size; i++)
            {
                list.Add(0);
            }
        }

        public void SetData(float data)
        {
            list[count] = data;
            count++;
            if(count >= listSize)
            {
                count = 0;
            }

            this.data = 0;

            for (int i = 0; i < listSize; i++)
            {
                this.data += list[i];
            }
            this.data /= listSize;
        }
    }

    MovingAvarage filter = new MovingAvarage(10);

    // Start is called before the first frame update
    void Start()
    {
        rightLegVel = new LegVelocity();
        leftLegVel = new LegVelocity();

        ncv = velocityObject.GetComponent<NetworkCharacterVelocity>();
    }

    float threshold = 0.01f;

    // Update is called once per frame
    void Update()
    {
        //calculate walking velocity
        rightLegVel.SetObject(baseObject, rightLeg, Time.deltaTime);
        leftLegVel.SetObject(baseObject, leftLeg, Time.deltaTime);

        filter.SetData(rightLegVel.velocity < leftLegVel.velocity ? leftLegVel.velocity : rightLegVel.velocity);
        velocity = filter.data;

        //get direction
        direction = (rightLeg.transform.position + leftLeg.transform.position) / 2;
        direction = (direction - baseObject.transform.position).normalized;
        direction.y = 0;

        ncv.velocity = direction * velocity;
        if (ncv.velocity.x < threshold)
            ncv.velocity.x = 0;
        if (ncv.velocity.z < threshold)
            ncv.velocity.z = 0;
    }
}
