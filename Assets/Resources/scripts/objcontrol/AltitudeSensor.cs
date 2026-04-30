using UnityEngine;
using System.Collections;

public class AltitudeSensorSingle {

    // reference variables
    public Transform following_transform;
    public Vector3 center_pos;
    public float height_offset_for_ray_origin = 100;

    // return variables
    public bool ground_hit;
    public Vector3 ground_point;
    public Vector3 ground_normal;

    public RaycastHit hit;

    /// <summary>
    /// Set following transform;
    /// </summary>
    /// <param name="pos"></param>
    public void SetPosByTransform(Transform pos)
    {
        following_transform = pos;
        SetPos(following_transform.position);
    }

    /// <summary>
    /// Set looking position
    /// </summary>
    /// <param name="pos">position in Vec3</param>
    public void SetPos(Vector3 pos)
    {
        center_pos = pos;
    }

    /// <summary>
    /// Compute hit by looking foot point
    /// </summary>
    public void ComputeGroundAltitude()
    {
        Vector3 sp = center_pos + new Vector3(0, height_offset_for_ray_origin, 0);

        //RaycastHit hit; // store to member var instead

        if (Physics.Raycast(sp, Vector3.down, out hit))
        {
            ground_hit = true;
            ground_point = hit.point;
            ground_normal = hit.normal;
        }
        else
        {
            ground_hit = false;
            ground_point = Vector3.zero;
            ground_normal = Vector3.zero;
        }

    }

}
