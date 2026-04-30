using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;

public class ObjectManager
{

    // max count?
    //const int CarNamesCount = 30;
    //string[] CarNames; //CarNames = { "Car01", "Car02" };

    public MasterObject parent;

    // set model bank object (object used as a model directory)
    public GameObject model_bank;

    // managed game objects
    public Dictionary<int, GameObject> cars = new Dictionary<int, GameObject>();
    public Dictionary<int, int> cars_meshid = new Dictionary<int, int>();
    public Dictionary<int, Vector3> cars_pos = new Dictionary<int, Vector3>();
    public Dictionary<int, Quaternion> cars_rot = new Dictionary<int, Quaternion>();
    public Dictionary<int, int> cars_winkers = new Dictionary<int, int>();

    // stored meshes, (model bank)
    public Dictionary<int, string> meshes = new Dictionary<int, string>();

    // Constructor needs parent MonoBehavior
    private ObjectManager() { }

    public delegate void SwitchGraphicsDelegate(ObjectManager obj, int car_id, int mesh_id);
    public SwitchGraphicsDelegate switchGraphicsCallback;


    // Constructor
    public ObjectManager(MasterObject _parent, DSUnityViz.UpdaterOption option)
    {
        parent = _parent;
        model_bank = _parent.ModelBank;
    }

    /// <summary>
    /// Set car position
    /// </summary>
    /// <param name="car_id">target car id</param>
    /// <param name="car_pos">position</param>
    /// <param name="car_rot">orientation</param>
    public void SetCarPosition(int car_id, Vector3 car_pos, Quaternion car_rot)
    {
        // just set the info
        cars_pos[car_id] = car_pos;
        cars_rot[car_id] = car_rot;

        CheckCar(car_id);

        cars[car_id].transform.position = cars_pos[car_id];
        cars[car_id].transform.rotation = cars_rot[car_id];

    }

    /// <summary>
    /// Checks the car.
    /// </summary>
    /// <param name="car_id">Car identifier.</param>
    public void CheckCar(int car_id)
    {
        EnsureCarExistance(car_id);
    }

    /// <summary>
    /// Change graphics
    /// </summary>
    /// <param name="car_id"></param>
    /// <param name="mesh_id"></param>
    public void SetCarMesh(int car_id, int mesh_id)
    {
        // if current mesh is directed, just return.
        if (cars_meshid.ContainsKey(car_id) && cars_meshid[car_id] == mesh_id) return;

        // if mesh must be switched...
        // set mesh id
        SwitchGraphics(car_id, mesh_id);
    }

    /// <summary>
    /// Switch mesh associated with car to new mesh
    /// </summary>
    /// <param name="car_id">Car ID</param>
    /// <param name="mesh_id">Graphics ID. 
    /// 0 is to invisible , -1 is to destroy</param>
    public void SwitchGraphics(int car_id, int mesh_id)
    {
        if (mesh_id == -1)
        {
            // Destroy car
            DestroyCar(car_id);
            return;
        }

        // Make all models attached target car invisible
        EnsureCarExistance(car_id);
        SetCarInvisible(car_id, false);

        cars_meshid[car_id] = mesh_id;

        //GameObject displaying_mesh = FindOrCreateSubModel(car_id, mesh_id);

        // Enable drawing
        SetMeshVisible(car_id, mesh_id);

        switchGraphicsCallback?.Invoke(this, car_id, mesh_id);
    }

