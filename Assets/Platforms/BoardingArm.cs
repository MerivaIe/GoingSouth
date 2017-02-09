using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardingArm : MonoBehaviour {

	public Rigidbody joinTo;
	public GameObject platform;

	private Rigidbody rb;
	private ConstantForce myConstantForce;
	private FixedJoint fixedJoint;
	private Train incomingTrain;
	private bool boarding = false;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		myConstantForce = GetComponent <ConstantForce> ();
		fixedJoint = GetComponent <FixedJoint> ();
		if (fixedJoint) {
			joinTo = fixedJoint.connectedBody;
		}
		incomingTrain = platform.GetComponent <Platform> ().incomingTrain;
	}

	void FixedUpdate() {
		if (incomingTrain && incomingTrain.status == Train.TrainStatus.BoardingTime && boarding == false) {
			myConstantForce.enabled = true;
			boarding = true;
		}
	}

	void Reset() {

	}

	void OnCollisionEnter(Collision coll) {
		if (coll.gameObject.tag == "PlatformBumper" || coll.gameObject.GetComponentInParent <Train>()) {
			rb.constraints = RigidbodyConstraints.FreezeAll;
			myConstantForce.enabled = false;
			if (fixedJoint) {
				fixedJoint.connectedBody = null;
			}
		}
	}
}
