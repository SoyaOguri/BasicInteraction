using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DSUnityViz
{

    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
    public struct GroundinfoRecord
    {
        public short car_id;
        public short tire_id;
        public float height;
        public float dx;
        public float dy;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
    public struct GroundinfoReturn
    {
        public const int NumOfMaxInfo = 32;

        public int number_of_info;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NumOfMaxInfo)]
        public GroundinfoRecord[] ground_info;  ///< car states

    }


    public class GetMeshAltitude : MonoBehaviour
    {

        // Altitude sensor, 
        AltitudeSensorSingle sensor;

        // FindUpdater
        PositionUpdater updater;

        // sender;
        public UdpSender<GroundinfoReturn> sender;

        // Use this for initialization
        void Start()
        {
            // find update attached MasterObject
            // updater = GameObject.Find("MasterObject").GetComponent<MasterObject>().updater;
            updater = DSGlobal.object_master.GetComponent<MasterObject>().updater;

            // create data sender
            sender = new UdpSender<GroundinfoReturn>();
            sender.Open("127.0.0.1", 30002, 30010);

            // create sensor
            sensor = new AltitudeSensorSingle();
            sensor.SetPosByTransform(this.transform);
        }

        // Update is called once per frame
        void Update()
        {
            GroundinfoReturn ret = new GroundinfoReturn();
            ret.ground_info = new GroundinfoRecord[GroundinfoReturn.NumOfMaxInfo];

            if (updater == null || updater.objman == null || updater.objman.cars == null) return;
            int i = 0;
            for (; i < Mathf.Min( updater.objman.cars.Count, 32); i++)
            {
                sensor.SetPos(updater.objman.cars_pos[i]);
                sensor.ComputeGroundAltitude();
                
                if (sensor.ground_hit)
                {
                    ret.ground_info[i].car_id = (short)i;
                    ret.ground_info[i].tire_id = 0xF;
                    ret.ground_info[i].height = sensor.ground_point.y;
                    ret.ground_info[i].dx = sensor.ground_normal.x;
                    ret.ground_info[i].dy = sensor.ground_normal.z;
                    //print("find" + sensor.hit.collider.gameObject.name + " at " + sensor.ground_point.y.ToString() + " for car " + i.ToString() );
                }
                else
                {
                    //print("not found");
                }
            }

            //ret.number_of_info = 3;
            ret.number_of_info = i;
            sender.Send(ret);

        }
    }

}