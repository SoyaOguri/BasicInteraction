using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

#if UNITY_EDITOR

using UnityEditor;
#endif

// ReSharper disable once CheckNamespace
public class LoadNodesFromFile
{

    public List<PathPoints> Paths = new List<PathPoints>();

#if UNITY_EDITOR
    private string[] _extentions = new string[]{
                "xml", "csv", "txt"
            };
#endif

    public LoadNodesFromFile()
    {

    }
    
    public void LoadAuto(string file, int type)
    {
#if UNITY_EDITOR
        string extention = type < _extentions.Length ? _extentions[type] : "txt";


        string filePath = EditorUtility.OpenFilePanel(
                            "Select File",
                            Application.dataPath,
                            extention
                         );

        if (string.IsNullOrEmpty(filePath)) return;

        switch (type)
        {
            case 0:
                LoadPathNodesFromUCWinTrafficXML(filePath);
                break;
            case 1:
                LoadPathNodesFromCSV(filePath);
                break;
            case 2:
            default:
                LoadPathNodesFromDsCourseTxt(filePath);
                break;
        }
#endif

    }

    //------------------------------ for XML from UC-win load
    
    public void LoadPathNodesFromUCWinTrafficXML(
        string filename
        )
    {
#if UNITY_EDITOR
        ParseXML(filename);
#endif
    }

    private void ParseXML(string filename)
    {

        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.Load(filename);

        // Pickup moving elements;
        XmlNodeList nodeList = xmlDocument.SelectNodes("Simulations/Simulation/Frames/Frame/MovingElmt");

        // Get point lists for each moving elements;
        for (int i = 0; i < nodeList.Count; i++)
        {
            //System.Diagnostics.Debug.Print(nodeList[i].InnerText + "\n");

            // get ID and make ID+MeshName stiring as a tag;
            int id = 0;
            int.TryParse(nodeList[i].Attributes["ID"].InnerText, out id);
            string meshname = nodeList[i].Attributes["Type"].InnerText;
            string unique_name = id.ToString("D4") + meshname.Replace(',', '_').Replace(' ', '_');
            string[] position_str_array = getPositionStringArray(nodeList[i]);
            string[] direction_str_array = getDirectionStringArray(nodeList[i]);



            PathPoints tmp = new PathPoints();
            tmp.unique_name = unique_name;
            tmp.points = parseVector(position_str_array);
            tmp.rotations = directionToQuaternion(parseVector(direction_str_array));

            Paths.Add(tmp);
        }

    }

    private string[] getPositionStringArray(XmlNode targetNode)
    {
        XmlNodeList plists = targetNode.SelectNodes("St/P");
        string[] ret = plists.Cast<XmlNode>().Select(o => o.InnerText).ToArray();
        return ret;
    }

    private string[] getDirectionStringArray(XmlNode targetNode)
    {
        XmlNodeList plists = targetNode.SelectNodes("St/D");
        string[] ret = plists.Cast<XmlNode>().Select(o => o.InnerText).ToArray();
        return ret;
    }

    // ---------------------------------- from CSV file

    public void LoadPathNodesFromCSV(string filename)
    {
        using (StreamReader sr = new StreamReader(filename))
        {
            ParseCsvFile(sr);
        }
    }

    public void ParseCsvFile(StreamReader sr)
    {
        string line = sr.ReadLine();
        if(string.IsNullOrEmpty(line))return;
        // Check first line is tag or not
        char[] delimiters = new char[] {',', '\t'};
        var tokens = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        float dummy;
        if (tokens.Any(str => float.TryParse(str, out dummy)==false))
        {   // first line is not number
            line = sr.ReadLine();
            tokens = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }

        int n = 1;
        PathPoints points = new PathPoints();
        points.unique_name = "Path 1";
        List<Vector3> posList = new List<Vector3>();
        List<Vector3> rotList = new List<Vector3>();
        while (true)
        {
            Vector3 pos = new Vector3();
            Vector3 upVec = new Vector3(0, 1, 0);
            if (tokens.Length >= 3)
            {
                float.TryParse(tokens[0], out pos.x);
                float.TryParse(tokens[1], out pos.y);
                float.TryParse(tokens[2], out pos.z);
            }
            else if(tokens.Length>=6)
            {
                float.TryParse(tokens[3], out upVec.x);
                float.TryParse(tokens[4], out upVec.y);
                float.TryParse(tokens[5], out upVec.z);
            }
            posList.Add(pos);
            rotList.Add(upVec);

            if (sr.EndOfStream) break;
            line = sr.ReadLine();
            tokens = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }

        points.points = posList.ToArray();
        points.rotations = directionToQuaternion(rotList.ToArray());
        Paths.Add(points);
    }


