using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightRoot : MonoBehaviour {

    public int TrafficLightId = 0;

    public GameObject[] ObjToShow;

    public int State = 0;

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
        
        try
        {
            int index = -1;
            for (int i = 0; i < DSGlobal.current_car_states.static_objects.count; i++)
            {
                if (DSGlobal.current_car_states.static_objects.id[i] == TrafficLightId)
                {
                    index = i;
                    State = DSGlobal.current_car_states.static_objects.state[index];
                }
            }
        }
        catch (System.Exception)
        {

            throw;
        }

        SelectObj(State);
	}

    void SelectObj(int i)
    {
        if (i >= ObjToShow.Length) return;
        GameObject toShow = ObjToShow[i];
        if (toShow == null) return;

        foreach(var obj in ObjToShow)
        {
            if (obj == null) continue;
            obj.SetActive(false);
        }
        toShow.SetActive(true);

    }
}
