using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode()]
[System.Serializable]
public class PathRoot : MonoBehaviour {

    public GameObject NodeMarker;
    public GameObject[] StartNodes;
    public GameObject[] PathNodes;

	// Use this for initialization
	void Start () {
#if UNITY_EDITOR

#else
        // invisible in play
        var meshrenderer_list = gameObject.GetComponentsInChildren<Renderer>();
        meshrenderer_list.Select(o => o.enabled = false);
        gameObject.SetActive(false);
#endif
    }

    // Update is called once per frame
    void Update () {

        UpdatePath();
    }

    public void UpdatePath()
    {
#if UNITY_EDITOR
        // get path nodes 
        Transform[] objList = gameObject.transform.GetComponentsInChildren<Transform>();

        List<GameObject> tmp_list = new List<GameObject>();

        for(int i=0; i < objList.Count(); i ++ )
        {
            if (objList[i].name == gameObject.name) continue;
            PathNode node_script = objList[i].GetComponentInChildren<PathNode>();
            if (node_script !=null )
            {
                tmp_list.Add(node_script.gameObject);
            }
        }

        PathNodes = tmp_list.ToArray();
#endif

    }

    static public GameObject CreateRootNode(string NewRootNodeName, string AssetPath)
    {
        var tmpr = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>(AssetPath + "RootNode"));
        tmpr.name = NewRootNodeName;
        var tmp = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>(AssetPath + "NodeStart"));
        tmp.name = "StartNode";
        tmp.transform.parent = tmpr.transform;

        PathRoot pr = tmpr.GetComponent<PathRoot>();
        if (pr != null)
        {
            pr.StartNodes = new GameObject[1] { tmp };
        }
        return tmpr;
    }

}
