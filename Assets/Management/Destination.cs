using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores info against each destination
/// </summary>
public class Destination {
	public Material materialColor{ get; private set; }
	public string name{ get; private set; }
	public int routeLength { get; private set; }
	public TimetableItem soonestTimetableItem { get; private set; }	//earliest timetable item available to this destination (that is ready to go i.e.  has a platform assignment?)
	public float meanSpawnDelay {get; private set;}

	int estimatedDailyFootfall;
	static List<Material> availableMaterialColors = new List<Material>();

	public Destination(string _name, int _routeLength, int _estimatedDailyFootfall) {
		name = _name;
		routeLength = _routeLength;	//in km
		estimatedDailyFootfall = _estimatedDailyFootfall;

		meanSpawnDelay = (GameManager.dayDurationInGameMinutes / GameManager.gameMinutesPerRealSecond) / estimatedDailyFootfall;	//==(day duration in real seconds)/no. of people to spawn

		int noColorsAvailable = availableMaterialColors.Count;

		if (noColorsAvailable == 0) {	//this will be called on the first instantiation to set up the static list, and will be called if we run out of colors (undesirable as we are just reusing old ones then)
			availableMaterialColors = GameManager.instance.defaultMaterialColors;
			noColorsAvailable = availableMaterialColors.Count;
		}

		if (noColorsAvailable == 1) {
			Debug.LogWarning ("About to use the last available color for Destinations. Will start using old colors for any further destinations.");
		}

		int randIndex = Random.Range (0, noColorsAvailable);
		materialColor = availableMaterialColors [randIndex];
		availableMaterialColors.RemoveAt (randIndex);
	}

	public void SetSoonestTimetableItem(TimetableItem timetableItem) {
		soonestTimetableItem = timetableItem;
	}

//	Color RandomColor() {	//may be required
//		return new Color(Random.value, Random.value, Random.value);
//	}
}
