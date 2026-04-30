using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using AsySocket;
using System;
using DSUnityViz;


[Serializable()]
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct ShrParticipanceState
{
    public int m_ID; // 車のID(インデックスと等しい)
    public int m_meshID; // 車種を特定するためのID，主に表示用(メッシュリストから適宜選ぶ)
    public int m_groupID; // 車のグループを特定するID(自車，左車線車，右車線車，対向車…)

    public double m_u, m_v, m_w; // コースに沿った座標(m_uは周回数含めた値で持つ)
    public double m_x, m_y, m_z; // グローバルな座標
    public double m_roll, m_pitch, m_yaw; // // グローバルな座標系に対する姿勢(roll->pitch->yaw) -pi/2～pi/2
    public double m_speed; // 直進速度
    public double m_accel; // 直進方向加速度

    public int m_animCount;
    public int m_animFlag;

    public int m_StateCount;
    public int m_StateFlag;

    public double m_throtpdl;  // スロットル(added by 奥田)
    public double m_brakepdl;  // ブレーキ(added by 奥田)
    public double m_steering;  // ステアリング(added by 奥田)

    public int m_subObject1;
    public int m_subObject2;
    public int m_subObject3;
    public int m_subObject4;
    public int m_subObject5;
    public int m_subObject6;

    public int m_winker_R;
    public int m_winker_L;

    public int m_flg;
}

[Serializable()]
[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct ShrParticipanceStateList
{
    public const int NumStates = 50;

    // header strings
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = NumStates)]
    public ShrParticipanceState[] states;

    public ShrParticipanceStateList(int i = 0)
    {
        states = new ShrParticipanceState[NumStates];
    }
}

public enum ParticipantType
{
    NULL,
    Car,
    Pedestrian,
    PM,
    Bicycle,
}

[System.SerializableAttribute]
public struct CarParam
{
    public int id;
    public int mesh;
    public GameObject traceTarget;
    public ParticipantType type;
}


