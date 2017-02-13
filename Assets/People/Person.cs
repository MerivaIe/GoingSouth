
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Person : MonoBehaviour {

	/// <summary>
	/// MovingToPlatform: nmAgent control to centre of platform | WrongPlatform: nmAgent control to waiting area | MovingToDoor: nmAgent control to one door location | Boarding: physics control avoidance of others
	/// </summary>
	public enum PersonStatus{MovingToPlatform,WrongPlatform,MovingToDoor,ReadyToBoard,Boarding,Alighted,Compromised}
	public PersonStatus status; 
	public string destination{ get; private set; }
	public Platform currentPlatform;
	public float jostlingStrength = 0.25f, centreOfMassYOffset = -0.5f, checkProximityEvery = 1f, sqrMagDeathVelocity = 1f;

	private NavMeshAgent nmAgent;
	private Rigidbody rb;
	private Vector3[] proximityDirections = new Vector3[8];
	private Vector3 platformTarget;
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
		//if (currentPlatform && currentPlatform.incomingTrain.status == Train.TrainStatus.BoardingTime && status == PersonStatus.MovingToDoor) { //if train is at platform
		if (status == PersonStatus.ReadyToBoard && currentPlatform.incomingTrain.status != Train.TrainStatus.BoardingTime) {
			rb.velocity = jostlingStrength * (platformTarget - transform.position).normalized;	//nudge in direction of platformTarget
		} else if (status == PersonStatus.MovingToDoor) {
			if (nmAgent.hasPath) {
				if (nmAgent.remainingDistance <= currentPlatform.GetNavTargetThreshold()) {	//as more people enter platform then easier to hit target
					//proximityDistance = currentPlatform.incomingTrain.length;
					ToggleAgentControl (false);
					status = PersonStatus.ReadyToBoard;	//may want a better way of checking to confirm boarded
				}
			} else {
				Debug.LogError ("NavMeshAgent has no path: slipped through cracks?");
			}
			//this code handles manual control of people finding space
//		} else if (status == PersonStatus.ReadyToBoard && Time.time > nextProximityCheck) {
//			nextProximityCheck = Time.time + checkProximityEvery;
//			Vector3 newVelocity = rb.velocity, proximityCorrection = Vector3.zero;
//			RaycastHit hit;
//			float longestRayLength = 0f;
//			foreach(Vector3 direction in proximityDirections) {
//				Physics.Raycast (transform.position, direction, out hit, proximityDistance);
//				if (hit.distance < proximityDistance && hit.distance > longestRayLength) {
//					longestRayLength = hit.distance;
//					proximityCorrection = direction.normalized;
//					Debug.DrawRay (transform.position,direction.normalized * hit.distance,Color.green,0.1f);
//				}
//			}
//			newVelocity += proximityCorrection * jostlingStrength;
//			rb.velocity = newVelocity;
//		} else {
//			return;
		}
	}


	void OnTriggerEnter(Collider coll) {
		if (status != PersonStatus.Compromised) { //if not compromised
			Train train = coll.gameObject.GetComponentInParent <Train> ();
			if (train && train.gameObject.GetComponent <Rigidbody> ().velocity.sqrMagnitude > sqrMagDeathVelocity) {
				DeathKnell ();
			} else {
				Platform platform = coll.gameObject.GetComponent<Platform> ();
				if (platform && status == PersonStatus.MovingToPlatform) {
					RegisterWithPlatform (platform);
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

	void RegisterWithPlatform (Platform platform)
	{
		currentPlatform = platform;
		if (destination == platform.nextDeparture) {
			status = PersonStatus.MovingToDoor;
			platform.RegisterPerson (this);
			platformTarget = platform.GetRandomWaitLocation ();
			nmAgent.SetDestination (platformTarget);
		}
		else {
			status = PersonStatus.WrongPlatform;
			//send nmAgent back to waiting area or something?
		}
	}
		
	public void ToggleAgentControl(bool isTurnOn) {
		rb.isKinematic = isTurnOn;
		nmAgent.enabled = isTurnOn;
	}
}
