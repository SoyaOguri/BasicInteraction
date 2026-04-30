using UnityEngine;
using System.Collections;

using DSUnityViz;

public interface IDSCarUpdater
{
	// this is called from PositionUpdater
	void UpdateByDSInfo( int car_id, CAR_UNIT_INFO car_info );
}


