using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnCollisionEnter(Collision coll) {
		Person person = coll.gameObject.GetComponent <Person> ();
		if (person && person.status != Person.PersonStatus.Compromised) {
			person.SetMovingToPlatform ();	//what if they hit ground from foyer
		}
	}
}
