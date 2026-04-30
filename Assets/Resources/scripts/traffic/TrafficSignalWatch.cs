using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficSignalWatch : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        RaycastHit hit;
        Vector3 sp = gameObject.transform.position; 

        if (Physics.Raycast(sp, Vector3.up, out hit))
        {
            //int n = hit.transform.gameObject.GetComponent<TrafficSignalArea>().AreaID;
            hit.transform.gameObject.GetComponent<TrafficSignalArea>().carsInThisArea.Add(gameObject);
        }
        else
        {
            
        }
    }
}
