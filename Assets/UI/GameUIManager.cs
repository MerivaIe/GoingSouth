﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GameUIManager : MonoBehaviour {

	public GameObject timetableItemsParent;
	public GameObject timetableItemPrefab;
	public GameObject trainTrackerParent;
	public GameObject trainTrackerPrefab;
	[Range(0.02f,1f)]
	public float displayUpdateInterval = 0.5f;
	public Sprite trainUISprite;

	private BiLookup<TimetableItemUIObject,TimetableItem> timetableUITracker;
	private Dictionary<Train,TrainTrackerUIObject> trainUITracker;
	private GameObject defaultOptionsMenu, itemCreationMenu, itemModificationMenu;
	private Dropdown creation_destinationDropdown, modification_trainDropdown, modification_platformDropdown;
	private Text clockText,creation_schedDepartureTimeText, modification_schedDepartureTimeText, modification_DestinationText;
	private Slider approvalSlider;
	private TimetableItem activeTimetableItem;	//only one timetableitem is created/modified at a time so we store a reference to it using this
	private Dropdown.OptionData emptyTrainOption,emptyPlatformOption;

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
		timetableUITracker = new BiLookup<TimetableItemUIObject, TimetableItem> ();
		trainUITracker = new Dictionary<Train,TrainTrackerUIObject> ();	

		if (timetableItemPrefab == null || timetableItemsParent == null || trainTrackerParent == null || trainTrackerPrefab == null) {
			Debug.LogWarning ("No timetable/train tracker item prefab and/or the parent object for them assigned.");
		}

		if (GameManager.instance.trainPool.AvailableOptions == null || GameManager.instance.trainPool.AvailableOptions.Count == 0) {
			Debug.LogWarning ("GameUIManager is trying to access GameManager's train pool but it is not initialised or empty.");
		} else {
			foreach (Train train in GameManager.instance.trainPool.AvailableOptions) {
				GameObject trainTracker = Instantiate (trainTrackerPrefab, trainTrackerParent.transform) as GameObject;
				TrainTrackerUIObject trainTrackerUIObject = trainTracker.GetComponent <TrainTrackerUIObject> ();
				trainUITracker.Add (train,trainTrackerUIObject);
				trainTrackerUIObject.trainIDText.text = train.trainSerialID;
				trainTrackerUIObject.statusText.text = "Ready to approach...";
			}
		}

		emptyTrainOption = new Dropdown.OptionData ("None",trainUISprite);
		emptyPlatformOption = new Dropdown.OptionData ("Platform -");

		clockText = MyFindUIObjectWithTag ("UI_ClockText").GetComponent <Text> ();
		approvalSlider = MyFindUIObjectWithTag ("UI_ApprovalSlider").GetComponent <Slider> ();
		defaultOptionsMenu = MyFindUIObjectWithTag ("UI_DefaultOptionsMenu");	//note: must have all of the menus activated initially to find their elements, then deactivate them later
		itemCreationMenu = MyFindUIObjectWithTag ("UI_ItemCreationMenu");
		itemModificationMenu = MyFindUIObjectWithTag ("UI_ItemModificationMenu");
		
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

		InvokeRepeating ("UpdateDisplayAtInterval",0f,displayUpdateInterval);		//start a repeating invoke that updates the display at interval for those UI items that don't need update per frame
	}

	void Update() {
		foreach (KeyValuePair<Train,TrainTrackerUIObject> trainUIPair in trainUITracker) {
			trainUIPair.Value.slider.value = trainUIPair.Key.GetJourneyProgress ();
		}
	}
		
	void UpdateDisplayAtInterval() {	//called every displayUpdateInterval seconds
		clockText.text = ConvertGameTimeToHHMM (GameManager.instance.GetCurrentGameTime());
		if (Person.peopleCount > 0) {approvalSlider.value = Person.totalApprovalRating / Person.peopleCount;}
	}

	GameObject MyFindUIObjectWithTag(string searchForTag) {
		GameObject foundGameObject = GameObject.FindGameObjectWithTag (searchForTag);
		if (foundGameObject == null) {
			Debug.LogWarning ("Could not find UI game object with tag: " + searchForTag + ". Display will not function as expected. Ensure all UI objects are active at game start.");
		}
		return foundGameObject;
	}

	public static string ConvertGameTimeToHHMM(float gameTime) {
		return string.Format("{0:#00}:{1:00}", Mathf.Floor(gameTime/60),Mathf.Floor(gameTime) % 60);
	}

	public void UpdateTrainStatus(Train train,string statusText) {
		TrainTrackerUIObject trainTrackerUIObject;
		if (trainUITracker.TryGetValue (train, out trainTrackerUIObject)) {
			trainTrackerUIObject.statusText.text = statusText;
		}
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
		//get the timetable item [Model] for this particular timetable UI object and store in the activeTimetable ref. NOTE: this is required so that Confirm Modified Item can use this ref. later
		if (timetableUITracker.TryGetValueByFirst (timetableUIObject, out activeTimetableItem)) {
			defaultOptionsMenu.SetActive (false);
			activeTimetableItem.modificationFlag = true;
			timetableItemsParent.GetComponent <CanvasGroup> ().interactable = false;	//set all timetable UI items so they cannot be modified
			//populate the 4 modification menu items with the values from this timetable item that is being modified
			modification_schedDepartureTimeText.text = ConvertGameTimeToHHMM (activeTimetableItem.scheduledDepartureTime);
			modification_DestinationText.text = activeTimetableItem.destination.name;

			int indexOfPreviouslySelected = -1;
			if (activeTimetableItem.train != null) {	//if the item selected for mods had a train already chosen previously then restore it to available options
				GameManager.instance.trainPool.RestoreItem (activeTimetableItem.train.trainSerialID, activeTimetableItem.train);
			}
			modification_trainDropdown.ClearOptions ();
			List<Dropdown.OptionData> dropdownOptions = new List<Dropdown.OptionData> ();
			dropdownOptions.Add (emptyTrainOption);	//an empty option at start
			foreach (Train train in GameManager.instance.trainPool.AvailableOptions) {
				dropdownOptions.Add (new Dropdown.OptionData(train.trainSerialID,trainUISprite));
				if (activeTimetableItem.train != null && activeTimetableItem.train.trainSerialID == train.trainSerialID) {
					indexOfPreviouslySelected = dropdownOptions.Count - 1;
				}
			}
			modification_trainDropdown.AddOptions (dropdownOptions);	
			modification_trainDropdown.value = (activeTimetableItem.train == null)? 0 : indexOfPreviouslySelected;	//if the item selected for mods had a train already chosen previously then select it as the dropdown option, otherwise the empty option

			indexOfPreviouslySelected = -1;
			if (activeTimetableItem.platform != null) {	//if the item selected for mods had a platform already chosen previously then restore it to available options and select it in the dropdown
				GameManager.instance.platforms.RestoreItem (activeTimetableItem.platform.platformNumber.ToString (),activeTimetableItem.platform);
			}
			modification_platformDropdown.ClearOptions ();
			dropdownOptions.Clear (); //used for trains prior to this
			dropdownOptions.Add (emptyPlatformOption);	//an empty option at start
			foreach (Platform platform in GameManager.instance.platforms.AvailableOptions) {
				dropdownOptions.Add (new Dropdown.OptionData("Platform " + platform.platformNumber.ToString ()));
				if (activeTimetableItem.platform != null && activeTimetableItem.platform.platformNumber == platform.platformNumber) {
					indexOfPreviouslySelected = dropdownOptions.Count - 1;
				}
			}
			modification_platformDropdown.AddOptions (dropdownOptions);
			modification_platformDropdown.value = (activeTimetableItem.platform == null)? 0 : indexOfPreviouslySelected;	//if the item selected for mods had a platform already chosen previously then select it in the dropdown, otherwise the empty option
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
		activeTimetableItem.modificationFlag = false;
		//below is premised on the fact that activeTimetableItem will hold the timetable item as it was when it was first selected for modification (see OnClick_TimetableItemForModification)
		if (activeTimetableItem.train) {	//remove previously chosen train from list of available trains
			GameManager.instance.trainPool.UseItem(activeTimetableItem.train.trainSerialID);
		}		
		if (activeTimetableItem.platform) {	//remove previously chosen platform from list of available trains
			GameManager.instance.platforms.UseItem (activeTimetableItem.platform.platformNumber.ToString ());
		}
		ReturnToDefaultOptionsMenu (itemModificationMenu);
	}
	public void OnClick_ConfirmModifiedItem() {
		activeTimetableItem.modificationFlag = false;
		TimetableItemUIObject timetableItemUIObject;
		timetableUITracker.TryGetValueBySecond (activeTimetableItem, out timetableItemUIObject);

		if (modification_platformDropdown.value == 0) {	//if blank option selected
			timetableItemUIObject.platformText.text = "";
			if (activeTimetableItem.platform) {	 		//and previously a platform was selected (premised on the fact that activeTimetableItem will hold the timetable item as it was when it was first selected for modification (see OnClick_TimetableItemForModification))
				GameManager.instance.RemovePlatformFromTimetableItem (activeTimetableItem);
			}
		} else if (modification_platformDropdown.options.Count > 1) {	//ignore this dropdown if it didnt have anything in it, otherwise something was selected
			GameManager.instance.AssignPlatformToTimetableItem (modification_platformDropdown.captionText.text.Substring (9),activeTimetableItem);	//Reference the platform selected by player
			timetableItemUIObject.platformText.text = activeTimetableItem.platform.platformNumber.ToString ();
		}
		if (modification_trainDropdown.value == 0) {	//if blank option selected
			timetableItemUIObject.trainText.text = "";
			if (activeTimetableItem.train) {	 		//and previously a train was selected (premised on the fact that activeTimetableItem will hold the timetable item as it was when it was first selected for modification (see OnClick_TimetableItemForModification))
				GameManager.instance.RemoveTrainFromTimetableItem (activeTimetableItem);
			}
		} else if (modification_trainDropdown.options.Count > 1) {	//ignore this dropdown if it didnt have anything in it, otherwise something was selected
			GameManager.instance.AssignTrainToTimetableItem (modification_trainDropdown.captionText.text,activeTimetableItem);			//Reference the train selected by player
			timetableItemUIObject.trainText.text = activeTimetableItem.train.trainSerialID;
		}
		ReturnToDefaultOptionsMenu (itemModificationMenu);
	}

	//Timetable UI Item Deletion/FadeOut
	public void OnTrainEnterStation(TimetableItem timetableItem) {
		TimetableItemUIObject timetableItemUIObject;
		timetableUITracker.TryGetValueBySecond (timetableItem, out timetableItemUIObject);
		if (timetableItemUIObject) {
			Button button =	timetableItemUIObject.GetComponent <Button> ();
			button.interactable = false;
		}
	}
	public void OnTrainOutOfStation(TimetableItem timetableItem) {
		TimetableItemUIObject timetableItemUIObject;
		timetableUITracker.TryGetValueBySecond (timetableItem, out timetableItemUIObject);
		if (timetableItemUIObject) {
			Animator animator = timetableItemUIObject.GetComponent <Animator> ();
			animator.SetTrigger ("FadeOutTrigger");
			Destroy (timetableItemUIObject.gameObject,animator.GetCurrentAnimatorStateInfo (0).length);
		}
	}
}
