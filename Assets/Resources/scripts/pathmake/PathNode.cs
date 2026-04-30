using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum PATH_NODE_TYPE
{
    LINE,
    ARC,
}


[ExecuteInEditMode()]
[System.Serializable]
public class PathNode : MonoBehaviour {

    [SerializeField]
    public GameObject NextNode;

    [SerializeField]
    public GameObject PreviousNode;

    [SerializeField]
    public PATH_NODE_TYPE NodeType;

    public double DistanceToNext = 0;
    //private Vector3 pos_from;
    //private Vector3 pos_to;

	// Use this for initialization
	void Start ()
	{

    }

    // Update is called once per frame
    void Update () {
	    if( NextNode!=null )
        {
            //transform.rotation = Quaternion.FromToRotation
            //    (transform.position, NextNode.transform.position);
            UpdateLength();
        }
	}

    // Update length of 
    public double UpdateLength()
    {
        if (NextNode == null)
        {
            DistanceToNext = 0;
            return DistanceToNext;
        }

        // pos_from = transform.position;
        // pos_to = NextNode.transform.position;

        switch (NodeType)
        {
            default:
            case PATH_NODE_TYPE.LINE:
                DistanceToNext = Vector3.Distance(transform.position, NextNode.transform.position);
                break;
                
        }
        
        return DistanceToNext;
    }

    // Get point of node
    public PositionWithUpVector GetNodePoint()
    {
        return new PositionWithUpVector() { pos = transform.position, up = transform.up };
    }

    // Get point of node
    public PositionWithUpVector GetNodePointByNormalizedT(float t)
    {
        return new PositionWithUpVector() { pos = GetNodePosByNormalizedT(t), up = GetNodeUpByNormalizedT(t) };
    }

    /// <summary>
    /// Node position interpolation from normalized t
    /// </summary>
    /// <param name="t">normalized parameter of this section</param>
    /// <returns></returns>
    public Vector3 GetNodePosByNormalizedT(float t)
    {
        Vector3 ret;
        switch (NodeType)
        {
            default:
            case PATH_NODE_TYPE.LINE:
                ret = Vector3.Lerp(transform.position, NextNode.transform.position, t);
                break;
        }

        return ret;
    }

    /// <summary>
    /// Node position interpolation from normalized t
    /// </summary>
    /// <param name="t">normalized parameter of this section</param>
    /// <returns></returns>
    public Vector3 GetNodeUpByNormalizedT(float t)
    {
        Vector3 ret;
        switch (NodeType)
        {
            default:
            case PATH_NODE_TYPE.LINE:
                ret = Vector3.Lerp(transform.up, NextNode.transform.up, t).normalized;
                break;
        }

        return ret;
    }

    /// <summary>
    /// Get interpolated points
    /// </summary>
    /// <param name="interpolate_threashold"></param>
    /// <returns></returns>
    public PositionWithUpVector[] GetInterpolatedPoints(float interpolate_threashold)
    {
        PositionWithUpVector[] ret = null;
        if (NextNode == null) return null;

        switch( NodeType )
        {
            default:
            case PATH_NODE_TYPE.LINE:
                UpdateLength();
                int N = (int)Math.Ceiling(DistanceToNext / interpolate_threashold);
                if (N < 1) N = 1;
                ret = new PositionWithUpVector[N];

                for ( int i=0;i< N;i++ )
                {
                    ret[i] = GetNodePointByNormalizedT(i / (float)N);
                }
                break;
        }
        return ret;
    }

    // set next node and return true if it success
    public bool SetNextNode(GameObject trg_to)
    {
        PathNode to = trg_to.GetComponent<PathNode>();

        if (to != null)
        {
            NextNode = to.gameObject;
            return true;
        }
        return false;
    }
    public bool SetPreviousNode(GameObject trg_frm)
    {
        PathNode to = trg_frm.GetComponent<PathNode>();

        if (to != null)
        {
            PreviousNode = to.gameObject;
            return true;
        }
        return false;
    }

    public void LookNext()
    {
        if( NextNode != null )
        transform.rotation =
            Quaternion.LookRotation(NextNode.transform.position - transform.position)
                * Quaternion.Euler(0, -90, 0);
    }
    public void LookMean()
    {
        if (NextNode != null && PreviousNode != null)
            transform.rotation =
                Quaternion.LookRotation(NextNode.transform.position - PreviousNode.transform.position)
                    * Quaternion.Euler(0, -90, 0);
    }
    public void LookSameAsPrevious()
    {
        if (PreviousNode != null)
            transform.rotation = PreviousNode.transform.rotation;
    }

    public enum LookingDirection
    {
        None = 0,
        Next = 1,
        Average = 2,
        SameAsPrevious = 3,
    }
    
    /// <summary>
    /// Look something
    /// </summary>
    /// <param name="type">what the node to see</param>
    /// 0: none(x+ direction)
    /// 1: here to next node
    /// 2: from prev node to next node
    /// 3: same as prev
    public void Look(int type)
    {
        switch( type )
        {
            default:
                transform.rotation = Quaternion.identity;
                break;
            case 0:
                transform.rotation = Quaternion.identity;
                break;
            case 1:
                LookNext();
                break;
            case 2:
                LookMean();
                break;
            case 3:
                LookSameAsPrevious();
                break;
        }
    }

    /// <summary>
    /// See Look(int type) function.
    /// </summary>
    /// <param name="type"></param>
    public void Look(LookingDirection type)
    {
        Look((int) type);
    }
}

