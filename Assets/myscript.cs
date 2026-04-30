using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class myscript : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    void OnTriggerExit(Collider collision)
    {
        Debug.Log("Hit"); // ログを表示する
        Destroy(collision.gameObject);
    }
}
