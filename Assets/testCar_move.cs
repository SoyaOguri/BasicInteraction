using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testCar_move : MonoBehaviour {

	public float jumpPower;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetButtonDown("jump")){
			GetComponent<Rigidbody>().velocity = new Vector3(0, jumpPower, 0);
			//GetComponent.velocity = new Vector3(0, jumpPower, 0);
		}
		
	}
}
