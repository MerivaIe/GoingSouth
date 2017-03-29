
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
public class Person : MonoBehaviour {

	public bool testMode = false;
	public bool insideTrain {get; private set;}
	/// <summary>
	/// PersonStatus: MovingToPlatform=nmAgent control to centre of platform | WrongPlatform=nmAgent control to waiting area | MovingToDoor=nmAgent control to one door location | Boarding=physics control avoidance of others
	/// </summary>
	public enum PersonStatus{MovingToPlatform,MovingToFoyer,ReadyToBoard,MovingToTrainDoor,BoardingTrain,FindingSeat,SatDown,Alighted,Compromised}
	public PersonStatus status; 
	public Destination destination{ get; private set; }
	public Platform currentPlatform;
	public float boardingForce = 20f, nudgeForce = 1f, checkingInterval = 0.5f,proximityDistance = 0.5f, centreOfMassYOffset = 0f;
	public static float sqrTargetThreshold = 4f;

	private NavMeshAgent nmAgent;
	private NavMeshObstacle nmObstacle;
	private Rigidbody rb;
	private Vector3 platformTarget, trainTarget, toPlatformTarget;
	private static float[] proximityAngles;
	private float nextCheckTime = 0f;
	private bool boardUsingForce = false, atPlatformTarget = false;

