using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class trialManager_P : MonoBehaviour {

    private TextMesh targetText_p;
    public string status_P;
    public int count_P;
    GameObject CAR_S;
    trialManager_S carS;

    // Use this for initializationB
    void Start()
    {
        this.targetText_p = this.GetComponent<TextMesh>();
    }

    // Update is called once per frame
    void Update()
    {
        Count_trial_P();
    }

    //count trial
    void Count_trial_P()
    {
        int count = this.count_P + 1;
        this.targetText_p.text = "Trial" + count + "\n" + status_P;
    }
}
