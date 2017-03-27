using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimetableItemUIObject : MonoBehaviour {

	//to be assigned at design time in the prefab
	public GameObject timeLabel,destinationLabel,platformLabel,trainLabel;
	//these are then assigned at runtime and are exposed for access
	public Text timeText {get; private set;}
	public Text destinationText {get; private set;}
	public Text platformText {get; private set;}
	public Text trainText { get; private set; }

	void Awake() {	//awake is required so this occurs before these components need to be used in calling class just after instantiation of this object
		timeText = timeLabel.GetComponent <Text> ();
		destinationText = destinationLabel.GetComponent <Text> ();
		platformText = platformLabel.GetComponent <Text> ();
		trainText = trainLabel.GetComponent <Text> ();
	}

	public void OnClick_TimetableItemUIObject(){	//at the moment the only state that this can happen in is when items are in timetable list and can be modified...
		GameUIManager.instance.OnClick_TimetableItemForModification (this);
	}
}
