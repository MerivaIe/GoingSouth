using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardingArm : MonoBehaviour {

	private Rigidbody joinTo;
	private Platform platform;
	private ConstantForce myConstantForce;
	private FixedJoint outboundFJ, inboundFJ;
	private Train incomingTrain;
	private bool boarding = false;
	private BoardingArm joinedToBoardingArm;
	private Vector3 originalPos;

	void Start () {
		originalPos = transform.position;
		myConstantForce = GetComponent <ConstantForce> ();
		outboundFJ = GetComponent <FixedJoint> ();
		if (outboundFJ) {
			joinTo = outboundFJ.connectedBody;											//get the initial RB that this is connected to
			joinedToBoardingArm = joinTo.gameObject.GetComponent <BoardingArm>();
			if (joinedToBoardingArm) {joinedToBoardingArm.SetJoinedTo (outboundFJ);}	//let that GO know we are connected to it						
		}
		platform = GetComponentInParent <Platform> ();
	}

	void FixedUpdate() {
		incomingTrain = platform.incomingTrain;
		if (incomingTrain && incomingTrain.status == Train.TrainStatus.BoardingTime && boarding == false) {
			myConstantForce.enabled = true;
			boarding = true;
			//TODO the following should be done once something has decided train is ready to depart... Platform? After wait time.
		} else if (incomingTrain && incomingTrain.status == Train.TrainStatus.Moving && boarding == true) {
			Reset ();
		}
	}

	void Reset() {
		myConstantForce.enabled = false;
		boarding = false;
		transform.position = originalPos;
		if (!outboundFJ) {	//if fixed joint was destroyed recreate it
			outboundFJ = gameObject.AddComponent <FixedJoint> ();
			outboundFJ.connectedBody = joinTo;
		}
	}

	void OnCollisionEnter(Collision coll) {
		if (coll.gameObject.GetComponentInParent <Train>()) {
			Component.Destroy (outboundFJ); //if this GO is attached to something disconnect it
			Component.Destroy (inboundFJ);	//if the GO attached to this one is connected then disconnect it
		}
	}

	public void SetJoinedTo(FixedJoint theirFixedJoint) {
		inboundFJ = theirFixedJoint;
	}
}
