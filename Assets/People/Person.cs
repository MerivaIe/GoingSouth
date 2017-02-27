
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
	public enum PersonStatus{MovingToPlatform,MovingToFoyer,ReadyToBoard,MovingToTrainDoor,BoardingTrain,FindingSeat,SatDown,Alighted,Compromised}
	public PersonStatus status; 
	public string destination{ get; private set; }
	public TimetableItem timetableItem;	//this will replace the destination field above
	public Platform currentPlatform;
	public float sqrMagDeathVelocity = 1f, boardingForce = 20f, dragBase = 20f, checkProximityEvery = 0.5f,proximityDistance = 0.5f;
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
			nmAgent.SetDestination(currentPlatform.transform.position);
		}
		if (proximityAngles == null) {
			GenerateFanAngles (5,90);
		}
	}

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
		if (status == PersonStatus.MovingToPlatform || status == PersonStatus.Compromised) {return;}	//could this be made quicker using bitwise operations on enum flags?

		if (currentPlatform && currentPlatform.incomingTrain.status == Train.TrainStatus.BoardingTime && status != PersonStatus.FindingSeat) {
			if (status == PersonStatus.ReadyToBoard) {	//this will execute just once when train arrives to ready person
				SetAgentControl (false);
				SetDoorTarget ();
				transform.LookAt (trainTarget);	//maybe look at random target?
				status = PersonStatus.MovingToTrainDoor;
			}
			//handle boarding situations (getting to door using force/velocity, getting into train using force)
			Vector3 boardingVector = trainTarget - transform.position;
			boardingVector.y = 0f;
			if (status == PersonStatus.MovingToTrainDoor) {
				if (Time.time > nextProximityCheck) {	//check proximity periodically to set boarding by velocity or by force
					boardUsingForce = !IsForwardClear (boardingVector.normalized);
					nextProximityCheck = Time.time + checkProximityEvery;
				}
				if (boardingVector.sqrMagnitude < sqrTargetThreshold) {	//finally, if within radius of door then shift target into train and set Boarding flag (for next FixedUpdate)
					trainTarget.z += 2f * currentPlatform.incomingTrain.transform.localScale.z;
					status = PersonStatus.BoardingTrain;
				}
			}
			if (status == PersonStatus.BoardingTrain || boardUsingForce) {
				MoveUsingForce (boardingVector);
			} else {
				rb.drag = 0f;
				rb.velocity = nmAgent.speed * boardingVector.normalized;
			}
		} else if (currentPlatform.incomingTrain.status == Train.TrainStatus.Moving) {	//else train is not at platform or it is not boarding time
			if (status == PersonStatus.FindingSeat) {
				Component.Destroy (rb);
				transform.parent = currentPlatform.incomingTrain.transform;
				status = PersonStatus.SatDown;
				//unregister from platform!!!!!!
				return;
			}
			NavMeshHit hit;
			if (status == PersonStatus.BoardingTrain || status == PersonStatus.MovingToTrainDoor) {	//if these statuses are hit we were unsuccessful boarding train so reset
				//get closest point on navmesh and move towards it
				NavMesh.SamplePosition (transform.position,out hit,nmAgent.height,NavMesh.AllAreas);
				if (transform.position != hit.position) {
					rb.velocity = (hit.position - transform.position).normalized * nmAgent.speed;
				}
				//TODO when you have internet: get people to stand up...Quaternion.FromToRotation (transform.up, Vector3.up)?...rb.AddTorque ()?
				status = PersonStatus.ReadyToBoard;
			}
			if (status == PersonStatus.ReadyToBoard) {
				if ((platformTarget - transform.position).sqrMagnitude < 0.01f && nmAgent.enabled) {	//if distance to target is very small
					SetAgentControl (false);
				} else if (NavMesh.SamplePosition (transform.position,out hit,nmAgent.radius + 0.0001f,NavMesh.AllAreas)) {	//else target still far away or nmAgent not enabled [TODO if they have come off the navmesh then this will error- sort]
					SetAgentControl (true);
					nmAgent.SetDestination (platformTarget);
					//transform.LookAt (trainTarget);	//maybe look at random target?
				}
			}
			//if we are just waiting for train to arrive at our platform target then use physics control to nudge towards our target?
			//if they have been waiting for more than 5 seconds they get a nearby location and move. or just shuffle their looking at.
		}
	}

	void OnTriggerEnter(Collider coll) {
		if (status != PersonStatus.Compromised) {
			if (coll.gameObject.CompareTag ("KillingTrigger") && coll.gameObject.GetComponentInParent <Rigidbody> ().velocity.sqrMagnitude > sqrMagDeathVelocity) {
				status = PersonStatus.Compromised;
				DeathKnell ();
			} else if (coll.gameObject.GetComponent <Train> () && status != PersonStatus.FindingSeat) {	//if this is the trigger inside the train carriage
				rb.drag = 0f;
				rb.constraints = RigidbodyConstraints.None;
				status = PersonStatus.FindingSeat;
			} else if (coll.gameObject.GetComponent<PlatformTrigger> ()) {
				if (status == PersonStatus.MovingToPlatform) {
					currentPlatform = coll.gameObject.GetComponentInParent<Platform> ();
					if (destination == currentPlatform.nextDeparture) {
						RegisterWithPlatform ();
						nmAgent.SetDestination (platformTarget);
						status = PersonStatus.ReadyToBoard;
					} else {
						status = PersonStatus.MovingToFoyer;	//currently unhandled
					}
				} else if ( status == PersonStatus.FindingSeat) {	//if person has fallen out of train whilst boarding
					//currentPlatform.incomingTrain.status == Train.TrainStatus.BoardingTime &&
					status = PersonStatus.BoardingTrain;
				}
			}
		}
	}

	bool IsForwardClear (Vector3 targetVector)
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
		rb.drag = dragModifier * dragBase;
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
		float shortestRoute = (currentPlatform.doorLocations[0] - transform.position).sqrMagnitude;
		int closestTargetIndex = 0;
		for (int i=1;i<currentPlatform.doorLocations.Count;i++) {
			float routeToTarget = (currentPlatform.doorLocations [i] - transform.position).sqrMagnitude;
			if (routeToTarget < shortestRoute) {
				shortestRoute = routeToTarget;
				closestTargetIndex = i;
			}
		}
		trainTarget = currentPlatform.doorLocations[closestTargetIndex];
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
}