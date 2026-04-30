using UnityEngine;

public class trialManager_S : MonoBehaviour {

    private TextMesh targetText_s;
    public int count_S;
    public string status_S;
    public string speed_S;

    // Use this for initializationB
    void Start () {
        this.targetText_s = this.GetComponent<TextMesh>();
    }
	
	// Update is called once per frame
	void Update () {
        Count_trial_S();
    }

        //count trial
    void Count_trial_S (){
        if (count_S > 1)
        {
            int count = count_S;
            this.targetText_s.text = "Trial" + count + "\n" + status_S + "\n" + speed_S + "km/h";
        }
        
    }

}

