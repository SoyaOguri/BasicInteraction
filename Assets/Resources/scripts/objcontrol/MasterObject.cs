using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq;

using DSUnityViz;


public class MasterObject : MonoBehaviour
{

    /// <summary>
    /// Car and mesh manager
    /// </summary>
    public ObjectManager objman;

    /// <summary>
    /// Car position updater
    /// </summary>
    public PositionUpdater updater;

    /// <summary>
    /// How to position update
    /// </summary>
    public UpdaterOption updaterOption = new UpdaterOption();

    /// <summary>
    /// Model bank; object registraion site of car models
    /// </summary>
    public GameObject ModelBank;

    /// <summary>
    /// Car mesh to display; this array is corresponding to Mesh_ID from DS
    /// </summary>
    public GameObject[] CarMeshes;

    /// <summary>
    /// Recording system
    /// </summary>
    /// recording the received data, or, replaying data
    //private StreamRecorder recorder = new StreamRecorder();

    private bool showInputDialog;
    private bool showInputDialogDone;
    private string showingFileName = "";
    private bool recordButtonPushed;
    private bool replayButtonPushed;
    private bool replaySimTimeButtonPushed;

    /// Initialization 
    void OnEnable()
    {
        // check Master object name is correct;
        Debug.Assert(
            DSParam.ObjectMasterName == this.transform.gameObject.name,
            "Master object name mismatch!!");


        // Set object master, 
        DSGlobal.object_master = //GameObject.Find(DSParam.ObjectMasterName);
            this.transform.gameObject;

        // initiate object manager
        objman = new ObjectManager(this, updaterOption);

        LoadParameter();
        FindModelBank();
        SetupModelBankModels();

        // init car position getter
        updater = new PositionUpdater(objman, updaterOption);
        updater.Initialize();
        updater.Update();
    }

    // Use this for getting object references
    void Start()
    {


    }

    /// <summary>
    /// Load parameters from file (under construction)
    /// </summary>
	void LoadParameter()
    {
        // Find data path; this will direct "Assets" directory;
        string cur_path = UnityEngine.Application.dataPath;
        // find local setting; 
        try
        {
            DSParamLoader.LoadParameterFromXMLFile(cur_path + "/Resources/local_setting/local_param.xml");
        }
        catch
        {
            // skip parameter load;
            // note the parameter will loaded partially when load error occur;
        }
    }

    /// <summary>Find a model bank</summary>
    void FindModelBank()
    {
        // if modelbank is selected by inspector, use it. 
        if (ModelBank == null)
        {   // if model_bank is not attached by editor, 
            // find the model bank object with name in parameters
            ModelBank = GameObject.Find(DSParam.ModelBankName);
        }
    }

    /// <summary>
    /// Register models with modelID and model name.
    /// </summary>
    void SetupModelBankModels()
    {
        // Setup a graphicx bank;
        // These strings are associated to graphics ID.
        // (ID = 0 means no show;)
        // e.g. 
        // objman.SetMeshName(0, "Blank");
        // objman.SetMeshName(1, "SkyCarWrap");
        // objman.SetMeshName(2, "SkyCarWrapRed");



        // if car meshes are set in inspector, use them.
        if (CarMeshes.Length > 0)
        {
            objman.SetMeshNames(CarMeshes.Select(o => o.name).ToArray());
        }
        else
        {
            Dictionary<int, string> models_to_regist;
            // or, store models to model bank from parameters;
            models_to_regist = DSParam.GetModelDictionary();

            // set the car mesh
            foreach (var x in models_to_regist)
            {
                objman.SetMeshName(x.Key, x.Value);
            }
        }

    }

    // Update is called once per frame
    void Update()
    {

        // check data arrival
        if (updater != null) updater.Update();

        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        /* comment out 2024.10.10
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (updater.IsRecording())
            {   // now recording, so stop recording
                updater.StopRecording();
            }else if (updater.IsReplaying())
            {
                updater.StopReplay();
            }
            else
            {   // now not recording, start recording
                showInputDialog = !showInputDialog;
            }
        }
        */

        if (showInputDialog && (recordButtonPushed || replayButtonPushed))
        {
            showInputDialog = false;
            showInputDialogDone = true;
        }

        if (showInputDialogDone)
        {
            if (recordButtonPushed)
                updater.StartRecording(showingFileName);
            if (replayButtonPushed)
                updater.StartReplay(showingFileName);
            if (replaySimTimeButtonPushed)
                updater.StartReplay(showingFileName);


            showInputDialogDone = false;
            recordButtonPushed = false;
            replayButtonPushed = false;
            replaySimTimeButtonPushed = false;
        }

    }

    void OnGUI()
    {
        Rect rct = new Rect(100, 100, 100, 50);
        if (showInputDialog)
        {
            showingFileName = UnityEngine.GUI.TextField(rct, showingFileName, 16);
            if (GUI.Button(new Rect(100, 150, 100, 20), "Record"))
            {
                recordButtonPushed = true;
            }
            if (GUI.Button(new Rect(100, 170, 100, 20), "Replay"))
            {
                replayButtonPushed = true;
            }
            if (GUI.Button(new Rect(100, 190, 100, 20), "Replay(Sim time)"))
            {
                replaySimTimeButtonPushed = true;
            }
        }

    }

    void OnApplicationQuit()
    {
    }



}
