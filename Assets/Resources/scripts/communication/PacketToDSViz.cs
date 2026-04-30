using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.InteropServices;

namespace DSUnityViz
{
    [Serializable()]
    public struct TIME_INFO
    {
        public Int32 frame_cnt;
        public float simulated_time;
        public float measured_time;
        public float deltaT;
    }

    /// <summary>
    /// Camera status
    /// </summary>
    /// camera_type : 
    ///  1: Camera follows car directed by camera_attached_car_id
    ///  2: Camera position is directed by coordination
    ///  3: 
    /// camera_attached_car_id : 
    ///  n: Car ID to follow by CameraMaster
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    public struct CAMERA_INFO
    {

        public int camera_type;
        public int camera_attached_car_id;

        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        //public float[] camera_parameters;
        /// <summary>
        /// 1st Camra position parameter 
        /// </summary>
        /// camera_type = 1 : this is ignored
        /// camera_type = 2 : camera_param1 means the camra position
        /// 
        /// camera_param1[] = [x, y, z, reserved]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] camera_param1;
        /// <summary>
        /// 2nd camera position parameter
        /// </summary>
        /// camera_type = 1 : this is ignored
        /// camera_type = 2, camera_param1 means the camera look for
        /// 
        /// camera_param2[] = [x, y, z, reserved]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] camera_param2;
        /// <summary>
        /// 3rd camera position parameter
        /// </summary>
        /// camera_type = 1 : this is ignored
        /// camera_type = 2, camera_param1 means the camera up vector
        /// 
        /// camera_param2[] = [x, y, z, reserved]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] camera_param3;
    }

    [Serializable()]
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    public struct CAR_UNIT_INFO
    {
        public int id;
        public float x;
        public float y;
        public float z;
        public float yaw;
        public float pitch;
        public float roll;

        public float steering;
        public float accelpedal;
        public float brakepedal;
        public float speed;
        public float eng_rpm;
        public int winkers;

        public int mesh_id;
        public int anim_cnt;
        public int anim_flag;

        public override string ToString()
        {
            return x.ToString("F2") + ", " + y.ToString("F2");
        }
    }


    [Serializable()]
    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
    public struct STATIC_OBJECTS
    {
        public ushort count;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public ushort[] id;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public ushort[] state;
    };

    [Serializable()]
    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
    public struct PacketToDSViz
    {
        // length of data setting
        public const int NCamera = 4;
        public const int NCar = 32;
        public const int NStaticObjects = 128;

        // header strings
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public char[] header_str;

        // Time info
        public TIME_INFO time_info;

        // camera data
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NCamera)]
        public CAMERA_INFO[] camera_info;

        // car info
        public int num_of_car_data;    ///< number of cars

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NCar)]
        public CAR_UNIT_INFO[] car_states;  ///< car states


        public STATIC_OBJECTS static_objects;

        public PacketToDSViz(int n = 0)
        {
            header_str = "NUDSDATA".ToCharArray();
            time_info = default(TIME_INFO);

            num_of_car_data = 0;
            car_states = new CAR_UNIT_INFO[NCar];
            camera_info = new CAMERA_INFO[NCamera];

            static_objects = default(STATIC_OBJECTS);
            static_objects.id = new ushort[NStaticObjects];
            static_objects.state = new ushort[NStaticObjects];
        }

        public CAMERA_INFO GetCamera(int n)
        {
            if (camera_info == null) initCameraInfoArray();
            if (camera_info[n].camera_type == 0)
                n = 0;
            return camera_info[n];
        }

        private void initCameraInfoArray()
        {
            camera_info = new CAMERA_INFO[NCamera];
            for (int i = 0; i < NCamera; i++)
                camera_info[i] = new CAMERA_INFO();
        }

        public static int[] heading(byte[] data)
        {
            int data_len = data.Length;
            int data_size = Marshal.SizeOf(typeof(PacketToDSViz));

            List<int> indices = new List<int>();

            int index = 0;
            byte target = (byte)'N';
            while (-1 != (index = Array.IndexOf(data, target, index)))
            {
                if (index + data_size > data_len) break;


                if (data[index + 1] == (byte)'U'
                    && data[index + 2] == (byte)'D'
                    && data[index + 3] == (byte)'S'
                    && data[index + 4] == (byte)'D'
                    && data[index + 5] == (byte)'A'
                    && data[index + 6] == (byte)'T'
                    && data[index + 7] == (byte)'A'
                    )
                { // packet is correct
                    indices.Add(index);
                    index += data_size;
                }
                else { index++; }
            }
            return indices.ToArray();
        }

    }


    [Serializable()]
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct ShrCarState
    {
        public int m_carID; // 車のID(インデックスと等しい)
        public int m_carTypeID; // 車種を特定するためのID，主に表示用(メッシュリストから適宜選ぶ)
        public int m_groupID; // 車のグループを特定するID(自車，左車線車，右車線車，対向車…)

        // 座標系は2通りとりあえず持っておき，どちらをおもに用いて制御するかはサブクラスに任せる
        public double m_u, m_v, m_w; // コースに沿った座標(m_uは周回数含めた値で持つ)
        public double m_x, m_y, m_z; // グローバルな座標
        public double m_roll, m_pitch, m_yaw; // // グローバルな座標系に対する姿勢(roll->pitch->yaw) -pi/2～pi/2
        public double m_speed; // 直進速度
        public double m_accel; // 直進方向加速度

        // added for bycycle model control by Okuda @ 2014 10/29
        public double m_slip_angle; // Slip angle of vehicle

        // スロットル，ブレーキ，ステアリング．
        // 実際に車両に適用される操作量
        public double m_rpm;       // エンジン回転数(added by 奥田)	//以下，他車も操作する可能性があるので
        public double m_throtpdl;  // スロットル(added by 奥田)
        public double m_brakepdl;  // ブレーキ(added by 奥田)
        public double m_steering;  // ステアリング(added by 奥田)

        // 入力 : added by okuda 2014/07/27
        public double m_throttle_input;
        public double m_brake_input;
        public double m_steering_input;

        public int m_winker_R;
        public int m_winker_L;

        // フラグとか
        // 0bit: 距離離れすぎフラグ＠pattern_gentle_lead
        // 1-bit : 空き
        public int m_flg;
    }
}