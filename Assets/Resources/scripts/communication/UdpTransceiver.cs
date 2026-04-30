using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

using System.Runtime.InteropServices;

namespace DSUnityViz
{
	public class UdpByteSender
	{
		string _remoteHost;
		int _lclPort;
		int _rmtPort;

		System.Net.Sockets.UdpClient udp;

		System.Net.IPEndPoint remoteEP; 

		/// <summary>
		/// UDP通信を開く．送信する．
		/// </summary>
		/// <param name="remote_host">送信先IP</param>
		/// <param name="remote_port">送信先ポート</param>
		/// <param name="local_port">送信元ポート</param>
		/// <returns></returns>
		public int Open( string remoteHost, int remotePort, int localPort = -1 ) {
			_remoteHost = remoteHost;
			_lclPort = localPort;
			_rmtPort = remotePort;


			// UDPクライアントを作成
            if(localPort != -1)
			    udp = new System.Net.Sockets.UdpClient(_lclPort);
            else
                udp = new System.Net.Sockets.UdpClient();

            // 送信先エンドポイント情報を作成
            remoteEP = new IPEndPoint(IPAddress.Parse(_remoteHost), _rmtPort);

			udp.Connect(remoteEP);

			return 0;
		}

		public int Send(byte[] msg)
		{
			return udp.Send(msg, msg.Length);
		}
		
		~UdpByteSender()
		{
			remoteEP = null;
			udp.Close();
			udp = null;
		}

	}

	public class UdpByteReceiver
	{
		// recorder
		StreamRecorder sr = new StreamRecorder();

		int lcl_port;
		//IPAddress remote_host;
		//int rmt_port; 

		System.Net.Sockets.UdpClient udp;

		System.Net.IPEndPoint remoteEP;

		// 受信したパケット群
		List<byte[]> recvedPackets;

		System.Threading.Mutex mutex = new System.Threading.Mutex();


	    private int _max_buffer_size = 1024*1024;

		public bool isOpen()
		{
			return udp.Client.Connected;
		}

