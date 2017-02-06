using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Train : MonoBehaviour {

	//tried doing all this with force but too difficult and also I imagine kinematic would be required

	public float speed = 10f;
	public float[] DoorLocations { get{return doorLocations;} }

	private TravelDirection travelDirection;
	private enum TravelDirection {Idle,Moving,SetRight,SetLeft,Brake}
	private Rigidbody rb;
	private float stoppingInterval = 0f;

	private float[] doorLocations = new float[2];						//X distance from very front of the train

	void Start () {
		rb = GetComponentInChildren <Rigidbody> ();
		doorLocations [1] = 6.7f;						//if trains are made at runtime later, then set doorLocations as they are generated. A pivot point on the front of the train would work nicely
		doorLocations [2] = 16.8f;

	}

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
			newVelocity = Vector3.Lerp (rb.velocity, Vector3.zero, stoppingInterval);				//reduce velocity gradually to zero, interval set OnTriggerEnter if triggered by Brake Signal
			if (newVelocity.Equals (Vector3.zero)) {travelDirection = TravelDirection.Idle;}
		} else {
			//TODO: make sure that this does not cause jitter as the trains are non-kinematic and setting every FixedUpdate could be bad
			Debug.Log ("Returning from FixedUpdate without setting speed.");
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
				stoppingInterval = Time.fixedDeltaTime * rb.velocity.x / coll.bounds.size.x;		//interval = dist travelled each fixed update / total dist to stop over
			}
		}
	}

}
