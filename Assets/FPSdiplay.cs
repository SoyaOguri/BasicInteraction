using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSdiplay : MonoBehaviour
{

    public int targetFPS;
    // Start is called before the first frame update
    void Awake()
    {
        Application.targetFrameRate = targetFPS;
    }

    // Update is called once per frame
    void Update()
    {
        float fps = 1f / Time.deltaTime;
        Debug.Log(fps);
    }
}
