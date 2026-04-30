using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;


/// <summary>
/// Set multi monitor setting
/// </summary>
/// Please set the number of monitors, 
/// and set the resolution in x(width) and y(height).
/// 
/// Note that if monitors has different size, 
/// some display may show un-usual offset.
/// Then please use just wide window in extended window mode but not fullscreen.
public class EnableMultiDisplay : MonoBehaviour {

    [SerializeField]
    public Vector2[] Monitors = new Vector2[1] ;

	// Use this for initialization
	void Start () {

        if(Monitors.Length == 1)
        {
            // Single and fullscreen monitor
            Screen.SetResolution((int)Monitors[0].x, (int)Monitors[0].y, true);
        }else if(Monitors.Length >= 2 && Monitors.Length <= 8)
        {
            // enable multi monitor in usual way
            for (int i = 0; i < Monitors.Length && i < Display.displays.Length; i++)
            {
                Display.displays[i].Activate();
            }
        }
    }

    // Update is called once per frame
    void Update () {

    }

}
