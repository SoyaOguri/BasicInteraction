using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Net.Http;

namespace AsySocket
{
    public class RecieveDataList
    {
        public IPEndPoint ep;
        List<byte[]> list;
        UInt32 count;
        int divCountNum;
        public bool isRecieved;
        public byte[] data;
        uint tmp;
        int sizeCount, size;

        public RecieveDataList(IPEndPoint endPoint)
        {
            ep = endPoint;
            list = new List<byte[]>();
            count = 0;
            divCountNum = 0;
            isRecieved = false;
        }

        public void SetBytes(byte[] bytes)
        {
            isRecieved = false;

            if (bytes.Length < 10)
                return;
            tmp = BitConverter.ToUInt32(bytes, 2);

            //when restart communication
            if (tmp == 1)
                count = 0;

            //old data
            if (count > tmp)
                return;

            //new data
            if (count < tmp)
            {
                //erease old data
                list.Clear();
                divCountNum = 0;
                count = tmp;
            }

            int divCount = bytes[7];
            //normal order
            if (list.Count == divCount)
            {
                byte[] btemp = new byte[bytes.Length - 10];
                System.Buffer.BlockCopy(bytes, 10, btemp, 0, btemp.Length);
                list.Add(btemp);
            }//already exist
            else if (list.Count > divCount)
            {
                if (list[divCount].Length < bytes.Length - 10)
                {
                    list[divCount] = new byte[bytes.Length - 10];
                }
                System.Buffer.BlockCopy(bytes, 10, list[divCount], 0, bytes.Length - 10);
            }//swapped order
            else
            {
                byte[] btemp;

                for (int i = list.Count; i < divCount; i++)
                {
                    btemp = new byte[bytes.Length - 10];
                    list.Add(btemp);
                }
                btemp = new byte[bytes.Length - 10];
                System.Buffer.BlockCopy(bytes, 10, btemp, 0, bytes.Length - 10);
                list.Add(btemp);
            }

            divCountNum++;

            //All data is got
            if (divCountNum == (int)bytes[6])
            {
                size = 0;
                for (int i = 0; i < divCountNum; i++)
                {
                    size += list[i].Length;
                }

                data = new byte[size];

                sizeCount = 0;
                for (int i = 0; i < divCountNum; i++)
                {
                    System.Buffer.BlockCopy(list[i], 0, data, sizeCount, list[i].Length);
                    sizeCount += list[i].Length;
                }

                divCountNum = 0;
                isRecieved = true;
            }

        }
    }

    public class UPnPManagerClass
    {
        UdpClient receiveClient;
        IPEndPoint router, remoteEP;
        int waitCount;
        int setPort;

        string mSearchString, controlURL, serviceType;
        string[] rn = { "\r\n" };
        string[] serviceSpl = { "<serviceList>" };
        string[] controlSpl = { "<controlURL>", "</controlURL>" };
        string routerAd, routerPo, routXML;

        int durationSec;

        static readonly string REQUEST_MESSAGE = String.Concat(
            "M-SEARCH * HTTP/1.1\r\n",
            "MX: 3\r\n",
            "HOST: 239.255.255.250:1900\r\n",
            "MAN: \"ssdp: discover\"\r\n",
            "ST: "
        );

        static readonly string[] HTTP_REQUEST = {
            "GET "," HTTP/1.1\r\nHost: ","\r\nConnection: keepalive\r\n"
        };

        static public readonly string[] SERVICE_TYPE = {
            "urn:schemas-upnp-org:service:WANIPConnection:1",
            "urn:schemas-upnp-org:service:WANPPPConnection:1"
        };


        public string requestMassage(string serviceType)
        {
            return String.Concat(REQUEST_MESSAGE, serviceType, "\r\n");
        }

        public UPnPManagerClass()
        {
            setPort = -1;

            router = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
            durationSec = 3600;//とりあえず1時間
        }

