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
		if (person && person.status != Person.PersonStatus.Compromised) {
			person.OnWaitingAreaEnter (myWaitingArea);
		}
	}

	void OnTriggerExit (Collider coll) {
		Person person = coll.gameObject.GetComponent <Person> ();
		if (person && person.status != Person.PersonStatus.Compromised) {
			person.OnWaitingAreaExit (myWaitingArea);
		}
	}
}
