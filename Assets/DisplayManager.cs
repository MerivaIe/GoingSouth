using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DisplayManager : MonoBehaviour {	//should this be static?

	private List<TrainTracker> trainTrackers = new List<TrainTracker>();

	void Start () {
		Slider[] sliders = GameObject.FindObjectsOfType <Slider> ();	//use this for now just to get sliders, but eventually you should create sliders from a prefab.. one for each train
		if (GameManager.trainPool == null || GameManager.trainPool.Count == 0) {
			Debug.LogWarning ("DisplayManager is trying to access GameManager's train pool but it is not initialised or empty.");
		} else {
			int i = 0;
			foreach (Train train in GameManager.trainPool) {
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

	private struct TrainTracker {
		public Train train;
		public Slider slider;

		public TrainTracker(Train _train, Slider _slider) {
			train = _train;
			slider = _slider;
		}
	}
}
