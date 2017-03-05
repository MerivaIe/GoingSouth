using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorTrigger : MonoBehaviour {

	void OnTriggerEnter(Collider coll) {	
		Person person = coll.gameObject.GetComponent <Person> ();
		if (person) {
			person.OnDoorEnter (coll.bounds.center);	//handles entry from outside or inside train
		}
	}

	void OnTriggerExit(Collider coll) {
		Person person = coll.gameObject.GetComponent <Person> ();
		if (person && person.insideTrain) {
			person.OnDoorExitIntoTrain ();				//handles just exit into train
		}
	}
}
