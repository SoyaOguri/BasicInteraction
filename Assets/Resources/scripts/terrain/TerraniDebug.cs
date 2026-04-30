using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System;
#if UNITY_EDITOR

using UnityEditor;


[System.Serializable]
public class TerraniDebug : EditorWindow
{
    // Path of this script
    public string AssetPath = "";

    // add menu item
    [MenuItem("Window/Terrain debugger")]
    static void Open()
    {
        // show editor window
        EditorWindow.GetWindow<TerraniDebug>("Terrain debug");
    }

    string _filename;

    void DrawSeparator() { GUILayout.Box("", GUILayout.Width(this.position.width - 5), GUILayout.Height(1)); }

    void OnGUI()
    {
        EditorGUILayout.LabelField("<Terrain debugger v0.1 by Okuda>");

        LayoutMarkerLoadFromFileSection();
        
    }


    void LayoutMarkerLoadFromFileSection()
    {
        EditorGUILayout.LabelField("File type: .stl ");

        bool loadFilePushed = GUILayout.Button("Load file");

        if (loadFilePushed)
        {
            LoadFromFile();
        }
    }

 // check the click event here
    void OnSceneGUI(SceneView sceneView)
    {
        Repaint();
    }
    
    void LoadFromFile()
    {

        //LoadNodesFromFile loader = new LoadNodesFromFile();

        _filename = EditorUtility.OpenFilePanel(
                    "Select File",
                    Application.dataPath,
                    "csv"
                 );


        GameObject directory = new GameObject("Terrain points");

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sphere.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        using (StreamReader sr = new StreamReader(_filename))
        {
            LoadPoints(sr, directory, sphere);
        }

    }

    void LoadPoints(StreamReader sr, GameObject parent, GameObject sphere)
    {
        int i = 0;
        while (!sr.EndOfStream)
        {
            i++;
            var line = sr.ReadLine();
            var token = line.Split(',');
            bool parseSucceeded = false;
            float x, y, z;
            Vector3 pos;
            if (token.Length >= 3)
            {   // parse XYZ
                parseSucceeded = float.TryParse(token[0], out x);
                parseSucceeded = parseSucceeded & float.TryParse(token[1], out y);
                parseSucceeded = parseSucceeded & float.TryParse(token[2], out z);

                if (parseSucceeded)
                {
                    pos.x = x;
                    pos.y = z;
                    pos.z = y;

                    var tmp = Instantiate(sphere);
                    tmp.name = "Points" + i.ToString();
                    tmp.transform.position = new Vector3(x,z,y);
                    tmp.transform.parent = parent.transform;
                }
            }
        }
    }



}


#endif

