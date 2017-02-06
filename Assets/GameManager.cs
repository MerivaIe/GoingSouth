using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

	//TODO I thought I'd need a Tuple here but really should think about what information you need stored together here... do you need a 'global' collection of info or can you retrieve this from individual objects
	private static Dictionary<string,GameObject> departures = new Dictionary<string,GameObject>();

	void Awake () {
		GameObject trainGO = GameObject.Find ("Complex Train (1)");
		departures.Add ("Bristol",trainGO);
	}
		
	public static Train GetDeparture(string destination) {
		GameObject trainGO;
		if (departures.TryGetValue (destination,out trainGO)) {
			return trainGO.GetComponent <Train>();
		}
		Debug.LogWarning("Couldn't find train for the requested departure.");
		return null;
	}
}