		/// <summary>
		/// UDP通信を開く．受信する．
		/// </summary>
		/// <param name="local_port">データを待ちうけるポート</param>
		/// <returns></returns>
		public int Open( int _local_port, int max_buffer_size ) {
			lcl_port = _local_port;
		    _max_buffer_size = max_buffer_size;

            try
            {

                // UDPクライアントを作成
                udp = new System.Net.Sockets.UdpClient(lcl_port);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
			
			// 送信先エンドポイント情報を作成
			remoteEP = new IPEndPoint(IPAddress.Any, 0);

			// 非同期受信を開始
			udp.BeginReceive(ReceiveCallBack, this);

			return 0;
		}


        public int MultiCastOpen(string ip,int _local_port, int max_buffer_size)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), _local_port);
            IPEndPoint m_endPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), _local_port);

            try
            {
                // UDPクライアントを作成
                udp = new System.Net.Sockets.UdpClient(m_endPoint);
                udp.JoinMulticastGroup(endPoint.Address);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            // 送信先エンドポイント情報を作成
            remoteEP = new IPEndPoint(IPAddress.Any, 0);

            // 非同期受信を開始
            udp.BeginReceive(ReceiveCallBack, this);

            return 0;
        }

		public void ClearBuffer() 
		{
			mutex.WaitOne();
			recvedPackets.Clear();
			mutex.ReleaseMutex();
		}

		public byte[] GetAllData()
		{
			mutex.WaitOne();
			int size = 0;
			for( int i = 0; i < recvedPackets.Count; i++)
			{
				size += recvedPackets[i].Length;
			}
			byte[] ret = new byte[size];
			int index = 0;
			for( int i = 0; i < recvedPackets.Count; i++)
			{
				recvedPackets[i].CopyTo( ret, index );
				index += recvedPackets[i].Length;
			}
			mutex.ReleaseMutex();
			return ret;
		}

		/// <summary>
		/// n番目のデータを返す
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public byte[] GetData(int n = -1)
		{
			if (recvedPackets.Count == 0 || recvedPackets.Count <= n) return null;
			if (n == -1) n = recvedPackets.Count - 1;
			
			mutex.WaitOne();
			//int last_index = recvedPackets.Count-1;
			byte[] ret = new byte[recvedPackets[n].Length];
			recvedPackets[n].CopyTo(ret, 0);
			mutex.ReleaseMutex();
			return ret;
		}
		
		private void ReceiveCallBack(IAsyncResult Asr)
		{
			byte[] dat = udp.EndReceive( Asr, ref remoteEP);

			mutex.WaitOne();
		    if (recvedPackets.Count > _max_buffer_size)
		    {
		        recvedPackets.RemoveRange(0, dat.Length);
		    }
			recvedPackets.Add(dat);
			mutex.ReleaseMutex();

			// 今一度非同期受信を開始
			udp.BeginReceive(ReceiveCallBack, this);
		}

		public UdpByteReceiver(int n = 100)
		{
			recvedPackets = new List<byte[]>(n);
		}

		~UdpByteReceiver()
		{
			remoteEP = null;
			udp.Close();
			recvedPackets.Clear();
			recvedPackets = null;
			udp = null;
		}

		public void StartRecording(string filename)
		{
			sr.SetReplayMode(StreamRecorder.RECORDING_TYPE.RECORD, filename);
		}

		public void StopRecording()
		{
			sr.SetReplayMode(StreamRecorder.RECORDING_TYPE.NOTHING, "");
		}

		public void LoadReplay(string filename)
		{
			sr.SetReplayMode(StreamRecorder.RECORDING_TYPE.REPLAY, filename);
		}


	}


	public class UdpSender<T>
	{
		UdpByteSender sender;

		public UdpSender()
		{
			sender = null;
		}

		~UdpSender()
		{
			sender = null;
		}

		/// <summary>
		/// UDPで送信するポートを開く
		/// </summary>
		/// <param name="_remote_host">送信先ホスト</param>
		/// <param name="_remote_port">送信先ポート</param>
		/// <param name="_local_port">送信元ポート</param>
		/// <returns></returns>
		public int Open( string _remote_host, int _remote_port, int _local_port)
		{
			sender = new UdpByteSender();
			int ret = sender.Open(_remote_host, _remote_port, _local_port);
			return ret;
		}

		/// ターゲットになる構造体かクラスを
		/// そのままバイナリイメージで送信する
		public int Send(T trg)
		{
			int size = Marshal.SizeOf(trg);
			byte[] bytes = new byte[size];
			GCHandle gch = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			Marshal.StructureToPtr(trg, gch.AddrOfPinnedObject(), false);

			sender.Send(bytes);
			gch.Free();

			return 0;
		}

	}

	public class UdpReceiver<T>
	{
		UdpByteReceiver recv;

		T lastReceived;

		public UdpReceiver()
		{
			recv = null;
			lastReceived = default(T);
		}

		~UdpReceiver()
		{
			recv = null;
		}

		public bool isOpen()
		{
			return recv.isOpen();
		}

		/// <summary>
		/// UDPの受信ポートを指定して開く
		/// </summary>
		/// <param name="_local_port">UDP受信を受け付けるポート番号</param>
		/// <returns></returns>
		public int Open( int _local_port)
		{
			recv = new UdpByteReceiver();
			int ret = recv.Open(_local_port, Marshal.SizeOf(lastReceived) *50);
			return ret;
		}

        /// <summary>
		/// UDPの受信ポートを指定して開く
		/// </summary>
		/// <param name="_local_port">UDP受信を受け付けるポート番号</param>
		/// <returns></returns>
		public int MultiCastOpen(string ip,int _local_port)
        {
            recv = new UdpByteReceiver();
            int ret = recv.MultiCastOpen(ip,_local_port, Marshal.SizeOf(lastReceived) * 50);
            return ret;
        }

        public T GetLast()
		{
			return lastReceived;
		}

		/// ターゲットになる構造体かクラスを
		/// そのままバイナリイメージで送信する
		public T Get()
		{
			//int size = Marshal.SizeOf( typeof(T) );
			//byte[] bytes = new byte[size];

			byte[] data = recv.GetData(-1);
			if (data == null) return default(T);
			//data.CopyTo(bytes, 0);

			GCHandle gch = GCHandle.Alloc(data, GCHandleType.Pinned);

			T ret = (T)(Marshal.PtrToStructure(gch.AddrOfPinnedObject(), typeof(T)));

			gch.Free();
			lastReceived = ret;
			return ret;
		}

		public T[] GetAll( Func<byte[], int[]> heading_function )
		{
			byte[] data = recv.GetAllData();
			if (data == null || data.Count()==0) return null;

			// find header
			int[] head_index = heading_function(data);
			T[] ret = new T[head_index.Count()];

			int N = Marshal.SizeOf(typeof(T));

			for (int i = 0; i < head_index.Count(); i++)
			{
				byte[] segment = new byte[N];
				Array.Copy(data, head_index[i], segment, 0, N);
				GCHandle gch = GCHandle.Alloc(segment, GCHandleType.Pinned);

				ret[i] = (T)(Marshal.PtrToStructure(
					gch.AddrOfPinnedObject(), 
					typeof(T))
					);
				gch.Free();
			}
			recv.ClearBuffer();
			if (ret.Length != 0)
			{
				lastReceived = ret[ret.Length - 1];
			}
			return ret;
		}
	}

	/// <summary>
	/// UDP Parameter sender
	/// </summary>
	/// Send the parameter by UDP,
	/// No dependability, but simple.
	/// Sending data is:
	/// All data should be ascii, printable code.
	/// Each command is separated by "\n".
	/// Each command contains "Key" and "Data".
	/// "Key" and "Data" are separated by ":"
	/// Key is parameter name, data is value.
	/// This class doesnt support type conversion, 
	/// only sending string is supported.
	public class UdpParameterSender
	{ 
		int lcl_port;       /// local port

		string remote_host; /// remote host to deliver parameter
		int rmt_port;       /// remote port to deliver

		System.Net.Sockets.UdpClient udp;   /// udp client

		System.Net.IPEndPoint remoteEP;     /// endpoint
			   
		/// <summary>
		/// Open UDP
		/// </summary>
		/// <param name="remote_host">Remote IP</param>
		/// <param name="remote_port">Remote Port</param>
		/// <param name="local_port">Sending local port</param>
		/// <returns></returns>
		public int Open( string _remote_host, int _remote_port, int _local_port ) {
			remote_host = _remote_host;
			lcl_port = _local_port;
			rmt_port = _remote_port;   

			// Create UDP client
			udp = new System.Net.Sockets.UdpClient(lcl_port);

			// Enable Broadcast
			udp.EnableBroadcast = true;

			// Set remote endpoint
			remoteEP = new IPEndPoint(IPAddress.Parse(remote_host), rmt_port);

			udp.Connect(remoteEP);

			return 0;
		}

		public int Send(string key, string data)
		{
			string msg = key + ":" + data + "\n";
			byte[] buf = Encoding.ASCII.GetBytes(msg);
			return udp.Send(buf, buf.Length);
		}

		~UdpParameterSender()
		{
			remoteEP = null;
			udp.Close();
			udp = null;
		}
	}

	public class UdpParameterReceiver
	{
		public Dictionary<string, string> param = new Dictionary<string, string>();

		int lcl_port;       /// local port

		//string remote_host; /// remote host to deliver parameter
		//int rmt_port;       /// remote port to deliver

		System.Net.Sockets.UdpClient udp;   /// udp client

		System.Net.IPEndPoint remoteEP;     /// endpoint

		string receive_buffer = "";

		/// <summary>
		/// async receive is alive
		/// </summary>
		public bool isAlive = false;

		/// <summary>
		/// for termination;
		/// </summary>
		public bool isTerminating = false;

		/// <summary>
		/// Open UDP
		/// </summary>
		/// <param name="remote_host">Remote IP</param>
		/// <param name="remote_port">Remote Port</param>
		/// <param name="local_port">Sending local port</param>
		/// <returns></returns>
		public int Open(int _local_port)
		{
			lcl_port = _local_port;

			// Create UDP client
			udp = new System.Net.Sockets.UdpClient(lcl_port);

			// Enable Broadcast
			udp.EnableBroadcast = true;

			// Set remote endpoint
			//remoteEP = new IPEndPoint(IPAddress.Any, 0);

			//udp.Connect(remoteEP);

			return 0;
		}

		public void StartReceiving()
		{
			receive_buffer = "";
			AsyncCallback callback = new AsyncCallback(ReceiveCallBack);
			udp.BeginReceive(callback, udp);
			isAlive = true;
			isTerminating = false;
		}

		public void StopReceiving()
		{
			isTerminating = true;
		}

		private void ReceiveCallBack(IAsyncResult res)
		{
			//if (!res.IsCompleted) return;
			byte[] data = udp.EndReceive(res, ref remoteEP);

			Encoding enc = Encoding.ASCII;
			string str = enc.GetString(data);
			receive_buffer = str;   // no concatenation now
			ParseString(receive_buffer);

			if (!isTerminating)
			{
				AsyncCallback callback = new AsyncCallback(ReceiveCallBack);
				udp.BeginReceive(callback, udp);
			}
			else {
				isAlive = false;
			}
		}

		private void ParseString(string str)
		{
			var x = str.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var y in x)
			{
				var z = y.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
				if (z.Count() != 2) continue;
				param[z[0]] = z[1];
			}
		}

	}

}
