using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Train : MonoBehaviour {

	//tried doing all this with force but too difficult and also I imagine kinematic would be required

	public float speed = 10f, length;
	public enum TrainStatus {Moving,SetRight,SetLeft,Brake,BoardingTime}
	public TrainStatus status{ get; private set; }

	private Rigidbody rb;
	private float totalStoppingDistance, startPosX, startVelocityX;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		length = 20f; //TODO hard coded
	}

	void FixedUpdate () {
		//Deciding direction of motion
		if (transform.position.x < -50f) {
			status = TrainStatus.SetRight;
		} else if (transform.position.x > 50f) {
			status = TrainStatus.SetLeft;
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
				status = TrainStatus.BoardingTime;
			}
			break;
		case TrainStatus.BoardingTime:
			break;
		default:
			return;
		}
		rb.velocity = newVelocity;				//TODO: make sure that this does not cause jitter as the trains are non-kinematic and setting every FixedUpdate could be bad
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
