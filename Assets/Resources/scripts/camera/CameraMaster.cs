using UnityEngine;
using System.Collections;

using DSUnityViz;

public class CameraMaster : MonoBehaviour {

	// camera objects
	Transform camera_master;
	//private Vector3 camera_pos;
	//private Quaternion camera_rot;

	public GameObject obj_to_follow = null;

    public bool EnableMultiMonitor = false;
    public int MultiMonitorNum = 0;

	// Use this for initialization
	void Start () {
               
        camera_master = DSGlobal.camera_master.transform;

        // Activate multi monitor
        if (EnableMultiMonitor && MultiMonitorNum > 0)
        {
            for (int disp_idx = 1; disp_idx < Display.displays.Length; disp_idx++)
            {
                if (disp_idx < MultiMonitorNum)
                {
                    Display.displays[disp_idx].Activate();
                }
            }
        }
    }

	//float phi = 0;

	// Update is called once per frame
	void Update () {

		int to_follow = DSGlobal.current_car_states.GetCamera(0).camera_attached_car_id;

		var mo = DSGlobal.object_master.GetComponent<MasterObject>();

		try
		{
			//var script = camera_master.GetComponent<CameraMaster3Scr>() as CameraMaster3Scr;
			//script.obj_to_follow = objman.cars[camera_attached_car_id];
			DSGlobal.camera_master.transform.parent =
				mo.objman.cars[to_follow].transform;
            DSGlobal.camera_master.transform.localPosition = new Vector3(0, 0, 0);
            DSGlobal.camera_master.transform.localRotation = Quaternion.identity;

        }
		catch
		{

		}

		if ( obj_to_follow == null) return;
		if (obj_to_follow != null)
		{
			camera_master.transform.parent = obj_to_follow.transform;
            //camera_master.localPosition = obj_to_follow.transform.localPosition;
            //camera_master.localRotation = obj_to_follow.transform.localRotation;
            DSGlobal.camera_master.transform.localPosition = new Vector3(0, 0, 0);
            DSGlobal.camera_master.transform.localRotation = Quaternion.identity;

        }

    }
	

}
