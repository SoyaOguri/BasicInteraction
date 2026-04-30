using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using DSUnityViz;

public class DSGlobal {

    /// <summary>
    /// Master object for object control
    /// </summary>
    static public GameObject object_master;

    /// <summary>
    /// Master object for camera control
    /// </summary>
    static public GameObject camera_master;

    /// <summary>
    /// Received car states from DS;
    /// </summary>
    static public PacketToDSViz current_car_states;

    /// <summary>
    /// Aux data from DS through serial command
    /// </summary>
    static public DSAuxDataReceive aux_data;

}
