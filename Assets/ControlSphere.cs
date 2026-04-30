using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlSphere : MonoBehaviour
{


    public class PedestrianLightState
    {
        public bool isVisible;
        public float direction;
        public Color color;
        public bool isAssigned;
        public float priority;

        public PedestrianLightState()
        {
            isVisible = false;
            direction = 0;
            color = Color.red;
            isAssigned = false;
        }
    }

    [System.Serializable]
    public class SphereLightState
    {
        public SphereRotate sphere;
        public int pedIndex;

        public SphereLightState()
        {
            sphere = null;
            pedIndex = -1;
        }
    }

    public int numPedestrian;
    public List<PedestrianLightState> pedestrianLightState = new List<PedestrianLightState>();
    public List<SphereLightState> spheres;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < numPedestrian; i++)
        {
            pedestrianLightState.Add(new PedestrianLightState());
        }

        for (int n = 0; n < spheres.Count; n++)
        {
            spheres[n].sphere.SetInvisible();
        }
    }

    // Update is called once per frame
    void Update()
    {
        //新たにSphereに登録
        for (int i = 0; i < numPedestrian; i++)
        {
            if (pedestrianLightState[i].isVisible == true && pedestrianLightState[i].isAssigned == false)
            {

                if (spheres.Count != 1)
                {
                    for (int n = 0; n < spheres.Count; n++)
                    {
                        if (spheres[n].pedIndex == -1)
                        {
                            spheres[n].pedIndex = i;
                            spheres[n].sphere.SetVisible();
                            pedestrianLightState[i].isAssigned = true;
                            //spheres[n].sphere.SetColor(pedestrianLightState[i].color);
                            //spheres[n].sphere.Rotate(pedestrianLightState[i].direction);
                            break;
                        }
                    }
                }
                else
                {
                    if (spheres[0].pedIndex != -1)
                    {
                        if (pedestrianLightState[spheres[0].pedIndex].priority < pedestrianLightState[i].priority)
                        {
                            pedestrianLightState[spheres[0].pedIndex].isAssigned = false;
                            spheres[0].pedIndex = i;
                            pedestrianLightState[i].isAssigned = true;
                            spheres[0].sphere.SetVisible();
                        }
                    }
                    else
                    {
                        spheres[0].pedIndex = i;
                        pedestrianLightState[i].isAssigned = true;
                        spheres[0].sphere.SetVisible();
                    }

                }

            }
        }
        //起動したSphereの状態を変更
        for (int n = 0; n < spheres.Count; n++)
        {
            if (spheres[n].pedIndex != -1)
            {
                //spheres[n].sphere.SetVisible();
                spheres[n].sphere.SetColor(pedestrianLightState[spheres[n].pedIndex].color);
                spheres[n].sphere.Rotate(pedestrianLightState[spheres[n].pedIndex].direction);
            }
        }

        //消失したSphereを消す
        for (int i = 0; i < numPedestrian; i++)
        {
            if (pedestrianLightState[i].isVisible == false && pedestrianLightState[i].isAssigned == true)
            {

                for (int n = 0; n < spheres.Count; n++)
                {
                    if (spheres[n].pedIndex == i)
                    {
                        spheres[n].pedIndex = -1;
                        spheres[n].sphere.SetInvisible();
                        pedestrianLightState[i].isAssigned = false;
                        break;
                    }
                }

            }
        }
    }
}
