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
	public BoxCollider boardingTrigger { get; private set; }
	public float length { get; private set; }

	private Rigidbody rb;
	private float totalDistance, startPosX, startSpeedX;
	private Animator animator;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		length = 20f; 				//TODO hard coded
		animator = GetComponent <Animator>();
		status = TrainStatus.Idle;
		//select color that be decided by GameManager for that destination
		//Color RandomColor() {
		//	return new Color(Random.value, Random.value, Random.value);
		//}

		doors = GetComponentsInChildren <SphereCollider>().ToArray ();

		foreach (BoxCollider coll in GetComponentsInChildren <BoxCollider>()) {	//linq this
			if (coll.gameObject.CompareTag ("BoardingTrigger")) {
				boardingTrigger = coll;
			}
		}
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
		foreach (SphereCollider door in doors) {
			door.enabled = false;
		}
		HandlePeopleOnboard ();
		startSpeedX = direction.x * 0.1f;
		totalDistance = length * accelerationModifier;
		status = TrainStatus.Accelerating;
	}

	void HandlePeopleOnboard() {
		int layerMask = 1 << LayerMask.NameToLayer ("People");
		Person[] peopleInTrain = Physics.OverlapBox (boardingTrigger.bounds.center, boardingTrigger.bounds.extents, Quaternion.identity, layerMask).Select (a => a.gameObject.GetComponent <Person> ()).ToArray ();
		foreach (Person person in peopleInTrain) {
			person.OnTrainLeaveStation ();
		}
	}
}
