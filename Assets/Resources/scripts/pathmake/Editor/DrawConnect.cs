using UnityEngine;
using System.Collections;


#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(PathNode))]
public class DrawConnect : Editor
{

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    // 
    void OnSceneGUI()
    {
        PathNode t = target as PathNode;

        PathRoot proot = null;

        if (t.gameObject.transform.parent == null) return;
        GameObject root = t.gameObject.transform.parent.gameObject;
        if (root == null) return;

        proot = root.GetComponent<PathRoot>();
        if (proot == null) return;

        PathConDraw.DrawAllArrow(proot);
        PathConDraw.DrawArrowToNext(t);

        //if(t.NextNode != null)
        //{ 
        //    Vector3 arrow_pos = t.transform.position;

        //    Handles.color = Color.green;
            
        //    //Handles.DrawLine(arrow_pos, t.NextNode.transform.position);
        //    Vector3[] tmp = new Vector3[2];
        //    tmp[0] = arrow_pos;
        //    tmp[1] = t.NextNode.transform.position;
        //    Handles.DrawAAPolyLine(7, tmp);
        //}

        DrawGizmoButton(t);
    }
    
    void DrawGizmoButton(PathNode t)
    {
        Handles.BeginGUI();
        Vector2 buttonSize = new Vector2(60, 20);

        var sceneCamera = SceneView.currentDrawingSceneView.camera; ;

        var pos = sceneCamera.WorldToScreenPoint(t.transform.position);

        var alignGlobalButton = new Rect(
                pos.x,// - buttonSize.x * 0.5f,
                SceneView.currentDrawingSceneView.position.height - pos.y - buttonSize.y * 0.5f,
                buttonSize.x,
                buttonSize.y);


        if (GUI.Button(alignGlobalButton, "Align"))
        {
            t.transform.rotation = Quaternion.identity;
        }

        if (t.NextNode != null)
        {
            var lookNext = alignGlobalButton;
            lookNext.position += new Vector2(0, buttonSize.y);

            if (GUI.Button(lookNext, "Look"))
            {

                t.transform.rotation =
                     // Quaternion.FromToRotation(t.transform.position, t.NextNode.transform.position);
                     Quaternion.LookRotation(t.NextNode.transform.position - t.transform.position)
                     * Quaternion.Euler(0, -90, 0);
            }
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(t);
        }

        Handles.EndGUI();
    }

#endif
}
