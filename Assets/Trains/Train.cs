using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Train : MonoBehaviour {

	public float speed = 40f, boardingDuration = 10f, accelerationModifier = 4f;
	public enum TrainStatus {EnteringStation,Braking,BoardingTime,Accelerating,LeavingStation,Idle}
	public TrainStatus status;
	public Vector3 direction;	//this should eventually be replaced with just transform.forward everywhere
	public Color color;
	public SphereCollider[] doors {get; private set;}	//has to be collider so that transform is queried once train reaches platform
	public float length { get; private set; }

	private Rigidbody rb;
	private float speedChangeDistance, startPosX, startSpeedX; 
	private Animator animator;
	private List<Person> peopleOnBoard = new List<Person> ();
	private float journeyStartTime = 0f, journeyEndTime = 0f;	//journeyProgress being calculated on demand, journeyEndTime will tell things when train is available to be assigned again
	private TimetableItem myCurrentTimetableItem;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		length = 20f; 				//TODO hard coded
		animator = GetComponent <Animator>();
		status = TrainStatus.Idle;

		doors = GetComponentsInChildren <SphereCollider>().ToArray ();
	}

	void FixedUpdate () {
		Vector3 newVelocity = rb.velocity;
		switch (status) {	//both of these rely on startSpeedX set at the point this status is set
		case TrainStatus.Braking:				
			if (rb.velocity.x <= 0.001f) {								//if we have stopped then open doors
				rb.constraints |= RigidbodyConstraints.FreezePositionX;	//Set freeze x position
				OpenDoors ();
			} else {													//otherwise we are braking: reduce velocity gradually to zero
				newVelocity.x = SmoothlyAccelerateToTargetSpeed (0f);	//TODO: need to apply direction to this??
			}
			break;
		case TrainStatus.Accelerating:
			if (rb.velocity.x >= speed) {
				status = TrainStatus.LeavingStation;
			} else {
				newVelocity.x = SmoothlyAccelerateToTargetSpeed (speed);
				//potentially Mathf.SmoothDamp( should be used for this... or SmoothStep
			}
			break;
		case TrainStatus.Idle:
			if (transform.position.x < -50f) {	//temp code to provide constant velocity when out of station
				direction = Vector3.right;
				newVelocity.x = speed;
				status = TrainStatus.EnteringStation;
			}
			break;
		}
		if (rb.velocity != newVelocity) {rb.velocity = newVelocity;}
	}

	float SmoothlyAccelerateToTargetSpeed (float targetSpeed)	//this is not actually linear because interval is dependent on distance
	{
		return Mathf.Lerp (startSpeedX, targetSpeed, direction.x * (transform.position.x - startPosX) / speedChangeDistance);
	}

	public void SetBraking (float stoppingPosX)	//if you are going to have global parameters for this then make the targetSpeed in SmoothlyAccelerate global as well....and combine this with the method below
	{
		startSpeedX = rb.velocity.x;
		startPosX = transform.position.x;
		speedChangeDistance =  stoppingPosX - startPosX;	//end of signal trigger - position of front of train
		status = TrainStatus.Braking;
	}

	void SetAccelerating() {
		startSpeedX = direction.x * 0.1f;
		startPosX = transform.position.x;
		speedChangeDistance = length * accelerationModifier;
		status = TrainStatus.Accelerating;
	}

	#region Braking Sequence
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
	}
	#endregion

	void HandlePeopleOnboard() {
		foreach (Person person in peopleOnBoard) {
			person.OnTrainLeaveStation ();
		}
	}

	void CalculateJourneyMetrics ()	//TODO: if we end up calling to our timetable item a lot then maybe store a reference to it
	{
		journeyStartTime = Time.time;
		float journeyDurationInGameMinutes = (myCurrentTimetableItem.destination.routeLength * 1000f) / (speed * 60f);	//distance in metres / speed in metres per min
		float journeyDurationInRealSeconds = journeyDurationInGameMinutes / GameManager.minutesPerSecond;
		journeyEndTime = journeyStartTime + journeyDurationInRealSeconds;
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

	public void SetTrainColor(Color _color) {
		color = _color;
		foreach (MeshRenderer meshRenderer in GetComponentsInChildren <MeshRenderer>()) {	//set all external faces of train to this color
			if (meshRenderer.material.name == "TrainExternal") {
				meshRenderer.material.color = color;
			}
		}
	}
	public void SetCurrentTimetableItem(TimetableItem timetableItem) {
		myCurrentTimetableItem = timetableItem;
	}

	public float GetJourneyProgress() {	//return 0-1 for slider value
		if (journeyStartTime == 0f) {
			return 0f;
		} else {
			float journeyProgress = (Time.time - journeyStartTime) / (journeyEndTime - journeyStartTime);	//note that this is both outbound and inbound
			return (1f-Mathf.Abs (2f*journeyProgress - 1));	//this handles outbound progress and inbound progress (just test some values of journeyProgress to understand it (e.g. 0.1, 0.8, 1.3)
		}
	}
}
