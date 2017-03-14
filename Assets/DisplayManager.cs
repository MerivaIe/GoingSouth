using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DisplayManager : MonoBehaviour {

	public GameObject timetableItemsParent;
	public GameObject timetableItemPrefab; 

	private List<TrainTracker> trainTrackers = new List<TrainTracker>();

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

	void Start () {
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
		InvokeRepeating ("UpdateJourneyTrackingSliders",0f,1f);
	}
	
	void UpdateJourneyTrackingSliders() {
		foreach (TrainTracker trainTracker in trainTrackers) {
			trainTracker.slider.value = trainTracker.train.GetJourneyProgress ();
		}
	}

	public void OnClick_NewTimetableItem() {
		//pass the message back to the GameManager to add an item to the model
		//once GameManager has confirmed that eveyrthing is ok then make a new display item
		GameObject timetableItemGO = Instantiate (timetableItemPrefab,timetableItemsParent.transform) as GameObject;

	}

	private struct TrainTracker {
		public Train train;
		public Slider slider;

		public TrainTracker(Train _train, Slider _slider) {
			train = _train;
			slider = _slider;
		}
	}
}