    /// <summary>
    /// Set the car visibility by set all children's activity 
    /// </summary>
    /// <param name="car_id"></param>
    /// <param name="visibility"></param>
    public void SetCarInvisible(int car_id, bool visibility)
    {
        // Hide all children in cars;  not recommended; 
        //for (int i=0; i < cars[car_id].transform.childCount; i ++)
        //{
        //    var x = cars[car_id].transform.GetChild(i).gameObject;
        //    if( x.name != DSParam.CameraMasterName )
        //        x.SetActive(visibility);
        //}

        foreach (var mesh_to_hide in meshes)
        {
            string name = GetMeshNameInCar(car_id, mesh_to_hide.Key);
            var target = cars[car_id].transform.Find(name);
            if (target != null)
            {
                target.gameObject.SetActive(visibility);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="car_id"></param>
    /// <param name="mesh_id"></param>
    /// <param name="visibility"></param>
    private void SetMeshVisible(int car_id, int mesh_id, bool active = true)
    {
        GameObject target_obj = FindOrCreateSubModel(car_id, mesh_id);
        if (target_obj != null)
        {
            target_obj.SetActive(active);
        }
    }

    /// <summary>
    /// Set object visibility. Do not use to Parent Object.
    /// </summary>
    /// <param name="trg"></param>
    /// <param name="visibility"></param>
    private void SetGameobjectVisible(GameObject trg, bool visibility)
    {
        // using activation
        trg.SetActive(visibility);

        // using renderer ability.
        // you can not use this if you enable renderer partially. 
        //var renderers = trg.GetComponentsInChildren<MeshRenderer>();
        //foreach( var x in renderers)
        //{
        //    x.enabled = visibility;
        //}

    }

    /// <summary>
    /// Check the submesh is loaded to each car or 
    /// Create submesh instance.
    /// </summary>
    /// <param name="car_id"></param>
    /// <param name="mesh_id"></param>
    /// <returns></returns>
    public GameObject FindOrCreateSubModel(int car_id, int mesh_id)
    {
        string model_name = GetMeshNameInCar(car_id, mesh_id);

        Transform trg = cars[car_id].transform.Find(model_name);
        GameObject target_obj;

        if (trg == null)
        {
            // if car is not created, find the model in ModelBank and instantiate it
            Transform child = null;
            if (meshes.ContainsKey(mesh_id))
            {
                child = model_bank.transform.Find(meshes[mesh_id]);
            }

            if (child != null)
            {
                GameObject original;
                original = child.gameObject;

                // The car has no targeting mesh 
                target_obj = (GameObject)GameObject.Instantiate(
                    original,
                    new Vector3(0, 0, 0), //cars_pos[car_id],
                    Quaternion.identity// cars_rot[car_id]
                );
                target_obj.name = model_name;
                target_obj.SetActive(false);
            }
            else
            {
                target_obj = new GameObject();
                target_obj.name = "InvalidMesh";
                target_obj.SetActive(false);
            }
        }
        else
        {
            target_obj = trg.gameObject;
        }

        // set created mesh as a child of car.
        //target_obj.transform.parent = cars[car_id].transform;
        target_obj.transform.SetParent(cars[car_id].transform, false);

        return target_obj;
    }

    /// <summary>
    /// Create empty car object
    /// </summary>
    /// <param name="car_id"></param>
    public void EnsureCarExistance(int car_id)
    {

        if (!cars.ContainsKey(car_id))
        {
            string car_name = GetCarName(car_id);

            // find car object
            var found = GameObject.Find(car_name);
            if (found == null)
            {
                found = new GameObject(car_name);
            }
            cars[car_id] = found;
            cars_meshid[car_id] = -1;
            cars_pos[car_id] = new Vector3();
            cars_rot[car_id] = Quaternion.identity;
        }
    }

    /// <summary>
    /// Destroy car
    /// </summary>
    /// <param name="car_id">target</param>
    public void DestroyCar(int car_id)
    {
        // if car is not instantiated, keep it.
        // if car is instantiated, delete.
        if (cars.ContainsKey(car_id))
        {
            if (cars[car_id] != null)
            {
                cars[car_id].name = "TobeDestroyed" + cars[car_id].name;
                Object.Destroy(cars[car_id]);
            }
            cars.Remove(car_id);
        }

        // clear mesh info too
        if (cars_meshid.ContainsKey(car_id))
        {
            cars_meshid.Remove(car_id);
        }
    }

    /// <summary>
    /// Reset cars, clear all cars.
    /// </summary>
    public void ResetCars()
    {
        // Destroy all cars
        foreach (var x in cars)
        {
            Object.Destroy(x.Value);
        }

        // unregister them
        cars.Clear();
        cars_meshid.Clear();
        cars_pos.Clear();
        cars_rot.Clear();
    }

    /// <summary>
    /// Reset meshes registered.
    /// </summary>
    public void ResetMeshes()
    {
        meshes.Clear();
    }

    /// -------------------------------------------
    #region // Mesh registration
    /// <summary>
    /// Set mesh names by dictionary
    /// </summary>
    /// <param name="_meshes"></param>
    public void SetMeshNames(Dictionary<int, string> _meshes)
    {
        meshes = new Dictionary<int, string>(_meshes);
    }

    /// <summary>
    /// Set mesh names by string array
    /// </summary>
    /// <param name="names"></param>
    public void SetMeshNames(IEnumerable<string> names)
    {
        //List<string> tmp = new List<string>();

        int id = 0;
        foreach (var x in names)
        {
            meshes.Add(id++, x);
        }
    }

    /// <summary>
    /// Add mesh registration. 
    /// </summary>
    public void SetMeshName(int mesh_id, string meshname)
    {
        meshes[mesh_id] = meshname;
    }

    /// <summary>
    /// Delete mesh registration.
    /// </summary>
    /// <param name="mesh_id"></param>
    public void ResetMeshName(int mesh_id)
    {
        if (meshes.ContainsKey(mesh_id))
            meshes.Remove(mesh_id);
    }

    #endregion // Mesh registration

    /// -------------------------------------------
    #region //Private utility function section

    /// <summary>
    /// CreateCarName 
    /// </summary>
    /// <param name="car_id"></param>
    /// <returns></returns>
    private string GetCarName(int car_id)
    {
        return DSParam.GetCarInstanceName(car_id);
    }

    /// <summary>
    /// Create Mesh name in car
    /// </summary>
    /// <param name="car_id"></param>
    /// <param name="mesh_id"></param>
    /// <returns></returns>
    private string GetMeshNameInCar(int car_id, int mesh_id)
    {
        string ret = "Invalid@" + GetCarName(car_id);

        try
        {
            ret = meshes[mesh_id] + DSParam.CarMeshNameSeparater + GetCarName(car_id);
        }
        catch
        {
            Debug.Log(mesh_id);
        }

        return ret;
    }
    #endregion //Private utility function section

}

