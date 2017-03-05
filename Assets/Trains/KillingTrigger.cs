using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillingTrigger : MonoBehaviour {
	
	public float sqrMagDeathVelocity = 1f;
	private Rigidbody myTrainRigidbody;

	void Start () {
		myTrainRigidbody = GetComponentInParent <Rigidbody> ();
	}

	void OnTriggerEnter(Collider coll) {
		Person person = coll.GetComponent <Person> ();
		if (person && myTrainRigidbody.velocity.sqrMagnitude >= sqrMagDeathVelocity) {
			person.OnHitTrain ();
		}
	}

}
