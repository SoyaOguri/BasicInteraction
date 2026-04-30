using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class trialManager_R : MonoBehaviour {

    private TextMesh targetText_r;
    public string status_R;
    GameObject CAR_S;
    trialManager_S carS;

    // Use this for initializationB
    void Start()
    {
        //carS
        CAR_S = GameObject.Find("CarS Trial Counter");
        carS = CAR_S.GetComponent<trialManager_S>();

        this.targetText_r = this.GetComponent<TextMesh>();
    }

    // Update is called once per frame
    void Update()
    {
        Count_trial_R();
    }

    //count trial
    void Count_trial_R()
    {
        if(carS.count_S > 1)
        {
            int count = carS.count_S;
            this.targetText_r.text = "Trial" + count + "\n" + status_R;
        }
        
    }
}
