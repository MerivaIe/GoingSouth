using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// These people are very bad at boarding trains quickly. They will fanny around and even when on the train will move around inefficiently and perhaps move off. They pay vast sums though.
/// </summary>
public class Gentry : MonoBehaviour {

	public float checkProximityEvery = 1f,jostlingStrength = 0.25f;

	private static Vector3[] proximityDirections = new Vector3[8];
	private float nextProximityCheck, proximityDistance;
	private Person person;
	private Rigidbody rb;

	void Start () {
		person = GetComponent <Person> ();
		rb = GetComponent <Rigidbody> ();
		if (proximityDirections != null) {
			proximityDirections = new Vector3[8];
			for (int i = 0; i < proximityDirections.Length; i++) {	//to take this even further you should work out the angle (pi/4 is specific to 8 lines)- nd take in the total angle of fan (in this case 360)
				proximityDirections [i] = new Vector3 (Mathf.Cos (i * Mathf.PI / 4f), 0f, Mathf.Sin (i * Mathf.PI / 4f));		//using square wave function to calculate 8 directions at compass points
			}
		}
	}

	//There is a danger here in controlling the RB directly that you might conflict with Person control of RB. However, I am confident of the PersonStatus control over actions.
	void FixedUpdate () {
		//proximity Distance needs to be set at some point ahead of use
		proximityDistance = person.currentPlatform.incomingTrain.length;

		//this code handles manual control of people finding space
		if (person.status == Person.PersonStatus.ReadyToBoard && Time.time > nextProximityCheck) {
			nextProximityCheck = Time.time + checkProximityEvery;
			Vector3 newVelocity = rb.velocity, proximityCorrection = Vector3.zero;
			RaycastHit hit;
			float longestRayLength = 0f;
			foreach (Vector3 direction in proximityDirections) {
				Physics.Raycast (transform.position, direction, out hit, proximityDistance);
				if (hit.distance < proximityDistance && hit.distance > longestRayLength) {
					longestRayLength = hit.distance;
					proximityCorrection = direction.normalized;
					Debug.DrawRay (transform.position, direction.normalized * hit.distance, Color.green, 0.1f);
				}
			}
			newVelocity += proximityCorrection * jostlingStrength;
			rb.velocity = newVelocity;
		} else {
			return;
		}
	}
}
