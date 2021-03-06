﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrainTrackerUIObject : MonoBehaviour {

	//to be assigned at design time in the prefab
	public GameObject trainIDLabel, statusLabel;
	//these are then assigned at runtime and are exposed for access
	public Text trainIDText {get; private set;}
	public Text statusText {get; private set;}
	public Slider slider {get; private set;}

	void Awake() {	//awake is required so this occurs before these components need to be used in calling class just after instantiation of this object
		trainIDText = trainIDLabel.GetComponentInChildren <Text> ();
		statusText = statusLabel.GetComponentInChildren <Text> ();
		slider = GetComponent <Slider> ();
	}

}
