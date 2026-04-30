using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;


public class startpoint_control : MonoBehaviour {
    TextAsset csvfiles;
    private List<string[]> csvDatas = new List<string[]>();
    private int row;
    GameObject masterobj;
    GameObject PEDE;
    trialManager_P Pede;
    public GameObject text_start;
    public GameObject text_skip;
    TextAsset csvfiles_start;
    private List<string[]> csvDatas_start = new List<string[]>();

    // Use this for initialization
    void Start () {
        //start point
        GameObject masterobj = GameObject.Find("MasterObject").gameObject;

        //Pede
        PEDE = GameObject.Find("Pedestrian Trial Counter");
        Pede = PEDE.GetComponent<trialManager_P>();

        //csv
        csvfiles = Resources.Load("kuroda_pedestrian") as TextAsset;
        StringReader reader = new StringReader(csvfiles.text);

        while(reader.Peek() != -1)
        {
            string line = reader.ReadLine();
            csvDatas.Add(line.Split(','));
        }

        csvfiles_start = Resources.Load("pedestrian_start") as TextAsset;
        StringReader reader_start = new StringReader(csvfiles_start.text);
        Pede = PEDE.GetComponent<trialManager_P>();

        while (reader_start.Peek() != -1)
        {
            string line = reader_start.ReadLine();
            csvDatas_start.Add(line.Split(','));
        }
        Vector3 pos = GameObject.Find("startpoint").transform.position;
        float delta = float.Parse(csvDatas[0][0]);
        GameObject.Find("startpoint").transform.position = new Vector3(delta, pos.y, pos.z);
        Pede.status_P = csvDatas[0][1];
        row = 0;
    }
	
	// Update is called once per frame
	void Update (){
        Debug.Log(row);
        //skip process
        if (Pede.status_P == "スキップ")
        {
            text_skip.SetActive(true);
            text_start.SetActive(false);
        }
        else
        {
            text_skip.SetActive(false);
            //start sign manager
            try
            {
                if (GameObject.Find("Car07").transform.position.z > float.Parse(csvDatas_start[row][0]) && GameObject.Find("Car07").transform.position.z < 250)
                {
                    text_start.SetActive(true);
                }
                else
                {
                    text_start.SetActive(false);
                }
            }
            catch
            {
                text_start.SetActive(false);
            }
        }

        //trial manager
        if (row == 0)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                row++;
                Vector3 pos = GameObject.Find("startpoint").transform.position;
                float delta = float.Parse(csvDatas[row][0]);
                GameObject.Find("startpoint").transform.position = new Vector3(delta, pos.y, pos.z);
                Pede.status_P = csvDatas[row][1];
                Pede.count_P = row;
            }
                
        }
        else if(row == csvDatas.Count() - 1)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                row--;
                Vector3 pos = GameObject.Find("startpoint").transform.position;
                float delta = float.Parse(csvDatas[row][0]);
                GameObject.Find("startpoint").transform.position = new Vector3(delta, pos.y, pos.z);
                Pede.status_P = csvDatas[row][1];
                Pede.count_P = row;
            }
        }
        else //center
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                row++;
                Vector3 pos = GameObject.Find("startpoint").transform.position;
                float delta = float.Parse(csvDatas[row][0]);
                GameObject.Find("startpoint").transform.position = new Vector3(delta, pos.y, pos.z);
                Pede.status_P = csvDatas[row][1];
                Pede.count_P = row;
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                row--;
                Vector3 pos = GameObject.Find("startpoint").transform.position;
                float delta = float.Parse(csvDatas[row][0]);
                GameObject.Find("startpoint").transform.position = new Vector3(delta, pos.y, pos.z);
                Pede.status_P = csvDatas[row][1];
                Pede.count_P = row;
            }
        }
    }
}
