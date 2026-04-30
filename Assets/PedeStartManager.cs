using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PedeStartManager : MonoBehaviour {

    public GameObject text;
    TextAsset csvfiles_start;
    private List<string[]> csvDatas_start = new List<string[]>();
    GameObject PEDE;
    private int row = 0;
    trialManager_P Pede;

    // Use this for initialization
    void Start () {
        //csv
        csvfiles_start = Resources.Load("pedestrian_start") as TextAsset;
        StringReader reader_start = new StringReader(csvfiles_start.text);
        Pede = PEDE.GetComponent<trialManager_P>();

        while (reader_start.Peek() != -1)
        {
            string line = reader_start.ReadLine();
            csvDatas_start.Add(line.Split(','));
        }
    }
	
	// Update is called once per frame
	void Update () {

        //if (Input.GetKeyDown(KeyCode.RightArrow))
        //{
        //    row++;
        //}
        //else if (Input.GetKeyDown(KeyCode.LeftArrow))
        //{
        //    row--;
        //}

        Debug.Log("row is " + row);

        if (GameObject.Find("Car07").transform.position.z > -float.Parse(csvDatas_start[row][0]) && GameObject.Find("Car07").transform.position.z < 130)
        {
            text.SetActive(true);
        }

        else
        {
            text.SetActive(false);
        }
    }
}
