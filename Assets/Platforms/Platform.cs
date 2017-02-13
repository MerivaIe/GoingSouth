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
	private float navTargetThreshold = 0.1f;

	void Start () {
		nextDeparture = "Bristol"; //hard coding
		incomingTrain = GameManager.GetDeparture (nextDeparture);
		RecalculateWaitLocations ();
	}

	void RecalculateWaitLocations() {
		waitLocations.Clear ();
		Door[] doors = incomingTrain.GetComponentsInChildren <Door> ();
		Vector3 newWait;
		newWait.y = GetComponentInChildren<Signal> ().gameObject.GetComponent <BoxCollider> ().bounds.max.y + 0.5f;
		newWait.z = transform.position.z;
		foreach (Door door in doors) {
			newWait.x = GetComponentInChildren<Signal>().gameObject.GetComponent <BoxCollider> ().bounds.max.x + door.gameObject.transform.localPosition.x;
			waitLocations.Add (newWait);
		}
	}

	public Vector3 GetRandomWaitLocation() {
		return waitLocations [Random.Range (0, waitLocations.Count)];
	}

	public float GetNavTargetThreshold() {
		return navTargetThreshold * peopleAtPlatform.Count;
	}

	public void RegisterPerson(Person person) {
		peopleAtPlatform.Add (person);
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
