using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Train : MonoBehaviour {

	//tried doing all this with force but too difficult and also I imagine kinematic would be required

	public float speed = 10f;
	public bool isBoardingTime = false;
	public TravelDirection travelDirection{ get; private set; }
	public enum TravelDirection {BoardingTime,Moving,SetRight,SetLeft,Brake}

	private Rigidbody rb;
	private float totalStoppingDistance;
	private float startPosX;
	private float startVelocityX;

	void Start () {
		rb = GetComponent <Rigidbody> ();
	}

	void FixedUpdate () {
		//Deciding direction of motion
		if (transform.position.x < -50f) {
			travelDirection = TravelDirection.SetRight;
		} else if (transform.position.x > 50f) {
			travelDirection = TravelDirection.SetLeft;
		}

		Vector3 newVelocity = rb.velocity;
		//Executing direction of motion
		if (travelDirection == TravelDirection.SetRight) {
			newVelocity = Vector3.right * speed;
			travelDirection = TravelDirection.Moving;
		} else if (travelDirection == TravelDirection.SetLeft) {
			newVelocity = Vector3.left * speed;
			travelDirection = TravelDirection.Moving;
		} else if (travelDirection == TravelDirection.Brake) {
			newVelocity.x = Mathf.Lerp (startVelocityX,0f,(transform.position.x-startPosX)/totalStoppingDistance);	//reduce velocity gradually to zero
			if (newVelocity.x <= 0.0001f) {
				newVelocity.x = 0f;
				travelDirection = TravelDirection.BoardingTime;
			}
		} else {
			return;
		}

		//TODO: make sure that this does not cause jitter as the trains are non-kinematic and setting every FixedUpdate could be bad
		rb.velocity = newVelocity;
	}
		
	void OnTriggerEnter(Collider coll) {
		Signal signal = coll.GetComponent <Signal>();
		if (signal) {
			if (signal.signalType == Signal.SignalType.Brake) {
				travelDirection = TravelDirection.Brake;
				startVelocityX = rb.velocity.x;
				startPosX = transform.position.x;
				totalStoppingDistance = coll.bounds.max.x-startPosX; //end of signal trigger - position of front of train
			}
		}
	}

}
