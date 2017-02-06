using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Person : MonoBehaviour {
	
	public enum PersonStatus{ToWaitingArea,AtWaitingArea,ToPlatform,AtPlatform,ToTrain,AtTrain}
	public PersonStatus Status { get { return personStatus; } }

	private PersonStatus personStatus;
	private string destination;
	private NavMeshAgent nmAgent;
	private Rigidbody rb;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		nmAgent = GetComponent <NavMeshAgent>();
		destination = "Bristol";
	}

	void OnCollisionEnter(Collision coll) {
		Debug.Log ("Person collided with " + coll.gameObject.name);
		//TODO: create a trigger box just ahead of the collider and the FIRST time a person enters it turn their kinemtic and nmagent off ready for beautiful physics interactions
		if (coll.gameObject.GetComponentInParent <Train> ()) {								//hit by train (trigger) so turn off kinematic
			rb.isKinematic = false;
			nmAgent.enabled = false;
			Debug.Log ("Train script found in parent object. Turning person to ragdoll");
		}
	}

	void OnTriggerEnter(Collider coll) {
		Platform platform = coll.gameObject.transform.parent.gameObject.GetComponentInChildren <Platform> ();
		if (platform) {
			if (platform.nextDeparture == destination) {
				Debug.Log("Destination matches desired destination so setting door location");
				personStatus = PersonStatus.AtPlatform;
				nmAgent.SetDestination (platform.GetRandomWaitLocation ());
				Debug.Log ("Set destination for person");
			}
		}
	}
}
