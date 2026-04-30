using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficSignalArea : MonoBehaviour {

    public int AreaID;
    public GameObject leftArea;
    public GameObject rightArea;

    public HashSet<GameObject> carsInThisArea = new HashSet<GameObject>();

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        //
        //for (int i = 0; i < DSGlobal.current_car_states.car_states.Length; i++)
        {

        }

        // rightArea.GetComponent<TrafficSignalArea>().carsInThisArea;

    }

    private void LateUpdate()
    {
        if (carsInThisArea.Count > 0)
            return;
        return;

    }
}

