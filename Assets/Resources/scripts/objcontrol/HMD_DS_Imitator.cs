using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace DSUnityViz
{
    public class HMD_udp_receiver<T>
        where T : new()
    {
        UdpByteReceiver recv;
        public List<T> list;
        public int receiveNum;
        int structSize;
        byte[] subBytes;
        GCHandle gch;
        public int Open(int _local_port, int listSize)
        {
            recv = new UdpByteReceiver();
            int ret = recv.Open(_local_port, 1024 * 50);
            list = new List<T>(listSize);
            for (int i = 0; i < listSize; i++)
            {
                list.Add(new T());
            }

            structSize = Marshal.SizeOf(list[0]);
            subBytes = new byte[structSize];

            return ret;
        }

        public int MutiCastOpen(string ip, int _local_port, int listSize)
        {
            recv = new UdpByteReceiver();
            int ret = recv.MultiCastOpen(ip, _local_port, 1024 * 50);
            list = new List<T>(listSize);
            for (int i = 0; i < listSize; i++)
            {
                list.Add(new T());
            }

            structSize = Marshal.SizeOf(list[0]);
            subBytes = new byte[structSize];

            return ret;
        }

        public bool GetData()
        {
            byte[] bytes = recv.GetData(-1);
            recv.ClearBuffer();
            T temp;
            if (bytes == null) return false;
            int n = 0;

            for (int i = 0; i < bytes.Length; i += structSize)
            {
                System.Buffer.BlockCopy(bytes, i, subBytes, 0, structSize);
                gch = GCHandle.Alloc(subBytes, GCHandleType.Pinned);
                temp = (T)(Marshal.PtrToStructure(gch.AddrOfPinnedObject(), typeof(T)));
                gch.Free();
                list[n] = temp;
                n++;
            }
            receiveNum = n;
            return true;
        }
    }

    [System.SerializableAttribute]
    public struct CarParam
    {
        public int id;
        public int mesh;
        public GameObject traceTarget;
    }

    public class HMD_udp_sender<T>
        where T : new()
    {
        public T[] list;
        List<UdpByteSender> sender = new List<UdpByteSender>();
        GCHandle gch;
        byte[] temp;
        int size;
        byte[] bytes;

        public HMD_udp_sender(int size)
        {
            Resize(size);
        }

        public void Resize(int size)
        {
            list = new T[size];
            this.size = Marshal.SizeOf(list[0]);

            bytes = new byte[this.size * list.Length];
        }

        public int Open(string _remote_host, int _remote_port)
        {
            sender.Add(new UdpByteSender());
            int ret = sender[sender.Count - 1].Open(_remote_host, _remote_port);


            return ret;
        }

        public int Send(int count)
        {
            for (int i = 0; i < list.Length; i++)
            {
                temp = new byte[this.size];
                gch = GCHandle.Alloc(temp, GCHandleType.Pinned);
                Marshal.StructureToPtr(list[i], gch.AddrOfPinnedObject(), false);
                System.Buffer.BlockCopy(temp, 0, bytes, i * size, size);
                gch.Free();
            }
            for (int i = 0; i < sender.Count; i++)
            {
                sender[i].Send(bytes);
            }

            return 0;
        }

        public int Send()
        {

            for (int i = 0; i < list.Length; i++)
            {
                temp = new byte[this.size];
                gch = GCHandle.Alloc(temp, GCHandleType.Pinned);
                Marshal.StructureToPtr(list[i], gch.AddrOfPinnedObject(), false);
                System.Buffer.BlockCopy(temp, 0, bytes, i * size, size);
                gch.Free();
            }
            for (int i = 0; i < sender.Count; i++)
            {
                sender[i].Send(bytes);
            }

            return 0;
        }

        public int Send(ref T obj)
        {
            temp = new byte[this.size];
            gch = GCHandle.Alloc(temp, GCHandleType.Pinned);
            Marshal.StructureToPtr(obj, gch.AddrOfPinnedObject(), false);
            System.Buffer.BlockCopy(temp, 0, bytes, 0, size);
            gch.Free();

            for (int i = 0; i < sender.Count; i++)
            {
                sender[i].Send(bytes);
            }

            return 0;
        }

    }



    public class HMD_DS_Imitator : MonoBehaviour
    {

        [System.Serializable]
        public class OtherTargetSender
        {
            public string sendIP;
            public int sendPort;
            HMD_udp_sender<ShrCarState> sender;
            public List<int> sendList;



            public void Open()
            {
                sender = new HMD_udp_sender<ShrCarState>(sendList.Count);
                sender.Open(sendIP, sendPort);
            }

            public void Set(ref CAR_UNIT_INFO[] car_states)
            {
                for (int i = 0; i < sendList.Count; i++)
                {
                    ChageState(ref sender.list[i], car_states[sendList[i]]);
                }
            }

            public void Send()
            {
                sender.Send();
            }
        }

        SharedMemoryManager<PacketToDSViz> shrMem;
        HMD_udp_receiver<ShrCarState> reciever;
        public HMD_udp_sender<ShrCarState> sender;
        //ObjectManager objman;
        HMD_udp_sender<PacketToDSViz> dsSender;
        bool isUsedSharedMem;

        //botton bo;

        public string multiCastIP;
        public int receivePort;

        public string sendIP;
        public string sendIP2;
        public int sendPort;

        public string shrememName;

        public string dsSendIP;
        public int dsSendPort;

        //public int cntlFlag;
        public int decision_Flag;
        public int pede_look_car;
        //public int trial=0;
        //public float last_z;

        [SerializeField]
        public List<CarParam> cars;
        /// <summary>
        /// Trace Target以外も送る場合IDを記述
        /// </summary>
        public List<int> optionalSend;
        List<CarParam> listHMDs = new List<CarParam>();
        List<CarParam> listNotHMDs = new List<CarParam>();

        /// <summary>
        /// 別のIPにさらに送る場合に設定
        /// Trace Targetを含め、Send listのみを送る
        /// </summary>
        [SerializeField]
        public OtherTargetSender otherSend;

        //kuroda add
        //CharacterController characterController;
        //Animator animator;
        //private float R_i = 1 / 0.3f;
        //private float last_x;
        //private float last_y;
        //private float last_z;

        List<int> listIDs = new List<int>();
        //public List<int> ids;

        // Use this for initialization
        void Start()
        {
            //var masterObject = GameObject.Find("MasterObject");
            //MasterObject mo = masterObject.GetComponent<MasterObject>();
            //objman = mo.objman;

            // bo = this.GetComponent<botton>();

            shrMem = new SharedMemoryManager<PacketToDSViz>();
            shrMem.data = new PacketToDSViz(0);
            if (shrememName == "")
            {
                isUsedSharedMem = false;
                dsSender = new HMD_udp_sender<PacketToDSViz>(1);
                dsSender.Open(dsSendIP, dsSendPort);
            }
            else
            {
                shrMem.CreateMemory(shrememName, shrememName + "Mutex");
                isUsedSharedMem = true;
            }

            shrMem.data.num_of_car_data = cars.Count;

            if (otherSend.sendIP != "" && otherSend.sendList.Count != 0)
                otherSend.Open();

            for (int i = 0; i < cars.Count; i++)
            {
                shrMem.data.car_states[i].mesh_id = cars[i].mesh;
                shrMem.data.car_states[i].id = cars[i].id;

                if (cars[i].id >= 0)
                {
                    for (; listIDs.Count <= cars[i].id;)
                    {
                        listIDs.Add(-1);
                    }
                    listIDs[cars[i].id] = i;
                }
                if (cars[i].traceTarget != null)
                {
                    listHMDs.Add(cars[i]);
                }
                else
                {
                    listNotHMDs.Add(cars[i]);
                }

            }

            if (listHMDs.Count != 0)
            {
                sender = new HMD_udp_sender<ShrCarState>(listHMDs.Count + optionalSend.Count);
                sender.Open(sendIP, sendPort);
                if (sendIP2 != "")
                {
                    sender.Open(sendIP2, sendPort);
                }
            }


            reciever = new HMD_udp_receiver<ShrCarState>();
            if (multiCastIP == "")
                reciever.Open(receivePort, cars.Count);
            else
                reciever.MutiCastOpen(multiCastIP, receivePort, cars.Count);
            if (isUsedSharedMem)
                shrMem.WriteData(shrMem.data);
            else
                dsSender.Send(ref shrMem.data);

            //kuroda add
            //characterController = GetComponent<CharacterController>();
            //animator = GetComponentInChildren<Animator>();
        }

        //string str;

        bool flag = false;

        // Update is called once per frame
        void Update()
        {
            //str = "";
            //
            for (int i = 0; i < listHMDs.Count; i++)
            {
                ChageState(ref shrMem.data.car_states[listIDs[listHMDs[i].id]], listHMDs[i].traceTarget);
            }

            if (reciever != null &&reciever.GetData())
            {
                for (int i = 0; i < reciever.receiveNum; i++)
                {
                    for (int n = 0; n < listNotHMDs.Count; n++)
                    {
                        if (reciever.list[i].m_carID == listNotHMDs[n].id)
                        {
                            ChageState(ref shrMem.data.car_states[listIDs[reciever.list[i].m_carID]], reciever.list[i]);
                            break;
                        }
                    }

                    //str += reciever.list[i].m_carID + ",";
                }
                //GameObject.Find("Text").GetComponent<TextMesh>().text = shrMem.data.car_states[3].x.ToString("0.000") + "," + shrMem.data.car_states[3].y.ToString("0.000") + "," + shrMem.data.car_states[3].z.ToString("0.000");
                //count loop time
                //Count_trial(reciever.list[6]);
                //shrMem.data.car_states[0].z = last_z;
                //if (shrMem.data.car_states[0].z < last_z)
                //{
                //    trial++;
                //}
                //Debug.Log("trial" + trial);
            }

            for (int i = 0; i < listHMDs.Count; i++)
            {
                ChageState(ref sender.list[i], shrMem.data.car_states[listIDs[listHMDs[i].id]]);
            }

            for (int i = 0; i < optionalSend.Count; i++)
            {
                ChageState(ref sender.list[listHMDs.Count + i], shrMem.data.car_states[optionalSend[i]]);
            }

            try
            {
                if (listHMDs.Count != 0)
                    sender.Send();
            }
            catch { }

            if (otherSend.sendIP != "")
            {
                otherSend.Set(ref shrMem.data.car_states);
                otherSend.Send();
            }

            //GameObject.Find("Text").GetComponent<TextMesh>().text = sender.list[0].m_roll.ToString("0.00") + "," + sender.list[0].m_pitch.ToString("0.00") + "," + sender.list[0].m_yaw.ToString("0.00") + "\n" +
            //    //listHMDs[0].traceTarget.transform.rotation.x.ToString("0.00") + "," + listHMDs[0].traceTarget.transform.rotation.y.ToString("0.00") + "," + listHMDs[0].traceTarget.transform.rotation.z.ToString("0.00");
            //    str;


            //to objectmanager
            if (isUsedSharedMem)
                shrMem.WriteData(shrMem.data);
            else
                dsSender.Send(ref shrMem.data);


        }

        static float Deg2Rad(float rad) { return rad / 180.0f * Mathf.PI; }



        /// <summary>
        /// Unity→DS
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="unity"></param>
        static void ChageState(ref ShrCarState ds, CAR_UNIT_INFO unity)
        {
            ds.m_carID = unity.id;
            ds.m_carTypeID = unity.mesh_id;
            ds.m_rpm = unity.eng_rpm;

            ds.m_roll = unity.roll;
            ds.m_pitch = unity.pitch;
            ds.m_yaw = unity.yaw;

            ds.m_steering = unity.steering;
            ds.m_speed = unity.speed;

            ds.m_x = unity.x;
            ds.m_y = unity.y;
            ds.m_z = unity.z;

            //ds.m_rpm = unity.decL;
            //ds.m_accel = unity.decR;

            ds.m_rpm = unity.anim_cnt;

            ds.m_accel = unity.brakepedal;

        }

        BicycleModelCalculator bmc;

        /// <summary>
        /// HMDなどの特殊objectをUnity Carに変換用
        /// Trace Targetで指定
        /// 用途に合わせて書き換える
        /// </summary>
        /// <param name="unity"></param>
        /// <param name="obj"></param>
        void ChageState(ref CAR_UNIT_INFO unity, GameObject obj)
        {
            bmc = obj.GetComponent<BicycleModelCalculator>();
            unity.x = bmc.x;
            unity.y = obj.transform.position.z;
            unity.z = bmc.y;

            unity.speed = bmc.velocity;

            unity.yaw = bmc.yaw;
            unity.pitch = obj.transform.rotation.x;
            unity.roll = bmc.rollAngle;
            unity.steering = bmc.steerAngle;
            unity.brakepedal = bmc.brakeData;
        }

        /// <summary>
        /// DS→Unity
        /// </summary>
        /// <param name="unity"></param>
        /// <param name="ds"></param>
        void ChageState(ref CAR_UNIT_INFO unity, ShrCarState ds)
        {
            unity.x = (float)ds.m_x;
            unity.y = (float)ds.m_y;
            unity.z = (float)ds.m_z;

            unity.roll = (float)ds.m_roll;
            unity.pitch = (float)ds.m_pitch;
            unity.yaw = (float)ds.m_yaw;

            unity.eng_rpm = (float)ds.m_rpm;

            unity.id = ds.m_carID;
            unity.mesh_id = ds.m_carTypeID;

            unity.brakepedal = (float)ds.m_brake_input;
            if(ds.m_winker_L == 1)
            {
                unity.winkers = 2;
            }
            else
            {
                if (ds.m_winker_R == 1)
                {
                    unity.winkers = 1;
                }
                else
                {
                    unity.winkers = 0;
                }
   
            }

            //if (ds.m_steering_input == 1)
            //    GameObject.Find("leftLegBase").transform.Translate(0.01f, 0, 0);
            //else if (ds.m_steering_input == -1)
            //    GameObject.Find("leftLegBase").transform.Translate(-0.01f, 0, 0);
            //else if (ds.m_steering_input == 2)
            //    GameObject.Find("leftLegBase").transform.Translate(0, 0, 0.01f);
            //else if (ds.m_steering_input == -2)
            //    GameObject.Find("leftLegBase").transform.Translate(0, 0, -0.01f);

            //if (ds.m_steering == 1)
            //    GameObject.Find("rightLegBase").transform.Translate(0.01f, 0, 0);
            //else if (ds.m_steering == -1)
            //    GameObject.Find("rightLegBase").transform.Translate(-0.01f, 0, 0);
            //else if (ds.m_steering == 2)
            //    GameObject.Find("rightLegBase").transform.Translate(0, 0, 0.01f);
            //else if (ds.m_steering == -2)
            //    GameObject.Find("rightLegBase").transform.Translate(0, 0, -0.01f);
        }

        //void Count_trial(ShrCarState ds)
        //{
        //    //trial = (int)ds.m_brake_input;
        //    Debug.Log("m_brake_input" + (int)ds.m_brake_input);
        //}

    }

}