using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System;

using AsySocket;

public class UPnPConnecter : MonoBehaviour
{
    UPnPManagerClass upnp;
    public int openPort;
    public string ip;
    public enum ServiceType
    {
        IP = 0,
        PPP = 1,
    }

    public ServiceType serviceType;

    // Start is called before the first frame update
    void Start()
    {
        upnp = new UPnPManagerClass();
        upnp.ConnectUPnP(UPnPManagerClass.SERVICE_TYPE[(int)serviceType], openPort,ip);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnApplicationQuit()
    {
        upnp.Close();
    }
}
