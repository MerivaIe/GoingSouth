using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class WaitingAreaTrigger : MonoBehaviour {

	private WaitingArea myWaitingArea;

	void Start() {
		myWaitingArea = GetComponentInParent <WaitingArea> ();
	}

	void OnTriggerEnter (Collider coll) {
		Person person = coll.gameObject.GetComponent <Person> ();
		if (person) {
			Vector3 freeWaitLocation = myWaitingArea.RegisterPerson (person);
			person.OnWaitingAreaEnter (freeWaitLocation);
		}
	}

	void OnTriggerExit (Collider coll) {
		Person person = coll.gameObject.GetComponent <Person> ();
		if (person) {
			myWaitingArea.UnregisterPerson (person);
			person.OnWaitingAreaExit ();
		}
	}
}
