using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformTrigger : MonoBehaviour {

	private Platform myPlatform;

	void Start() {
		myPlatform = GetComponentInParent <Platform> ();
	}

	void OnTriggerEnter (Collider coll) {
		Person person = coll.gameObject.GetComponent <Person> ();
		if (person) {
			Vector3 freeWaitLocation = myPlatform.RegisterPerson (person);
			person.OnPlatformEnter (myPlatform, freeWaitLocation);
		}
	}

	void OnTriggerExit (Collider coll) {
		Person person = coll.gameObject.GetComponent <Person> ();
		if (person) {
			myPlatform.UnregisterPerson (person);
			person.OnPlatformExit ();
		}
	}
}
