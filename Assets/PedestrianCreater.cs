using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.Demos;

public class PedestrianCreater : MonoBehaviour
{
    public enum PedestrainVelocityState
    {
        Normal,
        SlowDown,
        Stop,
    }

    public class PedestrainVelocityStateStruct
    {
        public PedestrainVelocityState state;
        public Transform Chara;
        public bool isNormal;
    }

    public GameObject area;
    //public GameObject listObject;
    List<GameObject> pedestrainList;
    int numberOfPedestrain;
    public List<GameObject> pedestrianList;
    List<NetworkCharacterVelocity> velocityList;
    List<GameObject> pedestrianController;
    List<PedestrainVelocityStateStruct> velocityStateList;

    List<double> distances;
    //List<TextMesh> meshes;

    public GameObject PMObject;
    public float slowDistance,stopDistance;
    double sqrSlowDistance, sqrStopDistance;

    float areaTop, areaBottom, areaRight, areaLeft;
    float velocity = 1;


    List<MyUserControlThirdPerson> MyCTPs;

    // Start is called before the first frame update
    void Start()
    {
        //Get walking area
        areaTop = area.transform.position.z + area.transform.localScale.z / 2;
        areaBottom = area.transform.position.z - area.transform.localScale.z / 2;
        areaRight = area.transform.position.x + area.transform.localScale.x / 2;
        areaLeft = area.transform.position.x - area.transform.localScale.x / 2;

        ////Get child list
        //int count = listObject.transform.childCount;
        //pedestrainList = new List<GameObject>();
        //for (int i = 0; i < count; i++)
        //{
        //    pedestrainList.Add(listObject.transform.GetChild(i).gameObject);
        //}

        //Create pedestrian
        numberOfPedestrain = pedestrianList.Count;

        velocityList = new List<NetworkCharacterVelocity>();
        pedestrianController = new List<GameObject>();
        MyCTPs = new List<MyUserControlThirdPerson>();
        //int temp = 0;
        for (int i = 0; i < numberOfPedestrain; i++)
        {
            //copiedPedestrianList.Add(Object.Instantiate(pedestrainList[temp] as GameObject));
            //copiedPedestrianList[i].SetActive(true);
            //copiedPedestrianList[i].name = "Pedestrian" + i;


            pedestrianController.Add(pedestrianList[i].transform.Find("Character Controller").gameObject);
            MyCTPs.Add(pedestrianController[i].GetComponent<MyUserControlThirdPerson>());
            velocityList.Add(pedestrianList[i].GetComponentInChildren< NetworkCharacterVelocity>());
            SetPedestrianPose(i);
            //temp++;
            //if (temp >= pedestrainList.Count)
            //    temp = 0;
        }


        sqrSlowDistance = slowDistance * slowDistance;
        sqrStopDistance = stopDistance * stopDistance;
        velocityStateList = new List<PedestrainVelocityStateStruct>();
        distances = new List<double>();
        //meshes = new List<TextMesh>();
        for (int i = 0; i < numberOfPedestrain; i++)
        {
            velocityStateList.Add(new PedestrainVelocityStateStruct());
            velocityStateList[i].Chara = pedestrianList[i].transform.GetChild(0);
            distances.Add(0);
            //meshes.Add(copiedPedestrianList[i].transform.GetComponentInChildren<TextMesh>());
        }
    }

    public void SetPedestrianPose(int count)
    {
        //pedestrianList[count].transform.position = new Vector3(Random.Range(areaLeft, areaRight), 0, Random.Range(areaBottom, areaTop));
        //pedestrianList[count].transform.eulerAngles = new Vector3(0, Random.Range(0, 359), 0);
        ////copiedPedestrianList[count].transform.parent = transform;
        //pedestrianList[count].transform.GetChild(0).transform.localPosition = new Vector3(0,0,0);
        //pedestrianList[count].transform.GetChild(0).transform.localEulerAngles = new Vector3(0, 0, 0);

        pedestrianList[count].transform.position = new Vector3(0, 0, 0);
        pedestrianList[count].transform.localPosition = new Vector3(0, 0, 0);
        pedestrianList[count].transform.eulerAngles = new Vector3(0, 0, 0);
        pedestrianList[count].transform.localEulerAngles = new Vector3(0, 0, 0);
        pedestrianList[count].transform.GetChild(0).transform.position = new Vector3(Random.Range(areaLeft, areaRight), 0, Random.Range(areaBottom, areaTop));
        pedestrianList[count].transform.GetChild(0).transform.eulerAngles = new Vector3(0, Random.Range(0, 359), 0);
        //copiedPedestrianList[count].transform.parent = transform;



        velocityList[count].velocity = new Vector3(Mathf.Sin(pedestrianList[count].transform.GetChild(0).transform.eulerAngles.y), 0, Mathf.Cos(pedestrianList[count].transform.GetChild(0).transform.eulerAngles.y)) * velocity;

        //too close
        if((PMObject.transform.position - pedestrianList[count].transform.position).magnitude < 2)
        {
            SetPedestrianPose(count);
        }
    }

