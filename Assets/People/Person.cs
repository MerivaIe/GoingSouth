
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using MoreLinq;

[RequireComponent(typeof(NavMeshAgent))]
public class Person : MonoBehaviour {

	public bool insideTrain {get; private set;}
	/// <summary>
	/// PersonStatus: MovingToPlatform=nmAgent control to centre of platform | WrongPlatform=nmAgent control to waiting area | MovingToDoor=nmAgent control to one door location | Boarding=physics control avoidance of others
	/// </summary>
	public enum PersonStatus{MovingToFoyer,AtFoyer,MovingToPlatform,ReadyToBoard,MovingToTrainDoor,BoardingTrain,FindingSeat,SatDown,Alighted,Compromised}
	public PersonStatus status;
	public Destination desiredDestination;
	public float boardingForce = 20f, nudgeForce = 1f, checkingInterval = 0.5f,proximityDistance = 0.5f, centreOfMassYOffset = 0f;
	public static float sqrTargetThreshold = 4f;

	private NavMeshAgent nmAgent;
	private NavMeshObstacle nmObstacle;
	private Rigidbody rb;
	private Vector3 waitingTarget, trainTarget, toWaitingTarget;
	private static float[] proximityAngles;
	private float nextCheckTime = 0f;
	private bool boardUsingForce = false, atWaitingTarget = false;
	private TimetableItem myTargetTimetableItem;

