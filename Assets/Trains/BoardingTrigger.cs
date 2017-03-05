using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardingTrigger : MonoBehaviour {

	private Train myTrain;

	void Start() {
		myTrain = GetComponentInParent <Train> ();
	}

	void OnTriggerEnter(Collider coll) {
		Person person = coll.GetComponent <Person> ();
		if (person) {
			person.OnTrainEnter ();
			myTrain.RegisterPerson (person);
		}
	}

	void OnTriggerExit(Collider coll) {
		Person person = coll.GetComponent <Person> ();
		if (person) {
			person.OnTrainExit ();
			myTrain.UnregisterPerson (person);
		}
	}
}
