
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

	private float tempTargetThreshold;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		nmAgent = GetComponent <NavMeshAgent>();
		nmObstacle = GetComponent <NavMeshObstacle> ();
		destination = "Bristol";	//TODO hard coded
		//if (Random.value > 0.5f) {
			rb.centerOfMass = new Vector3 (0f, centreOfMassYOffset, 0f);
			rb.mass = 50f;
//		} else {
//			rb.mass = 10f;
//		}
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
		tempTargetThreshold = Mathf.Sqrt (sqrTargetThreshold);
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
				if (boardingVector.sqrMagnitude > sqrTargetThreshold) {							//if still far from door
					if (Time.time > nextProximityCheck) {										//check proximity periodically to set boarding by velocity or by force
						boardUsingForce = !IsForwardClear (boardingVector.normalized);
						nextProximityCheck = Time.time + checkProximityEvery;
					}
				} else {																		//else close to the door so shift target inside train
					trainTarget.z += 2f * currentPlatform.incomingTrain.transform.localScale.z;	//TEST WITH JUST ONE PERSON AND DEBUG DRAW THE TRAIN TARGETS TO MAKE SURE THEY LOOK OK
					status = PersonStatus.BoardingTrain;
				}
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
//				NavMesh.SamplePosition (transform.position, out hit, nmAgent.height, NavMesh.AllAreas);
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
		} else if (currentPlatform.incomingTrain.status == Train.TrainStatus.Moving) {	//else train has set off fully [handle people on train]
			if (currentPlatform.incomingTrain.boardingTrigger.bounds.Contains (transform.position)) {
				Component.Destroy (rb);
				transform.parent = currentPlatform.incomingTrain.transform;
				status = PersonStatus.SatDown;
				//TODO unregister from platform!!!!!!
				return;
			}
		}
	}

	void OnTriggerEnter(Collider coll) {
		if (status != PersonStatus.Compromised && status != PersonStatus.SatDown) {
			if (status != PersonStatus.FindingSeat && currentPlatform && coll == currentPlatform.incomingTrain.boardingTrigger) {
				rb.drag = 0f;
				rb.constraints = RigidbodyConstraints.None;
				status = PersonStatus.FindingSeat;
				//Physics.IgnoreCollision (GetComponent <CapsuleCollider> (), currentPlatform.incomingTrain.boardingCollider, false);
			} else if (status == PersonStatus.MovingToPlatform && coll.gameObject.GetComponent<PlatformTrigger> ()) {
				currentPlatform = coll.gameObject.GetComponentInParent<Platform> ();
				if (destination == currentPlatform.nextDeparture) {
					RegisterWithPlatform ();
					nmAgent.SetDestination (platformTarget);
					status = PersonStatus.ReadyToBoard;
				} else {
					status = PersonStatus.MovingToFoyer;	//currently unhandled
				}
			} else if (coll.CompareTag ("KillingTrigger") && coll.gameObject.GetComponentInParent <Rigidbody> ().velocity.sqrMagnitude > sqrMagDeathVelocity) {
				status = PersonStatus.Compromised;
				DeathKnell ();
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
		//rb.AddForce (boardingForce * boardingVector.normalized, ForceMode.Acceleration);
		rb.AddForceAtPosition (boardingForce * boardingVector.normalized,rb.centerOfMass,ForceMode.Acceleration);
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
		Vector3[] doorLocations = currentPlatform.incomingTrain.doors.Select (a => a.transform.position).ToArray ();
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
		trainTarget.z += 1.25f * -currentPlatform.incomingTrain.transform.localScale.z;	//shift the target out a bit so people are not grinding against train trying to get to door
		//Physics.IgnoreCollision (GetComponent <CapsuleCollider> (), currentPlatform.incomingTrain.boardingCollider,true);
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