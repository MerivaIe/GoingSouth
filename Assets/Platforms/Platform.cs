using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : MonoBehaviour {

	//atm I envision Platform as fairly dumb, just holding next train details so People can query and get ready
	//and leaving the overall logic to GameManager who has knowledge of all platforms

	public string nextDeparture { get; private set; }
	public Train incomingTrain { get; private set; }

	private List<Vector3> waitLocations = new List<Vector3>();

	void Start () {
		nextDeparture = "Bristol"; //hard coding
		incomingTrain = GameManager.GetDeparture (nextDeparture);
		RecalculateWaitLocations ();
	}

	void RecalculateWaitLocations() {
		waitLocations.Clear ();
		foreach (float xLocation in incomingTrain.DoorLocations) {
			Vector3 offset = new Vector3 (-xLocation-0.1f,0.5f,-2f);		//messy addition of -0.1f to xOffset to acount for trigger time telling train to slow
			Vector3 newWait = transform.parent.gameObject.GetComponentInChildren <BoxCollider> ().bounds.max + offset;
			waitLocations.Add (newWait);
		}
	}

	public Vector3 GetRandomWaitLocation() {
		return waitLocations[Random.Range (0, waitLocations.Count)];
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
