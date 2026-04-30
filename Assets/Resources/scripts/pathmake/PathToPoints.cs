using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public struct PositionWithUpVector
{
    public Vector3 pos;
    public Vector3 up;

    public PositionWithUpVector(Vector3 _pos, Vector3 _up)
    {
        pos = _pos;
        up = _up;
    }
    public PositionWithUpVector(Vector3 _pos)
    {
        pos = _pos;
        up = new Vector3(0,1,0);
    }


    public override string ToString()
    {
        string str = pos.x.ToString("F") + ","
            + pos.y.ToString("F") + ","
            + pos.z.ToString("F") + ","
            + up.x.ToString("F") + ","
            + up.y.ToString("F") + ","
            + up.z.ToString("F");
        return str;
    }

    public string ToString(IFormatProvider prov)
    {
        string str = pos.x.ToString(prov) + ","
            + pos.y.ToString(prov) + ","
            + pos.z.ToString(prov) + ","
            + up.x.ToString(prov) + ","
            + up.y.ToString(prov) + ","
            + up.z.ToString(prov);
        return str;
    }
}

public class PathToPoints {

    public PositionWithUpVector[] computed;
    public List<PositionWithUpVector> work;
    public PathNode[] targetnodes;

    // proper linked nodes must be applied.
    public PathToPoints( PathNode[] nodes )
    {
        targetnodes = nodes;
        work = new List<PositionWithUpVector>(nodes.Length);
    }

    public void MakeLinearPointsByResample(float interp_thrsh, bool interp_enabled)
    {
        int N = targetnodes.Length;
        for( int i = 0; i< N; i++)
        {
            if(!interp_enabled || i == N-1)
            {
                work.Add(new PositionWithUpVector { pos = targetnodes[i].transform.position, up = targetnodes[i].transform.up });
            }else if (i < N - 1)
            {
                work.AddRange( targetnodes[i].GetInterpolatedPoints(interp_thrsh));

                //var prev = targetnodes[i];
                //var post = targetnodes[i + 1];
                //Linterp(prev.transform.position, post.transform.position, prev.transform.up, post.transform.up, interp_thrsh);
            }
        }

        computed = work.ToArray();
    }

    public void Linterp(Vector3 from_p, Vector3 to_p, Vector3 from_u, Vector3 to_u, float resample_length)
    {
        float dist = Vector3.Distance(from_p, to_p);
        int n_point = (int)Math.Ceiling(dist / resample_length);
        if (n_point < 0) n_point = 1;

        Vector3[] _pos = PathUtility.Vector3LinearInterpByN(from_p, to_p, n_point);
        Vector3[] _up = PathUtility.Vector3LinearInterpByN(from_u, to_u, n_point).Select(o => o.normalized ).ToArray();

        PositionWithUpVector[] pwu = PathUtility.CreatePwUVec(_pos, _up);

        work.AddRange(pwu);

    }
}

public class PathWriter
{
    static public void WriteToCsv(string filename, PositionWithUpVector[] trg)
    {
        StreamWriter sw = new StreamWriter(filename);

        foreach(var x in trg)
        {
            sw.WriteLine(x.ToString());
        }
        sw.Close();
    }
}






// Path interpolator
public class PathUtility
{
    static public PositionWithUpVector[] CreatePwUVec( Vector3[] _pos, Vector3[] _up)
    {
        PositionWithUpVector[] ret = new PositionWithUpVector[_pos.Length];
        for(int i=0; i < _pos.Length; i++)
        {
            ret[i] = new PositionWithUpVector{ pos = _pos[i], up = _up[i] };
        }
        return ret;
    }

    static public Vector3[] Vector3LinearInterpByTh(Vector3 pos_f, Vector3 pos_t, double th)
    {
        float dist = Vector3.Distance(pos_f, pos_t);
        int n = (int)(Math.Ceiling( dist / th ));
        if (n == 0) n = 1;

        return Vector3LinearInterpByN(pos_f, pos_t, n);
    }

    static public Vector3[] Vector3LinearInterpByN(Vector3 pos_f, Vector3 pos_t, int n)
    {
        Vector3[] ret = new Vector3[n];
        for( int i = 0; i < n; i++)
        {
            ret[i] = Vector3.Lerp(pos_f, pos_t, i / (float)n);
        }
        return ret;
    }









}
