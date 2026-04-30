using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor.Experimental.UIElements.GraphView;

/// <summary>
/// Make path points from dedicated point list
/// </summary>
public class PointsToPath
{
    private List<PositionWithUpVector> _pos;

    public float ClusteringDistance = 10;

    public PointsToPath(IList<PositionWithUpVector> posWithUpVec)
    {
        _pos = posWithUpVec.ToList();
    }

    public PointsToPath(IList<Vector3> pos)
    {
        _pos = pos.Select(o => new PositionWithUpVector(o)).ToList();
    }

    public List<Vector3> GetPathPoints(Vector3 startPoint, Vector3 direction)
    {
        ShrinkPointsToPath();
        return SortPoint(_pos.Select(o=>o.pos).ToList(), startPoint, direction.normalized);
    }

    List<Vector3> ShrinkPointsToPath()
    {
        var positions = _pos.Select(o => o.pos).ToArray();
        int n = positions.Count();
        float[,] pairWiseDistance = new float[n, n];

        bool[] deleted = new bool[n]; 
        List<Vector3> shrinked = new List<Vector3>();

        for (int i = 0; i < n; i++)
        {
            if (deleted[i]) continue;
            Vector3 from = positions[i];
            Vector3 tmp = from;
            for (int j = i; j < n; j++)
            {
                if (Mathf.Abs(positions[j].x - from.x) > ClusteringDistance) continue;
                if (Mathf.Abs(positions[j].y - from.y) > ClusteringDistance) continue;
                Vector3 diff = positions[j] - from;
                float dist = Norm(diff);
                if (dist > ClusteringDistance)
                {
                    deleted[j] = true;
                    tmp = (tmp + positions[j]) / 2;
                }
            }
            shrinked.Add(tmp);
        }

        return shrinked;
    }

    List<Vector3> SortPoint(List<Vector3> target, Vector3 startPoint, Vector3 direction)
    {
        target.Insert(0,startPoint);
        int n = target.Count;
        bool[] used = new bool[n];

        Func<Vector3, Vector3, float> dotProd = 
            (Vector3 a, Vector3 b) => { return a.x * b.x + a.y * b.y + a.z * b.z; };

        List<float[]> pairWiseDist = new List<float[]>();
        for (int q = 0; q < n; q++)
        {
            pairWiseDist[q] = new float[n];
        }

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                pairWiseDist[i][j] = Norm(target[i] - target[j]);
            }
        }

        List<Vector3> res = new List<Vector3>();
        int prevIndex = 0;
        Vector3 prevDirection = direction;
        // loop while all is done
        for (int q = 0; q < n; q++)
        {
            int[] index = new int[n];
            Array.Sort(pairWiseDist[prevIndex], index);
            for (int i = 1; i < n; i++)
            {
                if(used[index[i]])continue;
                //check whether direction met from minimum
                Vector3 canditate = target[index[i]];
                Vector3 diff = canditate - target[prevIndex];
                if (dotProd(diff, prevDirection) >= 0)
                {
                    res.Add(canditate);
                    used[index[i]] = true;
                    prevIndex = index[i];
                    prevDirection = diff.normalized;
                    break;
                }
            }
        }

        return res;
    }

    private int ArgMin( IEnumerable<float> values)
    {
        return values.Select( 
                (val, idx) => new { V = val, I = idx } )
            .Aggregate((max, working) => (max.V > working.V) ? max : working)
            .I;
    }

    private float Norm(Vector3 vec)
    {
        return Mathf.Sqrt(vec.x * vec.x + vec.y * vec.y + vec.z * vec.z);
    }

}

#endif