	void Awake () {	//needs to be Awake because Start() is not called before methods on instantiated gameObject it seems
		rb = GetComponent <Rigidbody> ();
		nmAgent = GetComponent <NavMeshAgent>();
		nmObstacle = GetComponent <NavMeshObstacle> ();
		nmAgent.speed = Random.Range(2f,5f);

		Vector3 newCentreOfMass = rb.centerOfMass;
		newCentreOfMass.y = centreOfMassYOffset;
		rb.centerOfMass = newCentreOfMass;

		toPlatformTarget.y = 0f;	//we will only ever modify the xz
		insideTrain = false;
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

	// Found out that you can use rb.SweepTest() to do pretty much this same thing. Some suggest custom raycasting slightly more efficient though so will leave it
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
		if (!currentPlatform.incomingTrain) {return;}

		if (currentPlatform.incomingTrain.status == Train.TrainStatus.BoardingTime) {
			if (status == PersonStatus.FindingSeat) {
				if (rb.mass == 10f && Vector3.Angle (transform.up, Vector3.up) < 30f) {	//if this is a light person and they are standing then add up force for crowd surfing
					rb.AddForce (Vector3.up, ForceMode.Acceleration);
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
			if (status == PersonStatus.MovingToTrainDoor && Time.time > nextCheckTime) {
				boardUsingForce = !IsDirectionClear (boardingVector.normalized);	//check proximity periodically to set boarding by velocity or by force
				nextCheckTime = Time.time + checkingInterval;
			}
			if (status == PersonStatus.BoardingTrain || boardUsingForce) {
				MoveUsingForce (boardingVector);
			} else {
				rb.velocity = nmAgent.speed * boardingVector.normalized;
			}
		} else { //catches all other train statuses
			if (status == PersonStatus.MovingToTrainDoor || status == PersonStatus.BoardingTrain) {	//pick up anyone who failed to board train
				status = PersonStatus.ReadyToBoard;
			}
			if (status == PersonStatus.ReadyToBoard) {
				// could add an interval to this check if this is too heavy on performance (i.e. 'if (Time.time > nextCheckTime) {')
				toPlatformTarget.x = platformTarget.x - transform.position.x;
				toPlatformTarget.z = platformTarget.z - transform.position.z;
				atPlatformTarget = toPlatformTarget.sqrMagnitude < 0.01f;	//<0.1^2
				if (atPlatformTarget && nmAgent.enabled) {			//if under agent control and close to target: turn off agent
					SetAgentControl (false);
				} else if (!atPlatformTarget && !nmAgent.enabled) {	//else under physics control and not close: nudge towards target
					rb.AddForce (toPlatformTarget.normalized * nudgeForce, ForceMode.Acceleration);	//TODO could improve performance further by setting direction just once? (normalized is heavy)
				}
			}
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
		rb.AddForce (boardingForce * boardingVector.normalized, ForceMode.Acceleration);	//TODO: use the below but apply at transform.position at start of push and at transform.position + rb.centerOfMass at the end of the pushing... this will mean people are not flopping over at the start of pushing
		//rb.AddForceAtPosition (boardingForce*boardingVector.normalized,transform.position,ForceMode.Acceleration);
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
	}

	public void OnHitTrain() {
		if (status != PersonStatus.Compromised) {
			status = PersonStatus.Compromised;
			SetAgentControl (false);
			rb.ResetCenterOfMass ();
			rb.constraints = RigidbodyConstraints.None;
		}
	}

	public void OnDoorEnter(Vector3 doorCentre) {
		if (status == PersonStatus.FindingSeat) {	//person is being pushed back out of train so make them apply boarding force again TODO: non-critical: this doesnt work if nearly all people board from one side
			Vector3 offset = transform.position - doorCentre;
			if (IsDirectionClear (-offset)) {	//if it is clear behind the person then push
				trainTarget = transform.position + offset;
				status = PersonStatus.BoardingTrain;
			}
		} else if (status == PersonStatus.MovingToTrainDoor) {
			trainTarget.z += 2f * currentPlatform.incomingTrain.transform.localScale.z;	//perhaps train width if we ever go wider trains
			status = PersonStatus.BoardingTrain;
		}
	}

	public void OnDoorExitIntoTrain() {
		if (status == PersonStatus.BoardingTrain) {
			if (Random.value > 0.67f) {	//33% chance of making a light person (for crowdsurfing)
				rb.mass = 10f;
				rb.ResetCenterOfMass ();
			}
			status = PersonStatus.FindingSeat;
		}
	}

	public void OnTrainDeparture() {
		Component.Destroy (rb);
		transform.parent = currentPlatform.incomingTrain.transform;
		status = PersonStatus.SatDown;
	}

	public void SetMovingToPlatform(Vector3 optionalPlatformTarget) {
		SetAgentControl (true);	//may jerk them up... change this to after a recovery period once they have stood up?
		platformTarget = optionalPlatformTarget;
		nmAgent.SetDestination (platformTarget);
		status = PersonStatus.MovingToPlatform;	
	}
	public void SetMovingToPlatform() {
		if (platformTarget == Vector3.zero) {	//TODO: this might on very rare occasions cause an error if there is a platform situated at 0,0,0
			Debug.LogWarning ("No platform target provided by caller and we do not have one already stored. Please specify platform target using overloaded method.");
			//TODO maybe return to foyer/default
		} else {
			SetAgentControl (true);	//may jerk them up... change this to after a recovery period once they have stood up?
			nmAgent.SetDestination (platformTarget);
			status = PersonStatus.MovingToPlatform;
		}
	}

	public void OnHitGround() {
		if (status != PersonStatus.Compromised) {	//TODO what if they hit ground from foyer rather than platform
			Invoke("SetMovingToPlatform",2f);	//TODO: invoke after they have stood up
		}
	}

	public void OnPlatformEnter(Platform platform, Vector3 waitLocation) {
		if (status == PersonStatus.MovingToPlatform) {	//this will exclude compromised people
			currentPlatform = platform;
			//if (destination == currentPlatform.nextDeparture) {
				platformTarget = waitLocation;
				nmAgent.SetDestination (platformTarget);
			rb.constraints = RigidbodyConstraints.FreezePositionY;	//TODO: believe this is what is causing some people to penetrate the platform slightly (they must be reentering at wrong height)
				status = PersonStatus.ReadyToBoard;
			//} else {
			//	status = PersonStatus.MovingToFoyer;	//currently unhandled
			//}
		}
	}

	public void OnPlatformExit() {	//TODO: do we need to nullify currentPlatform?
		rb.constraints = RigidbodyConstraints.None;
	}

	public void OnTrainEnter() {
		insideTrain = true;
	}

	public void OnTrainExit() {
		insideTrain = false;
	}
}