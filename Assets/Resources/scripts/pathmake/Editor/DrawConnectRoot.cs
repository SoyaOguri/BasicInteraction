using UnityEngine;

using System.Collections;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(PathRoot))]
public class DrawConnectRoot : Editor
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

		PathRoot t = target as PathRoot;

		PathConDraw.DrawAllArrow(t);

		//PathNode firstnode = t.gameObject.GetComponentsInChildren<PathNode>().First();

		//if ( firstnode !=null )
		//    PathConDraw.DrawArrowToNext(firstnode);

		DrawGizmoButton(t);
	}
	
	void DrawGizmoButton(PathRoot t)
	{

		// draw UI for whole path

		//Handles.BeginGUI();
		//Vector2 buttonSize = new Vector2(60, 20);

		//var sceneCamera = SceneView.currentDrawingSceneView.camera; ;

		//var pos = sceneCamera.WorldToScreenPoint(t.transform.position);

		//var alignGlobalButton = new Rect(
		//        pos.x,// - buttonSize.x * 0.5f,
		//        SceneView.currentDrawingSceneView.position.height - pos.y - buttonSize.y * 0.5f,
		//        buttonSize.x,
		//        buttonSize.y);


		//if (GUI.Button(alignGlobalButton, "Align"))
		//{
		//    t.transform.rotation = Quaternion.identity;
		//}

		//if (t.NextNode != null)
		//{
		//    var lookNext = alignGlobalButton;
		//    lookNext.position += new Vector2(0, buttonSize.y);

		//    if (GUI.Button(lookNext, "Look"))
		//    {

		//        t.transform.rotation =
		//             // Quaternion.FromToRotation(t.transform.position, t.NextNode.transform.position);
		//             Quaternion.LookRotation(t.NextNode.transform.position - t.transform.position)
		//             * Quaternion.Euler(0, -90, 0);
		//    }
		//}

		//if (GUI.changed)
		//{
		//    EditorUtility.SetDirty(t);
		//}

		//Handles.EndGUI();
	}

#endif
}
