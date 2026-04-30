using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if UNITY_EDITOR

using UnityEditor;


[System.Serializable]
public class PathMakerWindow : EditorWindow
{
    // Path of this script
    public string AssetPath = "scripts/pathmake/";

    // add menu item
    [MenuItem("Window/PathMaker")]
    static void Open()
    {
        // show editor window
        EditorWindow.GetWindow<PathMakerWindow>("PathMaker");
    }

    public GameObject Target;
    public GameObject Nodemarker;

    public GameObject LastCreated = null;

    private PathRoot _currentPath = null;

    private bool _enableNodeMarkerGeneration = false;
    private bool _outputPushed = false;

    // for output filename
    private string _filename;

    // vertex export filename
    private string _pointExportFileName;

    // folding;
    private bool _foldPrepSec = false;
    private bool _foldGenerationSec = false;
    private bool _foldOutputSec = false;
    private bool _foldLoadFile = false;
    private bool _foldVertexExportSec = false;
    private bool _foldVertexToPathSec = false;

    // display switch
    public static bool bShowToNext = true;
    public static bool bShowAllConnect = true;
    public static bool bShowNode = true;

    bool _outputInterpolatinEnabled = false;
    float _outputResampleThreashold = 0.5f;
    // file type on data load
    int _loadfileFileType = 0;
    // what the created node is looking for
    int _lookTypeOnNodeCreation = 0;

    // vertex export
    bool _vertexExportPushed = false;
    bool _vexportOutputInAdditionMode = false;
    bool _vexportOutputEdgeCenter = false;

    // Vertex to Path
    bool _vertextoPathPushed = false;

    void DrawSeparator() { GUILayout.Box("", GUILayout.Width(this.position.width - 5), GUILayout.Height(1)); }

    void OnGUI()
    {
        DetectAndSetPathRoot();

        EditorGUILayout.LabelField("<Path Maker v0.1 by Okuda>");

        DrawSeparator();
        LayoutPreparationSection();

        DrawSeparator();
        LayoutMarkerGenerationSection();

        DrawSeparator();
        LayoutMarkerLoadFromFileSection();

        DrawSeparator();
        LayoutOutputSection();

        DrawSeparator();
        LayoutVerticesExportSection();

        DrawSeparator();
        LayoutVerticesToPath();

        if (LastCreated != null)
        {
            //System.Threading.Thread.Sleep(100);
            //Selection.activeGameObject = last_created;
            //Selection.activeObject = last_created;
            //Selection.activeTransform = last_created.transform;
            //last_created = null;
            GameObject[] newSelection = new GameObject[1];
            newSelection[0] = LastCreated;
            Selection.objects = newSelection;
            //if (ChangedByHatena)
            {
                LastCreated = null;
                //  ChangedByHatena = false;
            }
        }
    }

    void LayoutPreparationSection()
    {
        _foldPrepSec = EditorGUILayout.Foldout(_foldPrepSec, "[Using mesh]");

        if (_foldPrepSec)
        {
            Target = (GameObject)EditorGUILayout.ObjectField("Path Root Node Type", Target, typeof(GameObject), true);
            Nodemarker = (GameObject)EditorGUILayout.ObjectField("Node Type", Nodemarker, typeof(GameObject), true);

            if (GUILayout.Button("Create new path"))
            {
                CreateRootNode();
            }
        }
    }

