using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Train controller. The position of this collider+script object ahead of the train will determine reaction time to upcoming signals
/// </summary>
[RequireComponent (typeof(BoxCollider))]
public class TrainController : MonoBehaviour {

	private Train train;

	void Start() {
		train = GetComponentInParent <Train> ();
	}

	void OnTriggerEnter(Collider coll) {
		Signal signal = coll.GetComponent <Signal>();
		if (signal) {
			if (signal.signalType == Signal.SignalType.Brake) {
				float signalEndX = coll.bounds.max.x;	//TODO this will be min different if the train is going the other way! Could just sample which is furthest away
				train.SetBraking (signalEndX);
			}
		}
	}
}
