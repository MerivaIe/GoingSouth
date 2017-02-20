
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
	public enum PersonStatus{MovingToPlatform,WrongPlatform,ReadyToBoard,MovingToDoor,Boarding,Boarded,Alighted,Compromised}
	public PersonStatus status; 
	public string destination{ get; private set; }
	public Platform currentPlatform;
	public float sqrMagDeathVelocity = 1f, boardingForce = 20f, dragBase = 20f, targetThreshold = 2f, checkProximityEvery = 0.5f,proximityDistance = 0.5f;

	private NavMeshAgent nmAgent;
	private NavMeshObstacle nmObstacle;
	private Rigidbody rb;
	private Vector3 platformTarget, trainTarget;
	private static float[] proximityAngles;
	private float nextProximityCheck = 0f;
	private bool boardWithVelocity = true;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		nmAgent = GetComponent <NavMeshAgent>();
		nmObstacle = GetComponent <NavMeshObstacle> ();
		destination = "Bristol";
		if (testMode) {
			nmAgent.SetDestination(currentPlatform.transform.position);
		}
		if (proximityAngles == null) {
			CreateProximityAngles (5,90);
		}
	}

	void CreateProximityAngles (int directionCount, int fanAngleInDegrees)
	{
		proximityAngles = new float[directionCount];
		int midPoint = Mathf.FloorToInt (directionCount / 2);
		float angleBetweenDirections = fanAngleInDegrees / (directionCount-1);
		for (int i = 0; i < directionCount; i++) {
			proximityAngles [i] = (i - midPoint) * angleBetweenDirections;
			Debug.Log ("Proximity angles... Index: " + i.ToString () + " AngleValue: " + proximityAngles[i].ToString () + " Sin/Cos(0): " + Mathf.Sin (proximityAngles[i]) + "|" + Mathf.Cos (proximityAngles[i]));
		}
	}

	void FixedUpdate() {
		if (currentPlatform && currentPlatform.incomingTrain.status == Train.TrainStatus.BoardingTime) {
			switch (status) {
			case PersonStatus.ReadyToBoard:
				SetAgentControl (false);
				SetDoorTarget ();
				status = PersonStatus.MovingToDoor;
				//free up their wait space? no... I guess people arriving will board straight away [think]
				break;
			case PersonStatus.MovingToDoor:
				Vector3 boardingVector = trainTarget - transform.position;
				boardingVector.y = 0f;
				transform.rotation = Quaternion.LookRotation (boardingVector);
				if (boardingVector.magnitude > targetThreshold) {	//if still far from door
					if (Time.time > nextProximityCheck) {			//check proximity periodically to set boarding by velocity or by force
						boardWithVelocity = !IsForwardClear (boardingVector.normalized);
						nextProximityCheck = Time.time + checkProximityEvery;
					}
					if (boardWithVelocity) {
						rb.drag = 0f;
						rb.velocity = nmAgent.speed * boardingVector.normalized;
						break;
					}
				} else {											//if within a metre of door then shift target into train and set Boarding flag
					trainTarget.z += 2f;
					boardingVector.z += 2f;
					status = PersonStatus.Boarding;
				}
				MoveUsingForce (boardingVector);					//if we get to this point then boarding by force is desired
				break;
			case PersonStatus.Boarding:
				boardingVector = trainTarget - transform.position;
				boardingVector.y = 0f;
				MoveUsingForce (boardingVector);
				break;
			case PersonStatus.Boarded:
				//maybe add some slight corrective rotation to try stay upright if feet are on ground
				break;
			//if they have been waiting for more than 5 seconds they get a nearby location and move. or just shuffle their looking at.
			//actually, to ensure that people can be shoved off the platform we should make it physics control trying to reach their assigned destination (and maybe add a NM Obstacle so agents know to avoid them).
			}
		} else {
			switch (status) {
			case PersonStatus.ReadyToBoard:
				//else if we are just waiting for train to arrive then use physics control to nudge towards our target
				break;
			}
		}
	}

	void OnTriggerEnter(Collider coll) {
		if (status != PersonStatus.Compromised) {
			Train train = coll.gameObject.GetComponentInParent <Train> ();	//if this is the front of the train, trigger will be in parent transform
			if (train && train.gameObject.GetComponent <Rigidbody> ().velocity.sqrMagnitude > sqrMagDeathVelocity) {
				status = PersonStatus.Compromised;
				DeathKnell ();
			} else if (coll.gameObject.GetComponent <Train> () && status != PersonStatus.Boarded) {	//if this is the trigger inside the train carriage
				rb.drag = 0f;
				rb.constraints = RigidbodyConstraints.None;
				status = PersonStatus.Boarded;
			} else {
				Platform platform = coll.gameObject.GetComponent<Platform> ();
				if (platform && status == PersonStatus.MovingToPlatform) {
					currentPlatform = platform;
					if (destination == currentPlatform.nextDeparture) {
						RegisterWithPlatform ();
						nmAgent.SetDestination (platformTarget);
						status = PersonStatus.ReadyToBoard;
					} else {
						status = PersonStatus.WrongPlatform;	//currently unhandled
					}
				}
			}
		}
	}

	void OnTriggerExit(Collider coll) {
		Platform platform = coll.gameObject.GetComponent<Platform> ();
		if (currentPlatform && platform == currentPlatform) {
			UnregisterWithPlatform ();
		} else if (coll.gameObject.GetComponent <Train> () && status == PersonStatus.Boarded) {			//if this is the trigger inside the train carriage
			status = PersonStatus.MovingToDoor;
		}
	}

	bool IsForwardClear (Vector3 targetVector)
	{
		Debug.DrawRay (transform.position,targetVector,Color.yellow,0.5f);
		foreach (float angle in proximityAngles) {
			Vector3 direction = Quaternion.AngleAxis(angle,Vector3.up) * targetVector;
			Debug.DrawRay (transform.position, direction * proximityDistance, Color.green, 0.5f);
			if (Physics.Raycast (transform.position,direction, proximityDistance,LayerMask.NameToLayer ("People"))) {
				return true;
			}
		}
		return false;
	}

	void MoveUsingForce(Vector3 boardingVector) {
		float angleDiff = Mathf.Deg2Rad * Vector3.Angle (rb.velocity, boardingVector);	// provide a bit of drag to prevent oscillation around boarding vector
		float dragModifier = Mathf.Sin (0.5f * angleDiff);
		rb.drag = dragModifier * dragBase;
		rb.AddForce (boardingForce * boardingVector.normalized, ForceMode.Acceleration);
	}

	void SetAgentControl(bool turnOn) {
		if (rb.isKinematic != turnOn) {	//ignore request if they are already set to requested bool
			rb.isKinematic = turnOn;
			nmAgent.enabled = turnOn;
			nmObstacle.enabled = !turnOn;
		}
	}

	void SetDoorTarget() {
		float shortestRoute = (currentPlatform.doorLocations[0] - transform.position).magnitude;
		int closestTargetIndex = 0;
		for (int i=1;i<currentPlatform.doorLocations.Count;i++) {
			float routeToTarget = (currentPlatform.doorLocations [i] - transform.position).magnitude;
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
	}

	void RegisterWithPlatform ()
	{
		currentPlatform.RegisterPerson (this);
		platformTarget = currentPlatform.GetNewWaitLocation ();
	}

	void UnregisterWithPlatform() {
		currentPlatform.UnregisterPerson (this);
		//send nmAgent back to waiting area or something?
	}
}