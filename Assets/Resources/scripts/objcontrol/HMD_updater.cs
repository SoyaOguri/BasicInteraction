using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DSUnityViz
{

    public class HMD_updater : MonoBehaviour
    {
        [System.SerializableAttribute]
        public struct HMDParam
        {
            public int id;
            public GameObject traceTarget;
        }

        public List<HMDParam> HMDAnchors;
        ObjectManager objman;
        //List<int> listIDs = new List<int>();

        GameObject targetObj;

        // Use this for initialization
        void Start()
        {
            var masterObject = GameObject.Find("MasterObject");
            MasterObject mo = masterObject.GetComponent<MasterObject>();
            objman = mo.objman;

            //for (int i = 0; i < HMDAnchors.Count; i++)
            //{
            //    if (HMDAnchors[i].id >= 0)
            //    {
            //        for (; listIDs.Count <= HMDAnchors[i].id;)
            //        {
            //            listIDs.Add(-1);
            //        }
            //        listIDs[HMDAnchors[i].id] = i;
            //    }
            //}
        }
         

        // Update is called once per frame
        void Update()
        {
            if(objman.cars.Count > 0)
            {
                for (int i = 0; i < HMDAnchors.Count; i++)
                {
                    if (HMDAnchors[i].traceTarget != null)
                    {
                        targetObj = objman.cars[HMDAnchors[i].id];
                        HMDAnchors[i].traceTarget.transform.position = targetObj.transform.position;
                        HMDAnchors[i].traceTarget.transform.rotation = targetObj.transform.rotation;
                    }

                }
            }
        }
    }
}