using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardingAssistant : MonoBehaviour {

	public Rigidbody joinTo;

	private Rigidbody rb;
	private ConstantForce myConstantForce;
	private FixedJoint fixedJoint;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		myConstantForce = GetComponent <ConstantForce> ();
		fixedJoint = GetComponent <FixedJoint> ();
		if (fixedJoint) {
			joinTo = fixedJoint.connectedBody;
		}
	}

	void OnCollisionEnter(Collision coll) {
		if (coll.gameObject.GetComponentInParent <Train> ()) {
			rb.constraints = RigidbodyConstraints.FreezeAll;
			myConstantForce.enabled = false;
			if (fixedJoint) {
				fixedJoint.connectedBody = null;
			}
		}
	}
}
