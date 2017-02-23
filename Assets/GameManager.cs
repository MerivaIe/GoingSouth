using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GameManager : MonoBehaviour {
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
		destinations.Add (new Destination("Edinburgh",10f,8));
		destinations.Add (new Destination("Basingstoke",50f,1000));

		GenerateTimetable (destinations);
	}

	void Update() {
		float timeToDisplay = minutesPerSecond * (dayStartInMinutes + Time.time);
		clockText.text = string.Format("{0:#00}:{1:00}", Mathf.Floor(timeToDisplay/60),Mathf.Floor(timeToDisplay) % 60);
	}

	public static void GenerateTimetable(List<Destination> destinations) {	//decided by player: for each destination, avg time between trains
		foreach (Destination destination in destinations) {
			//do loop to add a train every x mins
			timetable.Add (new TimetableItem(0f,GameObject.Find("Platform (1)").GetComponent <Platform>(),GameObject.Find ("Train (1)").GetComponent <Train>(),"Bristol"));
			timetable.Add (new TimetableItem(0f,GameObject.Find("Platform (2)").GetComponent <Platform>(),GameObject.Find ("Train (2)").GetComponent <Train>(),"Bristol"));
			timetable.Add (new TimetableItem(0f,GameObject.Find("Platform (3)").GetComponent <Platform>(),GameObject.Find ("Train (3)").GetComponent <Train>(),"Bristol"));
			timetable.Add (new TimetableItem(0f,GameObject.Find("Platform (4)").GetComponent <Platform>(),GameObject.Find ("Train (4)").GetComponent <Train>(),"Bristol"));
		}
	}

	public static Train GetNextTrain(Platform platform) {
		Train train = timetable.FirstOrDefault (a => a.platform == platform).train;
		if (!train) {
			Debug.LogWarning("Couldn't find train for the requested platform.");
		}
		return train;
	}
}
