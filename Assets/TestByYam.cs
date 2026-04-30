using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestByYam : MonoBehaviour
{
    public ControlSphere cs;

    // Start is called before the first frame update
    void Start()
    {

    }

    int count = 0;

    // Update is called once per frame
    void Update()
    {
        if (count > 10)
        {
            cs.pedestrianLightState[0].isVisible = true;
            cs.pedestrianLightState[0].direction = ((float)count) / 100;
            cs.pedestrianLightState[0].color = Color.green;
            cs.pedestrianLightState[0].priority = 1;
        }

        if (count > 30)
        {
            cs.pedestrianLightState[2].isVisible = true;
            cs.pedestrianLightState[2].direction = -1;
            cs.pedestrianLightState[2].color = Color.white;
            cs.pedestrianLightState[2].priority = 2;
        }
        if (count > 40)
        {
            cs.pedestrianLightState[0].isVisible = false;
            cs.pedestrianLightState[0].direction = 1;
            cs.pedestrianLightState[0].color = Color.green;
        }
        if (count > 100)
        {
            cs.pedestrianLightState[0].isVisible = true;
            cs.pedestrianLightState[0].direction = ((float)count) / 100; ;
            cs.pedestrianLightState[0].color = Color.green;
        }

        if (count > 200)
        {
            cs.pedestrianLightState[3].isVisible = true;
            cs.pedestrianLightState[3].direction = 2;
            cs.pedestrianLightState[3].color = Color.magenta;
            cs.pedestrianLightState[3].priority = 1;
        }

        if (count > 300)
        {
            cs.pedestrianLightState[1].isVisible = true;
            cs.pedestrianLightState[1].direction = 2;
            cs.pedestrianLightState[1].color = Color.red;
            cs.pedestrianLightState[1].priority = 5;
        }

        if (count > 350)
        {
            cs.pedestrianLightState[1].isVisible = false;
            cs.pedestrianLightState[1].direction = 2;
            cs.pedestrianLightState[1].color = Color.red;
            cs.pedestrianLightState[1].priority = 5;
        }

        if (count > 400)
        {
            cs.pedestrianLightState[0].isVisible = false;
            cs.pedestrianLightState[0].direction = 1;
            cs.pedestrianLightState[0].color = Color.green;
        }

        count++;
    }
}
