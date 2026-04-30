using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PMController : MonoBehaviour {

    [SerializeField]
    float horizontalInput;
    [SerializeField]
    float verticalInput;

    public float velocity;
    public float anglerVelocity;

    Vector3 hVector,vVector;
    float anglerVelocity_degree;

    // Use this for initialization
    void Start () {
        hVector = new Vector3(0, 0, 0);
        vVector = new Vector3(0, 0, 0);
        anglerVelocity_degree = anglerVelocity * 180 / Mathf.PI;
    }
	
	// Update is called once per frame
	void Update () {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        hVector.x = verticalInput * velocity * Time.deltaTime;
        vVector.y = horizontalInput * anglerVelocity_degree * Time.deltaTime;
        transform.position += (transform.rotation * hVector);
        transform.Rotate(vVector);
    }
}
