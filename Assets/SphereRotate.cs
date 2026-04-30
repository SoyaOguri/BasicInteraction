using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereRotate : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        vec.y = gameObject.transform.localPosition.y;
        rend = gameObject.GetComponent<Renderer>();
        rend.material.EnableKeyword("_EMISSION");
        flare = gameObject.GetComponent<LensFlare>();
    }

    public float rotate, distance = 0.5f;
    Vector3 vec;
    Renderer rend;

    float timer;
    bool isSwitch;
    Color color1 = new Color(255, 0, 0), color2 = new Color(0, 255, 0), color3 = new Color(0, 0, 255);
    LensFlare flare;

    // Update is called once per frame
    void Update()
    {

        
    }

    public void Rotate(float rad)
    {
        vec.x = distance * Mathf.Cos(rad);
        vec.z = distance * Mathf.Sin(rad);
        rotate = rad;
        gameObject.transform.localPosition = vec;
    }

    public void SetColor(Color color)
    {
        rend.material.SetColor("_EmissionColor", color);
        flare.color = color;
    }

    public void SetInvisible()
    {
        rend.enabled = false;
        flare.enabled = false;
    }

    public void SetVisible()
    {
        rend.enabled = true;
        flare.enabled = true;
    }
}
