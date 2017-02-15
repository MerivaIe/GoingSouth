using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersonTest : MonoBehaviour {

	private static GameObject target1, target2, myTarget;
	private Rigidbody rb;
	private float myForce = 2f;
	private bool pushPlease = true;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		if (!target1) {
			target1 = GameObject.Find ("Target (1)");
		}
		if (!target2) {
			target2 = GameObject.Find ("Target (2)");
		}
		myTarget = (Random.value > 0.5f) ? target1 : target2;
	}
	
	void FixedUpdate () {
		if (pushPlease) {
			rb.AddForce (myForce * (myTarget.transform.position - transform.position));
		}
	}

	void OnTriggerEnter(Collider coll) {
		if (coll.gameObject.GetComponent <Train> ()) {
			pushPlease = false;
		}
	}
}
