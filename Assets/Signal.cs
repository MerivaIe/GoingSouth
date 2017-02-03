using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Signal : MonoBehaviour {

	public enum SignalType {Brake,Accelerate}
	public SignalType signalType;

	// Use this for initialization
	void Start () {
//		if (signalType != null) {
//			Debug.LogWarning ("No signalType set. Please set signal type.");
//		}
	}

	void OnTriggerEnter(Collider coll) {
		Debug.Log ("Generic trigger");
		Train train = coll.gameObject.GetComponentInParent <Train>();
		if (train) {
			Debug.Log ("Triggered by train" + coll.name);
			//maybe make this select case if you stick with this architecture
			if (signalType == SignalType.Brake) {
				train.Brake(transform.position.x); //TODO change so this is point on platform it needs to stop at
			}
		}
	}
}