public class UdpDataSender<T>
    where T : new()
{
    public T[] list;
    List<UdpByteSender> sender = new List<UdpByteSender>();
    GCHandle gch;
    byte[] temp;
    int size;
    byte[] bytes;
    public bool isOpen;

    public UdpDataSender(int size)
    {
        Resize(size);
        isOpen = false;
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

        isOpen = true;
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


public class ServerCommunicator : MonoBehaviour
{
    AsynchronousSocket socket;
    ShrParticipanceStateList list;
    int dataSize, recieveStructCount;
    SharedMemoryManager<PacketToDSViz> shrMem;
    bool isUsedSharedMem, isRecieved;
    List<int> listIDs = new List<int>();
    List<CarParam> simulatedTargets, syncTargets;
    System.Threading.Mutex mutex;
    ShrParticipanceState sendTemp;
    int structSize;
    List<NetworkCharacterVelocity> velocities = new List<NetworkCharacterVelocity>();
    UdpDataSender<PacketToDSViz> dsSend;

    public int recievePort;
    public string sendIP;
    public int sendPort;
    public string shrememName;
    public string dsIP;
    public int dsPort;
    public List<CarParam> participants;

    // Start is called before the first frame update
    void Start()
    {
        socket = new AsynchronousSocket(recievePort);
        list = new ShrParticipanceStateList(0);
        for (int i = 0; i < list.states.Length; i++)
        {
            list.states[i].m_ID = -1;
        }
        mutex = new System.Threading.Mutex();

        socket.SetSendEndPoint(sendIP, sendPort);
        socket.OnRecieveData += RecieveData;

        dataSize = Marshal.SizeOf(list.states[0]);
        shrMem = new SharedMemoryManager<PacketToDSViz>();
        shrMem.data = new PacketToDSViz(0);

        isUsedSharedMem = false;
        if (shrememName != "")
        {
            shrMem.CreateMemory(shrememName, shrememName + "Mutex");
            isUsedSharedMem = true;
        }
        simulatedTargets = new List<CarParam>();
        syncTargets = new List<CarParam>();
        isRecieved = false;
        sendTemp = new ShrParticipanceState();
        listIDs = new List<int>();
        simulatedTargets = new List<CarParam>();
        syncTargets = new List<CarParam>();

        structSize = Marshal.SizeOf<ShrParticipanceStateList>();

        shrMem.data.num_of_car_data = participants.Count;
        for (int i = 0; i < participants.Count; i++)
        {
            shrMem.data.car_states[i].mesh_id = participants[i].mesh;
            shrMem.data.car_states[i].id = participants[i].id;

            if (participants[i].id >= 0)
            {
                for (; listIDs.Count <= participants[i].id;)
                {
                    listIDs.Add(-1);
                }
                listIDs[participants[i].id] = i;
            }
            if (participants[i].traceTarget != null)
            {
                simulatedTargets.Add(participants[i]);
                var temp = participants[i].traceTarget.GetComponent<NetworkCharacterVelocity>();
                if (temp == null)
                {
                    velocities.Add(null);
                }
                else
                {
                    velocities.Add(temp);
                }
            }
            else
            {
                velocities.Add(null);
                syncTargets.Add(participants[i]);
            }

        }

        dsSend = new UdpDataSender<PacketToDSViz>(1);
        if (dsIP != "")
        {
            dsSend.Open(dsIP, dsPort);
        }

        socket.Start();

        //In order to swich off meshes simulated in this scence, register an event.
        DSGlobal.object_master.GetComponent<MasterObject>().objman.switchGraphicsCallback += (ObjectManager obj, int car_id, int mesh_id) => {
            if (participants[car_id].traceTarget != null)
                obj.SetCarInvisible(car_id, false);
        };

    }

    // Update is called once per frame
    void Update()
    {
        //update DS Data from simulation
        for (int i = 0; i < simulatedTargets.Count; i++)
        {
            switch (simulatedTargets[i].type)
            {
                case ParticipantType.Pedestrian:
                    ChageStateFromVelcityObject(ref shrMem.data.car_states[listIDs[simulatedTargets[i].id]], simulatedTargets[i].traceTarget, listIDs[simulatedTargets[i].id]);
                    break;
                case ParticipantType.Bicycle:
                    ChageStateFromBicycle(ref shrMem.data.car_states[listIDs[simulatedTargets[i].id]], simulatedTargets[i].traceTarget, listIDs[simulatedTargets[i].id]);
                    break;
                case ParticipantType.PM:
                    ChageStateFromVelcityObject(ref shrMem.data.car_states[listIDs[simulatedTargets[i].id]], simulatedTargets[i].traceTarget, listIDs[simulatedTargets[i].id]);
                    break;
                default:
                    ChageStateFromNull(ref shrMem.data.car_states[listIDs[simulatedTargets[i].id]], simulatedTargets[i].traceTarget);
                    break;
            }

        }

        //update not simulated Data from recieved data;
        if (isRecieved)
        {
            mutex.WaitOne(1000);
            isRecieved = false;
            for (int i = 0; i < recieveStructCount; i++)
            {
                for (int n = 0; n < syncTargets.Count; n++)
                {
                    if (list.states[i].m_ID == syncTargets[n].id)
                    {
                        ChageState(ref shrMem.data.car_states[listIDs[list.states[i].m_ID]], list.states[i]);
                        break;
                    }
                }
            }
            mutex.ReleaseMutex();
        }

        //update send Data from simulation
        for (int i = 0; i < simulatedTargets.Count; i++)
        {
            ChageState(ref sendTemp, shrMem.data.car_states[listIDs[simulatedTargets[i].id]]);
            socket.SetPacket<ShrParticipanceState>(ref sendTemp);
        }

        if (simulatedTargets.Count != 0)
            socket.Send();

        if (dsSend.isOpen)
        {
            dsSend.Send(ref shrMem.data);
        }

        if (isUsedSharedMem)
            shrMem.WriteData(shrMem.data);
    }


    public void RecieveData(ref byte[] bytes, System.Net.IPEndPoint ep)
    {
        recieveStructCount = bytes.Length / dataSize;
        mutex.WaitOne(1000);
        isRecieved = true;
        list = socket.BytesToData<ShrParticipanceStateList>(ref bytes, structSize, 0);
        mutex.ReleaseMutex();
    }

    static float Deg2Rad(float rad) { return rad / 180.0f * Mathf.PI; }


    static void ChageState(ref ShrParticipanceState ds, CAR_UNIT_INFO unity)
    {
        ds.m_ID = unity.id;
        ds.m_meshID = unity.mesh_id;
        //ds.m_rpm = unity.eng_rpm;

        ds.m_roll = unity.roll;
        ds.m_pitch = unity.pitch;
        ds.m_yaw = unity.yaw;

        ds.m_x = unity.x;
        ds.m_y = unity.y;
        ds.m_z = unity.z;

        ds.m_accel = unity.accelpedal;
        ds.m_brakepdl = unity.brakepedal;
        ds.m_steering = unity.steering;
        ds.m_speed = unity.speed;


        //ds.m_rpm = unity.decL;
        //ds.m_accel = unity.decR;

        ds.m_animCount = unity.anim_cnt;

        // ds.m_brakepdl = unity.brakepedal;

    }

    void ChageStateFromBicycle(ref CAR_UNIT_INFO unity, GameObject obj, int id)
    {
        unity.x = obj.transform.position.x;
        unity.y = obj.transform.position.z;
        unity.z = obj.transform.position.y;

        unity.accelpedal = velocities[id].velocity.x;
        unity.brakepedal = velocities[id].velocity.y;
        unity.speed = velocities[id].velocity.z;
        unity.steering = velocities[id].subObject1;
        unity.eng_rpm = velocities[id].subObject2;

        unity.yaw = -Deg2Rad(obj.transform.rotation.eulerAngles.y);
        unity.pitch = Deg2Rad(obj.transform.rotation.eulerAngles.z);
        unity.roll = Deg2Rad(obj.transform.rotation.eulerAngles.x);
    }

    void ChageStateFromVelcityObject(ref CAR_UNIT_INFO unity, GameObject obj, int id)
    {
        unity.x = obj.transform.position.x;
        unity.y = obj.transform.position.z;
        unity.z = obj.transform.position.y;

        unity.accelpedal = velocities[id].velocity.x;
        unity.brakepedal = velocities[id].velocity.y;
        unity.speed = velocities[id].velocity.z;

        unity.yaw = -Deg2Rad(obj.transform.rotation.eulerAngles.y);
        unity.pitch = Deg2Rad(obj.transform.rotation.eulerAngles.z);
        unity.roll = Deg2Rad(obj.transform.rotation.eulerAngles.x);
    }


    void ChageStateFromNull(ref CAR_UNIT_INFO unity, GameObject obj)
    {
        unity.x = obj.transform.position.x;
        unity.y = obj.transform.position.z;
        unity.z = obj.transform.position.y;

        //unity.decL = cntlFlag;
        //unity.decR = cntrFlag;

        //unity.anim_cnt = decision_Flag;
        //unity.brakepedal = pede_look_car;

        unity.yaw = -Deg2Rad(obj.transform.rotation.eulerAngles.y);
        unity.pitch = Deg2Rad(obj.transform.rotation.eulerAngles.z);
        unity.roll = Deg2Rad(obj.transform.rotation.eulerAngles.x);
    }

    void ChageState(ref CAR_UNIT_INFO unity, ShrParticipanceState ds)
    {
        unity.x = (float)ds.m_x;
        unity.y = (float)ds.m_y;
        unity.z = (float)ds.m_z;

        unity.roll = (float)ds.m_roll;
        unity.pitch = (float)ds.m_pitch;
        unity.yaw = (float)ds.m_yaw;

        //unity.eng_rpm = (float)ds.;

        unity.id = ds.m_ID;
        unity.mesh_id = ds.m_meshID;


        unity.accelpedal = (float)ds.m_accel;
        unity.brakepedal = (float)ds.m_brakepdl;
        unity.steering = (float)ds.m_steering;
        unity.speed = (float)ds.m_speed;

        if(ds.m_winker_R == 1)
        {
            unity.winkers = 1;
        }
        else if(ds.m_winker_L == 1)
        {
            unity.winkers = 2;
        }
        else
        {
            unity.winkers = 0;
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
}
