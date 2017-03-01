﻿
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
public class Person : MonoBehaviour {

	public bool testMode = false;
	/// <summary>
	/// PersonStatus: MovingToPlatform=nmAgent control to centre of platform | WrongPlatform=nmAgent control to waiting area | MovingToDoor=nmAgent control to one door location | Boarding=physics control avoidance of others
	/// </summary>
	public enum PersonStatus{MovingToPlatform,MovingToFoyer,ReadyToBoard,MovingToTrainDoor,BoardingTrain,FindingSeat,SatDown,BoardingFailed,Alighted,Compromised}
	public PersonStatus status; 
	public string destination{ get; private set; }
	public TimetableItem timetableItem;	//this will replace the destination field above
	public Platform currentPlatform;
	public float sqrMagDeathVelocity = 1f, boardingForce = 20f, dragBase = 20f, checkProximityEvery = 0.5f,proximityDistance = 0.5f, centreOfMassYOffset = 0f;
	public static float sqrTargetThreshold = 4f;

	private NavMeshAgent nmAgent;
	private NavMeshObstacle nmObstacle;
	private Rigidbody rb;
	private Vector3 platformTarget, trainTarget;
	private static float[] proximityAngles;
	private float nextProximityCheck = 0f;
	private bool boardUsingForce = false;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		nmAgent = GetComponent <NavMeshAgent>();
		nmObstacle = GetComponent <NavMeshObstacle> ();
		destination = "Bristol";	//TODO hard coded
		if (testMode) {
			try {
				nmAgent.SetDestination(currentPlatform.transform.position);
			} catch {
				SetAgentControl (false);
			}
		}
		if (proximityAngles == null) {
			GenerateFanAngles (5,90);
		}
	}

	// Found out that you can use rb.SweepTest() to do pretty much this same thing. This might be slightly more efficient though so will leave it
	void GenerateFanAngles (int directionCount, int fanAngleInDegrees)
	{
		proximityAngles = new float[directionCount];
		int midPoint = Mathf.FloorToInt (directionCount / 2);
		float angleBetweenDirections = fanAngleInDegrees / (directionCount-1);
		for (int i = 0; i < directionCount; i++) {
			proximityAngles [i] = (i - midPoint) * angleBetweenDirections;
		}
	}

	//might be better to actually do all of this on a switch(on status) at the base level actually- it is getting hard to read
	void FixedUpdate() {
		if (status == PersonStatus.MovingToPlatform || status == PersonStatus.Compromised || status == PersonStatus.SatDown) {return;}	//could this be made quicker using bitwise operations on enum flags?

		if (currentPlatform && currentPlatform.incomingTrain.status == Train.TrainStatus.BoardingTime) {
			if (status == PersonStatus.FindingSeat) {
				if (rb.mass == 10f && Vector3.Angle (transform.up,Vector3.up)<30f) {	//if this is a light person and they are standing then add up force for crowd surfing
					rb.AddForce (Vector3.up,ForceMode.Acceleration);
				}
				return;
			}
			//this will execute just once if train is at station to ready person: could be done in a one off method called by either train at arrival time or platform if someone arrives and train is here... increases complexity of model but performance would likely improve
			if (status == PersonStatus.ReadyToBoard) {
				SetAgentControl (false);
				SetDoorTarget ();
				transform.LookAt (trainTarget);	//maybe look at random target? or smoother
				status = PersonStatus.MovingToTrainDoor;
			}
			//handle boarding situations (getting to door using force/velocity, getting into train using force)
			Vector3 boardingVector = trainTarget - transform.position;
			boardingVector.y = 0f;
			if (status == PersonStatus.MovingToTrainDoor && Time.time > nextProximityCheck) {
				boardUsingForce = !IsDirectionClear (boardingVector.normalized);	//check proximity periodically to set boarding by velocity or by force
				nextProximityCheck = Time.time + checkProximityEvery;
			}
			if (status == PersonStatus.BoardingTrain || boardUsingForce) {
				MoveUsingForce (boardingVector);
			} else {
				rb.drag = 0f;
				rb.velocity = nmAgent.speed * boardingVector.normalized;
			}
		} else if (currentPlatform.incomingTrain.status == Train.TrainStatus.Accelerating) {	//else train has just set off [handle people who didn't board]
//			NavMeshHit hit;
//			if (status == PersonStatus.BoardingTrain || status == PersonStatus.MovingToTrainDoor) {	//if these statuses are hit we were unsuccessful boarding train so reset
//				//get closest point on navmesh and move towards it
//				NavMesh.SamplePosition (transform.position, out hit, nmAgent.height, NavMesh.AllAreas);--use bit shifting now that you understans
//				Vector3 returnVector = hit.position - transform.position;
//				if (returnVector.sqrMagnitude > 0.001f) {	//if not quite or not on navmesh
//					rb.velocity = returnVector.normalized * nmAgent.speed;
//				}
//				//TODO when you have internet: get people to stand up...Quaternion.FromToRotation (transform.up, Vector3.up)?...rb.AddTorque ()?
//				status = PersonStatus.BoardingFailed;
//			}
//			if (status == PersonStatus.BoardingFailed) {
//				//!!!!!!!!!!!!!!!!!!!!!!!add returnVector here.. otherwise we just have one pump
//				if (NavMesh.SamplePosition (transform.position, out hit, nmAgent.radius + 0.001f, NavMesh.AllAreas)) {	//else target still far away or nmAgent not enabled [TODO if they have come off the navmesh then this will error- sort]
//					SetAgentControl (true);
//					nmAgent.SetDestination (platformTarget);
//					//transform.LookAt (trainTarget);	//maybe look at random target?
//					status = PersonStatus.ReadyToBoard;
//				}
//			}
//			if (status == PersonStatus.ReadyToBoard) {
//				if ((platformTarget - transform.position).sqrMagnitude < 0.01f && nmAgent.enabled) {	//if distance to target is very small
//					SetAgentControl (false);
//				}
//			}
			//if we are just waiting for train to arrive at our platform target then use physics control to nudge towards our target?
			//if they have been waiting for more than 5 seconds they get a nearby location and move. or just shuffle their looking at.
		}
	}

	void OnTriggerEnter(Collider coll) {
		if (status != PersonStatus.Compromised && status != PersonStatus.SatDown) {

			if (coll.gameObject.GetComponent<PlatformTrigger> ()) {
				if (status == PersonStatus.MovingToPlatform) {
					currentPlatform = coll.gameObject.GetComponentInParent<Platform> ();
					if (destination == currentPlatform.nextDeparture) {
						RegisterWithPlatform ();
						nmAgent.SetDestination (platformTarget);
						status = PersonStatus.ReadyToBoard;
					} else {
						status = PersonStatus.MovingToFoyer;	//currently unhandled
					}
				}
			} else if (coll.GetType () == typeof(SphereCollider)) {	//person is being pushed back out of train so make them apply boarding force again TODO: non-critical: this doesnt work if nearly all people board from one side
				if (status == PersonStatus.FindingSeat) {
					Vector3 offset = transform.position - coll.bounds.center;
					if (IsDirectionClear (-offset)) {	//if it is clear behind the person then push
						trainTarget = transform.position + offset;
						status = PersonStatus.BoardingTrain;
					}
				} else if (status == PersonStatus.MovingToTrainDoor) {
					trainTarget.z += 2f * currentPlatform.incomingTrain.transform.localScale.z;
					status = PersonStatus.BoardingTrain;
				}
			} else if (coll.CompareTag ("KillingTrigger") && coll.gameObject.GetComponentInParent <Rigidbody> ().velocity.sqrMagnitude > sqrMagDeathVelocity) {
				status = PersonStatus.Compromised;
				DeathKnell ();
			}
		}
	}

	void OnTriggerExit(Collider coll) {
		if (status == PersonStatus.BoardingTrain && coll.GetType () == typeof(SphereCollider) && currentPlatform && currentPlatform.incomingTrain.boardingTrigger.bounds.Contains (transform.position)) {
			rb.drag = 0f;
			if (Random.value > 0.33f) {
				rb.centerOfMass = new Vector3 (0f, centreOfMassYOffset, 0f);
			} else {
				rb.mass = 10f;
			}
			rb.constraints = RigidbodyConstraints.None;
			status = PersonStatus.FindingSeat;
		}
	}

	bool IsDirectionClear (Vector3 targetVector)	//targetVector must be in the xz plane because we use Vector3.up, could make more generic by parameterising this later
	{
		//Debug.DrawRay (transform.position,targetVector,Color.yellow,0.5f);
		foreach (float angle in proximityAngles) {
			Vector3 direction = Quaternion.AngleAxis(angle,Vector3.up) * targetVector;
			//Debug.DrawRay (transform.position, direction * proximityDistance, Color.green, 0.5f);
			if (Physics.Raycast (transform.position,direction, proximityDistance,LayerMask.NameToLayer ("People"))) {
				return false;
			}
		}
		return true;
	}

	void MoveUsingForce(Vector3 boardingVector) {
		float angleDiff = Mathf.Deg2Rad * Vector3.Angle (rb.velocity, boardingVector);	// provide a bit of drag to prevent oscillation around boarding vector
		float dragModifier = Mathf.Sin (0.5f * angleDiff);
		rb.drag = dragModifier * dragBase;	//maybe do this only when outside the train
		rb.AddForce (boardingForce * boardingVector.normalized, ForceMode.Acceleration);
	}

	void SetAgentControl(bool turnOn) {
		rb.isKinematic = turnOn;
		if (turnOn) {	//if turning on agent then turn off obstacle FIRST (having both active at same time causes warning)
			nmObstacle.enabled = !turnOn;
			nmAgent.enabled = turnOn;
		} else {
			nmAgent.enabled = turnOn;
			nmObstacle.enabled = !turnOn;
		}
	}

	void SetDoorTarget() {
		Vector3[] doorLocations = currentPlatform.incomingTrain.doors.Select (a => a.gameObject.transform.position).ToArray ();
		float shortestRoute = (doorLocations[0] - transform.position).sqrMagnitude;
		int closestTargetIndex = 0;
		for (int i=1;i<doorLocations.Count ();i++) {
			float routeToTarget = (doorLocations[i] - transform.position).sqrMagnitude;
			if (routeToTarget < shortestRoute) {
				shortestRoute = routeToTarget;
				closestTargetIndex = i;
			}
		}
		trainTarget = doorLocations[closestTargetIndex];
		//trainTarget.z += 1.25f * -currentPlatform.incomingTrain.transform.localScale.z;	//shift the target out a bit so people are not grinding against train trying to get to door
	}

	void DeathKnell() {
		SetAgentControl (false);	//turn their kinematic and nmagent off ready for beautiful physics interactions
		rb.constraints = RigidbodyConstraints.None;
		//TODO do we need to unregister from platform
	}

	void RegisterWithPlatform ()
	{
		platformTarget = currentPlatform.RegisterPerson (this);
	}

	void UnregisterWithPlatform() {
		currentPlatform.UnregisterPerson (this);
		status = PersonStatus.MovingToFoyer;
	}

	public void OnTrainLeaveStation() {
		Component.Destroy (rb);
		transform.parent = currentPlatform.incomingTrain.transform;
		status = PersonStatus.SatDown;
		//TODO unregister from platform!!!!!!
	}

	void OnDrawGizmos() {
		if (trainTarget != null) {
			Gizmos.color = Color.green;
			Gizmos.DrawSphere (trainTarget, 0.1f);
		}
	}
}