	void Awake () {	//needs to be Awake because Start() is not called before methods on instantiated gameObject it seems
		rb = GetComponent <Rigidbody> ();
		nmAgent = GetComponent <NavMeshAgent>();
		nmObstacle = GetComponent <NavMeshObstacle> ();
		nmAgent.speed = Random.Range(2f,5f);

		Vector3 newCentreOfMass = rb.centerOfMass;
		newCentreOfMass.y = centreOfMassYOffset;
		rb.centerOfMass = newCentreOfMass;

		toWaitingTarget.y = 0f;	//we will only ever modify the xz
		insideTrain = false;
		if (proximityAngles == null) {
			GenerateFanAngles (5,90);
		}

		InvokeRepeating ("CheckForTimetableChanges",0,checkingInterval * 10f);
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

	void CheckForTimetableChanges() {
		if (desiredDestination.soonestTimetableItem != null && desiredDestination.soonestTimetableItem != myTargetTimetableItem) {
			switch (status) {	//only statuses where person is at waiting area, moving to platform, or waiting at platform- any other statuses mean Person is already boarding train or compromised
			case PersonStatus.MovingToFoyer:
			case PersonStatus.AtFoyer:
			case PersonStatus.MovingToPlatform:
			case PersonStatus.ReadyToBoard:
				myTargetTimetableItem = desiredDestination.soonestTimetableItem;
				SetMovingToWaitingArea (true,myTargetTimetableItem.platform.transform.position);
				break;
			}
		} else if (desiredDestination.soonestTimetableItem == null && myTargetTimetableItem != null) {
			//TODO: make UI call into GameManager to wipe soonestTimetableItem from destination if platform wiped... the people will pick this up
		}
	}

	//might be better to actually do all of this on a switch(on status) at the base level actually- it is getting hard to read
	void FixedUpdate() {
		if (myTargetTimetableItem == null || myTargetTimetableItem.train == null) {return;}
		//only the four following statuses need to be handled in fixed update...
		if (status == PersonStatus.FindingSeat || status == PersonStatus.ReadyToBoard || status == PersonStatus.MovingToTrainDoor || status == PersonStatus.BoardingTrain) {
			if (myTargetTimetableItem.train.status == Train.TrainStatus.BoardingTime) {
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
				Vector3 boardingVectorNormalized = boardingVector.normalized;
				if (status == PersonStatus.MovingToTrainDoor && Time.time > nextCheckTime) {
					boardUsingForce = !IsDirectionClear (boardingVectorNormalized);	//check proximity periodically to set boarding by velocity or by force
					nextCheckTime = Time.time + checkingInterval;
				}
				if (status == PersonStatus.BoardingTrain || boardUsingForce) {
					rb.AddForce (boardingForce * boardingVectorNormalized, ForceMode.Acceleration);	//TODO: use the below but apply at transform.position at start of push and at transform.position + rb.centerOfMass at the end of the pushing... this will mean people are not flopping over at the start of pushing
					//rb.AddForceAtPosition (boardingForce*boardingVector.normalized,transform.position,ForceMode.Acceleration);
				} else {
					rb.velocity = nmAgent.speed * boardingVectorNormalized;
				}
			} else { //catches all other TRAIN statuses
				if (status == PersonStatus.MovingToTrainDoor || status == PersonStatus.BoardingTrain) {	//pick up anyone who failed to board train
					status = PersonStatus.ReadyToBoard;
				}
				if (status == PersonStatus.ReadyToBoard) {
					// could add an interval to this check if this is too heavy on performance (i.e. 'if (Time.time > nextCheckTime) {')
					toWaitingTarget.x = waitingTarget.x - transform.position.x;
					toWaitingTarget.z = waitingTarget.z - transform.position.z;
					atWaitingTarget = toWaitingTarget.sqrMagnitude < 0.01f;	//<0.1^2
					if (atWaitingTarget && nmAgent.enabled) {			//if under agent control and close to target: turn off agent
						SetAgentControl (false);
					} else if (!atWaitingTarget && !nmAgent.enabled) {	//else under physics control and not close: nudge towards target
						rb.AddForce (toWaitingTarget.normalized * nudgeForce, ForceMode.Acceleration);	//TODO could improve performance further by setting direction just once? (normalized is heavy)
					}
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
		Vector3[] doorLocations = myTargetTimetableItem.train.doors.Select (a => a.gameObject.transform.position).ToArray ();
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
			trainTarget.z += 2f * myTargetTimetableItem.train.transform.localScale.z;	//perhaps train width if we ever go wider trains
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
		transform.parent = myTargetTimetableItem.train.transform;
		status = PersonStatus.SatDown;
	}

	public void SetMovingToWaitingArea(bool isPlatform, Vector3 optionalWaitingTarget) {
		SetAgentControl (true);	//may jerk them up... change this to after a recovery period once they have stood up?
		waitingTarget = optionalWaitingTarget;
		nmAgent.SetDestination (waitingTarget);
		status = isPlatform? PersonStatus.MovingToPlatform : PersonStatus.MovingToFoyer;	
	}
	public void SetMovingToWaitingArea() {	//this exists purely so that this function can be invoked by OnHitGround()
		bool isPlatform = !(status == PersonStatus.AtFoyer || status == PersonStatus.MovingToFoyer);
		SetMovingToWaitingArea (isPlatform,waitingTarget);
	}

	public void OnHitGround() {				//make the person return to their waiting area
		if (status != PersonStatus.Compromised) {	//TODO what if they hit ground from foyer rather than platform
			Invoke("SetMovingToWaitingArea",2f);		//TODO: invoke after they have stood up
		}
	}

	public void OnWaitingAreaEnter(Vector3 waitLocation) {	
		waitingTarget = waitLocation;
		nmAgent.SetDestination (waitingTarget);
		rb.constraints = RigidbodyConstraints.FreezePositionY;	//TODO: believe this is what is causing some people to penetrate the platform slightly (they must be reentering at wrong height)

		if (status == PersonStatus.MovingToPlatform) {	//this will exclude compromised people
			status = PersonStatus.ReadyToBoard;
		} else if (status == PersonStatus.MovingToFoyer) {
			status = PersonStatus.AtFoyer;
		}
	}

	public void OnWaitingAreaExit() {
		rb.constraints = RigidbodyConstraints.None;
	}

	public void OnTrainEnter() {
		insideTrain = true;
	}

	public void OnTrainExit() {
		insideTrain = false;
	}
}