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

	private Dictionary<GameObject,TimetableItem> timetableUITracker = new Dictionary<GameObject, TimetableItem> ();
	private Dictionary<Slider,Train> trainUITracker = new Dictionary<Slider, Train> ();									//stored in a dic in case we need to use lookup in future, atm though it is only used in a full foreach loop over this dic to update sliders
	private GameObject defaultOptionsMenu, itemCreationMenu, itemModificationMenu;
	private Dropdown creation_destinationDropdown, modification_trainDropdown, modification_platformDropdown;
	private Text clockText,creation_schedDepartureTimeText, modification_schedDepartureTimeText, modification_DestinationText;
	//TODO MUST: Create a new timtableitem as soon as somsone clicks button and just reference that schedDepartureTime (via reference to currentTimetableItem, which is also used for modification)
	//also then make dropdowns call into here to change the model values as soon as they are changed i.e. currentTimetableItem (and then add this to model only once they click confirm)...would this add unecessary overhead?
	private float creation_schedDepartureTimeInGame;	//this is a float value to store the game time of timetable items being created (this will be changed and then to display just converts this to display formatting)
	private TimetableItem currentTimetableItem;

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
		creation_schedDepartureTimeInGame = GameManager.instance.GetCurrentGameTime ();
		creation_schedDepartureTimeText.text = ConvertGameTimeToHHMM (creation_schedDepartureTimeInGame);	//initially set the time to the current time
		itemCreationMenu.SetActive (true);
	}
	public void OnClick_TimetableItemForModification(GameObject timetableItemUIObject) {
		TimetableItem timetableItemForMod;
		if (timetableUITracker.TryGetValue (timetableItemUIObject, out timetableItemForMod)) {
			defaultOptionsMenu.SetActive (false);
			//populate the 4 modification menu items with the values from this timetable item that is being modified
			modification_schedDepartureTimeText.text = ConvertGameTimeToHHMM (timetableItemForMod.scheduledDepartureTime);
			modification_DestinationText.text = timetableItemForMod.destination.name;
			//note the following is premised upon the index of dropdown options and Model lists (i.e.trains,platforms) being identical as they were set up that way
			modification_platformDropdown.value = GameManager.instance.platforms.IndexOf (timetableItemForMod.platform);	
			modification_trainDropdown.value = GameManager.instance.trainPool.IndexOf (timetableItemForMod.train);
			currentTimetableItem = timetableItemForMod;
			itemModificationMenu.SetActive (true);
		} else {
			Debug.LogWarning ("Player clicked a Timetable UI Item for modification but it was not found in the timetableUITracker dictionary of such items. Modification will not occur.");
		}
	}
	#endregion

	#region Item Creation Menu Actions
	public void OnClick_CancelItemCreation() {
		itemCreationMenu.SetActive (false);
		defaultOptionsMenu.SetActive (true);
	}

	public void OnClick_ConfirmCreatedItem() {
		//pass message to the GameManager to add an item to the Model
		TimetableItem newTimetableItem = GameManager.instance.CreateTimetableItem (GameManager.instance.destinations[creation_destinationDropdown.value],creation_schedDepartureTimeInGame);
		//generate a new UI object to display the timetable item's details to the player [View]
		GameObject timetableItemUIObject = Instantiate (timetableItemPrefab,timetableItemsParent.transform) as GameObject;
		//add the two newly generated objects to a tracking dictionary which allows quick lookups between View/Model later
		timetableUITracker.Add (timetableItemUIObject,newTimetableItem);
		//get a reference to the text elements of the timetable item UI object and then update the time and destination (train/platform set later)
		TimetableItemUIReferenceWrapper timetableUIRef = timetableItemUIObject.GetComponent <TimetableItemUIReferenceWrapper>();
		timetableUIRef.timeText.text = ConvertGameTimeToHHMM(newTimetableItem.scheduledDepartureTime);
		timetableUIRef.destinationText.text = newTimetableItem.destination.name;

		itemCreationMenu.SetActive (false);
		defaultOptionsMenu.SetActive (true);
	}

	public void OnClick_ChangeSchedDeptTime(float changeValue) {
		creation_schedDepartureTimeInGame += changeValue;
		creation_schedDepartureTimeText.text = ConvertGameTimeToHHMM (creation_schedDepartureTimeInGame);
	}
	#endregion

	#region Item Modification Menu Actions
	public void OnClick_CancelItemModification() {
		itemModificationMenu.SetActive (false);
		defaultOptionsMenu.SetActive (true);
	}
	public void OnClick_ConfirmModifiedItem() {
		//TODO: make timetableUItrakcer a bidictionary as per Jon Skeet's implementation...
		//timetableItemUIObject = timetableUITracker.getuitimetableitem using currentTimetableItem
		TimetableItemUIReferenceWrapper timetableUIRef = timetableItemUIObject.GetComponent <TimetableItemUIReferenceWrapper>();
		if (currentTimetableItem.platform != null) {
			timetableUIRef.platformText.text = currentTimetableItem.platform.platformNumber.ToString ();
		}
		if (currentTimetableItem.train != null) {
			timetableUIRef.trainText.text = currentTimetableItem.train.trainSerialID.ToString ();
		}
	}
	#endregion
}
