//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;

//#if UNITY_EDITOR

//using UnityEditor;


//[System.Serializable]
//public class SettingFileMaker2014Window : EditorWindow
//{
//    // Path of this script
//    public string AssetPath = "scripts/settingmake/";

//    // add menu item
//    [MenuItem("Window/SettingFileMaker2014")]
//    static void Open()
//    {
//        // show editor window
//        EditorWindow.GetWindow<SettingFileMaker2014Window>("SettingFileMaker2014");
//    }

//    public GameObject CarInitPositionRoot;




//    bool OutputPushed = false;

//    string filename;

//    // folding;
//    bool FoldPrepSec = false;
//    bool FoldCoursesSec = false;
//    bool FoldOutputSec = false;
//    bool FoldLoadFile = false;

//    // display switch
//    static public bool bShowToNext = true;
//    static public bool bShowAllConnect = true;
//    static public bool bShowNode = true;

//    bool outputInterpolatinEnabled = false;
//    float outputResampleThreashold = 0.5f;
//    // file type on data load
//    int loadfileFileType = 0;
//    // what the created node is looking for
//    int LookTypeOnNodeCreation = 0;

//    void DrawSeparator() { GUILayout.Box("", GUILayout.Width(this.position.width - 5), GUILayout.Height(1)); }

//    void OnGUI()
//    {
//        EditorGUILayout.LabelField("<Setting File Maker v0.1 by Okuda>");

//        LayoutRootObject();

//        DrawSeparator();
//        LayoutMarkerGenerationSection();

//        DrawSeparator();
//        //LayoutMarkerLoadFromFileSection();

//        DrawSeparator();
//        //LayoutOutputSection();

//    }

//    void LayoutRootObject()
//    {
//        CarInitPositionRoot = 
//            (GameObject)EditorGUILayout.ObjectField(
//                "Root object", 
//                CarInitPositionRoot, 
//                typeof(GameObject), 
//                true);

//        if (GUILayout.Button("Create car init directory"))
//        {
//            CreateRoot();
//        }
        
//    }

//    void LayoutMarkerGenerationSection()
//    {
//        FoldCoursesSec = EditorGUILayout.Foldout(FoldCoursesSec, "[Courses]");

//        if (FoldCoursesSec)
//        {

//        }
//    }

//    //void LayoutMarkerLoadFromFileSection()
//    //{
//    //    FoldLoadFile = EditorGUILayout.Foldout(FoldLoadFile, "[Node load]");

//    //    EditorGUILayout.LabelField("File type:");

//    //    if (FoldLoadFile)
//    //    {
//    //        loadfileFileType = GUILayout.SelectionGrid(
//    //            loadfileFileType, 
//    //            new string[] { "UC-win XML", "Simple CSV(x,y,z)","DS course txt" }, 
//    //            1, EditorStyles.radioButton);
//    //    }

//    //    bool LoadFilePushed = GUILayout.Button("Load file");

//    //    if (LoadFilePushed)
//    //    {
//    //        LoadFromFile();
//    //    }
//    //}


//    //void LayoutOutputSection()
//    //{
//    //    FoldOutputSec = EditorGUILayout.Foldout(FoldOutputSec, "[Output]");

//    //    if (FoldOutputSec)
//    //    {
//    //        filename = EditorGUILayout.TextField("File name", filename);


//    //        outputInterpolatinEnabled = EditorGUILayout.ToggleLeft("Interpolate every:", outputInterpolatinEnabled);
//    //        outputResampleThreashold = EditorGUILayout.FloatField(outputResampleThreashold);

//    //        OutputPushed = GUILayout.Button("Output!");

//    //        if (OutputPushed)
//    //        {
//    //            OutputTest();
//    //        }
//    //    }
//    //}

//    //static bool initialized = false;
//    //void OnInspectorUpdate()
//    //{
//    //    if (!initialized)
//    //    {
//    //        if (SceneView.onSceneGUIDelegate != OnSceneGUI)
//    //        {
//    //            SceneView.onSceneGUIDelegate += OnSceneGUI;
//    //            //EditorApplication.playmodeStateChanged += OnPlayModeChanged;
//    //        }
//    //        initialized = true;
//    //    }

