
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Person : MonoBehaviour {

	public enum PersonStatus{ReadyToBoard,Boarded,Moving,Alighted,WrongPlatform}
	public PersonStatus status; 
	public string destination{ get; private set; }
	public Platform currentPlatform;
	public float navTargetThreshold = 0.1f, jostlingStrength = 0.25f, centreOfMassYOffset = -0.5f, proximityDistance = 4f, checkProximityEvery = 1f, randomProximityMod = 0.1f;

	private NavMeshAgent nmAgent;
	private Rigidbody rb;
	private Vector3[] proximityDirections = new Vector3[8];
	private float nextProximityCheck;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		nmAgent = GetComponent <NavMeshAgent>();
		destination = "Bristol";
		rb.centerOfMass = new Vector3(0f,centreOfMassYOffset,0f);
		for (int i=0;i<proximityDirections.Length;i++) {
			//using square wave function to calculate 8 directions at compass points
			proximityDirections [i] = new Vector3 (MyUtility.Sign(Mathf.Cos (i*Mathf.PI/4f)),0f,MyUtility.Sign(Mathf.Sin (i*Mathf.PI/4f)));
		}
	}

	void FixedUpdate() {
		Vector3 newVelocity = rb.velocity;
		if (currentPlatform) {		
			if (status == PersonStatus.ReadyToBoard && currentPlatform.incomingTrain.status == Train.TrainStatus.BoardingTime) { //if train is at platform
				if (!nmAgent.hasPath) {
					Debug.LogError ("NavMeshAgent has no path: slipped through cracks?");
				} else if (nmAgent.remainingDistance <= navTargetThreshold) {
					ToggleAgentControl (false);
					newVelocity = Vector3.forward * nmAgent.speed;
					status = PersonStatus.Boarded;											//may want a better way of checking to confirm boarded
				}
			} else {return;}
		} else if (!rb.isKinematic && status == PersonStatus.Boarded && Time.time > nextProximityCheck) {
			nextProximityCheck = Time.time + checkProximityEvery;
			Vector3 proximityCorrection = Vector3.zero;

			foreach(Vector3 direction in proximityDirections) {
				if (!Physics.Raycast (transform.position, direction, proximityDistance)) {	//if we don't hit something in this direction
					proximityCorrection = (proximityCorrection + direction)/2;				//average it with the accumulating proximityCorrection we are building up
					Debug.DrawRay (transform.position,direction.normalized*proximityDistance,Color.green,0.1f);
				}
			}
			proximityCorrection.x += Random.Range (-randomProximityMod, randomProximityMod);
			proximityCorrection.z += Random.Range (0f, randomProximityMod);
			newVelocity += jostlingStrength * proximityCorrection;

			//Use the following for random movement if anything in overlap
//			Collider[] hitColliders = Physics.OverlapSphere (transform.position, 1f);
//			foreach (Collider hitCollider in hitColliders) {
//				if (hitCollider.gameObject != gameObject) {
//					newVelocity += new Vector3 (Random.Range(-jostlingStrength,jostlingStrength),0f,Random.Range(-jostlingStrength,jostlingStrength));
//				}
//			}
		} else {return;}

		rb.velocity = newVelocity;
	}

	public void ToggleAgentControl(bool isTurnOn) {
			rb.isKinematic = isTurnOn;
			nmAgent.enabled = isTurnOn;
	}

	void OnCollisionEnter(Collision coll) {
		//TODO: create a trigger box just ahead of the collider and the FIRST time a person enters it turn their kinemtic and nmagent off ready for beautiful physics interactions
		if (coll.gameObject.GetComponentInParent <Train> ()) {					//hit by train (trigger) so turn off kinematic
			//TogglePhysicsControl (false);
			Debug.Log ("Train script found in parent object. Turning person to ragdoll");
		}
	}

	void OnTriggerEnter(Collider coll) {
		Platform platform = coll.gameObject.GetComponent <Platform> ();
		if (platform) {
			currentPlatform = platform;
			if (platform.nextDeparture == destination) {
				status = PersonStatus.ReadyToBoard;
				ToggleAgentControl (true);
				nmAgent.SetDestination (platform.GetRandomWaitLocation ());
			} else {
				status = PersonStatus.WrongPlatform;
			}
		}
	}

	void OnTriggerExit(Collider coll) {
		Platform platform = coll.gameObject.GetComponent <Platform> ();
		if (platform) {
			currentPlatform = null;
		}
	}
}
