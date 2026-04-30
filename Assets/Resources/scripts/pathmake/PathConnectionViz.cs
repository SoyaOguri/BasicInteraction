using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class PathConDraw {


    static public void DrawAllArrow(PathRoot proot)
    {
#if UNITY_EDITOR
        if (proot == null) return;

        if (!PathMakerWindow.bShowAllConnect) return;

        proot.UpdatePath();

        GameObject[] path_nodes = proot.PathNodes;

        if (path_nodes == null) return;

        HashSet<GameObject> bAlreadyDrawn = new HashSet<GameObject>();


        // draw connected graph
        for (int i = 0; i < proot.StartNodes.Length; i++)
        {
            PathNode sn = proot.StartNodes[i].GetComponent<PathNode>();
            if (sn == null) continue;

            HashSet<GameObject> bAlreadyDrawnInOnePath = new HashSet<GameObject>();

            // start nodes are done
            bAlreadyDrawnInOnePath.Add(proot.StartNodes[i]);

            // follow links
            while (sn.NextNode != null)
            {
                // if next node is already marked, finish this path.
                if (bAlreadyDrawnInOnePath.Contains(sn.NextNode)) break;

                PathNode next_node = sn.NextNode.GetComponent<PathNode>();
                if (next_node == null) break;

                // draw arrow to next
                Vector3 arrow_pos = sn.gameObject.transform.position;

                Handles.color = new Color(1, 1, 0, 0.5f);

                //Handles.DrawLine(arrow_pos, t.NextNode.transform.position);
                Vector3[] tmp = new Vector3[2];
                tmp[0] = arrow_pos;
                tmp[1] = sn.NextNode.transform.position;
                Handles.DrawAAPolyLine(5, tmp);
                bAlreadyDrawnInOnePath.Add(sn.gameObject);

                sn = next_node;
            }
            bAlreadyDrawn.UnionWith(bAlreadyDrawnInOnePath);
        }

        // draw arrow for which the nodes can not be reached
        // (draw red for warning)
        foreach (var target_obj in path_nodes)
        {
            PathNode trg = target_obj.GetComponent<PathNode>();
            if (trg == null) continue;
            if (bAlreadyDrawn.Contains(target_obj)) continue;
            if (trg.NextNode != null)
            {
                Vector3 arrow_pos = trg.gameObject.transform.position;

                Handles.color = Color.red;

                //Handles.DrawLine(arrow_pos, t.NextNode.transform.position);
                Vector3[] tmp = new Vector3[2];
                tmp[0] = arrow_pos;
                tmp[1] = trg.NextNode.transform.position;
                Handles.DrawAAPolyLine(5, tmp);
            }
        }
#endif
    }

    public static void DrawArrowToNext(PathNode t)
    {
#if UNITY_EDITOR
        if (!PathMakerWindow.bShowToNext) return;

        if (t.NextNode != null)
            {
                Vector3 arrow_pos = t.transform.position;

                Handles.color = Color.green;

                //Handles.DrawLine(arrow_pos, t.NextNode.transform.position);
                Vector3[] tmp = new Vector3[2];
                tmp[0] = arrow_pos;
                tmp[1] = t.NextNode.transform.position;
                Handles.DrawAAPolyLine(7, tmp);
            }
#endif

    }
}

