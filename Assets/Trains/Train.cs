using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Train : MonoBehaviour {

	//tried doing all this with force but too difficult and also I imagine kinematic would be required

	public float speed = 10f, length, boardingTime = 10f;
	public enum TrainStatus {Moving,SetRight,SetLeft,Brake,BoardingTime,SettingOff}
	public TrainStatus status{ get; private set; }

	private Rigidbody rb;
	private float totalStoppingDistance, startPosX, startVelocityX, boardingEndTime;
	private Animator animator;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		length = 20f; //TODO hard coded
		animator = GetComponent <Animator>();
	}

	void FixedUpdate () {
		//Deciding direction of motion
		if (transform.position.x < -50f) {
			status = TrainStatus.SetRight;
		} else if (transform.position.x > 50f) {
			//status = TrainStatus.SetLeft;
		}

		Vector3 newVelocity = rb.velocity;
		//Executing direction of motion
		switch (status) {
		case TrainStatus.SetRight:
			newVelocity = Vector3.right * speed;
			status = TrainStatus.Moving;
			break;
		case TrainStatus.SetLeft:
			newVelocity = Vector3.left * speed;
			status = TrainStatus.Moving;
			break;
		case TrainStatus.Brake:
			newVelocity.x = Mathf.Lerp (startVelocityX, 0f, (transform.position.x - startPosX) / totalStoppingDistance);	//reduce velocity gradually to zero
			if (newVelocity.x <= 0.0001f) {
				rb.constraints |= RigidbodyConstraints.FreezePositionX;	//Set freeze x position
				animator.SetTrigger ("doorOpen");
				status = TrainStatus.BoardingTime;
				boardingEndTime = Time.time + boardingTime;
			}
			break;
		case TrainStatus.BoardingTime:
			if (Time.time > boardingEndTime) {
				animator.SetTrigger ("doorClose");
				status = TrainStatus.SettingOff;
			}
			break;
		case TrainStatus.SettingOff:
			//maybe check if doors in idela nimation and then set off
			rb.constraints &= ~ RigidbodyConstraints.FreezePositionX;	//Remove freeze x position (and let the carriage drift slightly)
			//some velocity?
			Invoke("DepartPlatform",3f);
			break;
		default:
			return;
		}
		rb.velocity = newVelocity;
	}
		
	void OnTriggerEnter(Collider coll) {
		Signal signal = coll.GetComponent <Signal>();
		if (signal) {
			if (signal.signalType == Signal.SignalType.Brake) {
				status = TrainStatus.Brake;
				startVelocityX = rb.velocity.x;
				startPosX = transform.position.x;
				totalStoppingDistance = coll.bounds.max.x-startPosX; //end of signal trigger - position of front of train
			}
		}
	}

	public void DepartPlatform() {
		status = TrainStatus.SetRight;
	}

}
