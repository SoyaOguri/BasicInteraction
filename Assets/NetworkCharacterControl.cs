using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DSUnityViz;
using System.Runtime.InteropServices;

public class NetworkCharacterControl : MonoBehaviour {


    [System.Serializable]
    public struct PacketData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public bool isOriginal;
    }

    const int comDataSize = 64;

    [System.Serializable]
    public struct PacketDataArray
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = comDataSize)]
        public PacketData[] dataArray;

        public PacketDataArray(int i = 0)
        {
            dataArray = new PacketData[comDataSize];
        }
    }

    public enum ControlMode
    {
        Position,
        Velocity, //please attach NetworkCharacterVelocity.
        VelocityAndPosition,

    }

    public enum CommunicationMode
    {
        Send,
        Recieve,
        Playback,
        DSRecieve,
    }

    public CommunicationMode communication;
    bool addComSend = false, addComRecieve = false;

    public List<SettingFormat> dataList;

    public List<string> send_IP;
    public int port;

    public string playbackFile;
    public int playbackObjectNumber;

    [System.Serializable]
    public class PaketFormat
    {
        public GameObject communicationObject;
        public ControlMode syncMode;
    }

    /// <summary>
    /// This mode is used when some parts of data is sent/recieved despite the communication mode
    /// </summary>
    public enum AddtionalComMode
    {
        None,
        Send,
        Recieve,
    }

    [System.Serializable]
    public class SettingFormat :  PaketFormat
    {
        public AddtionalComMode addtionalComMode;
    }

    HMD_udp_sender<PacketDataArray> sender = null;
    HMD_udp_receiver<PacketDataArray> receiver = new HMD_udp_receiver<PacketDataArray>();

    PacketDataArray sendData;
    List<NetworkCharacterVelocity> vel;

    class TransformData
    {
        public Vector3 position,velocity;
        public Quaternion rotation;
    }

    class AgentsData
    {
        public float time;
        public List<TransformData> trans;
    }

    List<AgentsData> agents;

    void FileOpen()
    {
        System.IO.StreamReader sr;
        try
        {
            sr = new System.IO.StreamReader(playbackFile);
        }
        catch
        {
            return;
        }

        string[] strs;

        sr.ReadLine();

        int count = 0;

        agents = new List<AgentsData>();

        while (!sr.EndOfStream)
        {
            strs = sr.ReadLine().Split(',');

            agents.Add(new AgentsData());
            agents[count].time = float.Parse(strs[0]);

            agents[count].trans = new List<TransformData>();

            for (int i = 0; i < playbackObjectNumber; i++)
            {
                agents[count].trans.Add(new TransformData());
                agents[count].trans[i].position.x = float.Parse(strs[i * 6 + 1]);
                agents[count].trans[i].position.y = float.Parse(strs[i * 6 + 2]);
                agents[count].trans[i].position.z = float.Parse(strs[i * 6 + 3]);

                agents[count].trans[i].rotation = Quaternion.Euler(float.Parse(strs[i * 6 + 4])*180/Mathf.PI, float.Parse(strs[i * 6 + 5]) * 180 / Mathf.PI, float.Parse(strs[i * 6 + 6]) * 180 / Mathf.PI);
            }

            count++;
        }

        for (int i = 1; i < agents.Count; i++)
        {
            for (int n = 0; n < playbackObjectNumber; n++)
            {
                agents[i].trans[n].velocity = (agents[i].trans[n].position - agents[i - 1].trans[n].position) / (agents[i].time * agents[i - 1].time);
            }

        }

    }



    // Use this for initialization
    void Start () {

        sendData = new PacketDataArray(0);

        vel = new List<NetworkCharacterVelocity>();
        for (int i = 0; i < dataList.Count; i++)
        {
            vel.Add(null);
            vel[i] = (NetworkCharacterVelocity)dataList[i].communicationObject.GetComponent(typeof(NetworkCharacterVelocity));
            if (dataList[i].addtionalComMode == AddtionalComMode.Send)
                addComSend = true;
            if (dataList[i].addtionalComMode == AddtionalComMode.Recieve)
                addComRecieve = true;
        }


        if (send_IP.Count > 0)
        {
            sender = new HMD_udp_sender<PacketDataArray>(send_IP.Count);
            for (int i = 0; i < send_IP.Count; i++)
            {
                sender.Open(send_IP[i],port);
            }


        }

        receiver.Open(port, 1);

        if(dataList.Count > comDataSize)
        {
            throw new System.Exception("The size of DataList is over than default size. Resize the default size");
            //UnityEditor.EditorUtility.DisplayDialog("Notice", "DataListのサイズが想定より超過しています\nスクリプトを変更してください", "OK");
        }
        

        if(communication == CommunicationMode.Playback)
        {
            FileOpen();
        }
	}

    int frameCount = 0;

	// Update is called once per frame
	void Update () {

        //Regular procedure
        if(communication == CommunicationMode.Send)
        {
            for (int i = 0; i < dataList.Count; i++)
            {
                //Data are sent when additional Communication mode is not Recieve
                if(dataList[i].addtionalComMode != AddtionalComMode.Recieve)
                {
                    //sender
                    sendData.dataArray[i].position = dataList[i].communicationObject.transform.position;
                    sendData.dataArray[i].rotation = dataList[i].communicationObject.transform.rotation;
                    if (vel[i] != null)
                    {
                        //vel.velocity.x = Input.GetAxisRaw("Horizontal");
                        //vel.velocity.z = Input.GetAxisRaw("Vertical");
                        sendData.dataArray[i].velocity = vel[i].velocity;
                    }
                    sendData.dataArray[i].isOriginal = true;
                }
                else
                {
                    sendData.dataArray[i].isOriginal = false;
                }
            }
            sender.Send(ref sendData);
        }

        //Additional procedure
        if (addComSend)
        {
            for (int i = 0; i < dataList.Count; i++)
            {
                //Data are sent when additional Communication mode is only Send
                if (dataList[i].addtionalComMode == AddtionalComMode.Send)
                {
                    //sender
                    sendData.dataArray[i].position = dataList[i].communicationObject.transform.position;
                    sendData.dataArray[i].rotation = dataList[i].communicationObject.transform.rotation;
                    if (vel[i] != null)
                    {
                        //vel.velocity.x = Input.GetAxisRaw("Horizontal");
                        //vel.velocity.z = Input.GetAxisRaw("Vertical");
                        sendData.dataArray[i].velocity = vel[i].velocity;
                    }
                    sendData.dataArray[i].isOriginal = true;
                }
                else
                {
                    sendData.dataArray[i].isOriginal = false;
                }
            }
            sender.Send(ref sendData);
        }


        //Regular procedure
        if (communication == CommunicationMode.Recieve)
        {
            //reciever
            if (receiver.GetData())
            {
                for (int i = 0; i < dataList.Count; i++)
                {
                    if (dataList[i].addtionalComMode != AddtionalComMode.Send && receiver.list[0].dataArray[i].isOriginal)
                    {
                        dataList[i].communicationObject.transform.rotation = receiver.list[0].dataArray[i].rotation;
                        if (dataList[i].syncMode == ControlMode.Position)
                        {
                            dataList[i].communicationObject.transform.position = receiver.list[0].dataArray[i].position;
                        }
                        else if (dataList[i].syncMode == ControlMode.Velocity)
                        {
                            vel[i].velocity = receiver.list[0].dataArray[i].velocity;
                        }
                        else if (dataList[i].syncMode == ControlMode.VelocityAndPosition)
                        {
                            dataList[i].communicationObject.transform.position = receiver.list[0].dataArray[i].position;
                            vel[i].velocity = receiver.list[0].dataArray[i].velocity;
                        }
                    }
                }
            }
        }
        //Additional procedure
        else if (addComRecieve)
        {
            //reciever
            if (receiver.GetData())
            {
                for (int i = 0; i < dataList.Count; i++)
                {
                    if (dataList[i].addtionalComMode == AddtionalComMode.Recieve && receiver.list[0].dataArray[i].isOriginal)
                    {
                        dataList[i].communicationObject.transform.rotation = receiver.list[0].dataArray[i].rotation;
                        if (dataList[i].syncMode == ControlMode.Position)
                        {
                            dataList[i].communicationObject.transform.position = receiver.list[0].dataArray[i].position;
                        }
                        else if (dataList[i].syncMode == ControlMode.Velocity)
                        {
                            vel[i].velocity = receiver.list[0].dataArray[i].velocity;
                        }
                        else if (dataList[i].syncMode == ControlMode.VelocityAndPosition)
                        {
                            dataList[i].communicationObject.transform.position = receiver.list[0].dataArray[i].position;
                            vel[i].velocity = receiver.list[0].dataArray[i].velocity;
                        }
                    }
                }
            }
        }



        //Regular procedure
        if (communication == CommunicationMode.Playback)
        {
            if (frameCount < agents.Count && Time.time > agents[frameCount].time)
            {
                for (int i = 0; i < playbackObjectNumber; i++)
                {
                    dataList[i].communicationObject.transform.rotation = agents[frameCount].trans[i].rotation;
                    if (dataList[i].syncMode == ControlMode.Position)
                    {
                        dataList[i].communicationObject.transform.position = agents[frameCount].trans[i].position;
                    }
                    else if (dataList[i].syncMode == ControlMode.Velocity)
                    {
                        vel[i].velocity = agents[frameCount].trans[i].velocity;
                    }
                    else if (dataList[i].syncMode == ControlMode.VelocityAndPosition)
                    {
                        dataList[i].communicationObject.transform.position = agents[frameCount].trans[i].position;
                        vel[i].velocity = agents[frameCount].trans[i].velocity;
                    }

                }

                frameCount++;
            }
        }

    }
}
