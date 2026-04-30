using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Xml;
using System.Xml.XPath;

/// <summary>
/// Parameter of NUDS Vis
/// </summary>
public class DSParam
{
    
    /// <summary>Model names to register the models for car in default</summary>
    static public Dictionary<int, string> ModelBank = new Dictionary<int, string>
    {
        { 0, "Blank" },
        { 1, "Blank" },
        { 2, "Blank" },
        { 3, "SkyCarWrapRed" },
    };
    
    #region //Minor setting, not use in usual

    /// <summary>Master object name for car control</summary>
    /// !! This is not used, but just condirmation.
    /// The object "MasterObject.cs" attached is used as master.
    static public string ObjectMasterName = "MasterObject";

    /// <summary>Object name for camera position control</summary>
    static public string CameraMasterName = "CameraMaster";

    /// <summary>Camera shifter controller</summary>
    static public string CameraShifterName = "CameraShifter";

    /// <summary>Model storage name for car models</summary>
    /// Please disable models when you put them in model bank.
    static public string ModelBankName = "ModelBank";

    /// <summary>Model names to register the models for car.</summary>
    /// Please modify this to set a model mesh names.
    static public Dictionary<int, string> GetModelDictionary()
    {
        return ModelBank;
    }
    /// <summary>Separator of the name of model instance for each car</summary>
    /// Each model instance for each car is named as "[ModelName]@Car##".
    /// This separator can be modified. but no necessary.
    static public string CarMeshNameSeparater = "@";

    /// <summary>Names of car instances to control </summary>
    /// Car## is default, but you can change it. 
    /// Usually you dont have to modify them.
    /// <param name="car_id">Car ID.</param>
    /// <returns>Name of instance of car##</returns>
    static public string GetCarInstanceName(int car_id)
    {
        return "Car" + car_id.ToString("00");
    }

    /// <summary>Coodinate change for received data; x y z</summary>
    static public Vector3 TransformCoordinateXYZ( float org_x, float org_y, float org_z) {
        return new Vector3(org_x, org_z, org_y);
    }

    static float Rad2Deg(float rad) { return rad * 180.0f / Mathf.PI; }

    /// <summary>Coodinate change for received data; yaw pitch roll</summary>
    static public Quaternion TransformCoordinateYPR(float org_yaw, float org_pitch, float org_roll)
    {
        return Quaternion.Euler(
                        Rad2Deg(org_roll),
                        -Rad2Deg(org_yaw),
                        Rad2Deg(org_pitch)
                    );
        // yaw rotation is negative, because 
        // CCW from top view is positive in DS, 
        // but CW is positibe in Unity;
    }

    #endregion /// Minor setting, not use in usual
}

/// <summary>
/// Parameter loader; modify as you like...
/// </summary>
public class DSParamLoader
{
    /// <summary>Loading function</summary>
    public static void LoadParameterFromXMLFile(string file_name)
    {
        // Load XML;
        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.Load(file_name);

        // load parameters using Xpath 
        XmlNodeList node_texts = xmlDocument.SelectNodes(@"/root/modelbank/model");
        XmlNodeList node_attributes = xmlDocument.SelectNodes(@"/root/modelbank/model/@id");

        // ---- Load model bank settings ----
        Dictionary<int, string> model_bank = new Dictionary<int, string>();

        for( int i=0; i < node_texts.Count; i++ )
        {
            model_bank.Add( int.Parse(node_attributes[i].Value), node_texts[i].InnerText);
        }
        DSParam.ModelBank = model_bank;

        // ---- Load master object names ----
        // Use default;
        //DSParam.ObjectMasterName = xmlDocument.SelectSingleNode(@"/root/objectmaster_name").InnerText;
        //DSParam.CameraMasterName = xmlDocument.SelectSingleNode(@"/root/cameramaster_name").InnerText;
        //DSParam.ModelBankName = xmlDocument.SelectSingleNode(@"/root/modelbank_name").InnerText;

    }
}