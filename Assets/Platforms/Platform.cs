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

	public Vector3 GetRandomWaitLocation() {
		return waitLocations [Random.Range (0, waitLocations.Count)];
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
