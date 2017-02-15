using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

	//TODO I thought I'd need a Tuple here but really should think about what information you need stored together here... do you need a 'global' collection of info or can you retrieve this from individual objects
	private static Dictionary<string,GameObject> departures = new Dictionary<string,GameObject>();
	private static List<Departure> departuresNew = new List<Departure> ();

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

	public struct Departure {
		GameObject train, platform;
		string destination;
		Color color;
		public Departure (GameObject _train, GameObject _platform, string _destination) {
			train = _train;
			platform = _platform;
			destination = _destination;
			color = Color.red;
			//select color randomly from set that haven't already been picked??
		}
	}
}
