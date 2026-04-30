using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereOriginal : MonoBehaviour
{


    float rotate;
    // Start is called before the first frame update
    void Start()
    {
        vec.y = gameObject.transform.localPosition.y;
        rend = gameObject.GetComponent<Renderer>();
        rend.material.EnableKeyword("_EMISSION");
        flare = gameObject.GetComponent<LensFlare>();
    }
    public Vector3 vec;
    public float s;
    public float distance;
    public float anglarVelocity;
    Renderer rend;

    float timer;
    bool isSwitch;
    Color color1 = new Color(255, 0, 0), color2 = new Color(0, 255, 0);
    LensFlare flare;

    // Update is called once per frame
    void Update()
    {

        s = s + anglarVelocity;
        rotate = s;

        Rotate(rotate);

        timer += Time.deltaTime;
        // 0.1秒ごとに点滅
        if (timer > 0.1f)
        {
            if (isSwitch)
            {
                SetColor(color1);
                SetInvisible();
                isSwitch = false;
            }
            else
            {
                SetColor(color1);
                SetVisible();
                isSwitch = true;
            }
            // タイマーリセット
            timer = 0f;
        }
    }

    public void Rotate(float rad)
    {
        vec.x = distance * Mathf.Cos(rotate);
        vec.z = distance * Mathf.Sin(rotate);
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
    }

    public void SetVisible()
    {
        rend.enabled = true;
    }
}
