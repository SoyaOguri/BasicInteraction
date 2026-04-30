using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;

using DSUnityViz;
using System;

public class ShowTextInfo : MonoBehaviour
{

    public void Start()
    { 

    }
    
    public void OnGUI()
    {
        // FPS drawing
        Rect rec = new Rect(0, 0, 200, 100);
        string fps = (1 / Time.smoothDeltaTime).ToString("F1");
        GUI.Label(rec, "FPS: " + fps);

        // Size drawing
        Rect rec3 = new Rect(0, 10, 400, 100);
        string dispsize = "";
        
        for (int i = 0; i < Display.displays.Length; i++)
        {
            dispsize = dispsize + "(" + Display.displays[i].renderingWidth.ToString() 
                + "x" + Display.displays[i].renderingHeight.ToString() + ") ";
        }
        GUI.Label(rec3, "Display Resolution: " + dispsize);

        Rect rec4 = new Rect(0,20,400,100);
        string scrres = "";
        scrres = Screen.width.ToString() + "x" + Screen.height.ToString();
        GUI.Label(rec4, "Screen Resolution: " + scrres);

        // Text info
        Rect rec2 = new Rect(0, 30, 200, 100);
        string info = GetInfoText();
        GUI.Label(rec2, info);

    }

    // get text info
    public string GetInfoText()
    {
        // get data
        PacketToDSViz data = DSGlobal.current_car_states;

        string tmp = "";
        //int camera_type = data.GetCamera(0).camera_type;
        int targetCarId = data.GetCamera(0).camera_attached_car_id;

        if ( targetCarId < data.num_of_car_data)
        {

            // pos
            CAR_UNIT_INFO c = data.car_states[targetCarId];
            tmp += string.Format("Pos: ({0,8:F1}, {1,8:F1}, {2,8:F1})", c.x, c.y, c.z);
            tmp += Environment.NewLine;

            // rot
            tmp += string.Format("YPR: ({0,5:F1},{1,5:F1},{2,5:F1})",
                Rad2Deg(c.yaw),
                Rad2Deg(c.pitch),
                Rad2Deg(c.roll));
            tmp += Environment.NewLine;
        }
        else
        { // no such car
          // pos
            tmp += string.Format("Pos: ( - , - , - )");
            tmp += Environment.NewLine;

            // rot
            tmp += string.Format("YPR: ( - , - , - ,)");
            tmp += Environment.NewLine;
        }


        return tmp;
    }

    private float Rad2Deg(float rad)
    {
        return (float)(rad / Math.PI * 180f);
    }
    private double Rad2Deg(double rad)
    {
        return (rad / Math.PI * 180);
    }
    private float Deg2Rad(float deg)
    {
        return (float)(deg * Math.PI / 180f);
    }
    private double Deg2Rad(double deg)
    {
        return (deg * Math.PI / 180);
    }
}