//    //    //// if node is selected;
//    //    //if (Selection.activeGameObject != null)
//    //    //{
//    //    //    PathRoot pr = Selection.activeGameObject.GetComponent<PathRoot>();
//    //    //    if (pr != null)
//    //    //    {
//    //    //        target = pr.gameObject;
//    //    //        nodemarker = pr.NodeMarker;
//    //    //    }
//    //    //}
//    //}

//    //// check the click event here
//    //void OnSceneGUI(SceneView sceneView)
//    //{

//    //    // if perform right click with shift, place new marker
//    //    if (Event.current.type == EventType.MouseDown
//    //        && Event.current.shift == true && Event.current.control == false
//    //        && Event.current.button == 1)
//    //    {
//    //        if (EnableNodeMarkerGeneration &&
//    //           !(target == null || nodemarker == null))
//    //        {
//    //            last_created = CreateNewMarkerByClick();
//    //        }
//    //    }

//    //    // if perform right click with contorl, connect marker
//    //    if (Event.current.type == EventType.MouseDown
//    //        && Event.current.shift == false && Event.current.control == true
//    //        && Event.current.button == 1)
//    //    {
//    //        ConnectToClickedMarker();
//    //    }

//    //    Repaint();
//    //}

//    // create root node and return it
//    GameObject CreateRoot()
//    {
//        string new_root_directory = "Car init positions";
        
//        GameObject tmp = new GameObject(new_root_directory);
//        CarInitPositionRoot = tmp;

//        return tmp;

//    }

//    //GameObject CreateNewMarkerByClick()
//    //{
//    //    Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
//    //    RaycastHit hit;

//    //    if (Physics.Raycast(ray, out hit))
//    //    {
//    //        Debug.Log("Path node is created at " + hit.point.ToString());

//    //        GameObject previosNode = GetPreviousNode();

//    //        int nodes_count = target.transform.childCount + 1;
//    //        Quaternion prev_node_rotation = previosNode.transform.rotation;

//    //        // Create Marker at the point
//    //        GameObject created = CreateNewNode(nodes_count, hit.point, prev_node_rotation, previosNode);

//    //        // look something
//    //        created.GetPn().Look(3);
//    //        previosNode.GetPn().Look(LookTypeOnNodeCreation);

//    //        current_path.UpdatePath();

//    //        return created;
//    //    }
//    //    else
//    //        return null;
//    //}

//    //GameObject CreateNewNode(int nodenum, Vector3 position, Quaternion rotation, GameObject previosNode = null)
//    //{
//    //    // Create Marker at the point
//    //    GameObject created = GameObject.Instantiate(nodemarker);
        
//    //    created.name = "Node" + nodenum.ToString("D4");
//    //    created.transform.parent = target.transform;
//    //    created.transform.position = position;
//    //    created.transform.rotation = rotation;

//    //    // set next null
//    //    PathNode created_node = created.GetPn();
//    //    if (created_node != null) created_node.NextNode = null;

//    //    // set previous optionaly
//    //    if (previosNode != null)
//    //    {
//    //        // if previous node is directed, make a link
//    //        // set link to this 
//    //        created_node.SetPreviousNode(previosNode);
//    //        SetNextNode(previosNode, created);


//    //    }

//    //    return created;
//    //}

//    //void ConnectToClickedMarker()
//    //{
//    //    Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
//    //    RaycastHit hit;

//    //    if (Physics.Raycast(ray, out hit))
//    //    {
//    //        // check the clicked object is node or not
//    //        if (hit.collider.gameObject == null) return;
//    //        if (Selection.activeGameObject == null) return;

//    //        // connect arrow to next
//    //        SetNextNode(Selection.activeGameObject, hit.collider.gameObject);
//    //    }
//    //}


