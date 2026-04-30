using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarBarkeWinker : MonoBehaviour
{
    public int carID;
    public GameObject brake_normal,brake_ON,winkerR_normal,winkerR_ON,winkerL_normal,winkerL_ON;
    double time;
    int intTime;

    bool isUsedBrake, isUsedWinkerR, isUsedWinkerL,isLight;
    DSUnityViz.CAR_UNIT_INFO carU;

    enum WinkerState
    {
        OFF = 0,
        RIGHT = 1,
        LEFT = 2,
    }

    // Start is called before the first frame update
    void Start()
    {
        time = 0;
        isUsedBrake = false;
        isUsedWinkerL = false;
        isUsedWinkerR = false;
    }

    // Update is called once per frame
    void Update()
    {
        //状態量取得処理
        carU = DSGlobal.current_car_states.car_states[carID];

        if(carU.winkers == (int)WinkerState.RIGHT)
        {
            isUsedWinkerR = true;
            isUsedWinkerL = false;
        }
        else if(carU.winkers == (int)WinkerState.LEFT)
        {
            isUsedWinkerR = false;
            isUsedWinkerL = true;
        }
        else
        {
            isUsedWinkerR = false;
            isUsedWinkerL = false;
        }

        if(carU.brakepedal > 0.1)
        {
            isUsedBrake = true;
        }
        else
        {
            isUsedBrake = false;
        }

        //点滅処理
        intTime = (int)(time/0.5);
        if (intTime % 2 == 0)
            isLight = true;
        else
            isLight = false;

        if (isUsedBrake)
        {
            brake_normal.SetActive(false);
            brake_ON.SetActive(true);
        }
        else
        {
            brake_normal.SetActive(true);
            brake_ON.SetActive(false);
        }

        if (isUsedWinkerL)
        {
            if (isLight)
            {
                winkerL_normal.SetActive(false);
                winkerL_ON.SetActive(true);
            }
            else
            {
                winkerL_normal.SetActive(true);
                winkerL_ON.SetActive(false);
            }
        }
        else
        {
            winkerL_normal.SetActive(true);
            winkerL_ON.SetActive(false);
        }


        if (isUsedWinkerR)
        {
            if (isLight)
            {
                winkerR_normal.SetActive(false);
                winkerR_ON.SetActive(true);
            }
            else
            {
                winkerR_normal.SetActive(true);
                winkerR_ON.SetActive(false);
            }
        }
        else
        {
            winkerR_normal.SetActive(true);
            winkerR_ON.SetActive(false);
        }

        time += Time.deltaTime;
    }
}
