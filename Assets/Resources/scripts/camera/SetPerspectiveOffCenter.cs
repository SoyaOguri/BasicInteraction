
using System;
using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.Windows;
using System.Collections.Generic;

public class SetPerspectiveOffCenter : MonoBehaviour {

    public float ScreenWidth = 1.8f;
    public float ScreenHeight = 1.35f;

    public float VPOffsetX = 0f;
    public float VPOffsetY = 0f;

    public float PlaneDistance = 1.6f;
    public float NearClip = 1.0f;
    public float FarClip = 500f;


    public string SettingFileName;

    public bool Mirrored = false;

    public bool DynamicParameterChange;
    public int AddionalYPosition;

    PerspectiveOffcenter calculator = new PerspectiveOffcenter();

    private GameObject targetCamObj = null;
    Camera targetCam = null;

    bool isShowChageDialog;
    bool isCleated = false;

    // Use this for initialization
    void Start() {

        targetCamObj = this.gameObject;
        targetCam = targetCamObj.GetComponent<Camera>();

        // create example camera setting
        //calculator.GetParam(this);
        //PerspectiveOffcenter.SaveSetting("CameraSetTest.xml", calculator);

        // try to read setting from file
        if (!string.IsNullOrEmpty(SettingFileName))
        {
            LoadSettingFromFile(SettingFileName);
            calculator.GetParam(this);
        }
        else
        {   // use parameter from editor
            calculator.SetParam(this);
        }

        if (Mirrored)
        {
            calculator.MirrorEnabled = true;
            calculator.MirrorScale = new Vector3(-1,1,1);
        }

        calculator.SetProjMatrix(targetCam);


        if (DynamicParameterChange)
        {
            if (isCleated == false)
            {
                CreateDialog();
                isCleated = true;
            }
        }
    }

    // Update is called once per frame
    void Update() {
        if (targetCamObj == null) Start();
        if (DynamicParameterChange)
        {
            /* comment out 2024.10.10
            if (Input.GetKeyUp(KeyCode.P))
            {
                if(isShowChageDialog == true)
                {
                    HideDialog();
                    isShowChageDialog = false;
                }
                else
                {
                    ShowDialog();
                    isShowChageDialog = true;
                }
            }
            */
        }
        calculator.SetProjMatrix(targetCam);
    }

    List<HorizontalSlider> sliders;

    //public float ScreenWidth = 1.8f;
    //public float ScreenHeight = 1.35f;

    //public float VPOffsetX = 0f;
    //public float VPOffsetY = 0f;

    //public float PlaneDistance = 1.6f;
    //public float NearClip = 1.0f;
    //public float FarClip = 500f;

    void CreateDialog()
    {
        int x, y,plus;
        x = 200;y = 20+ AddionalYPosition; plus = 40;
        sliders = new List<HorizontalSlider>();
        sliders.Add(new HorizontalSlider(ScreenWidth, x, y, "ScreenWidth",0,3));
        sliders[sliders.Count-1].SetValue += ()=>{ ScreenWidth = sliders[0].hSliderValue; Start(); };
        y += plus;
        sliders.Add(new HorizontalSlider(ScreenHeight, x, y, "ScreenHeight", 0, 3));
        sliders[sliders.Count - 1].SetValue += () => { ScreenHeight = sliders[1].hSliderValue; Start(); };
        y += plus;
        sliders.Add(new HorizontalSlider(VPOffsetX, x, y, "VPOffsetX", -1, 1));
        sliders[sliders.Count - 1].SetValue += () => { VPOffsetX = sliders[2].hSliderValue; Start(); };
        y += plus;
        sliders.Add(new HorizontalSlider(VPOffsetY, x, y, "VPOffsetY", 0, 1));
        sliders[sliders.Count - 1].SetValue += () => { VPOffsetY = sliders[3].hSliderValue; Start(); };
        y += plus;
        sliders.Add(new HorizontalSlider(PlaneDistance, x, y, "PlaneDistance", 0.5f, 1.5f));
        sliders[sliders.Count - 1].SetValue += () => { PlaneDistance = sliders[4].hSliderValue; Start(); };
        y += plus;
        sliders.Add(new HorizontalSlider(NearClip, x, y, "NearClip"));
        sliders[sliders.Count - 1].SetValue += () => { NearClip = sliders[5].hSliderValue; Start(); };
        y += plus;
        sliders.Add(new HorizontalSlider(FarClip, x, y, "FarClip", 0, 1000));
        sliders[sliders.Count - 1].SetValue += () => { FarClip = sliders[6].hSliderValue; Start(); };
    }

    void HideDialog()
    {
        //slider.hideFlags = HideFlags.HideAndDontSave;
    }

    void ShowDialog()
    {
        //slider.hideFlags = HideFlags.None;
    }

    void OnGUI()
    {
        if (isShowChageDialog)
        {
            for (int i = 0; i < sliders.Count; i++)
            {
                sliders[i].OnGUI();
            }
        }
            
    }

    public class HorizontalSlider
    {
        public float x, y;
        public string str;
        public float hSliderValue = 0.0F;
        public delegate void SetValueDelegate();
        public SetValueDelegate SetValue = null;
        public float min,max;

        public HorizontalSlider(float value,float x, float y,string txt)
        {
            hSliderValue = value;
            this.x = x;
            this.y = y;
            str = txt;
            min = 0;
            max = 10f;
        }

