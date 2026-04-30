using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataStructGetter : MonoBehaviour
{
    public bool isSetCharacterVelocity;

    public DSUnityViz.CAR_UNIT_INFO unit;
    int id;

    NetworkCharacterVelocity vel;

    // Start is called before the first frame update
    void Start()
    {
        id = -1;
        var p = this.gameObject.transform.parent;
        if(p != null)
        {
            string name = p.gameObject.name;
            
            if(name.Length >= 5)
            {
                if(name[0] == 'C' && name[1] == 'a' && name[2] == 'r')
                {
                    id = int.Parse(name.Substring(3));
                }
            }
        }

        if (isSetCharacterVelocity)
        {
            vel = this.GetComponent<NetworkCharacterVelocity>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(id != -1)
        {
            unit = DSGlobal.current_car_states.car_states[id];
        }

        if (isSetCharacterVelocity)
        {
            vel.velocity.x = unit.accelpedal;
            vel.velocity.y = unit.brakepedal;
            vel.velocity.z = unit.speed;

            this.transform.localPosition = Vector3.zero;
        }
    }
}
