using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR

using UnityEditor;


[System.Serializable]
public class DSSetupWindow : EditorWindow
{
    // Path of this script
    public string AssetPath = "scripts/setup/";

    int numberOfCamera = 1;

    //bool takeinAllCam = false;

    bool multiDisplayInGroup = false;

    // eyePoint settings
    float eye_height = 1.2f;
    float[] camera_direction;
    bool[] camera_mirrored;

    // add menu item
    [MenuItem("Window/NUDS Setup")]
    static void Open()
    {
        // show editor window
        EditorWindow.GetWindow<DSSetupWindow>("NUDS Setup");
    }

    void DrawSeparator() { GUILayout.Box("", GUILayout.Width(this.position.width - 5), GUILayout.Height(1)); }

    void OnGUI()
    {
        EditorGUILayout.LabelField("<NUDS linker setup v0.1 by Okuda>");

        DrawSeparator();
        LayoutNUDSLinkerSetupCameraSection();

        DrawSeparator();
        LayoutNUDSLinkerSetupMasterObjectSection();


    }

    void LayoutNUDSLinkerSetupCameraSection()
    {
        
        bool pushed = GUILayout.Button("Setup camera");

        numberOfCamera = EditorGUILayout.IntField("# of camera",numberOfCamera);

        //takeinAllCam = EditorGUILayout.ToggleLeft("Take in existing cams", takeinAllCam);

        multiDisplayInGroup = EditorGUILayout.ToggleLeft("Multi disp group", multiDisplayInGroup);

        eye_height = EditorGUILayout.FloatField("Eye point altitude", eye_height);

        if(camera_direction == null || camera_direction.Count()!= numberOfCamera)
        {
            camera_direction = new float[numberOfCamera];
        }

        if (camera_mirrored == null || camera_mirrored.Count() != numberOfCamera)
        {
            camera_mirrored = new bool[numberOfCamera];
        }

        for (int i=0; i<numberOfCamera; i++)
        {
            camera_direction[i] = EditorGUILayout.FloatField(
                "Cam " + i.ToString() + ": Yaw", 
                camera_direction[i]);
            camera_mirrored[i] = EditorGUILayout.Toggle(
                "Cam " + i.ToString() + ": Mirrored",
                camera_mirrored[i]
                );
        }

        if (pushed)
        {
            CreateCameraSeries();
        }

        string helpstring = @"
Check and modify followings; 
  - Camera position, rotation, FOV
  - Viewport

Camera shifter is for instant moving of camera for debug.
It keeps initial relative position to CameraMaster. 
Please use 'SetCameraShift' in CameraShifter script
to override the camera shift in runtime.";

        EditorGUILayout.HelpBox(helpstring, MessageType.Info);
    }
    
    /// <summary>
    /// Setup camera master and camera shifter;
    /// </summary>
    void CreateCameraSeries()
    {
        GameObject camera_master;
        GameObject camera_shifter;
        CameraSetuper.CreateCameraMaster(out camera_master, out camera_shifter);
        DSGlobal.camera_master = camera_master;


        CreateCameras(camera_shifter);
    }

    /// <summary>
    /// Create cameras or asign existing cameras to children of root
    /// </summary>
    /// <param name="root"></param>
    void CreateCameras(GameObject root)
    {
        if (numberOfCamera <= 0) return;

        int done = 0;

        Camera[] cameras = new Camera[numberOfCamera];
        for (; done < numberOfCamera; done++)
        {
            GameObject cameraObj = CameraSetuper.CreateCamera(
                "Camera " + done.ToString(),
                new Vector3(0, eye_height, 0),
                new Vector3(0, camera_direction[done], 0),
                done == 0,
                camera_mirrored[done]
                );
            cameraObj.transform.SetParent(root.transform, false);
            cameras[done] = cameraObj.GetComponent<Camera>();
        }

        SetupCameraVieports(cameras);
    }


    void SetupCameraVieports(Camera[] cams)
    {
        // setup view port
        if (multiDisplayInGroup)
        {
            for (int i = 0; i < numberOfCamera; i++)
            {
                cams[i].rect = new Rect( 
                    ((float)i) / numberOfCamera , 0.0f, 
                    1.0f / numberOfCamera, 1.0f);
            }
        }

    }

    /// <summary>
    /// Draw master object setup section
    /// </summary>
    void LayoutNUDSLinkerSetupMasterObjectSection()
    {

        bool pushed = GUILayout.Button("Setup master object");

        if (pushed)
        {
            CreateMasterObjectSeries();
        }

        string helpstring = @"Keep model bank null if you use the array 'CarMeshes' in master object.
You DONT need to use 'ModelBank' directory if you use 'CarMeshes' configuration for mesh registration.";

        EditorGUILayout.HelpBox(helpstring, MessageType.Info);
    }

