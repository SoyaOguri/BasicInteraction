using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using DSUnityViz;

[System.Serializable]
public class WindowSetting
{
    public GameObject camera;
    public Vector2 position;
    public Vector2 size;

    public WindowSetting()
    {
        camera = null;
        position = new Vector2(0, 0);
        size = new Vector2(800, 640);
    }
}


public class CreateOtherWindow : MonoBehaviour {

    public string exePath;

    //public GameObject[] cameras;

    public WindowSetting[] setting;

    [DllImport("user32.dll")]
    static extern int GetForegroundWindow();

    [DllImport("user32.dll", EntryPoint = "MoveWindow")]
    static extern int MoveWindow(int hwnd, int x, int y, int nWidth, int nHeight, int bRepaint);

    //public void OnGUI()
    //{
    //    // Text info
    //    Rect rec2 = new Rect(200, 30, 200, 100);
    //    GUI.Label(rec2, str+ count + "\r\n" + camera_log);
    //    count++;
    //}

    SharedMemoryManager<int> shrFlag = new SharedMemoryManager<int>();  



    // Use this for initialization
    void Start () {
        
        if (setting.Length > 1)
        {
            int displayNum = 0;

            string[] strs = System.Environment.GetCommandLineArgs();

            shrFlag.CreateMemory("CreateOtherWindow", "CreateOtherWindowMutex");

            bool flag = false;
            for (int i = 0; i < strs.Length; i++)
            {
                if (strs[i] == "-ClientWindow")
                {
                    flag = true;
                    displayNum = int.Parse(strs[i+1]);
                    i++;
                }

            }

            int handle = GetForegroundWindow();
            int fWidth = Screen.width;
            int fHeight = Screen.height;
            MoveWindow(handle, (int)setting[displayNum].position.x, (int)setting[displayNum].position.y,
                (int)setting[displayNum].size.x, (int)setting[displayNum].size.y, 1);

            if (flag)//client
            {
                shrFlag.WriteData(0);//初期化終了
                for (int i = 0; i < setting.Length; i++)
                {
                    
                    if(i == displayNum)
                    {
                        setting[i].camera.GetComponent<Camera>().targetDisplay = 0;
                        setting[i].camera.gameObject.SetActive(true);
                    }
                    else
                    {
                        setting[i].camera.GetComponent<Camera>().targetDisplay = 1;
                        setting[i].camera.gameObject.SetActive(false);
                    }
                }

            }
            else//main
            {
                shrFlag.WriteData(0);//初期化

                for (int i = 1; i < setting.Length; i++)
                {
                    shrFlag.ReadData();
                    while (shrFlag.data != 0)//待機
                    {
                        Thread.Sleep(100);
                        shrFlag.ReadData();
                    }

                    if (exePath == "")
                        System.Diagnostics.Process.Start(strs[0], " -popupwindow -ClientWindow " + (i));
                    else
                        System.Diagnostics.Process.Start(exePath, " -popupwindow -ClientWindow " + (i));

                    shrFlag.WriteData(1);//立ち上がるまで待機
                }
            }

            shrFlag.CloseMemory();
        }
    }
	
}
