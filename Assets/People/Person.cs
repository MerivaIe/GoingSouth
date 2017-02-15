
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Person : MonoBehaviour {

	public bool testMode = false;
	/// <summary>
	/// PersonStatus: MovingToPlatform=nmAgent control to centre of platform | WrongPlatform=nmAgent control to waiting area | MovingToDoor=nmAgent control to one door location | Boarding=physics control avoidance of others
	/// </summary>
	public enum PersonStatus{MovingToPlatform,WrongPlatform,ReadyToBoard,Boarding,Boarded,Alighted,Compromised}
	public PersonStatus status; 
	public string destination{ get; private set; }
	public Platform currentPlatform;
	public float  centreOfMassYOffset = -0.5f, sqrMagDeathVelocity = 1f, boardingForce = 20f, dragBase = 20f, targetThreshold = 2f;

	private NavMeshAgent nmAgent;
	private Rigidbody rb;
	private Vector3 platformTarget, trainTarget;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		nmAgent = GetComponent <NavMeshAgent>();
		destination = "Bristol";
		rb.centerOfMass = new Vector3(0f,centreOfMassYOffset,0f);
		if (testMode) {
			nmAgent.SetDestination(currentPlatform.transform.position);
		}
	}

	void FixedUpdate() {
		if (currentPlatform && currentPlatform.incomingTrain.status == Train.TrainStatus.BoardingTime) {
			switch (status) {
			case PersonStatus.ReadyToBoard:
				SetAgentControl (false);
				SetNewTarget ();
				status = PersonStatus.Boarding;
				//free up their wait space? no... I guess people arriving will board straight away [think]
				break;
			case PersonStatus.Boarding:
				Vector3 boardingVector = trainTarget - transform.position;
				if (boardingVector.magnitude < targetThreshold) {	//if within a metre of door then shift target into train
					boardingVector.z += 2f;
				}
				boardingVector.y = 0f;
				float angleDiff = Mathf.Deg2Rad * Vector3.Angle (rb.velocity, boardingVector);	// provide a bit of drag to prevent oscillation around boarding vector
				float dragModifier = Mathf.Sin (0.5f * angleDiff);
				rb.drag = dragModifier * dragBase;
				rb.AddForce (boardingForce * boardingVector.normalized, ForceMode.Acceleration);
				break;
			}
		}
		//if they have been waiting for more than 5 seconds they get a nearby location and move.
		//actually, to ensure that people can be shoved off the platform we should make it physics control trying to reach their assigned destination (and maybe add a NM Obstacle so agents know to avoid them).
	}

	void OnTriggerEnter(Collider coll) {
		if (status != PersonStatus.Compromised) {
			Train train = coll.gameObject.GetComponentInParent <Train> ();	//if this is the front of the train
			if (train && train.gameObject.GetComponent <Rigidbody> ().velocity.sqrMagnitude > sqrMagDeathVelocity) {
				status = PersonStatus.Compromised;
				DeathKnell ();
			} else if (coll.gameObject.GetComponent <Train> ()) {	//if this is the trigger inside the train carriage
				status = PersonStatus.Boarded;
				rb.constraints = RigidbodyConstraints.None;
			} else {
				Platform platform = coll.gameObject.GetComponent<Platform> ();
				if (platform && status == PersonStatus.MovingToPlatform) {
					currentPlatform = platform;
					if (destination == currentPlatform.nextDeparture) {
						status = PersonStatus.ReadyToBoard;
						RegisterWithPlatform ();
					} else {
						status = PersonStatus.WrongPlatform;
					}
				}
			}
		}
	}

	void OnTriggerExit(Collider coll) {
		Platform platform = coll.gameObject.GetComponent<Platform> ();
		if (currentPlatform && platform == currentPlatform) {
			UnregisterWithPlatform ();
		}
	}

	void SetAgentControl(bool turnOn) {
		if (rb.isKinematic != turnOn) {	//ignore request if they are already set to requested bool
			rb.isKinematic = turnOn;
			nmAgent.enabled = turnOn;
		}
	}

	void SetNewTarget() {
		float shortestRoute = (currentPlatform.targetLocations[0] - transform.position).magnitude;
		int closestTargetIndex = 0;
		for (int i=1;i<currentPlatform.targetLocations.Count;i++) {
			float routeToTarget = (currentPlatform.targetLocations [i] - transform.position).magnitude;
			if (routeToTarget < shortestRoute) {
				shortestRoute = routeToTarget;
				closestTargetIndex = i;
			}
		}
		trainTarget = currentPlatform.targetLocations[closestTargetIndex];
	}

	void DeathKnell() {
		SetAgentControl (false);	//turn their kinematic and nmagent off ready for beautiful physics interactions
		rb.constraints = RigidbodyConstraints.None;
		rb.ResetCenterOfMass ();
	}

	void RegisterWithPlatform ()
	{
		currentPlatform.RegisterPerson (this);
		platformTarget = currentPlatform.GetNewWaitLocation ();
		nmAgent.speed /= 2f;
		nmAgent.SetDestination (platformTarget);
	}

	void UnregisterWithPlatform() {
		currentPlatform.UnregisterPerson (this);
		//send nmAgent back to waiting area or something?
	}
		

}
