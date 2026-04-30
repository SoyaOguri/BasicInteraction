using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;

using System;

namespace DSUnityViz
{
    public class DSAuxDataReceive
    {
        
        //Dictionary<int, MessageToShow> showText = new Dictionary<int, MessageToShow>();
    }

    public class MessageToShow
    {
        public Rect drawArea;
        public string message;
        public int fontSize;
    }

    
    class DSVizCommandRecv
    {
        UdpClient udp = null;
        IPEndPoint local_ep;

        public DSVizCommandRecv(int port)
        {
            udp = new UdpClient(port);
            local_ep = new IPEndPoint(IPAddress.Any, port);
        }


        public void StartReceiv()
        {
            udp.BeginReceive(ReceiveCallback, null);
        }


        public void ReceiveCallback( IAsyncResult ar )
        {
            
            Byte[] receiveBytes = udp.EndReceive(ar, ref local_ep);

            string receiveString = Encoding.ASCII.GetString(receiveBytes);

            Console.WriteLine("Received: {0}", receiveString);

            StartReceiv();
        }

    }


    /// <summary>
    /// Command parser
    /// </summary>
    class DSVizCommandParser
    {
        /// <summary>
        /// command queue
        /// </summary>
        public Queue<string> commands = null;

        /// <summary>
        /// Parse all commands in queue
        /// </summary>
        public void Parse()
        {
            string[] com;
            lock (commands)
            {
                com = commands.ToArray();
                commands.Clear();
            }
            foreach( var x in com)
            {
                ParseOne(x);
            }
        }

        /// <summary>
        /// Parse for one command
        /// </summary>
        /// <param name="msg"></param>
        public void ParseOne(string msg)
        {

            if (string.IsNullOrEmpty(msg)) return;

            if (msg.StartsWith("TEXT")) ParseTxtCommand(msg);


        }


        private void ParseTxtCommand(string msg)
        {
            //string[] tokens = msg.Split(',');

        }

    }

}