    void LayoutMarkerGenerationSection()
    {
        _foldGenerationSec = EditorGUILayout.Foldout(_foldGenerationSec, "[Setting]");

        if (_foldGenerationSec)
        {
            if (Target == null || Nodemarker == null)
            {
                _enableNodeMarkerGeneration = false;
                EditorGUILayout.ToggleLeft("Enable Create (please setup)", false);
            }
            else
            {
                _enableNodeMarkerGeneration = EditorGUILayout.ToggleLeft("Enable Create (Shift + R-Click to create)", _enableNodeMarkerGeneration);
            }

            EditorGUILayout.LabelField("Rotation on creation:");
            _lookTypeOnNodeCreation = GUILayout.SelectionGrid(
                _lookTypeOnNodeCreation,
                new string[] { "None", "Next", "Mean"},
                1, EditorStyles.radioButton);

            if (_currentPath != null && _currentPath.PathNodes != null)
            {
                EditorGUILayout.LabelField("node count: " + _currentPath.PathNodes.Count());
            }

            // visibility
            EditorGUILayout.LabelField("Connection visibility: ");
            GUILayout.BeginHorizontal("box");
            bool isChanged = false;
            bool temp = false;
            bShowToNext = EditorGUILayout.ToggleLeft("To next", bShowToNext, GUILayout.Width(80));
            bShowAllConnect = EditorGUILayout.ToggleLeft("All", bShowAllConnect, GUILayout.Width(80));
            temp = EditorGUILayout.ToggleLeft("Show Node", bShowNode, GUILayout.Width(80));
            isChanged |= temp ^ bShowNode; bShowNode = temp;
            GUILayout.EndHorizontal();
            if (Selection.activeGameObject != null) EditorUtility.SetDirty(Selection.activeObject);

            if (isChanged)
            {   // change node visibility 
                var list = _currentPath.PathNodes.Select(o => o.gameObject);
                foreach (var x in list)
                {
                    Renderer[] l = x.GetComponentsInChildren<Renderer>();
                    if (l.Count() > 0)
                    {
                        for (int i = 0; i < l.Count(); i++)
                        {
                            l[i].enabled = bShowNode;
                        }
                    }
                }
            }
        }
    }

    void LayoutMarkerLoadFromFileSection()
    {
        _foldLoadFile = EditorGUILayout.Foldout(_foldLoadFile, "[Node load]");

        EditorGUILayout.LabelField("File type:");

        if (_foldLoadFile)
        {
            _loadfileFileType = GUILayout.SelectionGrid(
                _loadfileFileType, 
                new string[] { "UC-win XML", "Simple CSV(x,y,z)","DS course txt" }, 
                1, EditorStyles.radioButton);
        }

        bool LoadFilePushed = GUILayout.Button("Load file");

        if (LoadFilePushed)
        {
            LoadFromFile();
        }
    }


    void LayoutOutputSection()
    {
        _foldOutputSec = EditorGUILayout.Foldout(_foldOutputSec, "[Output]");

        if (_foldOutputSec)
        {
            _filename = EditorGUILayout.TextField("File name", _filename);

            
            _outputInterpolatinEnabled = EditorGUILayout.ToggleLeft("Interpolate every:", _outputInterpolatinEnabled);
            _outputResampleThreashold = EditorGUILayout.FloatField(_outputResampleThreashold);

            _outputPushed = GUILayout.Button("Output!");

            if (_outputPushed)
            {
                OutputTest();
            }
        }
    }


    void LayoutVerticesExportSection()
    {
        _foldVertexExportSec = EditorGUILayout.Foldout(_foldVertexExportSec, "[Vertex Export]");

        if (_foldVertexExportSec)
        {
            string targetName = "none";
            if (Selection.activeGameObject != null)
            {
                targetName = Selection.activeGameObject.name;
            }

            _vexportOutputInAdditionMode = EditorGUILayout.ToggleLeft("Write file as 'addition' mode", _vexportOutputInAdditionMode);
            _vexportOutputEdgeCenter = EditorGUILayout.ToggleLeft("Output center of edge", _vexportOutputEdgeCenter);


            EditorGUILayout.LabelField( "Targeting obj:" + targetName);
            _pointExportFileName = EditorGUILayout.TextField("File name", _pointExportFileName);

            _vertexExportPushed = GUILayout.Button("Output!");

            if (_vertexExportPushed)
            {
                VertexExport();
            }
        }
    }


