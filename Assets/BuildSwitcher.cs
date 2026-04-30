using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BuildSwitcher : MonoBehaviour
{

    public List<BuildSwitchTarget> targets;



    //// Start is called before the first frame update
    //void Start()
    //{
        
    //}

    //// Update is called once per frame
    //void Update()
    //{
        
    //}

    public void SwitchCamera(int targetNum)
    {
        List<GameObject> list = GetAllCameraObjects();
        foreach (var item in list)
        {
            item.SetActive(false);
        }

        foreach (var item in targets[targetNum].cameras)
        {
            item.SetActive(true);
        }
        
    }

    public void SwitchMultiScreenScript(int targetNum)
    {
        List<MyScriptObject> list = GetAllMultiScreenScriptObjects();

        foreach (var item in list)
        {
            if(item.name != "" && item.scriptObject != null)
            {
                Type type = Type.GetType(item.name);
                ((MonoBehaviour)item.scriptObject.GetComponent(type)).enabled = false;
            }
        }
        if (targets[targetNum].multiScreen.name != "" && targets[targetNum].multiScreen.scriptObject != null)
        {
            Type type = Type.GetType(targets[targetNum].multiScreen.name);
            ((MonoBehaviour)targets[targetNum].multiScreen.scriptObject.GetComponent(type)).enabled = true;
        }
    }

    public void SwitchCommunicationScript(int targetNum)
    {
        List<MyScriptObject> list = GetAllCommunicationScriptObjects();

        foreach (var item in list)
        {
            if (item.name != "" && item.scriptObject != null)
            {
                Type type = Type.GetType(item.name);
                ((MonoBehaviour)item.scriptObject.GetComponent(type)).enabled = false;
            }
        }

        foreach (var item in targets[targetNum].communication)
        {
            if (item.name != "" && item.scriptObject != null)
            {
                Type type = Type.GetType(item.name);
                ((MonoBehaviour)item.scriptObject.GetComponent(type)).enabled = true;
            }
        }

    }

    public List<GameObject> GetAllCameraObjects()
    {
        List<GameObject> list = new List<GameObject>();

        foreach (var item in targets)
        {
            foreach (var cam in item.cameras)
            {
                if (!list.Contains(cam))
                {
                    list.Add(cam);
                }
            }
        }

        return list;
    }

    public List<MyScriptObject> GetAllMultiScreenScriptObjects()
    {
        List<MyScriptObject> list = new List<MyScriptObject>();

        foreach (var item in targets)
        {
            if (!list.Contains(item.multiScreen))
            {
                list.Add(item.multiScreen);
            }
        }

        return list;
    }

    public List<MyScriptObject> GetAllCommunicationScriptObjects()
    {
        List<MyScriptObject> list = new List<MyScriptObject>();

        foreach (var item in targets)
        {

            foreach (var it in item.communication)
            {
                if (!list.Contains(it))
                {
                    list.Add(it);
                }
            }

        }

        return list;
    }

}

[System.Serializable]
public class MyScriptObject : System.IEquatable<MyScriptObject>
{
    public string name;
    public GameObject scriptObject;

    public bool Equals(MyScriptObject other)
    {
        if(this.name == other.name && this.scriptObject == other.scriptObject)
        {
            return true;
        }else
        {
            return false;
        }
    }
}

[System.Serializable]
public class BuildSwitchTarget
{
    public string targetName;
    public List<GameObject> cameras;
    public MyScriptObject multiScreen;
    public List<MyScriptObject> communication;
}