using UnityEngine;
using System.Collections;
using System.Linq;

public class CarSoundManager : MonoBehaviour {

	public AudioClip engine_sound;
	public AudioClip road_sound;

	private AudioSource src_engine = null;
	private AudioSource src_roadnoise = null;

	public int AsignedCarID;


	// Use this for initialization
	void Awake () {
		GameObject target = gameObject;
                            src_roadnoise = target.AddComponent<AudioSource>();
		if (src_engine == null)
		{
            AudioSource[] list;
            try
            {
                list = target.GetComponents<AudioSource>().Where(o => o.clip.name == engine_sound.name).ToArray();

            }
            catch
            {
                return;
            }
            if (list.Count() == 0)
			{

				src_engine = target.AddComponent<AudioSource>();
				src_engine.clip = engine_sound;
				src_engine.loop = enabled;
				src_engine.maxDistance = 50;
				src_engine.minDistance = 1;
                src_engine.spatialBlend = 1;
			}
		}

		if (src_roadnoise == null)
		{
            src_roadnoise = target.AddComponent<AudioSource>();
            try
            {
                var list = target.GetComponents<AudioSource>().Where(o => o.clip.name == road_sound.name).ToArray();


                if ( list.Count() == 0)
                {

                    //src_roadnoise.name = "Road noise";
                    src_roadnoise.clip = road_sound;
                    src_roadnoise.loop = enabled;
                    src_roadnoise.maxDistance = 50;
                    src_roadnoise.minDistance = 1;
                    src_roadnoise.spatialBlend = 1;
                }
            }
            catch
            {
                return;
            }
        }

		src_engine.Play();
		src_roadnoise.Play();








	}
	
	// Update is called once per frame
	void Update () {

		float engine_pitch = 0.8f;
		float engine_vol = 1.0f;

		float road_pitch = 0.8f;
		float road_vol = 1.0f;

		// engine sound
		int trg_car = AsignedCarID;
		float rpm;
		float speed;
		
		trg_car = DSGlobal.current_car_states.camera_info[0].camera_attached_car_id;
		try {
			rpm = DSGlobal.current_car_states.car_states[trg_car].eng_rpm;
			speed = DSGlobal.current_car_states.car_states[trg_car].speed;
		}
		catch
		{
			rpm = 600;
			speed = 1;
		}

		if (rpm < 60) rpm = 60;
		if (rpm > 10000) rpm = 10000;
		engine_vol = 0.2f * (rpm / 10000.0f) + 0.8f;
		engine_pitch = 0.7f * ((rpm - 5000.0f) / 10000.0f) + 1.0f;

		if (speed < 1) speed = 1;
		if (speed > 40) speed = 40;
		
		src_engine.pitch = engine_pitch;
		src_engine.volume = engine_vol;

		src_roadnoise.pitch = road_pitch;
		src_roadnoise.volume = road_vol;

	}
}