        public HorizontalSlider(float value, float x, float y, string txt,float min,float max)
        {
            hSliderValue = value;
            this.x = x;
            this.y = y;
            str = txt;
            this.min = min;
            this.max = max;
        }

        public void OnGUI()
        {
            // スライダー（水平）を表示する
            hSliderValue = GUI.HorizontalSlider(new Rect(x, y, 600, 30), hSliderValue, min, max);
            // ラベルにhSliderValueの値を表示する
            GUI.Label(new Rect(x, y + 10, 200, 20), str + ":" + hSliderValue);
            SetValue();
        }
    }

    public void LoadSettingFromFile(string file)
    {
        PerspectiveOffcenter.LoadSetting(file, out calculator);
    }
       
    private bool oldCulling;

    void OnPreRender()
    {
        if (Mirrored)
        {
            oldCulling = GL.invertCulling;
            GL.invertCulling = true;
            // GL.wireframe = true;
        }
    }

    void OnPostRender()
    {
        if (Mirrored)
        {
            GL.invertCulling = oldCulling;
            // GL.wireframe = true;
        }
    }

}

public class PerspectiveOffcenter
{
    public Vector3 eyePosition;
    public Vector3 eyeEulerAngles;

    public float ScreenWidth = 1.8f;
    public float ScreenHeight = 1.35f;

    public float VPOffsetX = 0f;
    public float VPOffsetY = 0f;

    public float PlaneDistance = 1.6f;
    public float NearClip = 1.0f;
    public float FarClip = 500f;

    public bool MirrorEnabled = false;
    public Vector3 MirrorScale = new Vector3(1,1,1);

    public void SetParam( SetPerspectiveOffCenter offc)
    {
        eyePosition = offc.transform.localPosition;
        eyeEulerAngles = offc.transform.localEulerAngles;

        ScreenWidth = offc.ScreenWidth;
        ScreenHeight = offc.ScreenHeight;

        VPOffsetX = offc.VPOffsetX;
        VPOffsetY = offc.VPOffsetY;

        PlaneDistance = offc.PlaneDistance;
        NearClip = offc.NearClip;
        FarClip = offc.FarClip;
    }

    public void GetParam(SetPerspectiveOffCenter offc)
    {
        offc.transform.localPosition = eyePosition;
        offc.transform.localEulerAngles = eyeEulerAngles;

        offc.ScreenWidth = ScreenWidth;
        offc.ScreenHeight = ScreenHeight;

        offc.VPOffsetX = VPOffsetX;
        offc.VPOffsetY = VPOffsetY;

        offc.PlaneDistance = PlaneDistance;
        offc.NearClip = NearClip;
        offc.FarClip = FarClip;
    }

    public void SetProjMatrix( Camera cam)
	{
		const float nclimit = 0.0001f;
		if (NearClip < nclimit) NearClip = nclimit;

		const float pdlimit = 0.0001f;
		if (PlaneDistance < pdlimit) PlaneDistance = pdlimit;

		float scale = NearClip / PlaneDistance;

		float LeftOfNearPlane = -ScreenWidth / 2.0f - VPOffsetX;
		float RightNearPlane = ScreenWidth / 2.0f - VPOffsetX;

        float TopOfNearPlane = ScreenHeight / 2.0f - VPOffsetY;
		float BottomOfNearPlane = - ScreenHeight / 2.0f - VPOffsetY;

        float left = LeftOfNearPlane * scale;
        float right = RightNearPlane * scale;
        float bottom = BottomOfNearPlane * scale;
        float top = TopOfNearPlane * scale;
        float near = PlaneDistance * scale;
        float far = FarClip;
		
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
        if (!MirrorEnabled)
        {
            // m = m * Matrix4x4.Scale(new Vector3(1,1,1));
            cam.projectionMatrix = m;
        }
        else
        {
            //cam.ResetWorldToCameraMatrix();
            //cam.ResetProjectionMatrix();
            cam.projectionMatrix = m * Matrix4x4.Scale(MirrorScale);
        }
	}


    public static void SaveSetting(string fileName, PerspectiveOffcenter target)
	{
		//XmlSerializerオブジェクトを作成
		//オブジェクトの型を指定する
		System.Xml.Serialization.XmlSerializer serializer =
			new System.Xml.Serialization.XmlSerializer(typeof(PerspectiveOffcenter));
		//書き込むファイルを開く（UTF-8 BOM無し）
		System.IO.StreamWriter sw = new System.IO.StreamWriter(
			fileName, false, new System.Text.UTF8Encoding(false));
		//シリアル化し、XMLファイルに保存する
		serializer.Serialize(sw, target);
		//ファイルを閉じる
		sw.Close();
	}

    public static void LoadSetting(string fileName, out PerspectiveOffcenter target)
	{
		//XmlSerializerオブジェクトを作成
		System.Xml.Serialization.XmlSerializer serializer =
			new System.Xml.Serialization.XmlSerializer(typeof(PerspectiveOffcenter));
		//読み込むファイルを開く
		System.IO.StreamReader sr = new System.IO.StreamReader(
			fileName, new System.Text.UTF8Encoding(false));
		//XMLファイルから読み込み、逆シリアル化する
		target = (PerspectiveOffcenter)serializer.Deserialize(sr);
		//ファイルを閉じる
		sr.Close();
	}

}