    // ---------------------------------- from DS course file


    public void LoadPathNodesFromDsCourseTxt(
        string filename
        )
    {
        using (StreamReader sr = new StreamReader(filename))
        {
            ParseDsCourseFile(sr);
        }
    }

    public void ParseDsCourseFile(StreamReader sr)
    {
        string line = sr.ReadLine();

        if( string.IsNullOrEmpty(line))return;

        char[] delimiters = new[] {'\t', ',',' '};

        // if first line is line number;
        string[] devided = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

        int lineCount = 0;
        if (devided.Length == 1)
        {
            int.TryParse(devided[0], out lineCount);
            line = sr.ReadLine();
            devided = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }

        List<Vector3> positions = new List<Vector3>();
        List<Vector3> ups = new List<Vector3>();

        //List<Quaternion> rotations = new List<Quaternion>();

        while (!sr.EndOfStream)
        {
            devided = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            bool parseSucceeded = false;
            float x, y, z;
            Vector3 pos;
            if(devided.Length >= 3)
            {   // parse XYZ
                parseSucceeded = float.TryParse(devided[0], out x);
                parseSucceeded = parseSucceeded & float.TryParse(devided[1], out y);
                parseSucceeded = parseSucceeded & float.TryParse(devided[2], out z);

                if (parseSucceeded)
                {
                    pos.x = x;
                    pos.y = z;
                    pos.z = y;
                    positions.Add(pos);
                }
            }
            Vector3 up;
            if (devided.Length >= 6)
            {   // parse Up vector
                parseSucceeded = float.TryParse(devided[0], out x);
                parseSucceeded = parseSucceeded & float.TryParse(devided[1], out y);
                parseSucceeded = parseSucceeded & float.TryParse(devided[2], out z);

                if (parseSucceeded)
                {
                    up.x = x;
                    up.y = y;
                    up.z = z;
                    ups.Add(up);
                }
            }
            
            line = sr.ReadLine();
        }

        PathPoints paths = new PathPoints();
        paths.unique_name = "Path";
        paths.points = positions.ToArray();
        //paths.rotations = new Quaternion[];

        Paths.Add(paths);

    }
    





    

    Vector3[] parseVector(string[] position_str)
    {
        int n = position_str.Length;
        Vector3[] ret = new Vector3[n];
        for( int i = 0; i< n; i++ )
        {
            string[] xyz = position_str[i].Split(' ');
            ret[i].x = float.Parse(xyz[0]);
            ret[i].y = float.Parse(xyz[1]);
            ret[i].z = float.Parse(xyz[2]);
        }
        return ret;
    }

    static Quaternion[] directionToQuaternion( Vector3[] direction )
    {
        int n = direction.Length;
        Quaternion[] ret = new Quaternion[n];
        for (int i = 0; i < n; i++)
        {
            ret[i] = Quaternion.Euler(direction[i]);
        }
        return ret;
    }


    public class PathPoints
    {
        public string unique_name;
        public Vector3[] points;
        public Quaternion[] rotations;

        public PathPoints()
        {

        }

        public PathPoints(string name, Vector3[] pos)
        {
            unique_name = name;
            points = pos;
            rotations = Enumerable.Repeat
                (Quaternion.identity, pos.Length).ToArray();
        }

        public PathPoints(string name, Vector3[] pos, Quaternion[] rot)
        {
            unique_name = name;
            points = pos;
            rotations = rot;
        }

        public PathPoints(string name, Vector3[] pos, Vector3[] up)
        {
            unique_name = name;
            points = pos;
            rotations = directionToQuaternion(up);
        }

    }


    
}

