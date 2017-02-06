using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Person : MonoBehaviour {

	public enum Status{ReadyToBoard,Boarded,Moving,Alighted}
	public Status status; 
	public string destination{ get; private set; }
	public Platform currentPlatform;
	public float throwBodyForce = 10f;
	public float targetThreshold = 0.1f;

	private NavMeshAgent nmAgent;
	private Rigidbody rb;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		nmAgent = GetComponent <NavMeshAgent>();
		destination = "Bristol";
	}

	void FixedUpdate() {
		if (currentPlatform) {
			if (currentPlatform.incomingTrain.travelDirection == Train.TravelDirection.BoardingTime && status == Status.ReadyToBoard && nmAgent.remainingDistance <=targetThreshold) {
				status = Status.Boarded;
				TogglePhysicsControl ();
				rb.velocity = Vector3.forward * throwBodyForce;
			}
		}
	}

	void TogglePhysicsControl() {
		rb.isKinematic = !rb.isKinematic;
		nmAgent.enabled = !nmAgent.enabled;
	}

	void OnCollisionEnter(Collision coll) {
		Debug.Log ("Person collided with " + coll.gameObject.name);
		//TODO: create a trigger box just ahead of the collider and the FIRST time a person enters it turn their kinemtic and nmagent off ready for beautiful physics interactions
		if (coll.gameObject.GetComponentInParent <Train> ()) {								//hit by train (trigger) so turn off kinematic
			//TogglePhysicsControl ();
			Debug.Log ("Train script found in parent object. Turning person to ragdoll");
		}
	}


}