    void LayoutVerticesToPath()
    {
        _foldVertexToPathSec = EditorGUILayout.Foldout(_foldVertexToPathSec, "[Vertex To Path]");

        if (_foldVertexToPathSec)
        {
            string targetName = "none";
            if (Selection.activeGameObject == null) return;

            targetName = Selection.activeGameObject.name;

            //_vexportOutputInAdditionMode = EditorGUILayout.ToggleLeft("Write file as 'addition' mode", _vexportOutputInAdditionMode);
            _vexportOutputEdgeCenter = EditorGUILayout.ToggleLeft("Output center of edge", _vexportOutputEdgeCenter);

            EditorGUILayout.LabelField("Targeting obj:" + targetName);

            _vertextoPathPushed = GUILayout.Button("Output!");

            if (_vertextoPathPushed)
            {
                VertexToPath();
            }
        }
    }

    static bool initialized = false;
    void OnInspectorUpdate()
    {
        if (!initialized)
        {
            if (SceneView.onSceneGUIDelegate != OnSceneGUI)
            {
                SceneView.onSceneGUIDelegate += OnSceneGUI;
                //EditorApplication.playmodeStateChanged += OnPlayModeChanged;
            }
            initialized = true;
        }

        // if node is selected;
        if (Selection.activeGameObject != null)
        {
            PathRoot pr = Selection.activeGameObject.GetComponent<PathRoot>();
            if (pr != null)
            {
                Target = pr.gameObject;
                Nodemarker = pr.NodeMarker;
            }
        }
    }

    // check the click event here
    void OnSceneGUI(SceneView sceneView)
    {

        // if perform right click with shift, place new marker
        if (Event.current.type == EventType.MouseDown
            && Event.current.shift == true && Event.current.control == false
            && Event.current.button == 1)
        {
            if (_enableNodeMarkerGeneration &&
               !(Target == null || Nodemarker == null))
            {
                LastCreated = CreateNewMarkerByClick();
            }
        }

        // if perform right click with contorl, connect marker
        if (Event.current.type == EventType.MouseDown
            && Event.current.shift == false && Event.current.control == true
            && Event.current.button == 1)
        {
            ConnectToClickedMarker();
        }

        Repaint();
    }

    // create root node and return it
    GameObject CreateRootNode()
    {
        if (Target == null || Nodemarker == null) return null;

        string newpathRootName = "New Path";

        // find unique name
        for (int i = 1; i < 1000; i++)
        {
            if (GameObject.Find(newpathRootName + i.ToString())) continue;
            newpathRootName += i.ToString();
            break;
        }

        return PathRoot.CreateRootNode(newpathRootName, AssetPath);
    }



    GameObject CreateNewMarkerByClick()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log("Path node is created at " + hit.point.ToString());

            GameObject previosNode = GetPreviousNode();

            int nodes_count = Target.transform.childCount + 1;
            Quaternion prev_node_rotation = previosNode.transform.rotation;

            // Create Marker at the point
            GameObject created = CreateNewNode(nodes_count, hit.point, prev_node_rotation, previosNode);

            // look something
            created.GetPathNode().Look(3);
            previosNode.GetPathNode().Look(_lookTypeOnNodeCreation);

            if(_currentPath!=null)
                _currentPath.UpdatePath();