    /// <summary>
    /// Create master object
    /// </summary>
    void CreateMasterObjectSeries()
    {
        // if object master is not found, create it.
        if (GameObject.Find(DSParam.ObjectMasterName) == null)
        {
            var objmaster = new GameObject(DSParam.ObjectMasterName);
            DSGlobal.object_master = objmaster;
            objmaster.AddComponent<MasterObject>();
            objmaster.AddComponent<ShowTextInfo>();
        }

        // if model bank is not found, create it
        if (GameObject.Find(DSParam.ModelBankName) == null)
        {
            new GameObject(DSParam.ModelBankName);
        }
        

    }

}







class CameraSetuper
{
    public static GameObject MirrorViewsMaster = null;

    public static GameObject CreateCameraMaster(out GameObject cameraMaster, out GameObject cameraShifter)
    {

        GameObject tmpr = new GameObject(DSParam.CameraMasterName);

        var tmp = new GameObject(DSParam.CameraShifterName);
        tmp.transform.parent = tmpr.transform;
        tmp.AddComponent<CameraShifter>();
        tmp.transform.localRotation = Quaternion.Euler(0, 90, 0);
        tmp.transform.localPosition = tmp.transform.localPosition + new Vector3(0, 0, 0);

        cameraMaster = tmpr;
        cameraShifter = tmp;

        return tmpr;

    }


    public static GameObject CreateCamera(string name, Vector3 pos, Vector3 ori, bool mainCam, bool mirrored)
    {
        if (mirrored)
            return CreateCameraMirror(name, pos, ori, mainCam);
        else
            return CreateCameraSimple(name, pos, ori, mainCam);
    }

    public static GameObject CreateCameraSimple(string name, Vector3 pos, Vector3 ori, bool mainCam)
    {
        var cgo = new GameObject(name);
        cgo.transform.localPosition = pos;
        cgo.transform.localEulerAngles = ori;

        var camera = cgo.AddComponent<Camera>();
        cgo.AddComponent<Canvas>();
        cgo.AddComponent<FlareLayer>();

        if (mainCam)
        {   // make main camera
            cgo.tag = "MainCamera";
            cgo.AddComponent<AudioListener>();
        }
        else
        {
            camera.tag = "Untagged";
        }
        return cgo;
    }

    public static GameObject CreateCameraMirror(string name, Vector3 pos, Vector3 ori, bool mainCam)
    {
        var cgo = new GameObject(name);
        cgo.transform.localPosition = pos;
        cgo.transform.localEulerAngles = ori;

        if(MirrorViewsMaster==null)
        {
            MirrorViewsMaster = new GameObject("MirrorCamMaster");
        }

        int n = MirrorViewsMaster.transform.childCount + 1;
        float camera_workspace_height = (n + 1) * 500;

        // ミラー映像の映写スクリーンを撮るカメラのセットアップ
        var cgo4mirror = new GameObject(name+"_MirScrCam");
        cgo4mirror.transform.localPosition = new Vector3(0, camera_workspace_height, 0);
        cgo4mirror.transform.localEulerAngles = new Vector3(0, 0, 0);

        var cam2 = cgo4mirror.AddComponent<Camera>();
        cam2.projectionMatrix = Matrix4x4.Ortho(-2f,2f,-1.5f,1.5f,0f,10f);
        cgo4mirror.AddComponent<Canvas>();
        cgo4mirror.AddComponent<FlareLayer>();

        cgo4mirror.transform.parent = MirrorViewsMaster.transform;

        // Quadの設定(ミラー映像の映写スクリーン)
        var cgo4mirrorScr = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cgo4mirrorScr.name = "ScreenFor" + name;
        cgo4mirrorScr.transform.localPosition = new Vector3(0, camera_workspace_height, 1);
        cgo4mirrorScr.transform.localEulerAngles = new Vector3(0, 0, 0);
        cgo4mirrorScr.transform.localScale = new Vector3(-4, 3, 1);
        cgo4mirrorScr.transform.parent = MirrorViewsMaster.transform;
        cgo4mirrorScr.GetComponent<MeshCollider>().enabled = false;


        // ミラー映像を撮るカメラのセットアップ
        var camera = cgo.AddComponent<Camera>();
        cgo.AddComponent<Canvas>();
        cgo.AddComponent<FlareLayer>();

        if (mainCam)
        {   // make main camera
            cgo.tag = "MainCamera";
            cgo.AddComponent<AudioListener>();
        }
        else
        {
            camera.tag = "Untagged";
        }

        var rt = CreateRenderTarget(name);
        cgo4mirrorScr.GetComponent<MeshCollider>().enabled = false;

        // set rendering target for Camera
        camera.targetTexture = rt;

        return cgo;
    }

    public static RenderTexture CreateRenderTarget(string name)
    {
        var target = new RenderTexture(1024, 1024, 16, RenderTextureFormat.ARGB32);

        name = "RenderTexture" + name;
        // 出力先ファイル名
        var filename = "Assets/Resources/" + name + ".asset";

        // Assetへ保存
        UnityEditor.AssetDatabase.CreateAsset(target, filename);

        return target;
    }

}

#endif
