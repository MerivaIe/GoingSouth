
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Person : MonoBehaviour {

	public enum PersonStatus{ReadyToBoard,Boarding,MovingToPlatform,Alighted,WrongPlatform,Compromised}
	public PersonStatus status; 
	public string destination{ get; private set; }
	public Platform currentPlatform;
	public float navTargetThreshold = 0.1f, jostlingStrength = 0.25f, centreOfMassYOffset = -0.5f, checkProximityEvery = 1f, sqrMagDeathVelocity = 1f, boardingSpeed = 10f;

	private NavMeshAgent nmAgent;
	private Rigidbody rb;
	private Vector3[] proximityDirections = new Vector3[8];
	private float nextProximityCheck, proximityDistance;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		nmAgent = GetComponent <NavMeshAgent>();
		destination = "Bristol";
		rb.centerOfMass = new Vector3(0f,centreOfMassYOffset,0f);
		for (int i=0;i<proximityDirections.Length;i++) {
			proximityDirections [i] = new Vector3 (Mathf.Cos (i*Mathf.PI/4f),0f,Mathf.Sin (i*Mathf.PI/4f));		//using square wave function to calculate 8 directions at compass points
		}
	}

	void FixedUpdate() {
		Vector3 newVelocity = rb.velocity;
		if (currentPlatform && currentPlatform.incomingTrain.status == Train.TrainStatus.BoardingTime && status == PersonStatus.ReadyToBoard) { //if train is at platform
			if (!nmAgent.hasPath) {
				Debug.LogError ("NavMeshAgent has no path: slipped through cracks?");
			} else if (nmAgent.remainingDistance <= navTargetThreshold) {
				ToggleAgentControl (false);
				//newVelocity = Vector3.forward * boardingSpeed;	//removed this because of boarding pressure now
				proximityDistance = currentPlatform.incomingTrain.length;
				status = PersonStatus.Boarding;											//may want a better way of checking to confirm boarded
			}
		} else if (!rb.isKinematic && status == PersonStatus.Boarding && Time.time > nextProximityCheck) {
			nextProximityCheck = Time.time + checkProximityEvery;
			Vector3 proximityCorrection = Vector3.zero;
			RaycastHit hit;
			float longestRayLength = 0f;
			foreach(Vector3 direction in proximityDirections) {
				Physics.Raycast (transform.position, direction, out hit, proximityDistance);
				if (hit.distance < proximityDistance && hit.distance > longestRayLength) {
					longestRayLength = hit.distance;
					proximityCorrection = direction.normalized;
					Debug.DrawRay (transform.position,direction.normalized * hit.distance,Color.green,0.1f);
				}
			}

			newVelocity += proximityCorrection * jostlingStrength;

			//Use the following for random movement if anything in overlap
			//newVelocity += new Vector3 (Random.Range(-jostlingStrength,jostlingStrength),0f,Random.Range(-jostlingStrength,jostlingStrength));
		} else {return;}
		rb.velocity = newVelocity;
	}

	public void ToggleAgentControl(bool isTurnOn) {
		rb.isKinematic = isTurnOn;
		nmAgent.enabled = isTurnOn;
	}

	void OnTriggerEnter(Collider coll) {
		if (status != PersonStatus.Compromised) { //if not compromised
			Train train = coll.gameObject.GetComponentInParent <Train> ();
			if (train && train.gameObject.GetComponent <Rigidbody> ().velocity.sqrMagnitude > sqrMagDeathVelocity) {
				DeathKnell ();
				//rb.AddExplosionForce (100f,coll.bounds.max,1f);
			} else {
				Platform platform = coll.gameObject.GetComponent <Platform> ();
				if (platform) {
					if (status == PersonStatus.MovingToPlatform) {
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
			}
		}
	}
		
	//TODO perhaps some OnTriggerExit stuff when leaving platform... make this all abit simpler

	void DeathKnell() {
		status = PersonStatus.Compromised;
		ToggleAgentControl (false);	//turn their kinematic and nmagent off ready for beautiful physics interactions
		rb.ResetCenterOfMass ();
		Debug.Log ("Train or compromised person has hit me. Turning into ragdoll");
	}
}
