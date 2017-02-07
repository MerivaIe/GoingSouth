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
	public float navTargetThreshold = 0.1f;
	public float standingThreshold = 15f;

	private NavMeshAgent nmAgent;
	private Rigidbody rb;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		nmAgent = GetComponent <NavMeshAgent>();
		destination = "Bristol";
		rb.centerOfMass = new Vector3(0f,-0.5f,0f);

	}

	void FixedUpdate() {
		Vector3 newVelocity = rb.velocity;
		if (currentPlatform) {
			if (currentPlatform.incomingTrain.travelDirection == Train.TravelDirection.BoardingTime && status == Status.ReadyToBoard && nmAgent.remainingDistance <=navTargetThreshold) {
				status = Status.Boarded;		//may want a better way of checking to confirm boarded
				ToggleAgentControl (false);
				newVelocity = Vector3.forward * throwBodyForce;
			}
		} else if (!rb.isKinematic && status == Status.Boarded) {
			//			RaycastHit hit;
			//			Ray myRay = new Ray (transform.position,new Vector3(Random.Range(-1f,1f),0f,Random.Range(-1f,1f)));
			//			Physics.Raycast (myRay, out hit,5f);

			Collider[] hitColliders = Physics.OverlapSphere (transform.position, 1f);
			foreach (Collider hitCollider in hitColliders) {
				if (hitCollider.gameObject != gameObject) {
					newVelocity += new Vector3 (Random.Range(-1f,1f),0f,Random.Range(-1f,1f));
				}
			}
		} else {
			return;
		}

		rb.velocity = newVelocity;
	}

	public void ToggleAgentControl(bool isTurnOn) {
			rb.isKinematic = isTurnOn;
			nmAgent.enabled = isTurnOn;
	}

	void OnCollisionEnter(Collision coll) {
		Debug.Log ("Person collided with " + coll.gameObject.name);
		//TODO: create a trigger box just ahead of the collider and the FIRST time a person enters it turn their kinemtic and nmagent off ready for beautiful physics interactions
		if (coll.gameObject.GetComponentInParent <Train> ()) {					//hit by train (trigger) so turn off kinematic
			//TogglePhysicsControl (false);
			Debug.Log ("Train script found in parent object. Turning person to ragdoll");
		}
	}
}
