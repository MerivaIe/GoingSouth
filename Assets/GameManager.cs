using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GameManager : MonoBehaviour {
	public const float minutesPerSecond = 1f;
	public const int dayStartInMinutes = 180;
	public static Platform[] platforms{ get; private set; }
	public static List<Destination> destinations{ get; private set; }
	public static List<TimetableItem> timetable{ get; private set; }
	public static List<Color> defaultColors{ get; private set; }
	public static List<Train> trainPool { get; private set; }

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
			destinations.Add (new Destination ("Bristow", 50f, 6));
			destinations.Add (new Destination ("Lomdom", 10f, 8));
			destinations.Add (new Destination ("Basimgstoke", 5f, 1000));

			timetable = new List<TimetableItem>();
			GenerateTimetable (destinations);
		}
	}

	void Update() {
		//TODO: do this from the display and calculate on demand once every second, alongside journey times and other interface things....
		float timeToDisplay = dayStartInMinutes + minutesPerSecond * Time.time;
		clockText.text = string.Format("{0:#00}:{1:00}", Mathf.Floor(timeToDisplay/60),Mathf.Floor(timeToDisplay) % 60);
	}

	static void GenerateTimetable(List<Destination> destinations) {	//decided by player: for each destination, avg time between trains
		//...but at the moment just some dummy stuff in here for the five trains in scene
		TimetableItem timetableItem = new TimetableItem(destinations[0],dayStartInMinutes + minutesPerSecond * 50f);
		timetableItem.SetTrain (trainPool[0]);
		timetableItem.SetPlatform (platforms[0]);
		timetable.Add (timetableItem);

		timetableItem = new TimetableItem(destinations[0],dayStartInMinutes + minutesPerSecond * 100f);
		timetableItem.SetTrain (trainPool[1]);
		timetableItem.SetPlatform (platforms[1]);
		timetable.Add (timetableItem);

		timetableItem = new TimetableItem(destinations[1],dayStartInMinutes + minutesPerSecond * 50f);
		timetableItem.SetTrain (trainPool[2]);
		timetableItem.SetPlatform (platforms[2]);
		timetable.Add (timetableItem);

		timetableItem = new TimetableItem(destinations[1],dayStartInMinutes + minutesPerSecond * 100f);
		timetableItem.SetTrain (trainPool[3]);
		timetableItem.SetPlatform (platforms[3]);
		timetable.Add (timetableItem);

		timetableItem = new TimetableItem(destinations[2],dayStartInMinutes + minutesPerSecond * 100f);
		timetableItem.SetTrain (trainPool[4]);
		timetableItem.SetPlatform (platforms[4]);
		timetable.Add (timetableItem);

	}

	public static Train GetNextTrain(Platform platform) {
		TimetableItem timeTableItem = timetable.FirstOrDefault (a => a.platform == platform);
		if (timeTableItem == default(TimetableItem)) {
			Debug.LogWarning ("This platform is requesting next train but has no entries in the timetable.");
		} else {
			if (timeTableItem.train) {
				return timeTableItem.train;
			} else {
				Debug.LogWarning ("Platform requested next train from timetable but no train is assigned to this platform.");
			}
		}
		return null;
	}

	static void AssignTrainToNewTimetableItem(Train train,Destination destination) {	//TODO call on train once first assigned or reassigned to destination
		train.SetTrainColor (destination.color);
	}
}
