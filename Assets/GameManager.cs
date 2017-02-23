using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GameManager : MonoBehaviour {

	public GameObject trainPrefab;
	public const float minutesPerSecond = 0.5f;
	public const int dayStartInMinutes = 180;

	private static List<TimetableItem> timetable = new List<TimetableItem>();
	private static List<Train> trainPool = new List<Train>();
	private Text clockText;

	void Awake () {
		clockText = GameObject.Find ("ClockText").GetComponent <Text> ();
		trainPool = GameObject.FindObjectsOfType<Train>().ToList ();	//this would eventually be Instantiating trains at level load based on user decisions
		List<Destination> destinations = new List<Destination>();
		destinations.Add (new Destination("Bristol",5f,6));
		GenerateTimetable (destinations);
	}

	void Update() {
		float timeToDisplay = minutesPerSecond * (dayStartInMinutes + Time.time);
		clockText.text = string.Format("{0:#00}:{1:00}", Mathf.Floor(timeToDisplay/60),Mathf.Floor(timeToDisplay) % 60);
	}

	public static void GenerateTimetable(List<Destination> destinations) {	//decided by player: for each destination, avg time between trains
		foreach (Destination destination in destinations) {
			//do loop to add a train every x mins
			timetable.Add (new TimetableItem(0f,GameObject.FindObjectOfType <Platform>(),GameObject.Find ("Complex Train (1)").GetComponent <Train>(),"Bristol"));
		}
	}

	public static Train GetNextTrain(Platform platform) {
		Train train = timetable.FirstOrDefault (a => a.platform == platform).train;
		if (!train) {
			Debug.LogWarning("Couldn't find train for the requested platform.");
		}
		return train;
	}
		
	/// <summary>
	/// ATM: centralised data repository.
	/// </summary>
	public struct TimetableItem {
		public float scheduledArrivalTime;
		public Platform platform;
		public Train train;
		public string destination;
		public TimetableItem(float _schedArrivalTime, Platform _platform, Train _train, string _destination) {
			scheduledArrivalTime = _schedArrivalTime;
			platform = _platform;
			train = _train;
			destination = _destination;
		}
	}

	/// <summary>
	/// Stores info against each destination
	/// </summary>
	public class Destination {
		string destination;
		float avgTimeBetweenTrains;
		float routeTimeInSeconds;
		int noTrainsAssigned;
		int estimatedDailyFootfall;
		public Destination(string _destination, float _avgTimeBetweenTrains, int _noTrainsAssigned) {
			destination = _destination;
			avgTimeBetweenTrains = _avgTimeBetweenTrains;
			noTrainsAssigned = _noTrainsAssigned;
		}
	}
}
