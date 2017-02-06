using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Person : MonoBehaviour {

	public PersonStatus personStatus;				//public get, privat set?
	public enum PersonStatus{ToWaitingArea,AtWaitingArea,ToPlatform,AtPlatform,ToTrain,AtTrain}
	public bool manualTestingOn =false;

	private NavMeshAgent nmAgent;
	private Rigidbody rb;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		nmAgent = GetComponent <NavMeshAgent>();
	}
	
	void Update () {

	}

	void OnCollisionEnter(Collision coll) {
		//TODO: ccreate a trigger box just ahead of the collider and the FIRST time a person enters it turn their kinemtic and nmagent off ready for beautiful physics interactions
		if (coll.gameObject.GetComponentInParent <Train> ()) {								//hit by train (trigger) so turn off kinematic
			rb.isKinematic = false;
			nmAgent.enabled = false;
			Debug.Log ("Train script found in parent object. Turning person to ragdoll");
		}
		Person person = coll.gameObject.GetComponent <Person>();
		if (person) {

		}
	}
}
