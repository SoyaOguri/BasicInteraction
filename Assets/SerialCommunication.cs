using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class SerialCommunication : MonoBehaviour
{
    SerialReceiver distanceSensor = new SerialReceiver();

    public string DeviceName;

    public string portName;
    public int baudRate;

    public float data;

    public string rawData;

    //public bool needSplitter;

    public enum SerialDataFormat
    {
        Int,
        SplitInt,
        Float,
        String,
    }

    public SerialDataFormat dataFormat;

    // Start is called before the first frame update
    void Start()
    {
        distanceSensor.SetSplitFlag(dataFormat);
        distanceSensor.Open(portName, baudRate, Parity.None,8,StopBits.One);
    }

    // Update is called once per frame
    void Update()
    {
        data = distanceSensor.data;
        rawData = distanceSensor.message;
    }

    private void OnApplicationQuit()
    {
        distanceSensor.Close();
    }
}

public class SerialReceiver : SerialCommunicator
{
    public float data;
    float preData;

    bool splitFlag;
    string[] strs;

    SerialCommunication.SerialDataFormat dataFormat;

    public void SetSplitFlag(SerialCommunication.SerialDataFormat format)
    {
        dataFormat = format;
    }

    private void DataReceiveFunction()
    {
        try
        {
            preData = int.Parse(message);
            data = preData;
        }
        catch { }
    }

    private void DataFloatReceiveFunction()
    {
        try
        {
            preData = float.Parse(message);
            data = preData;
        }
        catch { }
    }

    private void DataSplitReceiveFunction()
    {
        strs = message.Split('=');
        try
        {
            preData = int.Parse(strs[1]);
            data = preData;
        }
        catch { }
    }

    private void DataStringReceiveFunction()
    {
        strs = message.Split(',');
        try
        {
            if(strs.Length == 2)
            {
                preData = int.Parse(strs[0]);
            }
            else
            {
                preData = 0;
            }
            data = preData;
        }
        catch { }
    }

    public new void Open(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
    {
        base.Open(portName, baudRate, parity, dataBits, stopBits);
        if(dataFormat == SerialCommunication.SerialDataFormat.Int)
            DataTransformHandler += DataReceiveFunction;
        else if(dataFormat == SerialCommunication.SerialDataFormat.SplitInt)
            DataTransformHandler += DataSplitReceiveFunction;
        else if(dataFormat == SerialCommunication.SerialDataFormat.Float)
            DataTransformHandler += DataFloatReceiveFunction;
        else if (dataFormat == SerialCommunication.SerialDataFormat.String)
            DataTransformHandler += DataStringReceiveFunction;
    }
}

public class SerialCommunicator : System.IDisposable
{
    protected SerialPort serialPort;
    public string message;
    Thread thread;

    public delegate void DataTransform();
    public DataTransform DataTransformHandler;

    protected bool isRunning = false;

    public void Open(string portName,int baudRate,Parity parity,int dataBits,StopBits stopBits)
    {
        serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
        serialPort.Open();

        isRunning = true;

        thread = new Thread(Read);

        thread.Start();

    }

    private void Read()
    {
        while(isRunning && serialPort != null && serialPort.IsOpen)
        {
            if (serialPort.BytesToRead > 0)
            {
                message = serialPort.ReadLine();
                DataTransformHandler?.Invoke();
            }
            Thread.Sleep(0);
        }
    }

    public void Close()
    {
        isRunning = false;

        if (thread != null && thread.IsAlive)
            thread.Join();

        if(serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            serialPort.Dispose();
        }
    }

    public void Dispose()
    {
        Close();
    }


}




