using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GameManager : MonoBehaviour {	//Singleton [I'm sorry]
	public const float minutesPerSecond = 1f;
	public const int dayStartInMinutes = 180;
	public List<Platform> platforms{ get; private set; }
	public List<Destination> destinations{ get; private set; }
	public List<TimetableItem> timetable{ get; private set; }
	public List<Train> trainPool { get; private set; }
	public List<Material> defaultMaterialColors;
	public List<ExhaustibleOption<Train>> testTrainPool;

	private static GameManager s_Instance = null;

	public static GameManager instance {
		get {
			if (s_Instance == null) {	//Find GameManager in hierarchy
				s_Instance =  FindObjectOfType<GameManager>();
			}
				
			if (s_Instance == null) {	// If it is still null, create a new instance
				GameObject obj = new GameObject("GameManager");
				s_Instance = obj.AddComponent<GameManager>();
				Debug.LogWarning ("Could not locate a GameManager object so one was added to the Scene automatically. This is a problem: colors are set at design time.");
			}
			return s_Instance;
		}
	}

	void OnApplicationQuit() {	//is this required
		s_Instance = null;
	}

	void Awake () {
		if (defaultMaterialColors.Count == 0) {
			Debug.LogWarning ("No materials assigned to default color array in GameManager. Please do so.");
		}

		if (platforms != null || trainPool!= null || destinations!= null) {
			Debug.LogWarning ("Another GameManager has somehow assigned to variables. There should only be one GameManager in the scene.");
		} else {
//			//trainPool = GameObject.FindObjectsOfType<Train> ().ToList ();	//this would eventually be Instantiating trains at level load based on user decisions
//			trainPool = new List<Train>();
//			trainPool.Add(GameObject.Find ("Train (1)").GetComponent <Train>());
//			trainPool.Add(GameObject.Find ("Train (2)").GetComponent <Train>());
//			trainPool.Add(GameObject.Find ("Train (3)").GetComponent <Train>());
//			trainPool.Add(GameObject.Find ("Train (4)").GetComponent <Train>());
//			//trainPool.Add(GameObject.Find ("Complex Train (1)").GetComponent <Train>());
//			foreach (Train train in trainPool) {	//Initialise some of Trains' properties early as they are required in DisplayManager before Trains' Start() method is called
//				train.Initialise ();
//			}

			//trainPool = GameObject.FindObjectsOfType<Train> ().ToList ();	//this would eventually be Instantiating trains at level load based on user decisions
			testTrainPool = new List<ExhaustibleOption<Train>>();
			testTrainPool.Add(new ExhaustibleOption<Train> (GameObject.Find ("Train (1)").GetComponent <Train>()));
			testTrainPool.Add(new ExhaustibleOption<Train> (GameObject.Find ("Train (2)").GetComponent <Train>()));
			testTrainPool.Add(new ExhaustibleOption<Train> (GameObject.Find ("Train (3)").GetComponent <Train>()));
			testTrainPool.Add(new ExhaustibleOption<Train> (GameObject.Find ("Train (4)").GetComponent <Train>()));
			//trainPool.Add(GameObject.Find ("Complex Train (1)").GetComponent <Train>());
			foreach (Train train in testTrainPool.Select (t=>t.option)) {	//Initialise some of Trains' properties early as they are required in DisplayManager before Trains' Start() method is called
				train.Initialise ();
			}

			platforms = GameObject.FindObjectsOfType<Platform> ().OrderBy (a => a.transform.position.z).ToList ();	//order by arrangement on z axis so that platforms can then be numbered sensibly
			for (int i = 0; i < platforms.Count; i++) {
				platforms [i].platformNumber = i + 1;
			}

			destinations = new List<Destination> ();
			destinations.Add (new Destination ("Bristow", 200, 300));
			destinations.Add (new Destination ("Lomdom", 70, 2000));
			destinations.Add (new Destination ("Basimgstoke", 100, 1000));

			timetable = new List<TimetableItem>();
			GenerateTimetable (destinations);
		}
	}

	void GenerateTimetable(List<Destination> destinations) {	//decided by player: for each destination, avg time between trains
		//...but at the moment just some dummy stuff in here for the five trains in scene

		//TODO configure these so that they are the correct trains going to the correct platforms
		TimetableItem timetableItem = new TimetableItem(destinations[0],dayStartInMinutes + minutesPerSecond * 50f);
		timetableItem.SetPlatform (platforms[0]);
		timetableItem.SetTrain (trainPool[0]);
		timetable.Add (timetableItem);

		timetableItem = new TimetableItem(destinations[0],dayStartInMinutes + minutesPerSecond * 100f);
		timetableItem.SetPlatform (platforms[1]);
		timetableItem.SetTrain (trainPool[1]);
		timetable.Add (timetableItem);

		timetableItem = new TimetableItem(destinations[1],dayStartInMinutes + minutesPerSecond * 50f);
		timetableItem.SetPlatform (platforms[2]);
		timetableItem.SetTrain (trainPool[2]);
		timetable.Add (timetableItem);

		timetableItem = new TimetableItem(destinations[2],dayStartInMinutes + minutesPerSecond * 100f);
		timetableItem.SetPlatform (platforms[3]);
		timetableItem.SetTrain (trainPool[3]);
		timetable.Add (timetableItem);
	}

	public float GetCurrentGameTime() {
		return dayStartInMinutes + minutesPerSecond * Time.time;
	}

	public Train GetNextTrain(Platform platform) {
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

	public TimetableItem CreateTimetableItem(Destination destination,float scheduledDepartureTime) {
		TimetableItem newTimetableItem = new TimetableItem (destination, scheduledDepartureTime);
		timetable.Add (newTimetableItem);
		return newTimetableItem;
		//TODO: do we need to send message back to calling (e.g. to DisplayManager)? ALSO should timetable item dislay have a reference to timetableitem model or should this be done some other way
	}

	void AssignTrainToNewTimetableItem(Train train,Destination destination) {	//TODO call on train once first assigned or reassigned to destination
		train.SetTrainColor (destination.materialColor);
	}

	public struct ExhaustibleOption<MyType> {
		public MyType option;
		public bool available;

		public ExhaustibleOption(MyType _option) {
			option = _option;
			available = true;
		}
	}
}
