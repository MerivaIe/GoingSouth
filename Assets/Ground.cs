using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour {

	void OnCollisionEnter(Collision coll) {
		Person person = coll.gameObject.GetComponent <Person> ();
		if (person) {
			person.OnHitGround ();	
		}
	}
}
