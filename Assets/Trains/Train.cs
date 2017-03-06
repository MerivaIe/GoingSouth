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
	private float totalDistance, startPosX, startSpeedX;
	private Animator animator;
	private List<Person> peopleOnBoard = new List<Person> ();

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
				//potentially Mathf.SmoothDamp( should be used for this
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
		return Mathf.Lerp (startSpeedX, targetSpeed, direction.x * (transform.position.x - startPosX) / totalDistance);
	}

	public void SetBraking (float stoppingPosX)
	{
		startSpeedX = rb.velocity.x;
		startPosX = transform.position.x;
		totalDistance =  stoppingPosX - startPosX;	//end of signal trigger - position of front of train
		status = TrainStatus.Braking;
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
		startSpeedX = direction.x * 0.1f;
		totalDistance = length * accelerationModifier;
		status = TrainStatus.Accelerating;
	}
	#endregion

	void HandlePeopleOnboard() {
		foreach (Person person in peopleOnBoard) {
			person.OnTrainLeaveStation ();
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

	public void SetTrainColor(Color _color) {
		color = _color;
		foreach (MeshRenderer meshRenderer in GetComponentsInChildren <MeshRenderer>()) {	//set all external faces of train to this color
			if (meshRenderer.material.name == "TrainExternal") {
				meshRenderer.material.color = color;
			}
		}
	}
}
