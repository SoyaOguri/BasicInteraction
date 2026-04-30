using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCoordinator;

/// <summary>
/// NetworkCharacterVelocityで速度参照しているので同じオブジェクトに貼ること
/// </summary>
/// 
public class C_PedestrianController : MonoBehaviour
{
    [Header("値を入力")]
    [Space(5), Tooltip("初期位置と最終位置の座標")] public List<Vector3> path;
    [Space(5), Tooltip("動き出すための車椅子の通過点")] public Vector3 triggerPoint;
    PathLine line;
    GameObject obj;
    Vector3 position;
    NetworkCharacterVelocity vel;
    public float velocity;
    [Header("----------------------------------------")]
    public float lookAheadDistance;
    [SerializeField]
    Vector3 uvw;
    [SerializeField]
    Vector3 targetPoint;
    Vector3 tmp;

    // Use this for initialization
    void Start()
    {
        line = new PathLine();
        line.SetPath(path);
        position = transform.position;
        line.SearchClosedIndexFromWholeArea(ref position);
        vel = GetComponent<NetworkCharacterVelocity>();
        obj = GameObject.Find("Wheelchair_High");

    }

    // Update is called once per frame
    void Update()
    {
        position = transform.position;
        uvw = line.GetUVW(ref position);

        if (uvw.x > line.GetLength())
        {
            vel.velocity.x = 0;
            vel.velocity.z = 0;
        }
        else
        {
            if (lookAheadDistance < (uvw.y > 0 ? uvw.y : -uvw.y))
            {
                targetPoint = line.GetPathRootPoint() + line.GetPathRootDirection() * uvw.x;
            }
            else
            {
                targetPoint = line.GetDistantPoint(ref position, lookAheadDistance);
            }

            tmp = targetPoint - position;

            if (obj.transform.position.z > triggerPoint.z)
            //不等号の向きは区間によって適宜変更
            {
                vel.velocity.x = tmp.normalized.x * velocity;
                vel.velocity.z = tmp.normalized.z * velocity;
            }
        }

    }
}
