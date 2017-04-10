
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
	public enum PersonStatus{MovingToFoyer,AtFoyer,MovingToPlatform,ReadyToBoard,MovingToTrainDoor,BoardingTrain,FindingSeat,SatDown,Compromised}
	public PersonStatus status;
	public Destination desiredDestination;
	public float boardingForce = 20f, nudgeForce = 1f, checkingInterval = 0.5f,proximityDistance = 0.5f, centreOfMassYOffset = 0f;
	public static float sqrTargetThreshold = 4f;
	public static float nmAgentRadius {get; private set;}
	public static int peopleCount { get; private set; }
	public static int totalApprovalRating {get; private set;}

	private NavMeshAgent nmAgent;
	private NavMeshObstacle nmObstacle;
	private Rigidbody rb;
	private Vector3 waitingTarget, trainTarget, toWaitingTarget;	//different wait and train targets so that
	private static float[] proximityAngles;
	private float nextCheckTime = 0f, arrivalTimeAtStation;
	private bool boardUsingForce = false, atWaitingTarget = false;
	private TimetableItem myTargetTimetableItem;

	void Awake () {	//Awake used because Start() is not called before methods on instantiated gameObject it seems
		rb = GetComponent <Rigidbody> ();
		nmAgent = GetComponent <NavMeshAgent>();
		nmObstacle = GetComponent <NavMeshObstacle> ();
		nmAgent.speed = Random.Range(2.5f,5f);
		if (nmAgentRadius == 0) {	//i.e. not set
			nmAgentRadius = nmAgent.radius;
		}

		Vector3 newCentreOfMass = rb.centerOfMass;
		newCentreOfMass.y = centreOfMassYOffset;
		rb.centerOfMass = newCentreOfMass;

		peopleCount++;
		totalApprovalRating += 50;	//people start off at 50/100

		arrivalTimeAtStation = Time.time;

		toWaitingTarget.y = 0f;	//we will only ever modify the xz
		insideTrain = false;
		if (proximityAngles == null) {
			GenerateFanAngles (5,90);
		}
	}

	void Start() {	//we want timetable checking to start after instantiating class has set desiredDestination... calling this in Awake is too soon
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
		if (desiredDestination.soonestTimetableItem != null) {
			if (desiredDestination.soonestTimetableItem != myTargetTimetableItem) {
				switch (status) {	//only statuses where person is at waiting area, moving to platform, or waiting at platform- any other statuses mean Person is already boarding train or compromised
				case PersonStatus.MovingToFoyer:
				case PersonStatus.AtFoyer:
				case PersonStatus.MovingToPlatform:
					myTargetTimetableItem = desiredDestination.soonestTimetableItem;
					SetMovingToWaitingArea (true, myTargetTimetableItem.platform.waitingArea);
					break;
				case PersonStatus.ReadyToBoard:
					myTargetTimetableItem = desiredDestination.soonestTimetableItem;
					if (myTargetTimetableItem.platform.waitingArea.IsPersonRegistered (this)) {	//if already at platform then perform the following inefficient reset
						myTargetTimetableItem.platform.waitingArea.UnregisterPerson (this);
						status = PersonStatus.MovingToPlatform;
						OnWaitingAreaEnter (myTargetTimetableItem.platform.waitingArea);
					} else {
						SetMovingToWaitingArea (true, myTargetTimetableItem.platform.waitingArea);
					}
					break;
				}
			}
		} else {	//else destination's soonest timetable item is null
			if (status != PersonStatus.SatDown && status != PersonStatus.Compromised) {	//(people on train or compromised should ignore)
				if (myTargetTimetableItem != null) {	//...and this person has a timetableItem set then it means platform has been deselected or timetable item already satisfied before Person could reach the train
					myTargetTimetableItem = null;
					if (GameManager.instance.foyer.IsPersonRegistered (this)) {	//if person already in foyer then perform series of fairly inefficient resetting steps
						GameManager.instance.foyer.UnregisterPerson (this);
						status = PersonStatus.MovingToFoyer;
						OnWaitingAreaEnter (GameManager.instance.foyer);
					} else {	//else person is outside the foyer so just retarget the foyer
						SetMovingToWaitingArea (false, GameManager.instance.foyer);
					}
				} else {	//destination soonest timetable item null and Person doesn't have one set so still waiting... (yawn)
					if (Time.time - arrivalTimeAtStation > (30/GameManager.gameMinutesPerRealSecond)) {	//if waiting for more than 30 minutes
						totalApprovalRating -= 1;
					}
				}
			}
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
				//handle boarding situations (getting to door using force/velocity, getting into train using force)
				Vector3 boardingVector = trainTarget - transform.position;
				boardingVector.y = 0f;
				Vector3 boardingVectorNormalized = boardingVector.normalized;
				if (status == PersonStatus.MovingToTrainDoor && Time.time > nextCheckTime) {
					boardUsingForce = !IsDirectionClear (boardingVectorNormalized);	//check proximity periodically to set boarding by velocity or by force
					nextCheckTime = Time.time + checkingInterval;
				}
				if (status == PersonStatus.BoardingTrain || boardUsingForce) {
					rb.AddForce (boardingForce * boardingVectorNormalized, ForceMode.Acceleration);	//TODO: VERY LOW PRIORITY use the below but apply at transform.position at start of push and at transform.position + rb.centerOfMass at the end of the pushing... this will mean people are not flopping over at the start of pushing
					//rb.AddForceAtPosition (boardingForce*boardingVector.normalized,transform.position,ForceMode.Acceleration);
				} else {
					rb.velocity = nmAgent.speed * boardingVectorNormalized;
				}
			} else { //catches all other TRAIN statuses when train is not boarding
				if (status == PersonStatus.MovingToTrainDoor || status == PersonStatus.BoardingTrain) {	//pick up anyone who failed to board train
					status = PersonStatus.ReadyToBoard;	//TODO: URGENT I do not think this is required now that in CheckForTimetableChanges it will send people back to foyer
				}
				if (status == PersonStatus.ReadyToBoard) {
					// could add an interval to this check if this is too heavy on performance (i.e. 'if (Time.time > nextCheckTime) {')
					toWaitingTarget.x = waitingTarget.x - transform.position.x;
					toWaitingTarget.z = waitingTarget.z - transform.position.z;
					atWaitingTarget = toWaitingTarget.sqrMagnitude < 0.01f;	//<0.1^2
					if (atWaitingTarget && nmAgent.enabled) {			//if under agent control and close to target: turn off agent
						SetAgentControl (false);
					} else if (!atWaitingTarget && !nmAgent.enabled) {	//else under physics control and not close: nudge towards target
						rb.AddForce (toWaitingTarget.normalized * nudgeForce, ForceMode.Acceleration);	//TODO VERY LOW PRIORITY could improve performance further by setting direction just once? (normalized is heavy)
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

	public void OnDoorEnter(Vector3 doorCentre) {
		if (status == PersonStatus.FindingSeat) {	//person is being pushed back out of train so make them apply boarding force again TODO: non-critical: this doesnt work if nearly all people board from one side
			Vector3 offset = transform.position - doorCentre;
			if (IsDirectionClear (-offset)) {	//if it is clear behind the person then push
				trainTarget = transform.position + offset;
				status = PersonStatus.BoardingTrain;
			}
		} else if (status == PersonStatus.MovingToTrainDoor) {
			trainTarget.z += myTargetTimetableItem.platform.isLeftHanded? 2f : -2f;	//perhaps train width if we ever go wider trains
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

	public void OnTrainBoardingTime() {
		SetAgentControl (false);
		SetDoorTarget ();
		transform.LookAt (trainTarget);	//maybe look at random target? or smoother
		status = PersonStatus.MovingToTrainDoor;
	}

	public void OnTrainDeparture() {
		Component.Destroy (rb);
		transform.parent = myTargetTimetableItem.train.transform;
		status = PersonStatus.SatDown;
		totalApprovalRating += 50;
	}

	public void SetMovingToWaitingArea(bool isPlatform, WaitingArea waitingArea) {
		SetAgentControl (true);	//may jerk them up... change this to after a recovery period once they have stood up?
		nmAgent.SetDestination (waitingArea.waitAreaTriggerBounds.center);
		status = isPlatform? PersonStatus.MovingToPlatform : PersonStatus.MovingToFoyer;	
	}
	public void SetMovingToWaitingArea() {	//this exists purely so that this function can be invoked by OnHitGround() [targetWaitingArea will already have been set if someone hits ground]
		bool isPlatform = !(status == PersonStatus.AtFoyer || status == PersonStatus.MovingToFoyer);
		WaitingArea targetWaitingArea = isPlatform ? myTargetTimetableItem.platform.waitingArea : GameManager.instance.foyer;
		SetMovingToWaitingArea (isPlatform,targetWaitingArea);
	}
		
	public void OnWaitingAreaEnter(WaitingArea waitingAreaEnterred) {
		WaitingArea desiredWaitingArea = (myTargetTimetableItem != null && myTargetTimetableItem.platform != null) ? myTargetTimetableItem.platform.waitingArea : GameManager.instance.foyer;
		if (waitingAreaEnterred == desiredWaitingArea) {
			if (status == PersonStatus.BoardingTrain) {	//for any people that fall out of train while boarding just register them as passing through
				waitingAreaEnterred.RegisterPersonPassingThrough (this);
			} else if (status == PersonStatus.MovingToPlatform) {
				if (myTargetTimetableItem != null && myTargetTimetableItem.train != null && myTargetTimetableItem.train.status == Train.TrainStatus.BoardingTime) {	//if we have entered a platform whilst train is boarding already then call OnTrainBoarding and just register that we are passing through
					waitingAreaEnterred.RegisterPersonPassingThrough(this);
					OnTrainBoardingTime ();
				} else {
					status = PersonStatus.ReadyToBoard;
					SetMovingToWaitLocationInWaitingArea (waitingAreaEnterred);
				}
			} else if (status == PersonStatus.MovingToFoyer) {
				status = PersonStatus.AtFoyer;
				SetMovingToWaitLocationInWaitingArea (waitingAreaEnterred);
			}
		} else {
			waitingAreaEnterred.RegisterPersonPassingThrough (this);
		}
	}

	public void OnWaitingAreaExit(WaitingArea waitingArea) {
		waitingArea.UnregisterPerson (this);
		if (status != PersonStatus.MovingToPlatform) {	//fixed: do not want people moving out of foyer and onto platform to suddenly go limp
			rb.constraints = RigidbodyConstraints.None;
		}
	}

	void SetMovingToWaitLocationInWaitingArea(WaitingArea waitingAreaEnterred) {
		waitingTarget = waitingAreaEnterred.RegisterPersonForWaiting (this);
		SetAgentControl (true);	//we were getting some people under physics control entering waiting areas and trying to SetDestination
		nmAgent.SetDestination (waitingTarget);
		rb.constraints = RigidbodyConstraints.FreezePositionY;	//TODO: [this is required to get people who have fallen back to y lock] MEDIUM PRIORITY believe this is what is causing some people to penetrate the platform slightly (they must be reentering at wrong height)
	}

	public void OnHitGround() {						//make the person return to their waiting area
		if (status != PersonStatus.Compromised) {
			Invoke("SetMovingToWaitingArea",2f);	//TODO: LOW PRIORITY invoke after they have stood up
		}
	}

	public void OnHitTrain() {
		if (status != PersonStatus.Compromised) {
			status = PersonStatus.Compromised;
			SetAgentControl (false);
			rb.ResetCenterOfMass ();
			rb.constraints = RigidbodyConstraints.None;
			totalApprovalRating -= 50;
		}
	}

	public void OnTrainEnter() {
		insideTrain = true;
	}

	public void OnTrainExit() {
		insideTrain = false;
	}

	public void OnEnterOutOfStationTrigger() {
		Destroy (gameObject);
	}

	public void ChangeApprovalRating(int valueChange) {
		totalApprovalRating += valueChange;
	}

	public void OnDrawGizmos() {
		if (trainTarget != Vector3.zero) {
			Gizmos.color = Color.green;
			Gizmos.DrawSphere (trainTarget,0.2f);
		}
	}
}