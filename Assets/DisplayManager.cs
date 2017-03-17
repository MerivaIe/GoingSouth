using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DisplayManager : MonoBehaviour {

	public GameObject timetableItemsParent;
	public GameObject timetableItemPrefab;
	[Range(0.02f,1f)]
	public float displayUpdateInterval;

	private List<TrainTracker> trainTrackers = new List<TrainTracker>();
	private GameObject defaultOptionsMenu, itemCreationMenu, itemModificationMenu;
	private Dropdown destinationDropdown, trainDropdown, platformDropdown;
	private Text clockText,schedDepartureTimeText;
	private float schedDepartureTimeInGame;	//this is a float value to store the game time of timetable items being created (this will be changed and then to display just converts this to display formatting)

	private static DisplayManager s_Instance = null;

	public static DisplayManager instance {
		get {
			if (s_Instance == null) {	//Find GameManager in hierarchy
				s_Instance =  FindObjectOfType<DisplayManager>();
			}

			if (s_Instance == null) {	// If it is still null, create a new instance
				GameObject obj = new GameObject("DisplayManager");
				s_Instance = obj.AddComponent<DisplayManager>();
				Debug.Log ("Could not locate a DisplayManager object so one was added to the Scene automatically.");
			}
			return s_Instance;
		}
	}

	void OnApplicationQuit() {	//is this required
		s_Instance = null;
	}

	void Start () {				//must be after GameManager's Awake()
		if (timetableItemPrefab == null || timetableItemsParent == null) {
			Debug.LogWarning ("No timetable item prefab and/or the parent object for them assigned. Please do so.");
		}

		Slider[] sliders = GameObject.FindObjectsOfType <Slider> ();	//use this for now just to get sliders, but eventually you should create sliders from a prefab.. one for each train
		if (GameManager.instance.trainPool == null || GameManager.instance.trainPool.Count == 0) {
			Debug.LogWarning ("DisplayManager is trying to access GameManager's train pool but it is not initialised or empty.");
		} else {
			int i = 0;
			foreach (Train train in GameManager.instance.trainPool) {
				trainTrackers.Add (new TrainTracker(train,sliders[i]));
				i++;
			}
		}

		defaultOptionsMenu = MyFindUIObjectWithTag ("UI_DefaultOptionsMenu");	//note: must have all of the menus activated initially to find their elements, then deactivate them later
		itemCreationMenu = MyFindUIObjectWithTag ("UI_ItemCreationMenu");	
		itemModificationMenu = MyFindUIObjectWithTag ("UI_ItemModificationMenu");	

		destinationDropdown = MyFindUIObjectWithTag ("UI_DestinationDropdown").GetComponent <Dropdown>();
		//need to create an entire new menu which does the following:
		//displays departure time, and destination labels
		//can edit train and platform (code for these is set up below but GameObjects required)
		//can cancel or delay departure...work out the cost for this later

		trainDropdown = MyFindUIObjectWithTag ("UI_TrainDropdown").GetComponent <Dropdown>();
		platformDropdown = MyFindUIObjectWithTag ("UI_PlatformDropdown").GetComponent <Dropdown>();
		clockText = MyFindUIObjectWithTag ("UI_ClockText").GetComponent <Text> ();
		schedDepartureTimeText = MyFindUIObjectWithTag ("UI_SchedDepartureTimeText").GetComponent <Text> ();

		destinationDropdown.ClearOptions ();
		destinationDropdown.AddOptions (GameManager.instance.destinations.Select (a=>a.name).ToList ());
		platformDropdown.ClearOptions ();
		platformDropdown.AddOptions (GameManager.instance.platforms.Select (a=>a.platformNumber.ToString ()).ToList ());
		trainDropdown.ClearOptions ();
		trainDropdown.AddOptions (GameManager.instance.trainPool.Select (a=>a.trainSerialID).ToList ());

		itemCreationMenu.SetActive (false);	//finally hide the non-default menus
		itemModificationMenu.SetActive (false);

		InvokeRepeating ("UpdateDisplay",0f,displayUpdateInterval);		//start a repeating invoke that updates the display at interval
	}

	GameObject MyFindUIObjectWithTag(string searchForTag) {
		GameObject foundGameObject = GameObject.FindGameObjectWithTag (searchForTag);
		if (foundGameObject == null) {
			Debug.LogWarning ("Could not find UI game object with tag: " + searchForTag + ". Display will not function as expected.");
		}
		return foundGameObject;
	}
	
	void UpdateDisplay() {	//called every x seconds
		foreach (TrainTracker trainTracker in trainTrackers) {
			trainTracker.slider.value = trainTracker.train.GetJourneyProgress ();
		}
		clockText.text = ConvertGameTimeToHHMM (GameManager.instance.GetCurrentGameTime());
	}

	string ConvertGameTimeToHHMM(float gameTime) {
		return string.Format("{0:#00}:{1:00}", Mathf.Floor(gameTime/60),Mathf.Floor(gameTime) % 60);
	}

	#region Default Options Menu Actions
	public void OnClick_NewTimetableItem() {
		defaultOptionsMenu.SetActive (false);
		schedDepartureTimeInGame = GameManager.instance.GetCurrentGameTime ();
		schedDepartureTimeText.text = ConvertGameTimeToHHMM (schedDepartureTimeInGame);	//initially set the time to the current time
		itemCreationMenu.SetActive (true);
	}
	#endregion

	#region Item Creation Menu Actions
	public void OnClick_CancelItemCreation() {
		itemCreationMenu.SetActive (false);
		defaultOptionsMenu.SetActive (true);
	}

	public void OnClick_ConfirmCreatedItem() {
		
		//pass message to the GameManager to (validate?) and add an item to the model
		TimetableItem newTimetableItem = GameManager.instance.CreateTimetableItem (GameManager.instance.destinations[destinationDropdown.value],schedDepartureTimeInGame);
		GameObject timetableItemGO = Instantiate (timetableItemPrefab,timetableItemsParent.transform) as GameObject;
		//TODO here we should get all 4 of the items to update for this component and do so
		//this should be another Struct as for TrainTracker called TimetableItemDisplay, linking TimetableItem to instaniated GameObject
	}

	public void OnClick_ChangeTimetableTime(float changeValue) {
		schedDepartureTimeInGame += changeValue;
		schedDepartureTimeText.text = ConvertGameTimeToHHMM (schedDepartureTimeInGame);
	}
	#endregion

	private struct TrainTracker {
		public Train train;
		public Slider slider;

		public TrainTracker(Train _train, Slider _slider) {
			train = _train;
			slider = _slider;
		}
	}
}
