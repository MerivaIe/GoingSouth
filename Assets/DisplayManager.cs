using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DisplayManager : MonoBehaviour {

	public GameObject timetableItemsParent;
	public GameObject timetableItemPrefab;
	[Range(0.02f,1f)]
	public float displayUpdateInterval = 0.5f;

	private Dictionary<TimetableItemUIObject,TimetableItem> timetableUITracker = new Dictionary<TimetableItemUIObject, TimetableItem> ();
	private Dictionary<Slider,Train> trainUITracker = new Dictionary<Slider, Train> ();									//stored in a dic in case we need to use lookup in future, atm though it is only used in a full foreach loop over this dic to update sliders
	private GameObject defaultOptionsMenu, itemCreationMenu, itemModificationMenu;
	private Dropdown creation_destinationDropdown, modification_trainDropdown, modification_platformDropdown;
	private Text clockText,creation_schedDepartureTimeText, modification_schedDepartureTimeText, modification_DestinationText;
	private TimetableItem activeTimetableItem;	//only one timetableitem is created/modified at a time so we store a reference to it using this

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
				trainUITracker.Add (sliders[i],train);
				i++;
			}
		}

		defaultOptionsMenu = MyFindUIObjectWithTag ("UI_DefaultOptionsMenu");	//note: must have all of the menus activated initially to find their elements, then deactivate them later
		itemCreationMenu = MyFindUIObjectWithTag ("UI_ItemCreationMenu");	
		itemModificationMenu = MyFindUIObjectWithTag ("UI_ItemModificationMenu");	
		
		clockText = MyFindUIObjectWithTag ("UI_ClockText").GetComponent <Text> ();
		creation_schedDepartureTimeText = MyFindUIObjectWithTag ("UI_Create_SchedDepartureTimeText").GetComponent <Text> ();
		modification_schedDepartureTimeText = MyFindUIObjectWithTag ("UI_Mod_SchedDepartureTimeText").GetComponent <Text> ();
		modification_DestinationText = MyFindUIObjectWithTag ("UI_DestinationText").GetComponent <Text> ();

		creation_destinationDropdown = MyFindUIObjectWithTag ("UI_DestinationDropdown").GetComponent <Dropdown>();
		modification_trainDropdown = MyFindUIObjectWithTag ("UI_TrainDropdown").GetComponent <Dropdown>();
		modification_platformDropdown = MyFindUIObjectWithTag ("UI_PlatformDropdown").GetComponent <Dropdown>();
		creation_destinationDropdown.ClearOptions ();
		creation_destinationDropdown.AddOptions (GameManager.instance.destinations.Select (a=>a.name).ToList ());
		modification_platformDropdown.ClearOptions ();
		modification_platformDropdown.AddOptions (GameManager.instance.platforms.Select (a=>a.platformNumber.ToString ()).ToList ());
		modification_trainDropdown.ClearOptions ();
		modification_trainDropdown.AddOptions (GameManager.instance.trainPool.Select (a=>a.trainSerialID).ToList ());

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
		foreach (KeyValuePair<Slider,Train> trainUIItem in trainUITracker) {
			trainUIItem.Key.value = trainUIItem.Value.GetJourneyProgress ();
		}
		clockText.text = ConvertGameTimeToHHMM (GameManager.instance.GetCurrentGameTime());
	}

	string ConvertGameTimeToHHMM(float gameTime) {
		return string.Format("{0:#00}:{1:00}", Mathf.Floor(gameTime/60),Mathf.Floor(gameTime) % 60);
	}

	#region Default Options Menu Actions
	public void OnClick_NewTimetableItem() {
		defaultOptionsMenu.SetActive (false);
		//pass message to the GameManager to add an item to the Model and store in our activeTimetableItem reference for later manipulation
		activeTimetableItem = GameManager.instance.CreateTimetableItem (GameManager.instance.destinations[0],GameManager.instance.GetCurrentGameTime ());
		creation_schedDepartureTimeText.text = ConvertGameTimeToHHMM (activeTimetableItem.scheduledDepartureTime);	//initially set the time to the current time
		itemCreationMenu.SetActive (true);
	}
	public void OnClick_TimetableItemForModification(GameObject timetableItemGO) {
		TimetableItemUIObject timetableUIObject = timetableItemGO.GetComponent <TimetableItemUIObject> ();
		//place the timetable item [Model] for this particular timetable UI object in the activeTimetableItem reference
		if (timetableUITracker.TryGetValue (timetableUIObject, out activeTimetableItem)) {
			defaultOptionsMenu.SetActive (false);
			//populate the 4 modification menu items with the values from this timetable item that is being modified
			modification_schedDepartureTimeText.text = ConvertGameTimeToHHMM (activeTimetableItem.scheduledDepartureTime);
			modification_DestinationText.text = activeTimetableItem.destination.name;
			//note the following is premised upon the index of dropdown options and Model lists (i.e.trains,platforms) being identical as they were set up that way
			modification_platformDropdown.value = GameManager.instance.platforms.IndexOf (activeTimetableItem.platform);	
			modification_trainDropdown.value = GameManager.instance.trainPool.IndexOf (activeTimetableItem.train);
			itemModificationMenu.SetActive (true);
		} else {
			Debug.LogWarning ("Player clicked a Timetable UI Item for modification but it was not found in the timetableUITracker dictionary of such items. Modification will not occur.");
		}
	}
	#endregion

	#region Item Creation Menu Actions
	public void OnClick_ValueChangedDestination(int indexOfDropdown) {

		activeTimetableItem.destination = GameManager.instance.destinations [indexOfDropdown];
	}
	public void OnClick_ChangeSchedDeptTime(float changeValue) {

		activeTimetableItem.scheduledDepartureTime += changeValue;
		creation_schedDepartureTimeText.text = ConvertGameTimeToHHMM (activeTimetableItem.scheduledDepartureTime);
	}
	public void OnClick_CancelItemCreation() {
		ReturnToDefaultOptionsMenu (itemCreationMenu);
	}
	public void OnClick_ConfirmCreatedItem() {
		//Generate a new UI object to display the timetable item's details to the player [View]
		GameObject timetableItemGO = Instantiate (timetableItemPrefab,timetableItemsParent.transform) as GameObject;
		TimetableItemUIObject timetableItemUIObject = timetableItemGO.GetComponent <TimetableItemUIObject>();
		//Get the reference to the text elements of the timetable item UI object and then update the time and destination (train/platform set later)
		timetableItemUIObject.timeText.text = ConvertGameTimeToHHMM(activeTimetableItem.scheduledDepartureTime);
		timetableItemUIObject.destinationText.text = activeTimetableItem.destination.name;
		//add the two newly generated objects to a tracking bi-lookup which allows quick lookups between View/Model later
		timetableUITracker.Add (timetableItemUIObject,activeTimetableItem);

		ReturnToDefaultOptionsMenu (itemCreationMenu);
	}
	#endregion

	#region Item Modification Menu Actions
	public void OnClick_ValueChangedTrain(int indexOfDropdown) {
		activeTimetableItem.train = GameManager.instance.trainPool [indexOfDropdown];
	}
	public void OnClick_ValueChangedPlatform(int indexOfDropdown) {
		activeTimetableItem.platform = GameManager.instance.platforms [indexOfDropdown];
	}
	public void OnClick_CancelItemModification() {
		ReturnToDefaultOptionsMenu (itemModificationMenu);
	}
	public void OnClick_ConfirmModifiedItem() {
		TimetableItemUIObject timetableItemUIObject;
		//TryGetValueBySecond
		//timetableUITracker.TryGetValue (activeTimetableItem, out timetableItemUIObject);
		if (activeTimetableItem.platform != null) {
			//timetableItemUIObject.platformText.text = activeTimetableItem.platform.platformNumber.ToString ();
		}
		if (activeTimetableItem.train != null) {
			//timetableItemUIObject.trainText.text = activeTimetableItem.train.trainSerialID.ToString ();
		}
		ReturnToDefaultOptionsMenu (itemModificationMenu);
	}
	#endregion

	void ReturnToDefaultOptionsMenu(GameObject currentMenuToClose) {	//could make this more generic with menu to open to if UI gets more complex
		activeTimetableItem = null;
		currentMenuToClose.SetActive (false);
		defaultOptionsMenu.SetActive (true);
	}
}
