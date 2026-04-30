using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;

namespace DSUnityViz
{

    public class SharedMemoryDLL
    {
        // アクセス制限設定等
        //==========================
        public const UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public const UInt32 SECTION_QUERY = 0x0001;
        public const UInt32 SECTION_MAP_WRITE = 0x0002;
        public const UInt32 SECTION_MAP_READ = 0x0004;
        public const UInt32 SECTION_MAP_EXECUTE = 0x0008;
        public const UInt32 SECTION_EXTEND_SIZE = 0x0010;
        public const UInt32 SECTION_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | SECTION_QUERY |
        SECTION_MAP_WRITE |
        SECTION_MAP_READ |
        SECTION_MAP_EXECUTE |
        SECTION_EXTEND_SIZE);
        public const UInt32 FILE_MAP_ALL_ACCESS = SECTION_ALL_ACCESS;
        public const UInt32 PAGE_READWRITE = 4;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFileMapping(IntPtr hFile,
        IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh,
        uint dwMaximumSizeLow, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint
        dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow,
        uint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll")]
        public static extern void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

        [DllImport("kernel32.dll")]
        public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);
    }

    public class SharedMemoryManager<Type>
    {
        public Type data;
        private int dataSize;

        //共有メモリ確保用のハンドルとポインタを定義 
        private IntPtr shrMemHandle = IntPtr.Zero;
        private IntPtr shrMemPtr = IntPtr.Zero;

        private bool isOpen = false;

        //共有メモリへのアクセス用排他ミューテックス
        private Mutex m_Mutex = null;

        public int Size
        {
            get { return dataSize; }
        }
        public bool IsOpen
        {
            get { return isOpen; }
        }


        public SharedMemoryManager()
        {
            dataSize = Marshal.SizeOf(data);
        }

        ~SharedMemoryManager()
        {
            CloseMemory();
        }

        /// <summary>
        /// 共有メモリを開く
        /// </summary>
        /// <param name="objectName">識別子</param>
        /// <returns>エラー番号</returns>
        public int CreateMemory(string objectName,string mutexName)
        {
            try
            {
                shrMemHandle = SharedMemoryDLL.CreateFileMapping((IntPtr)(-1), // ファイルハンドル 
                IntPtr.Zero, // セキュリティ属性 
                SharedMemoryDLL.PAGE_READWRITE, // 保護属性(R/W) 
                0, // サイズ上位 
                (uint)dataSize, // サイズ下位 
                objectName); // オブジェクト名

                if (shrMemHandle == IntPtr.Zero)
                    throw new Exception();

                // マッピング処理
                shrMemPtr = SharedMemoryDLL.MapViewOfFile(shrMemHandle, SharedMemoryDLL.SECTION_ALL_ACCESS, 0, 0, 0);

                isOpen = true;

                m_Mutex = new System.Threading.Mutex(false, mutexName);

                return System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            }
            catch (Exception )
            {
                return System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            }
        }

        /// <summary>
        /// 共有メモリを閉じる
        /// 重複して読んでもOK
        /// </summary>
        public void CloseMemory()
        {
            if (shrMemPtr != IntPtr.Zero)
            {
                SharedMemoryDLL.UnmapViewOfFile(shrMemPtr);
                shrMemPtr = IntPtr.Zero;
            }
            if (shrMemHandle != IntPtr.Zero)
            {
                SharedMemoryDLL.CloseHandle(shrMemHandle);
                shrMemHandle = IntPtr.Zero;
                isOpen = false;
            }
            if (m_Mutex != null)
            {
                m_Mutex.Close();
                m_Mutex = null;
            }
        }

        /// <summary>
        /// データ読む(メンバ変数へ)
        /// </summary>
        public void ReadData()
        {
            //排他制御 ミューテックス取得 
            m_Mutex.WaitOne(Timeout.Infinite, true);
            object obj = Marshal.PtrToStructure(shrMemPtr, typeof(Type));
            //ミューテックス解放
            m_Mutex.ReleaseMutex();

            data = (Type)obj;
        }

        /// <summary>
        /// データ読む(引数へ)
        /// </summary>
        public void ReadData(ref Type sharedData)
        {
            //排他制御 ミューテックス取得 
            m_Mutex.WaitOne(Timeout.Infinite, true);
            sharedData = (Type)Marshal.PtrToStructure(shrMemPtr, typeof(Type));
            //ミューテックス解放
            m_Mutex.ReleaseMutex();
        }

        /// <summary>
        /// データ書く
        /// </summary>
        /// <param name="sharedData"></param>
        public void WriteData(Type sharedData)
        {
            //排他制御 ミューテックス取得 
            m_Mutex.WaitOne(Timeout.Infinite, true);
            Marshal.StructureToPtr(sharedData, shrMemPtr, true);
            //ミューテックス解放
            m_Mutex.ReleaseMutex();
        }
    }
}