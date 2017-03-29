using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GameManager : MonoBehaviour {	//Singleton [I'm sorry]
	public const float minutesPerSecond = 1f;
	public const int dayStartInMinutes = 180;
	public ExhaustibleList<Platform> platforms{ get; private set; }
	public ExhaustibleList<Train> trainPool { get; private set; }
	public List<Destination> destinations{ get; private set; }
	public ExhaustibleList<Vector3> trainDockingPoints { get; private set; }
	public List<TimetableItem> timetable{ get; private set; }
	public List<Material> defaultMaterialColors;
	public int trainCount = 4;
	public GameObject trainPrefab;
	public float destructionInterval = 0.05f;
	public Collider outOfStationTrigger { get; private set; }

	private static GameManager s_Instance = null;
	private List<GameObject> destructionQueue;

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
		destructionQueue = new List<GameObject> ();

		foreach (Signal signal in GameObject.FindObjectsOfType <Signal>()) {
			if (signal.signalType == Signal.SignalType.OutOfStation) {
				outOfStationTrigger = signal.gameObject.GetComponent <BoxCollider> ();
				break;
			}
		}
		if (!outOfStationTrigger) {Debug.LogWarning ("Out of station trigger not found. Please add one to the scene.");}

		if (defaultMaterialColors.Count == 0) {Debug.LogWarning ("No materials assigned to default color array in GameManager. Please do so.");}

		if (platforms != null || trainPool!= null || destinations!= null) {
			Debug.LogWarning ("Another GameManager has somehow assigned to variables. There should only be one GameManager in the scene.");
		} else {
			platforms = new ExhaustibleList<Platform> ();
			platforms.AddRange(GameObject.FindObjectsOfType<Platform> ().OrderBy (a => a.transform.position.z).ToList ());	//order by arrangement on z axis so that platforms can then be numbered sensibly
			for (int i = 0; i < platforms.AllOptions.Count; i++) {
				platforms.AllOptions [i].platformNumber = i + 1;
			}

			trainPool = new ExhaustibleList<Train>();
			trainDockingPoints = new ExhaustibleList<Vector3> ();
			Vector3 trainDockingPoint;
			trainDockingPoint.x = -75f;
			trainDockingPoint.y = 1.56f;
			trainDockingPoint.z = 20f;
			for (int i = 0; i < trainCount; i++) {
				trainDockingPoint.z += 5f;	//position trains along the z axis... when they are called into service/ journey time is complete just need to change z position to that of the platform's signal trigger and then go
				GameObject trainGO = Instantiate (trainPrefab,trainDockingPoint,Quaternion.identity) as GameObject;
				Train train = trainGO.GetComponent <Train> ();
				train.Initialise ();	//Initialise some of Trains' properties early as they are required in DisplayManager before Trains' Start() method is called
				trainDockingPoints.Add (trainDockingPoint);
				trainPool.Add (train);
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
		timetableItem.SetPlatform (platforms.AvailableOptions[0]);
		timetableItem.SetTrain (trainPool.AvailableOptions[0]);
		timetable.Add (timetableItem);

		timetableItem = new TimetableItem(destinations[0],dayStartInMinutes + minutesPerSecond * 100f);
		timetableItem.SetPlatform (platforms.AvailableOptions[1]);
		timetableItem.SetTrain (trainPool.AvailableOptions[1]);
		timetable.Add (timetableItem);

		timetableItem = new TimetableItem(destinations[1],dayStartInMinutes + minutesPerSecond * 50f);
		timetableItem.SetPlatform (platforms.AvailableOptions[2]);
		timetableItem.SetTrain (trainPool.AvailableOptions[2]);
		timetable.Add (timetableItem);

		timetableItem = new TimetableItem(destinations[2],dayStartInMinutes + minutesPerSecond * 100f);
		timetableItem.SetPlatform (platforms.AvailableOptions[3]);
		timetableItem.SetTrain (trainPool.AvailableOptions[3]);
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
	}

	public void AssignTrainToTimetableItem(int trainIndex, TimetableItem timetableItem) {
		timetableItem.train = trainPool.AvailableOptions [trainIndex];
		trainPool.ExhaustOption (timetableItem.train);
	}

	public void AssignPlatformToTimetableItem(int platformIndex, TimetableItem timetableItem) {
		timetableItem.platform = platforms.AvailableOptions [platformIndex];
		platforms.ExhaustOption (timetableItem.platform);
	}

	public void AssignDestinationToTimetableItem(int destinationIndex, TimetableItem timetableItem) {
		timetableItem.destination = destinations [destinationIndex];
	}

	public void AddObjectsToDeletionQueue(List<GameObject> gameObjectToDelete) {
		destructionQueue.AddRange (gameObjectToDelete);
		if (!IsInvoking ("DestroyObjectsInQueue")) {
			InvokeRepeating ("DestroyObjectsInQueue", 0f, destructionInterval);
		}
	}

	void DestroyObjectsInQueue() {
		if (destructionQueue.Count >= 0) {
			CancelInvoke ();
		} else {
			GameObject.Destroy (destructionQueue[0]);
		}
	}
}