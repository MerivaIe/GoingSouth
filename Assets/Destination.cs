using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores info against each destination
/// </summary>
public class Destination {
	public string name;
	float avgTimeBetweenTrains;	//player decided
	float routeLength;
	int noTrainsAssigned;		//player decided
	int estimatedDailyFootfall;
	public Color color{ get; private set; }
	static List<Color> availableColors;

	public Destination(string _name, float _routeLength, int _estimatedDailyFootfall) {
		name = _name;
		routeLength = _routeLength;	//in km
		estimatedDailyFootfall = _estimatedDailyFootfall;

		int nocolorsAvailable = availableColors.Count;

		if (nocolorsAvailable == 0) {	//this will be called on the first instantiation to set up the static list, and will be called if we run out of colors (undesirable as we are just reusing old ones then)
			availableColors = GameManager.defaultColors;
			nocolorsAvailable = availableColors.Count;
		}

		if (nocolorsAvailable == 1) {
			Debug.LogWarning ("About to use the last available color for Destinations. Will start using old colors for any further destinations.");
		}

		int randIndex = Random.Range (0, nocolorsAvailable);
		color = availableColors [randIndex];
		availableColors.RemoveAt (randIndex);
	}

//	Color RandomColor() {	//may be required
//		return new Color(Random.value, Random.value, Random.value);
//	}
}
