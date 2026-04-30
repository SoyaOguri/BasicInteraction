using UnityEngine;
using System.Collections;

using DSUnityViz;

public class Walker1_alpha : MonoBehaviour, IDSCarUpdater
{

	// Use this for initialization
	void Start ()
	{
	
	}
	
	// Update is called once per frame
	void Update ()
	{
	
	}

	public void UpdateByDSInfo( int car_id, CAR_UNIT_INFO car_info )
	{
		transform.localPosition = new Vector3 (car_info.x, car_info.y, car_info.z);

		transform.localRotation = DSParam.TransformCoordinateYPR(
			car_info.yaw,
			car_info.pitch,
			car_info.roll
		);

		// something special here
	}
}

