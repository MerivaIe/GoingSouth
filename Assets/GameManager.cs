using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GameManager : MonoBehaviour {
	public const float minutesPerSecond = 0.5f;
	public const int dayStartInMinutes = 180;
	public static Platform[] platforms;
	public static List<Destination> destinations;
	public static List<TimetableItem> timetable;
	public static List<Color> defaultColors;

	private static List<Train> trainPool;
	private Text clockText;

	void Awake () {
		clockText = GameObject.Find ("ClockText").GetComponent <Text> ();

		if (platforms != null || trainPool!= null || destinations!= null) {
			Debug.LogWarning ("Another GameManager has already assigned to static variables. There should only be one GameManager in the scene.");
		} else {
			defaultColors = new List<Color> ();
			defaultColors.Add (Color.blue);
			defaultColors.Add (Color.cyan);
			defaultColors.Add (Color.green);
			defaultColors.Add (Color.magenta);
			defaultColors.Add (Color.red);
			defaultColors.Add (Color.yellow);

			trainPool = GameObject.FindObjectsOfType<Train> ().ToList ();	//this would eventually be Instantiating trains at level load based on user decisions
			platforms = GameObject.FindObjectsOfType <Platform> ();

			destinations = new List<Destination> ();
			destinations.Add (new Destination ("Bristow", 5f, 6));
			destinations.Add (new Destination ("Lomdom", 10f, 8));
			destinations.Add (new Destination ("Basimgstoke", 50f, 1000));

			timetable = new List<TimetableItem>();
			GenerateTimetable (destinations);
		}
	}

	void Update() {
		float timeToDisplay = dayStartInMinutes + minutesPerSecond * Time.time;
		clockText.text = string.Format("{0:#00}:{1:00}", Mathf.Floor(timeToDisplay/60),Mathf.Floor(timeToDisplay) % 60);
	}

	static void GenerateTimetable(List<Destination> destinations) {	//decided by player: for each destination, avg time between trains
		foreach (Destination destination in destinations) {
			//do loop to add a train every x mins
			timetable.Add (new TimetableItem(destination,dayStartInMinutes + minutesPerSecond * 50f,platforms[0],trainPool[0]));
			timetable.Add (new TimetableItem(0f,platforms[1],GameObject.Find ("Train (2)").GetComponent <Train>(),"Bristol"));
			timetable.Add (new TimetableItem(0f,platforms[2],GameObject.Find ("Train (3)").GetComponent <Train>(),"Bristol"));
			timetable.Add (new TimetableItem(0f,platforms[3],GameObject.Find ("Train (4)").GetComponent <Train>(),"Bristol"));
			//timetable.Add (new TimetableItem(0f,GameObject.Find("PlatformTest").GetComponent <Platform>(),GameObject.Find ("Complex Train (1)").GetComponent <Train>(),"Bristol"));
		}
	}

	public static Train GetNextTrain(Platform platform) {
		Train train = timetable.FirstOrDefault (a => a.platform == platform).train;
		if (!train) {
			Debug.LogWarning("Couldn't find train for the requested platform.");
		}
		return train;
	}

	static void AssignTrainToNewTimetableItem(Train train,Destination destination) {	//TODO call on train once first assigned or reassigned to destination
		train.SetTrainColor (destination.color);
	}
}