        public bool ConnectUPnP(string service, int port, string ip)
        {
            try
            {
                serviceType = service;
                MSearch(ip);
                setPort = port;
                RequestUPnP(port, ip);
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void ApplicationExit(object sender, EventArgs e)
        {

        }

        //
        public void Close()
        {
            if (setPort != -1)
            {
                RequestDeleteUPnP(setPort);
                setPort = -1;
            }
        }



        private string RequestDeleteUPnP(int port)
        {
            return DeletePortMapping("http://" + routerAd + ":" + routerPo + controlURL, serviceType, port);
        }

        private void GetControlURL()
        {
            WebClient webClient = new WebClient();

            string str = webClient.DownloadString("http://" + routerAd + ":" + routerPo + routXML);

            string[] serviceStrs = str.Split(serviceSpl, StringSplitOptions.None);

            string serviceText = "";

            foreach (var item in serviceStrs)
            {
                if (item.Contains(serviceType))
                {
                    serviceText = item;
                    break;
                }
            }

            if (serviceText == "")
            {
                throw new Exception("Download XML file was wrong");
            }

            controlURL = serviceText.Split(controlSpl, StringSplitOptions.None)[1];
        }

        private string RequestUPnP(int port, string ip)
        {
            GetControlURL();

            string result;

            result = SetPortMapping("http://" + routerAd + ":" + routerPo + controlURL, serviceType, port, ip);

            if (result == null)
            {
                throw new Exception("UPnP Request was failed");
            }

            return result;
        }

        public void SetDurationSec(int sec)
        {
            durationSec = sec;
        }

        private string SetPortMapping(string controlURL, string serviceType, int port, string localAddress)
        {
            return GetSoapResult(controlURL, serviceType, "AddPortMapping", CreateSoapBody(AddPortMappingBody(serviceType, port, localAddress, "Test", durationSec, "UDP")));
        }

        private string DeletePortMapping(string controlURL, string serviceType, int port)
        {
            return GetSoapResult(controlURL, serviceType, "DeletePortMapping", CreateSoapBody(DeletePortMappingBody(serviceType, port, "UDP")));
        }

        private string GetSoapResult(string controlURL, string serviceType, string methodName, byte[] soapMessage)
        {
            WebRequest webRequest = WebRequest.Create(controlURL);

            webRequest.Method = "POST";
            webRequest.Headers.Add("SOAPAction", String.Format(System.Globalization.CultureInfo.InvariantCulture, @"""{0}#{1}""", serviceType, methodName));

            webRequest.ContentType = @"text/xml;charset=""utf-8""";
            webRequest.ContentLength = soapMessage.Length;

            using (System.IO.Stream stream = webRequest.GetRequestStream())
            {
                stream.Write(soapMessage, 0, soapMessage.Length);
                stream.Flush();
                stream.Close();
            }

            HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse;
            string result = null;

            if (webResponse.StatusCode == HttpStatusCode.OK)
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(webResponse.GetResponseStream()))
                {
                    result = sr.ReadToEnd();
                    sr.Close();
                }
            }

            return result;
        }

        private byte[] CreateSoapBody(string body)
        {
            string soapMessage = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                @"<?xml version=""1.0"" ?>
                <SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/"" SOAP-ENV:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
                <SOAP-ENV:Body>
                {0}
                </SOAP-ENV:Body>
                </SOAP-ENV:Envelope>", body);

            return UTF8Encoding.ASCII.GetBytes(soapMessage);
        }

        public string AddPortMappingBody(string serviceType, int port, string localAddress, string description, int duration, string protocol)
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture,
                @" <m:AddPortMapping xmlns:m=""{0}"">
<NewRemoteHost></NewRemoteHost>
<NewExternalPort>{1}</NewExternalPort>
<NewProtocol>{5}</NewProtocol>
<NewInternalPort>{1}</NewInternalPort>
<NewInternalClient>{2}</NewInternalClient>
<NewEnabled>1</NewEnabled>
<NewPortMappingDescription>{3}</NewPortMappingDescription>
<NewLeaseDuration>{4}</NewLeaseDuration>
</m:AddPortMapping>", serviceType, port, localAddress, description, duration, protocol);
        }

