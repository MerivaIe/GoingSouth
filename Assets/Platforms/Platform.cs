using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Platform : MonoBehaviour {

	//atm I envision Platform as fairly dumb, just holding next train details so People can query and get ready
	//and leaving the overall logic to GameManager who has knowledge of all platforms

	public string nextDeparture { get; private set; }
	public Train incomingTrain { get; private set; }
	public List<Person> peopleAtPlatform = new List<Person> ();

	private List<Vector3> waitLocations = new List<Vector3>();

	void Start () {
		nextDeparture = "Bristol"; //hard coding
		incomingTrain = GameManager.GetDeparture (nextDeparture);
		RecalculateWaitLocations ();
	}

	void RecalculateWaitLocations() {
		waitLocations.Clear ();
		Door[] doors = incomingTrain.GetComponentsInChildren <Door> ();
		Vector3 doorOffset = new Vector3 (0f,0.5f,-2f);//messy addition of -0.1f to xOffset to acount for trigger time telling train to slow
		foreach (Door door in doors) {
			doorOffset.x = door.gameObject.transform.localPosition.x;
			Vector3 newWait = GetComponentInChildren<Signal>().gameObject.GetComponent <BoxCollider> ().bounds.max + doorOffset;
			waitLocations.Add (newWait);
		}
	}

	void OnTriggerEnter(Collider coll) {
		Person person = coll.gameObject.GetComponent <Person> ();
		if (person) {
			peopleAtPlatform.Add (person);
			if (nextDeparture == person.destination) {
				Debug.Log("Destination matches desired destination so setting door location");
				person.GetComponent <NavMeshAgent>().SetDestination (waitLocations[Random.Range (0, waitLocations.Count)]);
				person.currentPlatform = this;
				person.status = Person.Status.ReadyToBoard;
			}
		}
	}

	void OnTriggerExit(Collider coll) {
		Person person = coll.gameObject.GetComponent <Person> ();
		if (person) {
			person.currentPlatform = null;
			try {
				peopleAtPlatform.Remove (person);
			} catch {
				Debug.LogWarning ("Trying to remove person from platform list but could not find them.");
			}
		}
	}

	void OnDrawGizmos() {
		if (waitLocations.Count > 0) {
			Gizmos.color = Color.green;
			foreach (Vector3 waitLocation in waitLocations) {
				Gizmos.DrawWireSphere (waitLocation, 1f);
			}
		}
	}
}
