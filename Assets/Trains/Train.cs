using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Train : MonoBehaviour {

	//tried doing all this with force but too difficult and also I imagine kinematic would be required

	public float speed = 10f;

	private TravelDirection travelDirection;
	private enum TravelDirection {Idle,Moving,SetRight,SetLeft,Brake}
	private Rigidbody rb;
	private float stoppingInterval = 0f;

	void Start () {
		rb = GetComponentInChildren <Rigidbody> ();
	}

	//maybe use force to apply movement and brake.
	void FixedUpdate () {
		//Setting direction of motion
		if (transform.position.x < -50f) {
			travelDirection = TravelDirection.SetRight;
		} else if (transform.position.x > 50f) {
			travelDirection = TravelDirection.SetLeft;
		}

		Vector3 newVelocity;
		//Executing direction of motion
		if (travelDirection == TravelDirection.SetRight) {
			newVelocity = Vector3.right * speed;
			travelDirection = TravelDirection.Moving;
		} else if (travelDirection == TravelDirection.SetLeft) {
			newVelocity = Vector3.left * speed;
			travelDirection = TravelDirection.Moving;
		} else if (travelDirection == TravelDirection.Brake) {
			//float stopDuration = (stoppingPointX - transform.position.x) / Mathf.Pow (rb.velocity.x, 2);
			//reduce velocity gradually to zero
			newVelocity = Vector3.Lerp (rb.velocity, Vector3.zero, stoppingInterval);
			if (newVelocity.Equals (Vector3.zero)) {travelDirection = TravelDirection.Idle;}
		} else {
			//keep same TODO: make sure that this does not cause jitter as the trains are non-kinematic and setting every FixedUpdate could be bad
			return;
		}

		rb.velocity = newVelocity;
	}
		
	void OnTriggerEnter(Collider coll) {
		Signal signal = coll.GetComponent <Signal>();
		if (signal) {
			Debug.Log ("Train " + gameObject.name + " triggered by signal" + coll.gameObject.name);
			if (signal.signalType == Signal.SignalType.Brake) {
				travelDirection = TravelDirection.Brake;
				float stoppingPointX = coll.gameObject.transform.position.x + coll.bounds.extents.x; //TODO change so this is point on platform it needs to stop at
				stoppingInterval = Time.fixedDeltaTime * rb.velocity.x / (stoppingPointX - transform.position.x);
			}
		}
	}

}
