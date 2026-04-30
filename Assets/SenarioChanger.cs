using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SenarioChanger : MonoBehaviour
{
    List<List<double>> csvList;

    public Canvas canvas;
    public int currentSenarioNumber;
    public int numberOfPedestrians;
    public int pmStartPos;

    double textCount;
    const double textDisplayTime = 4; 
    double keyCount;
    const double keyDownTime = 1;

    public GameObject goalObject, PMObject;
    float areaTop, areaBottom, areaRight, areaLeft;

    PedestrianCreater pc;
    DataRecorder dr;
    public GameObject dataRecorderObject;

    // Start is called before the first frame update
    void Start()
    {
        currentSenarioNumber = 1;
        textCount = textDisplayTime;
        keyCount = 0;

        pc = GetComponent<PedestrianCreater>();

        dr = dataRecorderObject.GetComponent<DataRecorder>();

        //Get walking area
        areaTop = pc.area.transform.position.z + pc.area.transform.localScale.z / 2;
        areaBottom = pc.area.transform.position.z - pc.area.transform.localScale.z / 2;
        areaRight = pc.area.transform.position.x + pc.area.transform.localScale.x / 2;
        areaLeft = pc.area.transform.position.x - pc.area.transform.localScale.x / 2;
        System.IO.StreamReader sr;
        try
        {
            sr = new System.IO.StreamReader(Application.dataPath + @"\Senario.csv");
        }
        catch
        {
            EnableCanvas("開けません："+ Application.dataPath);
            return;
        }
        string str = sr.ReadLine();
        string[] strs;
        csvList = new List<List<double>>();

        int count = 0;
        while (!sr.EndOfStream)
        {
            csvList.Add(new List<double>());
            str = sr.ReadLine();
            strs = str.Split(',');
            foreach (var item in strs)
            {
                csvList[count].Add(double.Parse(item));
            }
            count++;
        }
        currentSenarioNumber = 0;
        StartSenario(currentSenarioNumber);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            keyCount += Time.deltaTime;
            if (keyCount >= keyDownTime)
            {
                keyCount = 0;
                currentSenarioNumber--;
                StartSenario(currentSenarioNumber);
                textCount = textDisplayTime;
            }
        }
        else if (Input.GetKey(KeyCode.L))
        {
            keyCount += Time.deltaTime;
            if (keyCount >= keyDownTime)
            {
                keyCount = 0;
                currentSenarioNumber++;
                StartSenario(currentSenarioNumber);
                textCount = textDisplayTime;
            }
        }else
        {
            keyCount = 0;
        }


        textCount -= Time.deltaTime;
        if(textCount <= 0)
        {
            textCount = 0;
            DisableCanvas();
        }
    }

    void StartSenario(int count)
    {
        if (count < 0)
        {
            count = 0;
            currentSenarioNumber = count;
        }
        else if (count > csvList.Count)
        {
            count = csvList.Count ;
            currentSenarioNumber = count;
        }

        EnableCanvas(count);

        int numPed;
        int startPos;

        if (count == 0)
        {
            numPed = 0;
            startPos = 0;
        }
        else
        {
            numPed = (int)csvList[count - 1][1];
            startPos = (int)csvList[count - 1][2];
        }

        numberOfPedestrians = numPed;
        pmStartPos = startPos;

        //Arrange PM
        if (startPos == 0)
        {
            goalObject.transform.position = new Vector3(0, -10, 0);
            PMObject.transform.position = new Vector3(24, 0, -68);
            PMObject.transform.eulerAngles = new Vector3(0, -90, 0);
        }else if(startPos == 1)
        {
            goalObject.transform.position = new Vector3(areaRight - 3.5f, 1, areaTop -3.5f);
            PMObject.transform.position = new Vector3(areaLeft + 3.5f, 0, areaBottom +3.5f);
            PMObject.transform.eulerAngles = new Vector3(0, -45, 0);
        }
        else if (startPos == 2)
        {
            goalObject.transform.position = new Vector3(areaLeft + 3.5f, 1, areaTop - 3.5f);
            PMObject.transform.position = new Vector3(areaRight - 3.5f, 0, areaBottom + 3.5f);
            PMObject.transform.eulerAngles = new Vector3(0, -135, 0);
        }
        else if (startPos == 3)
        {
            goalObject.transform.position = new Vector3(areaLeft + 3.5f, 1, areaBottom + 3.5f);
            PMObject.transform.position = new Vector3(areaRight - 3.5f, 0, areaTop - 3.5f);
            PMObject.transform.eulerAngles = new Vector3(0, 135, 0);
        }
        else if (startPos == 4)
        {
            goalObject.transform.position = new Vector3(areaRight - 3.5f, 1, areaBottom + 3.5f);
            PMObject.transform.position = new Vector3(areaLeft + 3.5f, 0, areaTop - 3.5f);
            PMObject.transform.eulerAngles = new Vector3(0, 45, 0);
        }

        for (int i = 0; i < pc.pedestrianList.Count; i++)
        {
            if(i < numPed)
            {
                pc.SetPedestrianPose(i);
            }else
            {
                pc.DisablePedestrianPose(i);
            }
        }

        dr.CloseFile();
        dr.OpenFile();
    }


    void EnableCanvas(int count)
    {
        var text = canvas.gameObject.GetComponentInChildren<UnityEngine.UI.Text>();
        text.text = "試行：" + count;
        canvas.gameObject.SetActive(true);
    }

    void EnableCanvas(string str)
    {
        var text = canvas.gameObject.GetComponentInChildren<UnityEngine.UI.Text>();
        text.text = str;
        textCount = textDisplayTime;
        canvas.gameObject.SetActive(true);
    }

    void DisableCanvas()
    {
        canvas.gameObject.SetActive(false);
    }

}
