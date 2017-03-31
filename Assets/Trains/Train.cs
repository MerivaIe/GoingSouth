using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Train : MonoBehaviour {

	public float speed = 40f, boardingDuration = 10f;
	public enum TrainStatus {Arriving,Braking,BoardingTime,Departing,Parked,Idle}
	public TrainStatus status;
	public Vector3 direction;	//this should eventually be replaced with just transform.forward everywhere
	public Material materialColor;
	public SphereCollider[] doors {get; private set;}	//has to be collider so that transform is queried once train reaches platform
	public string trainSerialID { get; private set; }

	private Rigidbody rb;
	private Animator animator;
	private List<Person> peopleOnBoard = new List<Person> ();
	private float accelerationTargetX, journeyStartTime = 0f, journeyEndTime = 0f;	//journeyProgress being calculated on demand, journeyEndTime will tell things when train is available to be assigned again
	private TimetableItem myCurrentTimetableItem;

	public void Initialise() {	//custom method which will be called in GameManager's Awake() to perform actions required before Start()
		trainSerialID = string.Format("{0:X}",this.GetHashCode ()).Substring (4);	//this is required for serial ID because DisplayManager needs this info before Trains' Start() method is called
	}

	void Start () {
		rb = GetComponent <Rigidbody> ();
		animator = GetComponent <Animator>();
		status = TrainStatus.Parked;

		doors = GetComponentsInChildren <SphereCollider>().ToArray ();
	}

	void FixedUpdate () {
		Vector3 newVelocity = rb.velocity;
		switch (status) {	//both of these rely on startSpeedX set at the point this status is set
		case TrainStatus.Braking:				
			if (rb.velocity.x <= 0.001f) {								//if we have stopped then open doors
				status = TrainStatus.Idle;
				rb.constraints |= RigidbodyConstraints.FreezePositionX;	//Set freeze x position
				direction = -direction;									//change directions because train is at terminal
				OpenDoors ();
			} else {													//otherwise we are braking: reduce velocity to zero as we reach target position
				Mathf.SmoothDamp (transform.position.x,accelerationTargetX,ref newVelocity.x,2f,speed,Time.fixedDeltaTime);
			}
			break;
		case TrainStatus.Departing:
			Mathf.SmoothDamp (transform.position.x, accelerationTargetX, ref newVelocity.x,2f,speed,Time.fixedDeltaTime);
			break;
		}
		if (rb.velocity != newVelocity) {rb.velocity = newVelocity;}
	}

	public void SetBraking (float stoppingPosX)	//if you are going to have global parameters for this then make the targetSpeed in SmoothlyAccelerate global as well....and combine this with the method below
	{
		accelerationTargetX = stoppingPosX;
		status = TrainStatus.Braking;
	}

	void SetAccelerating() {
		accelerationTargetX = GameManager.instance.outOfStationTrigger.bounds.center.x;
		status = TrainStatus.Departing;
	}

	#region Platform Sequence
	void OpenDoors ()
	{
		animator.SetTrigger ("doorOpen");
		Invoke ("SetBoardingTime", animator.GetCurrentAnimatorStateInfo (0).length);
	}
	
	void SetBoardingTime() {
		animator.ResetTrigger ("doorOpen");
		foreach (SphereCollider door in doors) {
			door.enabled = true;
		}
		status = TrainStatus.BoardingTime;
		Invoke ("CloseDoors", boardingDuration);
	}

	void CloseDoors() {
		animator.SetTrigger ("doorClose");
		status = TrainStatus.Idle;
		rb.constraints &= ~RigidbodyConstraints.FreezePositionX;	//Remove freeze x position (and let the carriage drift slightly)
		rb.velocity = -0.01f * speed * direction;
		Invoke ("Depart", 2f);
	}

	void Depart() {
		animator.ResetTrigger ("doorClose");
		foreach (SphereCollider doorTrigger in doors) {
			doorTrigger.enabled = false;
		}
		HandlePeopleOnboard ();
		SetAccelerating ();
		CalculateJourneyMetrics ();
		Invoke ("CheckIfClearToEnterStation",journeyEndTime - journeyStartTime);	//invoke reenter station after journey duration
	}
	#endregion

	void HandlePeopleOnboard() {
		foreach (Person person in peopleOnBoard) {
			person.OnTrainDeparture ();
		}
	}

	void CalculateJourneyMetrics ()
	{
		journeyStartTime = Time.time;
		float journeyDurationInGameMinutes = (myCurrentTimetableItem.destination.routeLength * 1000f) / (speed * 60f);	//distance in metres / speed in metres per min
		float journeyDurationInRealSeconds = journeyDurationInGameMinutes / GameManager.gameMinutesPerRealSecond;
		journeyEndTime = journeyStartTime + journeyDurationInRealSeconds;
	}

	void CheckIfClearToEnterStation() {	//called once at the end of journey duration (invoked in Depart) and every time the train is assigned to a new timetable item
		if (journeyEndTime >= Time.time && myCurrentTimetableItem != null && myCurrentTimetableItem.platform) {
			SetTrainColor (myCurrentTimetableItem.destination.materialColor);

			Vector3 trackPosition = transform.position;
			trackPosition.z = myCurrentTimetableItem.platform.platformSignalBounds.center.z;
			transform.position = trackPosition;

			direction = Vector3.right;
			rb.velocity = direction * speed;
			status = TrainStatus.Arriving;
			journeyStartTime = 0f;
			journeyEndTime = 0f;

			//TODO lock down the timetable UI item
		}
	}

	public void OnAssignedToTimetableItem(TimetableItem timetableItem) {
		myCurrentTimetableItem = timetableItem;
		CheckIfClearToEnterStation ();
	}

	public float GetJourneyProgress() {	//return 0-1 for slider value
		if (journeyStartTime == 0f) {
			return 0f;
		} else {
			float journeyProgress = (Time.time - journeyStartTime) / (journeyEndTime - journeyStartTime);	//note that this is both outbound and inbound
			return (1f-Mathf.Abs (2f*journeyProgress - 1));	//this handles outbound progress and inbound progress (just test some values of journeyProgress to understand it (e.g. 0.1, 0.8, 1.3)
		}
	}

	public void OnEnterOutOfStationTrigger() {	//reset most things apart from journey time etc.
		if (status == TrainStatus.Departing) {
			GameManager.instance.AddObjectsToDeletionQueue (peopleOnBoard.Select (p => p.gameObject).ToList ());
			peopleOnBoard.Clear ();
			status = TrainStatus.Parked;
			rb.velocity = Vector3.zero;
			transform.position = GameManager.instance.trainDockingPoints.AvailableOptions [0];
			GameManager.instance.trainDockingPoints.ExhaustOption (0);
		}
	}

	public void SetTrainColor(Material _materialColor) {
		materialColor = _materialColor;
		foreach (MeshRenderer meshRenderer in GetComponentsInChildren <MeshRenderer>()) {	//set all external faces of train to this color
			if (meshRenderer.sharedMaterial.name.Length >= 13 && meshRenderer.sharedMaterial.name.Substring (0, 13) == "TrainExternal") {
				meshRenderer.sharedMaterial = materialColor;
			}
		}
	}

	public void RegisterPerson(Person person) {
		peopleOnBoard.Add (person);
	}

	public void UnregisterPerson(Person person) {
		try {
			peopleOnBoard.Remove (person);
		} catch {
			Debug.LogError ("Could not find expected person in list of onboard people.");
		}
	}

	void OnDrawGizmos() {
		if (accelerationTargetX != 0f) {
			Gizmos.DrawSphere (new Vector3(accelerationTargetX,transform.position.y,transform.position.z), 0.5f);
		}
	}
}
