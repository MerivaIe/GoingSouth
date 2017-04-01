using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GameUIManager : MonoBehaviour {

	public GameObject timetableItemsParent;
	public GameObject timetableItemPrefab;
	[Range(0.02f,1f)]
	public float displayUpdateInterval = 0.5f;
	public Sprite trainUISprite;

	private BiLookup<TimetableItemUIObject,TimetableItem> timetableUITracker;
	private Dictionary<Slider,Train> trainUITracker;	//stored in a dic in case we need to use lookup in future, atm though it is only used in a full foreach loop over this dic to update sliders
	private GameObject defaultOptionsMenu, itemCreationMenu, itemModificationMenu;
	private Dropdown creation_destinationDropdown, modification_trainDropdown, modification_platformDropdown;
	private Text clockText,creation_schedDepartureTimeText, modification_schedDepartureTimeText, modification_DestinationText;
	private TimetableItem activeTimetableItem;	//only one timetableitem is created/modified at a time so we store a reference to it using this

	private static GameUIManager s_Instance = null;

	public static GameUIManager instance {
		get {
			if (s_Instance == null) {	//Find GameManager in hierarchy
				s_Instance = GameObject.FindObjectOfType<GameUIManager>();
			}

			if (s_Instance == null) {	// If it is still null, create a new instance
				GameObject gameUI_GO = GameObject.Find ("GameUI");
				if (gameUI_GO == null) {
					Debug.LogWarning ("No GameUIManager could be found and neither could a GameUI object to initialise and attach one to. Problems will now ensue.");
				} else {
					s_Instance = gameUI_GO.AddComponent<GameUIManager> ();
					Debug.Log ("Could not locate a GameUIManager so one was added to the GameUI object automatically. It will need to have some public references set in the Editor.");
				}
			}
			return s_Instance;
		}
	}

	void OnApplicationQuit() {	//is this required
		s_Instance = null;
	}

	void Start () {				//must be after GameManager's Awake()
		timetableUITracker =  new BiLookup<TimetableItemUIObject, TimetableItem> ();
		trainUITracker = new Dictionary<Slider, Train> ();	

		if (timetableItemPrefab == null || timetableItemsParent == null) {
			Debug.LogWarning ("No timetable item prefab and/or the parent object for them assigned. Please do so.");
		}

		Slider[] sliders = GameObject.FindObjectsOfType <Slider> ();	//use this for now just to get sliders, but eventually you should create sliders from a prefab.. one for each train
		if (GameManager.instance.trainPool.AvailableOptions == null || GameManager.instance.trainPool.AvailableOptions.Count == 0) {
			Debug.LogWarning ("GameUIManager is trying to access GameManager's train pool but it is not initialised or empty.");
		} else {
			int i = 0;
			foreach (Train train in GameManager.instance.trainPool.AvailableOptions) {
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

		itemCreationMenu.SetActive (false);	//finally hide the non-default menus
		itemModificationMenu.SetActive (false);

		InvokeRepeating ("UpdateDisplay",0f,displayUpdateInterval);		//start a repeating invoke that updates the display at interval
	}

	GameObject MyFindUIObjectWithTag(string searchForTag) {
		GameObject foundGameObject = GameObject.FindGameObjectWithTag (searchForTag);
		if (foundGameObject == null) {
			Debug.LogWarning ("Could not find UI game object with tag: " + searchForTag + ". Display will not function as expected. Ensure all UI objects are active at game start.");
		}
		return foundGameObject;
	}
	
	void UpdateDisplay() {	//called every x seconds
		foreach (KeyValuePair<Slider,Train> trainUIItem in trainUITracker) {
			trainUIItem.Key.value = trainUIItem.Value.GetJourneyProgress ();
		}
		clockText.text = ConvertGameTimeToHHMM (GameManager.instance.GetCurrentGameTime());
	}

	public static string ConvertGameTimeToHHMM(float gameTime) {
		return string.Format("{0:#00}:{1:00}", Mathf.Floor(gameTime/60),Mathf.Floor(gameTime) % 60);
	}

	void ReturnToDefaultOptionsMenu(GameObject currentMenuToClose) {	//could make this more generic with menu to open to if UI gets more complex
		activeTimetableItem = null;
		timetableItemsParent.GetComponent <CanvasGroup> ().interactable = true;
		currentMenuToClose.SetActive (false);
		defaultOptionsMenu.SetActive (true);
	}

	//Default Options Menu
	public void OnClick_NewTimetableItem() {
		timetableItemsParent.GetComponent <CanvasGroup> ().interactable = false;
		defaultOptionsMenu.SetActive (false);
		//pass message to the GameManager to add an item to the Model and store in our activeTimetableItem reference for later manipulation
		activeTimetableItem = GameManager.instance.CreateTimetableItem (GameManager.instance.GetCurrentGameTime ());
		creation_schedDepartureTimeText.text = ConvertGameTimeToHHMM (activeTimetableItem.scheduledDepartureTime);	//initially set the time to the current time
		itemCreationMenu.SetActive (true);
	}
	public void OnClick_TimetableItemForModification(TimetableItemUIObject timetableUIObject) {
		//get the timetable item [Model] for this particular timetable UI object and store in the activeTimetableItem reference
		if (timetableUITracker.TryGetValueByFirst (timetableUIObject, out activeTimetableItem)) {
			defaultOptionsMenu.SetActive (false);
			timetableItemsParent.GetComponent <CanvasGroup> ().interactable = false;	//set all timetable UI items so they cannot be modified
			//populate the 4 modification menu items with the values from this timetable item that is being modified
			modification_schedDepartureTimeText.text = ConvertGameTimeToHHMM (activeTimetableItem.scheduledDepartureTime);
			modification_DestinationText.text = activeTimetableItem.destination.name;

			if (activeTimetableItem.train) {	//if the item selected for mods had a train already chosen previously then restore it to available options
				GameManager.instance.trainPool.RestoreOption (activeTimetableItem.train);
			}
			modification_trainDropdown.ClearOptions ();
			List<Dropdown.OptionData> dropdownOptions = new List<Dropdown.OptionData> ();
			foreach (Train train in GameManager.instance.trainPool.AvailableOptions) {
				dropdownOptions.Add (new Dropdown.OptionData(train.trainSerialID,trainUISprite));
			}
			modification_trainDropdown.AddOptions (dropdownOptions);	
			modification_trainDropdown.value = !activeTimetableItem.train? 0: GameManager.instance.trainPool.AvailableOptions.IndexOf (activeTimetableItem.train);	//if the item selected for mods had a train already chosen previously then select it as the dropdown option

			if (activeTimetableItem.platform) {	//if the item selected for mods had a platform already chosen previously then restore it to available options and select it in the dropdown
				GameManager.instance.platforms.RestoreOption (activeTimetableItem.platform);
			}
			modification_platformDropdown.ClearOptions ();
			modification_platformDropdown.AddOptions (GameManager.instance.platforms.AvailableOptions.Select (a => "Platform " + a.platformNumber).ToList ());
			modification_platformDropdown.value = !activeTimetableItem.platform? 0 : GameManager.instance.platforms.AvailableOptions.IndexOf (activeTimetableItem.platform);	//if the item selected for mods had a platform already chosen previously then select it in the dropdown
			itemModificationMenu.SetActive (true);
		} else {
			Debug.LogWarning ("Player clicked a Timetable UI Item for modification but it was not found in the timetableUITracker dictionary of such items. Modification will not occur.");
		}
	}

	//Item Creation Menu Actions
	public void OnClick_ChangeSchedDeptTime(float changeValue) {
		activeTimetableItem.scheduledDepartureTime += changeValue;
		creation_schedDepartureTimeText.text = ConvertGameTimeToHHMM (activeTimetableItem.scheduledDepartureTime);
	}
	public void OnClick_CancelItemCreation() {
		ReturnToDefaultOptionsMenu (itemCreationMenu);
	}
	public void OnClick_ConfirmCreatedItem() {
		GameManager.instance.ConfirmCreatedTimetableItem (creation_destinationDropdown.value, activeTimetableItem);
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

	//Item Modification Menu Actions
	public void OnClick_CancelItemModification() {
		//below is premised on the fact that activeTimetableItem will hold the timetable item as it was when it was first selected for modification (see OnClick_TimetableItemForModification)
		if (activeTimetableItem.train) {	//remove previously chosen train from list of available trains
			GameManager.instance.trainPool.ExhaustOption (activeTimetableItem.train);
		}		
		if (activeTimetableItem.platform) {	//remove previously chosen platform from list of available trains
			GameManager.instance.platforms.ExhaustOption (activeTimetableItem.platform);
		}
		ReturnToDefaultOptionsMenu (itemModificationMenu);
	}
	public void OnClick_ConfirmModifiedItem() {
		if (modification_platformDropdown.options.Count > 0) {	//ignore this dropdown if it didnt have anything in it
			GameManager.instance.AssignPlatformToTimetableItem (modification_platformDropdown.value,activeTimetableItem);	//Reference the platform selected by player N.B this is premised upon the dropdown options being populated by Model lists (i.e.trains,platforms) above meaning indexes of dropdown/Model will be identical
		}
		if (modification_trainDropdown.options.Count > 0) {		//ignore this dropdown if it didnt have anything in it
			GameManager.instance.AssignTrainToTimetableItem (modification_trainDropdown.value,activeTimetableItem);	//Reference the train selected by player N.B this is premised upon the dropdown options being populated by Model lists (i.e.trains,platforms) above meaning indexes of dropdown/Model will be identical
		}
		TimetableItemUIObject timetableItemUIObject;
		timetableUITracker.TryGetValueBySecond (activeTimetableItem, out timetableItemUIObject);
		if (activeTimetableItem.platform != null) {	//if a platform was selected by the player (
			timetableItemUIObject.platformText.text = activeTimetableItem.platform.platformNumber.ToString ();
		}
		if (activeTimetableItem.train != null) {	//if a train was selected by the player
			timetableItemUIObject.trainText.text = activeTimetableItem.train.trainSerialID;
		}
		ReturnToDefaultOptionsMenu (itemModificationMenu);
	}
	public void OnClick_WipeModifiedItem() {
		TimetableItemUIObject timetableItemUIObject;
		timetableUITracker.TryGetValueBySecond (activeTimetableItem, out timetableItemUIObject);
		activeTimetableItem.SetPlatform (null);
		timetableItemUIObject.platformText.text = "";
		activeTimetableItem.SetTrain (null);
		timetableItemUIObject.trainText.text = "";
		ReturnToDefaultOptionsMenu (itemModificationMenu);
	}

	//Timetable UI Item Deletion
	public void OnTrainEnterStation(TimetableItem timetableItem) {
		TimetableItemUIObject timetableItemUIObject;
		timetableUITracker.TryGetValueBySecond (timetableItem, out timetableItemUIObject);
		timetableItemUIObject.gameObject.GetComponent <Button>().interactable = false;
		//TODO: MUST DO make it so that this fades out... animation moving to left
	}
}
