using UnityEngine;
using System.Collections;

using System.Linq;

public class pitch : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

		float f = (Time.frameCount % 100) / 100f;
		float p = Mathf.Sin(f * 2 * Mathf.PI) * 0.1f + 1.0f ;

		var audiosrcs = this.gameObject.GetComponents<AudioSource>();

		int i = 0;
		for (; i < audiosrcs.Count(); i++)
		{
			if (audiosrcs[i].clip.name == "engine_ol_base")
			{
				break;
			}
		}

		audiosrcs[i].pitch = p;

	}
}