//    //// set next node and return true if it success
//    //bool SetNextNode(GameObject trg_from, GameObject trg_to )
//    //{
//    //    PathNode to = trg_to.GetPn();
//    //    PathNode from  = trg_from.GetPn();

//    //    if(to != null && from != null)
//    //    {
//    //        from.NextNode = to.gameObject;
//    //        return true;
//    //    }
//    //    return false;
//    //}

//    //GameObject GetPreviousNode()
//    //{
//    //    GameObject previosNode;
//    //    PathNode pn = Selection.gameObjects.Last().GetComponent<PathNode>();

//    //    if (pn != null)
//    //    {   // PathNode is selected, so use selected as previous node
//    //        previosNode = pn.gameObject;
//    //    }
//    //    else
//    //    {   // PathNode is not selected.
//    //        // so use last gameObject in path nodes under root;
//    //        int n_nodes = target.transform.childCount;
//    //        previosNode = target.transform.GetChild(n_nodes - 1).gameObject;
//    //    }
//    //    return previosNode;
//    //}


//    //void OutputTest()
//    //{
//    //    PathRoot proot = target.GetComponent<PathRoot>();
//    //    if (proot == null) return;
        
//    //    // draw connected graph
//    //    for (int i = 0; i < proot.StartNodes.Length; i++)
//    //    {
//    //        PathNode sn = proot.StartNodes[i].GetComponent<PathNode>();
//    //        if (sn == null) continue;

//    //        HashSet<GameObject> bAlreadyReached = new HashSet<GameObject>();

//    //        // start nodes are done
//    //        bAlreadyReached.Add(proot.StartNodes[i]);

//    //        List<PathNode> one_path = new List<PathNode>();
//    //        one_path.Add(sn);

//    //        // follow links
//    //        while (sn.NextNode != null)
//    //        {
//    //            // if next node is already marked, finish this path.
//    //            if (bAlreadyReached.Contains(sn.NextNode)) break;

//    //            PathNode next_node = sn.NextNode.GetComponent<PathNode>();
//    //            if (next_node == null) break;

//    //            // draw arrow to next
//    //            one_path.Add( next_node );
//    //            bAlreadyReached.Add(sn.gameObject);

//    //            sn = next_node;
//    //        }
//    //        PathToPoints ptp = new PathToPoints(one_path.ToArray());
//    //        ptp.MakeLinearPointsByResample(outputResampleThreashold, outputInterpolatinEnabled);

//    //        string str = filename;

//    //        if (proot.StartNodes.Length != 1)
//    //        {
//    //            if (str.EndsWith(".csv")) str.Replace(".csv", "");
//    //            str = str + i.ToString() + ".csv";
//    //        }
//    //        PathWriter.WriteToCsv(str, ptp.computed);

//    //    }

//    //    //var path_nodes = pr.PathNodes.Select(o => o.GetComponent<PathNode>());


//    //    //PathToPoints ptp = new PathToPoints(path_nodes.ToArray());
//    //    //ptp.MakeLinearPointsByResample( outputResampleThreashold, outputInterpolatinEnabled );
//    //    //PathWriter.WriteToCsv(filename, ptp.computed);

//    //}


//    //void LoadFromFile()
//    //{

//    //    LoadNodesFromFile loader = new LoadNodesFromFile();

//    //    loader.LoadAuto("", loadfileFileType);

//    //    GameObject directory = new GameObject("LoadedNodes");

//    //    for (int i = 0; i < loader.Paths.Count; i++)
//    //    {
//    //        GameObject newRoot = createRootNodeImple( loader.Paths[i].unique_name );
//    //        newRoot.transform.parent = directory.transform;

//    //        target = newRoot;
//    //        GameObject previousNode = newRoot;
//    //        for (int j = 0; j < loader.Paths[i].points.Length; j++)
//    //        {
//    //            GameObject created = CreateNewNode(j, loader.Paths[i].points[j], Quaternion.identity, previousNode);
//    //            previousNode = created;
//    //        }
//    //    }

//    //}


//}


//#endif
