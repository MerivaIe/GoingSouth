using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using MoreLinq;

public class GameManager : MonoBehaviour {	//Singleton [I'm sorry]
	public const float gameMinutesPerRealSecond = 1f;
	public const int dayStartInGameMinutes = 180, dayDurationInGameMinutes = 1080;
	public ExhaustibleList<Platform> platforms{ get; private set; }
	public ExhaustibleList<Train> trainPool { get; private set; }
	public ExhaustibleList<Vector3> trainDockingPoints { get; private set; }
	public List<Destination> destinations{ get; private set; }
	public List<TimetableItem> timetable{ get; private set; }
	public Collider outOfStationTrigger { get; private set; }
	public WaitingArea foyer { get; private set; }
	public List<Material> defaultMaterialColors;	//set in Editor
	public int oneCarriageTrainCount = 4;			//set in Editor
	public int twoCarriageTrainCount = 1;			//set in Editor
	public GameObject oneCarriageTrainPrefab;		//set in Editor
	public GameObject twoCarriageTrainPrefab;		//set in Editor
	public float destructionInterval = 0.05f;		//set in Editor

	private static GameManager s_Instance = null;
	private Queue<GameObject> destructionQueue;

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
		destructionQueue = new Queue <GameObject> ();

		foreach (WaitingArea waitingArea in GameObject.FindObjectsOfType <WaitingArea>()) {
			if (!waitingArea.GetComponent <Platform> ()) {
				foyer = waitingArea;	//foyer will be the only waiting area without a platform... if more foyers are added this could be a List
			}
		}

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
			trainDockingPoint.x = outOfStationTrigger.bounds.center.x;
			trainDockingPoint.y = 1.56f;
			trainDockingPoint.z = 20f;
			for (int i = 0; i < oneCarriageTrainCount; i++) {
				trainDockingPoint.z += 5f;	//position trains along the z axis... when they are called into service/ journey time is complete just need to change z position to that of the platform's signal trigger and then go
				GameObject trainGO = Instantiate (oneCarriageTrainPrefab,trainDockingPoint,Quaternion.identity) as GameObject;
				Train train = trainGO.GetComponent <Train> ();
				train.Initialise ();	//Initialise some of Trains' properties early as they are required in DisplayManager before Trains' Start() method is called
				trainDockingPoints.Add (trainDockingPoint);
				trainPool.Add (train);
			}
			for (int i = 0; i < twoCarriageTrainCount; i++) {
				trainDockingPoint.z += 5f;	//position trains along the z axis... when they are called into service/ journey time is complete just need to change z position to that of the platform's signal trigger and then go
				GameObject trainGO = Instantiate (twoCarriageTrainPrefab,trainDockingPoint,Quaternion.identity) as GameObject;
				Train train = trainGO.GetComponent <Train> ();
				train.Initialise ();	//Initialise some of Trains' properties early as they are required in DisplayManager before Trains' Start() method is called
				trainDockingPoints.Add (trainDockingPoint);
				trainPool.Add (train);
			}

			destinations = new List<Destination> ();
			destinations.Add (new Destination ("Bristow", 200,100));
			destinations.Add (new Destination ("Lomdom", 70, 500));
			destinations.Add (new Destination ("Basimgstoke", 100, 100));
			destinations.Add (new Destination ("Edimburgh", 500, 20));	//takes long time so you want to wait for people to build up

			timetable = new List<TimetableItem>();
		}
	}

	public float GetCurrentGameTime() {
		return dayStartInGameMinutes + gameMinutesPerRealSecond * Time.time;
	}

	public TimetableItem CreateTimetableItem(float scheduledDepartureTime) {
		TimetableItem newTimetableItem = new TimetableItem (scheduledDepartureTime);
		return newTimetableItem;
	}

	public void AssignTrainToTimetableItem(int trainIndex, TimetableItem timetableItem) {
		Train train = trainPool.AvailableOptions [trainIndex];
		timetableItem.SetTrain (train);
		train.OnAssignedToTimetableItem (timetableItem);
		trainPool.ExhaustOption (train);
	}

	public void AssignPlatformToTimetableItem(int platformIndex, TimetableItem timetableItem) {
		Platform platform = platforms.AvailableOptions [platformIndex];
		timetableItem.SetPlatform (platform);
		platform.OnAssignedToTimetableItem (timetableItem);
		platforms.ExhaustOption (platform);
		RecalculateSoonestTimetableItemForDestination (timetableItem.destination);
	}

	public void ConfirmCreatedTimetableItem(int destinationIndex, TimetableItem timetableItem) {
		//scheduled departure time of the timetableItem passed to us is already set as it is changed by GameUIManager in response to player input
		timetableItem.SetDestination (destinations [destinationIndex]);
		timetable.Add (timetableItem);
	}

	public void WipeTimetableItemPlatformAndTrain(TimetableItem timetableItem) {
		timetableItem.SetTrain (null);
		timetableItem.SetPlatform (null);
		//for future reference: do not need to RestoreOption for train or platform because they are restored when item is first selected for modification, if it is then wiped they will stay restored!
		RecalculateSoonestTimetableItemForDestination (timetableItem.destination);	//will set it to null unless there is another timetable item for this dest with a platform assignment
	}
		
	public void OnTrainOutOfStation(TimetableItem timetableItem) {
		trainPool.RestoreOption (timetableItem.train);
		platforms.RestoreOption (timetableItem.platform);
		timetable.Remove (timetableItem);
		RecalculateSoonestTimetableItemForDestination (timetableItem.destination);
	}

	void RecalculateSoonestTimetableItemForDestination(Destination dest) {
		IEnumerable<TimetableItem> timetableToDestination = GameManager.instance.timetable.Where(t => t.destination == dest && t.platform != null);	//collection of timetable items to this destination that have a platform assigned
		if (timetableToDestination.Any ()) {																					//if no timetable items to this destination skip
			dest.SetSoonestTimetableItem (timetableToDestination.MinBy (t => t.scheduledDepartureTime));						//if there are, retrieve the earliest one
		} else {
			dest.SetSoonestTimetableItem (null);
		}
	}

	public void AddObjectToDeletionQueue(GameObject gameObjectToDelete) {
		destructionQueue.Enqueue (gameObjectToDelete);
		if (!IsInvoking ("DestroyObjectsInQueue")) {
			InvokeRepeating ("DestroyObjectsInQueue", 0f, destructionInterval);
		}
	}

	void DestroyObjectsInQueue() {
		if (destructionQueue.Count > 0) {
			GameObject.Destroy (destructionQueue.Dequeue ());
		} else {
			CancelInvoke ();
		}
	}
}