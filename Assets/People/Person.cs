using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Person : MonoBehaviour {

	private NavMeshAgent nmAgent;
	private Rigidbody rb;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		nmAgent = GetComponent <NavMeshAgent>();
		nmAgent.SetDestination (GameObject.Find("Target").transform.position);
	}
	
	void Update () {
		//nmAgent.SetDestination (GameObject.Find("Target").transform.position);

		RaycastHit hit;
		if (Input.GetMouseButtonDown(0)) {
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out hit))
				nmAgent.SetDestination(hit.point);

		}

	}

	void OnCollisionEnter(Collision coll) {
		Debug.Log ("Colliding");
		if (coll.gameObject.GetComponent <Train> ()) {
			rb.isKinematic = false;
			nmAgent.enabled = false;
			Debug.Log ("Train script found in object");

		} else if (coll.gameObject.GetComponentInParent <Train> ()) {

			rb.isKinematic = false;
			nmAgent.enabled = false;
			Debug.Log ("Train script found in parent object");
		}
	}
}