    public void DisablePedestrianPose(int count)
    {
        pedestrianList[count].transform.Translate(0, -10, 0);
        velocityList[count].velocity = new Vector3(0,0,0);
    }

    // Update is called once per frame
    void Update()
    {
        //turn back
        for (int i = 0; i < numberOfPedestrain; i++)
        {
            if(pedestrianController[i].transform.position.x < areaLeft + 0.5)
            {
                if(velocityList[i].velocity.x < 0)
                    velocityList[i].velocity.x *= -1;
            }
            else if (pedestrianController[i].transform.position.x > areaRight - 0.5)
            {
                if (velocityList[i].velocity.x > 0)
                    velocityList[i].velocity.x *= -1;
            }

            if (pedestrianController[i].transform.position.z < areaBottom + 0.5)
            {
                if (velocityList[i].velocity.z < 0)
                    velocityList[i].velocity.z *= -1;
            }
            else if (pedestrianController[i].transform.position.z > areaTop - 0.5)
            {
                if (velocityList[i].velocity.z > 0)
                    velocityList[i].velocity.z *= -1;
            }
        }

        float distance;
        //slow down
        //for me
        for (int i = 0; i < numberOfPedestrain; i++)
        {
            velocityStateList[i].isNormal = true;
            distances[i] = 100000000;
        }
        for (int i = 0; i < numberOfPedestrain; i++)
        {
            distance = (PMObject.transform.position - velocityStateList[i].Chara.position).sqrMagnitude;

            if (distances[i] > distance)
            {
                if (DirectionCheck(velocityStateList[i].Chara.position, velocityList[i].velocity, PMObject.transform.position))
                {
                    ChangeVelocity(distance, i);
                    distances[i] = distance;
                    //meshes[i].text = "PM" + distance.ToString() + velocityStateList[i].state.ToString() + "\n" + velocityStateList[i].Chara.position.ToString();
                }
            }
        }

        for (int i = 0; i < numberOfPedestrain; i++)
        {
            for (int n = i + 1; n < numberOfPedestrain; n++)
            {
                distance = (velocityStateList[n].Chara.position - velocityStateList[i].Chara.position).sqrMagnitude;

                if (distances[i] > distance)
                {
                    //ChangeVelocity(distance, i);
                    distances[i] = distance;
                    //meshes[i].text = n + "P" + distance.ToString() + velocityStateList[i].state.ToString() + "\n" + velocityStateList[i].Chara.position.ToString();
                }
                if (distances[n] > distance)
                {
                    if (DirectionCheck(velocityStateList[n].Chara.position, velocityList[n].velocity, velocityStateList[i].Chara.position)) { 
                        ChangeVelocity(distance, n);
                        distances[n] = distance;
                        //meshes[n].text = i + "P" + distance.ToString() + velocityStateList[n].state.ToString() + "\n" + velocityStateList[n].Chara.position.ToString();
                    }
                }
            }
        }
        for (int i = 0; i < numberOfPedestrain; i++)
        {
            if (velocityStateList[i].isNormal && velocityStateList[i].state != PedestrainVelocityState.Normal)
            {
                velocityList[i].velocity = velocityList[i].velocity.normalized * velocity;
                velocityStateList[i].state = PedestrainVelocityState.Normal;
                MyCTPs[i].deviceControllerVelocity = (float)PedestrainVelocityState.Normal;
            }
        }
    }

    Vector3 posVec,dirVec;
    bool DirectionCheck(Vector3 basePos,Vector3 baseVel,Vector3 otherPos)
    {
        posVec = otherPos - basePos;
        posVec = posVec.normalized;
        dirVec = baseVel.normalized;
        if (posVec.x*dirVec.x+posVec.z*dirVec.z > -0.17365)
        {
            return true;
        }

        return false;
    }

    void ChangeVelocity(float distance,int i)
    {
        if (distance < sqrSlowDistance)
        {
            velocityStateList[i].isNormal = false;
            if (distance < sqrStopDistance)
            {
                if (velocityStateList[i].state != PedestrainVelocityState.Stop)
                {
                    velocityList[i].velocity = velocityList[i].velocity.normalized * 0.001f;
                    velocityStateList[i].state = PedestrainVelocityState.Stop;
                    MyCTPs[i].deviceControllerVelocity = (float)PedestrainVelocityState.Stop;
                }

            }
            else
            {
                if (velocityStateList[i].state != PedestrainVelocityState.SlowDown)
                {
                    velocityList[i].velocity = velocityList[i].velocity.normalized * 0.3f;
                    velocityStateList[i].state = PedestrainVelocityState.SlowDown;
                    MyCTPs[i].deviceControllerVelocity = (float)PedestrainVelocityState.SlowDown;
                }
            }
        }
    }
}
