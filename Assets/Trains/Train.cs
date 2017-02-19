using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Train : MonoBehaviour {

	//tried doing all this with force but too difficult and also I imagine kinematic would be required

	public float speed = 10f, length, boardingTime = 10f;
	public enum TrainStatus {Moving,Braking,BoardingTime,Accelerating,Idle}
	public TrainStatus status{ get; private set; }
	public Vector3 direction;

	private Rigidbody rb;
	private float totalDistance, startPosX, startSpeedX;
	private Animator animator;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		length = 20f; //TODO hard coded
		animator = GetComponent <Animator>();
		status = TrainStatus.Idle;
	}

	void FixedUpdate () {
		Vector3 newVelocity = rb.velocity;

		switch (status) {	//both of these rely on startSpeedX set at the point this status is set
		case TrainStatus.Braking:				
			if (rb.velocity.x <= 0.0001f) {								//if we have stopped then open doors
				rb.constraints |= RigidbodyConstraints.FreezePositionX;	//Set freeze x position
				OpenDoors ();
			} else {													//otherwise we are braking: reduce velocity gradually to zero
				newVelocity.x = SmoothlyAccelerateToTargetSpeed (0f);	//TODO: need to apply direction to this??
			}
			break;
		case TrainStatus.Accelerating:
			if (rb.velocity.x >= speed) {
				status = TrainStatus.Moving;
			} else {
				newVelocity.x = SmoothlyAccelerateToTargetSpeed (speed);
				//potentially Mathf.SmoothDamp( should be used for this
			}
			break;
		case TrainStatus.Idle:
			if (transform.position.x < -50f) {	//temp code to provide constant velocity when out of station
				direction = Vector3.right;
				startSpeedX = direction.x * 0.1f;
				startPosX = transform.position.x;
				totalDistance = length;
				status = TrainStatus.Accelerating;
			}
			break;
		}
		rb.velocity = newVelocity;
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
		status = TrainStatus.BoardingTime;
		Invoke ("CloseDoors", boardingTime);
	}

	void CloseDoors() {
		animator.SetTrigger ("doorClose");
		rb.constraints &= ~RigidbodyConstraints.FreezePositionX;	//Remove freeze x position (and let the carriage drift slightly)
		rb.velocity = -0.01f * speed * direction;
		Invoke ("Depart", 2f);
	}

	void Depart() {
		startSpeedX = direction.x * 0.1f;
		status = TrainStatus.Accelerating;
	}

}
