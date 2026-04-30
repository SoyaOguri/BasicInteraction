using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereBlinking : MonoBehaviour
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
    [Header("値を入力")]
    [Space(5), Tooltip("点滅の間隔")] public float freqencyOfLighting;
    [Space(5), Tooltip("回転運動時の初期位置")]public float rad = 0f ;
    [Space(5), Tooltip("回転運動時の角速度")]public float anglarVelocity;
    [Space(5), Tooltip("回転運動時の半径")]public float distance = 0.4f ;
    
    Renderer rend;

    float timer;
    bool isSwitch;
    Color color1 = new Color(255, 0, 0);//赤
    Color color2 = new Color(0, 255, 0);//緑
    Color color3 = new Color(0, 0, 255);//青
    LensFlare flare;

    // Update is called once per frame
    void Update()
    {
        rad = rad + anglarVelocity;
        rotate = rad;
        Rotate(rad);


        timer += Time.deltaTime;
        // 0.1秒ごとに点滅
        if (timer > freqencyOfLighting)
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
