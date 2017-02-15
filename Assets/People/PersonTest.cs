using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersonTest : MonoBehaviour {

	private GameObject target1, target2, myTarget;
	private Rigidbody rb;
	private float myForce = 2f;
	private bool pushPlease = true;
	private ForceMode forceMode;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		forceMode = ForceMode.Force;

		if (transform.parent.gameObject.tag == "Test1") {
			if (!target1) {
				target1 = GameObject.Find ("Target (1)");
			}
			if (!target2) {
				target2 = GameObject.Find ("Target (2)");
			}
		} else if (transform.parent.gameObject.tag == "Test2") {
			if (!target1) {
				target1 = GameObject.Find ("Target (3)");
			}
			if (!target2) {
				target2 = GameObject.Find ("Target (4)");
			}
		}

		GameObject shortestRoute;
		if ((target1.transform.position - transform.position).magnitude > (target2.transform.position - transform.position).magnitude) {
			shortestRoute = target2;
		} else {
			shortestRoute = target1;
		}
		myTarget = shortestRoute;

	}
	
	void FixedUpdate () {
		if (pushPlease) {
			rb.AddForce (myForce * (myTarget.transform.position - transform.position), forceMode);
		}
	}

	void OnTriggerEnter(Collider coll) {
		if (coll.gameObject.GetComponent <Train> ()) {
			pushPlease = false;
		}
	}
}