            return created;
        }
        else
            return null;
    }

    GameObject CreateNewNode(int nodenum, Vector3 position, Quaternion rotation, GameObject previosNode = null)
    {
        // Create Marker at the point
        GameObject created = GameObject.Instantiate(Nodemarker);
        
        created.name = "Node" + nodenum.ToString("D4");
        created.transform.parent = Target.transform;
        created.transform.position = position;
        created.transform.rotation = rotation;

        // set next null
        PathNode created_node = created.GetPathNode();
        if (created_node != null) created_node.NextNode = null;

        // set previous optionaly
        if (previosNode != null)
        {
            // if previous node is directed, make a link
            // set link to this 
            created_node.SetPreviousNode(previosNode);
            SetNextNode(previosNode, created);


        }

        return created;
    }

    void ConnectToClickedMarker()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // check the clicked object is node or not
            if (hit.collider.gameObject == null) return;
            if (Selection.activeGameObject == null) return;

            // connect arrow to next
            SetNextNode(Selection.activeGameObject, hit.collider.gameObject);
        }
    }


    // set next node and return true if it success
    bool SetNextNode(GameObject trg_from, GameObject trg_to )
    {
        PathNode to = trg_to.GetPathNode();
        PathNode from  = trg_from.GetPathNode();

        if(to != null && from != null)
        {
            from.NextNode = to.gameObject;
            return true;
        }
        return false;
    }

    GameObject GetPreviousNode()
    {
        GameObject previosNode;
        PathNode pn = Selection.gameObjects.Last().GetComponent<PathNode>();

        if (pn != null)
        {   // PathNode is selected, so use selected as previous node
            previosNode = pn.gameObject;
        }
        else
        {   // PathNode is not selected.
            // so use last gameObject in path nodes under root;
            int n_nodes = Target.transform.childCount;
            previosNode = Target.transform.GetChild(n_nodes - 1).gameObject;
        }
        return previosNode;
    }



    void DetectAndSetPathRoot()
    {
        if (Selection.gameObjects.Count() < 1) return;

        PathNode pn = Selection.gameObjects.First().GetComponent<PathNode>();

        if (pn == null) return;
        if (pn.transform.parent == null) return;
        GameObject root = pn.transform.parent.gameObject;
        Target = root;
        _currentPath = root.GetComponent<PathRoot>();
        Nodemarker = _currentPath.NodeMarker;
    }

    void OutputTest()
    {
        PathRoot proot = Target.GetComponent<PathRoot>();
        if (proot == null) return;
        
        // draw connected graph
        for (int i = 0; i < proot.StartNodes.Length; i++)
        {
            PathNode sn = proot.StartNodes[i].GetComponent<PathNode>();
            if (sn == null) continue;

            HashSet<GameObject> bAlreadyReached = new HashSet<GameObject>();

            // start nodes are done
            bAlreadyReached.Add(proot.StartNodes[i]);

            List<PathNode> one_path = new List<PathNode>();
            one_path.Add(sn);

            // follow links
            while (sn.NextNode != null)
            {
                // if next node is already marked, finish this path.
                if (bAlreadyReached.Contains(sn.NextNode)) break;

                PathNode next_node = sn.NextNode.GetComponent<PathNode>();
                if (next_node == null) break;

                // draw arrow to next
                one_path.Add( next_node );
                bAlreadyReached.Add(sn.gameObject);

                sn = next_node;
            }
            PathToPoints ptp = new PathToPoints(one_path.ToArray());
            ptp.MakeLinearPointsByResample(_outputResampleThreashold, _outputInterpolatinEnabled);

            string str = _filename;

            if (proot.StartNodes.Length != 1)
            {
                if (str.EndsWith(".csv")) str.Replace(".csv", "");
                str = str + i.ToString() + ".csv";
            }
            PathWriter.WriteToCsv(str, ptp.computed);

        }

        //var path_nodes = pr.PathNodes.Select(o => o.GetComponent<PathNode>());


        //PathToPoints ptp = new PathToPoints(path_nodes.ToArray());
        //ptp.MakeLinearPointsByResample( outputResampleThreashold, outputInterpolatinEnabled );
        //PathWriter.WriteToCsv(filename, ptp.computed);

    }


    void VertexExport()
    {
        if( string.IsNullOrEmpty( _pointExportFileName ) )return;

        string data;
        if (Selection.activeGameObject == null) return;
        data = GetPointStringsFromMesh(Selection.activeGameObject);

        if (_vexportOutputInAdditionMode)
        {
            System.IO.File.AppendAllText(_pointExportFileName,data);
        }
        else
        {
            System.IO.File.WriteAllText(_pointExportFileName, data);
        }

    }

    void LoadFromFile()
    {
        LoadNodesFromFile loader = new LoadNodesFromFile();

        loader.LoadAuto("", _loadfileFileType);

        GameObject directory = new GameObject("LoadedPath");

        for (int i = 0; i < loader.Paths.Count; i++)
        {
            CreatePathNodesFromPathPoints(loader.Paths[i], directory);
        }
    }

    void CreatePathNodesFromPathPoints(LoadNodesFromFile.PathPoints pathPoints, GameObject parent)
    {
            GameObject newRoot = 
                PathRoot.CreateRootNode( pathPoints.unique_name, AssetPath );
            newRoot.transform.parent = parent.transform;

            var rootScript = newRoot.GetComponent<PathRoot>();
            GameObject startNode = rootScript.StartNodes[0];

            GameObject previousNode = startNode;
            Target = newRoot;
            for (int j = 0; j < pathPoints.points.Length; j++)
            {
                GameObject created = CreateNewNode(j, pathPoints.points[j], Quaternion.identity, previousNode);
                previousNode = created;
            }
    }



    void AddPoint(ref StringBuilder sb, Vector3 pt)
    {
        sb.Append(pt.x.ToString()).Append(", ");
        sb.Append(pt.y.ToString()).Append(", ");
        sb.Append(pt.z.ToString()).AppendLine();
    }

    /// <summary>
    /// Get point from mesh
    /// </summary>
    string GetPointStringsFromMesh(GameObject mesh)
    {
        string ret = "";
        StringBuilder sb = new StringBuilder(100000);

        var points = GetVertexPoints(mesh);

        foreach (Vector3 item in points)
        {
            AddPoint(ref sb, item);
        }

        ret = sb.ToString();

        return ret;
    }

    List<Vector3> GetVertexPoints(GameObject mesh)
    {
        MeshFilter mf = mesh.GetComponent<MeshFilter>();
        if (mf == null) return null;
        //Vector3[] test = mf.mesh.vertices;
        Vector3[] test = mf.sharedMesh.vertices;

        int[] ptidx = mf.sharedMesh.triangles;

        Matrix4x4 trans = mesh.transform.localToWorldMatrix;

        List<Vector3> transformedVertices = test.Select(o => trans.MultiplyPoint(o)).ToList();

        if (_vexportOutputEdgeCenter)
        {
            for (int i = 0; i < ptidx.Length; i += 3)
            {
                Vector3 center1 = (transformedVertices[ptidx[i]] + transformedVertices[ptidx[i + 1]]) / 2;
                transformedVertices.Add(center1);
                Vector3 center2 = (transformedVertices[ptidx[i + 1]] + transformedVertices[ptidx[i + 2]]) / 2;
                transformedVertices.Add(center2);
                Vector3 center3 = (transformedVertices[ptidx[i]] + transformedVertices[ptidx[i + 2]]) / 2;
                transformedVertices.Add(center3);
            }
        }
        return transformedVertices.ToList();
    }




    void VertexToPath()
    {
        if (Selection.activeGameObject == null) return;

        // Get points,
        var data = GetVertexPoints(Selection.activeGameObject);

        // Compute Path from Points
        PointsToPath computer = new PointsToPath(data);
        var pathAnchors =  computer.GetPathPoints(new Vector3(0, 0, 0), new Vector3(1, 0, 0));


        //PathToPoints pp = new PathToPoints();
        LoadNodesFromFile.PathPoints pp = new LoadNodesFromFile.PathPoints();
        pp.unique_name = "testNodes";
        pp.points = pathAnchors.ToArray();
        pp.rotations = Enumerable.Repeat(Quaternion.identity, pp.points.Length).ToArray();

        GameObject directory = new GameObject("CreatedPath");

        CreatePathNodesFromPathPoints(pp, directory);

    }
}


#endif


internal static class PathNodeExtentions
{
    // extention for simple access
    public static PathNode GetPathNode(this GameObject trg)
    {
        return trg.GetComponent<PathNode>();
    }

}
