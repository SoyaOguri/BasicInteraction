using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BicycleAnimaterChanger : MonoBehaviour
{
    [Range(0, 3)]
    public float velocity;

    [Range(-0.5f, 0.5f)]
    public float steer;

    [Range(-0.3f, 0.3f)]
    public float roll;


    public GameObject bicycle;

    Animator bicycleAnim;

    public GameObject steerObj;

    //[Header("----------------------")]
    //public Vector3 position;
    float hight = 0.53f;
    Vector3 hightVec;

    // Start is called before the first frame update
    void Start()
    {
        bicycleAnim = bicycle.GetComponent<Animator>();
        hightVec = new Vector3(0, hight, 0);
    }

    // Update is called once per frame
    void Update()
    {
        bicycleAnim.SetFloat("Speed", velocity);

        //this.transform.localRotation = Quaternion.Euler(0, yaw * 180 / Mathf.PI, roll * 180 / Mathf.PI);
        //this.transform.localPosition = position+transform.rotation * hightVec ;
    }

    void LateUpdate()
    {
        //steerObj.transform.localRotation = Quaternion.Euler(0, steer * 180 / Mathf.PI + 90, 69.2f);
    }
}