        public string DeletePortMappingBody(string serviceType, int port, string protocol)
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture,
                @" <m:DeletePortMapping xmlns:m=""{0}"" >
                        <NewRemoteHost></NewRemoteHost>
                        <NewExternalPort>{1}</NewExternalPort>
                        <NewProtocol>{2}</NewProtocol>
                        </m:DeletePortMapping> ", serviceType, port, protocol);
        }


        public void MSearch(string ip)
        {
            using (UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(ip), 54755)))
            {

                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(requestMassage(serviceType));


                udpClient.BeginReceive(OnReceive, udpClient);

                waitCount = 6000;

                int res = udpClient.Send(bytes, bytes.Length, router);


                //udpClient.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1900));

                for (int i = 0; i < waitCount; i++)
                {
                    System.Threading.Thread.Sleep(10);
                }
                udpClient.Close();
            }


            if (waitCount > 0)
            {
                throw new Exception("No M-Search response from router");
            }


            string[] strs = mSearchString.Split(rn, StringSplitOptions.None);
            foreach (var item in strs)
            {
                if (item.Contains("LOCATION:"))
                {
                    string[] addirStrings = item.Split('/');
                    string[] addirTemp = addirStrings[2].Split(':');
                    routerAd = addirTemp[0];
                    routerPo = addirTemp[1];
                    routXML = "";
                    for (int i = 3; i < addirStrings.Length; i++)
                    {
                        routXML += "/" + addirStrings[i];
                    }
                    break;
                }
                if (item.Contains("location:"))
                {
                    string[] addirStrings = item.Split('/');
                    string[] addirTemp = addirStrings[2].Split(':');
                    routerAd = addirTemp[0];
                    routerPo = addirTemp[1];
                    routXML = "";
                    for (int i = 3; i < addirStrings.Length; i++)
                    {
                        routXML += "/" + addirStrings[i];
                    }
                    break;
                }
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            byte[] bytes;
            receiveClient = (System.Net.Sockets.UdpClient)ar.AsyncState;

            //非同期受信を終了する
            remoteEP = null;
            try
            {
                bytes = receiveClient.EndReceive(ar, ref remoteEP);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                Console.WriteLine("ReceiveError({0}/{1}).",
                    ex.Message, ex.ErrorCode);
                return;
            }
            catch (ObjectDisposedException ex)
            {
                //すでに閉じている時は終了
                Console.WriteLine("Socket is already closed.");
                return;
            }

            mSearchString = System.Text.Encoding.ASCII.GetString(bytes);

            waitCount = -1;
        }
    }


    public class AsynchronousSocket
    {

        IPEndPoint local, sendEndPoint;
        UdpClient udpClient, receiveClient;
        bool isContinue = false;

        public delegate void RecieveData(ref Byte[] bytes, IPEndPoint ep);
        public event RecieveData OnRecieveData;
        public delegate void ErrorEvent();
        public event ErrorEvent OnErrorEvent;

        byte[] rcvBytes, sndBytes;

        byte[] sendBytes;

        UInt32 sendCount = 0;
        byte[] bitCon;
        const int MAX_BYTE_LENGTH = 1400;
        const int MAX_SEND_BYTE = 20000;
        int sendBytePosition;

        List<RecieveDataList> list;
        System.Net.IPEndPoint remoteEP;
        List<byte[]> divBytes;
        List<int> divBytesSize;
        int divisionNum;

        UPnPManagerClass upnp;

        public AsynchronousSocket(int port = 0)
        {



            local = new IPEndPoint(IPAddress.Any, port);

            udpClient = new UdpClient(local);

            list = new List<RecieveDataList>();
            sendBytes = new byte[MAX_SEND_BYTE];
            sendBytePosition = 10;

            sndBytes = new byte[MAX_SEND_BYTE];

            divBytes = new List<byte[]>();
            divBytesSize = new List<int>();

            for (int i = 0; i < MAX_SEND_BYTE;)
            {
                divBytes.Add(new byte[MAX_BYTE_LENGTH]);
                i += MAX_BYTE_LENGTH;
                divBytesSize.Add(0);
            }


            upnp = new UPnPManagerClass();
            //upnp.ConnectUPnP(UPnPManagerClass.SERVICE_TYPE[0], port, "192.168.11.55");
            //upnp.Close();
        }

        public enum ServiceType
        {
            IP = 0,
            PPP = 1,
        }

        public void ConnectUPnP(ServiceType type, int port, string ip)
        {
            upnp.ConnectUPnP(UPnPManagerClass.SERVICE_TYPE[(int)type], port, ip);
        }

        public void Close()
        {
            upnp.Close();
            udpClient.Close();
        }

        public void Start()
        {
            udpClient.BeginReceive(OnReceive, udpClient);
        }

        public void Stop()
        {
            isContinue = false;

        }

        public void SetSendEndPoint(string ip, int port)
        {
            sendEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public void Send<T>(T data, string ip, int port)
        {
            SetPacket<T>(ref data);
            UDPSend(new IPEndPoint(IPAddress.Parse(ip), port));
        }

        public void Send<T>(T data)
        {
            SetPacket<T>(ref data);
            UDPSend(sendEndPoint);
        }

        public void Send<T>(T data, IPEndPoint ep, int structSize = 0)
        {
            SetPacket<T>(ref data, structSize);
            UDPSend(ep);
        }

        public void SetPacket<T>(ref T data, int structSize = 0)
        {
            if (structSize == 0)
            {
                GCHandle gch = GCHandle.Alloc(sndBytes, GCHandleType.Pinned);
                Marshal.StructureToPtr(data, gch.AddrOfPinnedObject(), false);
                System.Buffer.BlockCopy(sndBytes, 0, sendBytes, sendBytePosition, Marshal.SizeOf(data));
                gch.Free();
                sendBytePosition += Marshal.SizeOf(data);
            }
            else
            {
                GCHandle gch = GCHandle.Alloc(sndBytes, GCHandleType.Pinned);
                Marshal.StructureToPtr(data, gch.AddrOfPinnedObject(), false);
                System.Buffer.BlockCopy(sndBytes, 0, sendBytes, sendBytePosition, structSize);
                gch.Free();
                sendBytePosition += structSize;
            }

        }


        /// <summary>
        /// 大きいパケットの場合は分割して送信する
        /// パケット規則 
        /// 0-1 NU
        /// 2-5 送信の回数
        /// 6 分割数
        /// 7 分割番号
        /// 8-9 割り当てなし
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="ep"></param>
        private void SetSetPacket()
        {

            sendBytes[0] = (byte)'N'; sendBytes[1] = (byte)'U';
            bitCon = BitConverter.GetBytes(sendCount);
            sendBytes[2] = bitCon[0]; sendBytes[3] = bitCon[1]; sendBytes[4] = bitCon[2]; sendBytes[5] = bitCon[3];

            if (sendBytePosition < MAX_BYTE_LENGTH)
            {
                sendBytes[6] = 1;
                sendBytes[7] = 0;
                divisionNum = 1;
                System.Buffer.BlockCopy(sendBytes, 0, divBytes[0], 0, sendBytePosition);
                divBytesSize[0] = sendBytePosition;
                //udpClient.Send(bytes, bytes.Length, ep);
            }
            else
            {
                divisionNum = (int)((float)sendBytePosition / MAX_BYTE_LENGTH) + 1;

                int byteLength;
                for (int i = 0; i < divisionNum; i++)
                {
                    if (i == divisionNum - 1)
                    {
                        divBytes[i][0] = (byte)'N'; divBytes[i][1] = (byte)'U'; divBytes[i][2] = bitCon[0]; divBytes[i][3] = bitCon[1]; divBytes[i][4] = bitCon[2]; divBytes[i][5] = bitCon[3];
                        byteLength = sendBytePosition - (MAX_BYTE_LENGTH - 10) * i;
                        System.Buffer.BlockCopy(sendBytes, (MAX_BYTE_LENGTH - 10) * i + 10, divBytes[i], 10, byteLength - 10);
                        divBytesSize[i] = byteLength;
                    }
                    else if (i == 0)
                    {
                        System.Buffer.BlockCopy(sendBytes, 0, divBytes[i], 0, MAX_BYTE_LENGTH);
                        byteLength = MAX_BYTE_LENGTH;
                        divBytesSize[0] = MAX_BYTE_LENGTH;
                    }
                    else
                    {
                        divBytes[i][0] = (byte)'N'; divBytes[i][1] = (byte)'U'; divBytes[i][2] = bitCon[0]; divBytes[i][3] = bitCon[1]; divBytes[i][4] = bitCon[2]; divBytes[i][5] = bitCon[3];
                        System.Buffer.BlockCopy(sendBytes, (MAX_BYTE_LENGTH - 10) * i + 10, divBytes[i], 10, MAX_BYTE_LENGTH - 10);
                        byteLength = MAX_BYTE_LENGTH;
                        divBytesSize[i] = MAX_BYTE_LENGTH;
                    }

                    divBytes[i][6] = (byte)divisionNum;
                    divBytes[i][7] = (byte)i;

                    //udpClient.Send(divBytes, byteLength, ep);
                }
            }

            sendCount++;
        }

        public void Send(IPEndPoint ep)
        {
            UDPSend(ep);
        }

        public void Send()
        {
            UDPSend(sendEndPoint);
        }

        public void NoCleaerSend(IPEndPoint ep)
        {
            NoClearUDPSend(ep);
        }

        public void ClearPacket()
        {
            sendBytePosition = 10;
        }

        private void UDPSend(IPEndPoint ep)
        {
            NoClearUDPSend(ep);
            ClearPacket();
        }




        private void NoClearUDPSend(IPEndPoint ep)
        {
            SetSetPacket();
            for (int i = 0; i < divisionNum; i++)
            {
                udpClient.Send(divBytes[i], divBytesSize[i], ep);
            }
        }

        public T BytesToData<T>(ref byte[] bytes, int structSize, int srcOffset)
        {
            rcvBytes = new byte[structSize];

            System.Buffer.BlockCopy(bytes, srcOffset, rcvBytes, 0, bytes.Length);
            GCHandle gch = GCHandle.Alloc(rcvBytes, GCHandleType.Pinned);
            T t = (T)(Marshal.PtrToStructure(gch.AddrOfPinnedObject(), typeof(T)));
            gch.Free();

            return t;
        }

        public byte[] DataToBytes<T>(ref T t, int structSize, int distOffset = 0)
        {
            byte[] bytes = new byte[structSize + distOffset];
            sndBytes = new byte[structSize + distOffset];

            GCHandle gch = GCHandle.Alloc(sndBytes, GCHandleType.Pinned);
            Marshal.StructureToPtr(t, gch.AddrOfPinnedObject(), false);
            System.Buffer.BlockCopy(sndBytes, 0, bytes, distOffset, structSize);
            gch.Free();

            return bytes;
        }


        private void OnReceive(IAsyncResult ar)
        {
            byte[] bytes;
            receiveClient = (System.Net.Sockets.UdpClient)ar.AsyncState;

            if (isContinue)
                return;

            //非同期受信を終了する
            remoteEP = null;
            try
            {
                bytes = receiveClient.EndReceive(ar, ref remoteEP);

            }
            catch (System.Net.Sockets.SocketException ex)
            {
                Console.WriteLine("ReceiveError({0}/{1}).",
                    ex.Message, ex.ErrorCode);
                OnErrorEvent();
                return;
            }
            catch (ObjectDisposedException ex)
            {
                //すでに閉じている時は終了
                Console.WriteLine("Socket is already closed.");
                OnErrorEvent();
                return;
            }

            int epNum = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].ep.Equals(remoteEP))
                {
                    epNum = i;
                    break;
                }
            }
            if (epNum == -1)//new EP
            {
                if (bytes[0] == (byte)'N' && bytes[1] == (byte)'U')
                {//check packet
                    epNum = list.Count;
                    list.Add(new RecieveDataList(remoteEP));
                }
                else//unkown packet
                {
                    return;
                }


            }

            list[epNum].SetBytes(bytes);

            if (list[epNum].isRecieved)
            {
                OnRecieveData(ref list[epNum].data, list[epNum].ep);
            }

            try
            {
                //再びデータ受信を開始する
                receiveClient.BeginReceive(OnReceive, receiveClient);
            }
            catch
            {
                OnErrorEvent();
            }
        }
    }
}
