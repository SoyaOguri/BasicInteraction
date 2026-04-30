using UnityEngine;
using System.Collections;



/// <summary>
/// 3 screen formating for concatinated windows mode
/// </summary>
/// This script is deprecated.
/// Use indivisual perspective setting to each camera
[System.Serializable]
public class CameraMaster3Scr : MonoBehaviour {

	// camera objects
	Transform _cameraMaster;
	//private Vector3 camera_pos;
	//private Quaternion camera_rot;

	public GameObject ObjToFollow = null;

    CameraMaster3Scr()
    {
        _cameraMaster = transform;
    }
	
	// Use this for initialization
	void Start () {
		GetCameraObjects ();

		string CameraAttachedCarName = "CarA";
		GetFollowingObject( CameraAttachedCarName );
	}

	//float phi = 0;

	// Update is called once per frame
	void Update () {


//#if UNITY_EDITOR
//		return;
//#else
        if ( ObjToFollow == null) return;
		if (ObjToFollow != null)
		{
			_cameraMaster.transform.parent = ObjToFollow.transform;
			//_cameraMaster.localPosition = ObjToFollow.transform.localPosition;
			//_cameraMaster.localRotation = ObjToFollow.transform.localRotation;
		}
//#endif

    }


    void GetCameraObjects()
	{
		// for getting camera
		_cameraMaster = transform;
		//camera_pos = transform.localPosition;
		//camera_rot = transform.localRotation;
		//camera_pos = GetComponent<Transform>().localPosition;
		//camera_rot = GetComponent<Transform>().localRotation;


		//Camera cam = Camera.main;//GetComponent<Camera>();
		//(Camera)GameObject.Find("CameraC");

		Camera[] cams = new Camera[3];
		Camera.GetAllCameras (cams);

		float near_shorting_factor = 0.1f;

		float left = -1.8f * near_shorting_factor;
		float right = 1.8f * near_shorting_factor;
		float top = 1.35f * near_shorting_factor;
		float bottom = -1.35f * near_shorting_factor;
		float near = 3.1177f * near_shorting_factor;
		float far = 500f;
		
		var m = PerspectiveOffCenter (left, right, bottom, top, near, far);
		
		for (int i=0; i<cams.Length; i++) {
			cams [i].projectionMatrix = m;
		}
	}
	
	static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom,float  top,float  near,float  far) {
		var x = 2.0F * near / (right - left);
		var y = 2.0F * near / (top - bottom);
		var a = (right + left) / (right - left);
		var b = (top + bottom) / (top - bottom);
		var c = -(far + near) / (far - near);
		var d = -(2.0F * far * near) / (far - near);
		var e = -1.0F;
		var m = new Matrix4x4();
		m[0, 0] = x;
		m[0, 1] = 0;
		m[0, 2] = a;
		m[0, 3] = 0;
		m[1, 0] = 0;
		m[1, 1] = y;
		m[1, 2] = b;
		m[1, 3] = 0;
		m[2, 0] = 0;
		m[2, 1] = 0;
		m[2, 2] = c;
		m[2, 3] = d;
		m[3, 0] = 0;
		m[3, 1] = 0;
		m[3, 2] = e;
		m[3, 3] = 0;
		return m;
	}

	private void GetFollowingObject(string name)
	{
		GameObject tmp = GameObject.Find(name);
		if(tmp !=null)
			ObjToFollow = tmp;
	}


}
