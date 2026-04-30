using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereVisible : MonoBehaviour
{


    float rotate;
    // Start is called before the first frame update
    void Start()
    {

        rend = gameObject.GetComponent<Renderer>();
        rend.material.EnableKeyword("_EMISSION");
        flare = gameObject.GetComponent<LensFlare>();
    }

    
    Renderer rend;
    Color color1 = new Color(255, 0, 0), color2 = new Color(0, 255, 0);
    LensFlare flare;

    

    public void SetColor(Color color)
    {
        rend.material.SetColor("_EmissionColor", color);
        flare.color = color;
    }

    public void SetInvisible()
    {
        rend.enabled = false;
    }

    public void SetVisible()
    {
        rend.enabled = true;
    }
}