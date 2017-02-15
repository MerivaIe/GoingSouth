using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Platform : MonoBehaviour {

	//atm I envision Platform as fairly dumb, just holding next train details so People can query and get ready
	//and leaving the overall logic to GameManager who has knowledge of all platforms

	public float waitSpacing = 1f;
	public string nextDeparture { get; private set; }
	public Train incomingTrain { get; private set; }
	public List<Vector3> targetLocations {get; private set;}

	private List<Person> peopleAtPlatform = new List<Person> ();
	private List<Vector3> waitLocations = new List<Vector3> ();
	private Bounds platformTriggerBounds, platformSignalBounds;

	void Start () {
		platformTriggerBounds = GetComponent <BoxCollider>().bounds;
		platformSignalBounds = GetComponentInChildren<Signal> ().gameObject.GetComponent <BoxCollider> ().bounds;
		nextDeparture = "Bristol"; //hard coding
		incomingTrain = GameManager.GetDeparture (nextDeparture);

		targetLocations = new List<Vector3> ();
		RecalculateTargetLocations ();
		CalculateNewWaitLocations ();
	}
	void Update() {
		if (Input.GetMouseButtonDown (0)) {
			CalculateNewWaitLocations ();
		}
	}

	void RecalculateTargetLocations() {
		targetLocations.Clear ();
		Door[] doors = incomingTrain.GetComponentsInChildren <Door> ();
		Vector3 newTarget;
		newTarget.y = platformSignalBounds.max.y + 0.5f;
		newTarget.z = platformSignalBounds.center.z;
		foreach (Door door in doors) {
			newTarget.x = platformSignalBounds.max.x + door.gameObject.transform.localPosition.x;
			targetLocations.Add (newTarget);
		}
	}

	void CalculateNewWaitLocations(){
		PoissonDiscSampler sampler = new PoissonDiscSampler (platformTriggerBounds.size.x, platformTriggerBounds.size.z, waitSpacing);
		foreach (Vector2 sample in sampler.Samples()) {
			Vector3 waitLocation = platformTriggerBounds.min;	//place at the '0,0' location for the generated grid
			waitLocation.x += sample.x;
			waitLocation.z += sample.y;
			NavMeshHit hit;
			if (NavMesh.SamplePosition (waitLocation, out hit, 0.5f, NavMesh.AllAreas)) {
				waitLocations.Add (hit.position);
			}
		}
	}

	public Vector3 GetNewWaitLocation() {
		if (waitLocations.Count == 0) {
			CalculateNewWaitLocations ();
		}
		int randIndex = Random.Range (0, waitLocations.Count);
		Vector3 waitLocation = waitLocations [randIndex];
		waitLocations.RemoveAt (randIndex);
		return waitLocation;
	}
		
	public void RegisterPerson(Person person) {
		peopleAtPlatform.Add (person);
	}

	public void UnregisterPerson(Person person) {
		peopleAtPlatform.Remove (person);	//perhaps the wait locations should be the store for people...
	}

	void OnDrawGizmos() {
		if (targetLocations.Count > 0) {
			Gizmos.color = Color.green;
			foreach (Vector3 targetLocation in targetLocations) {
				Gizmos.DrawWireSphere (targetLocation, 1f);
			}
		}
		if (waitLocations.Count > 0) {
			Gizmos.color = Color.yellow;
			foreach (Vector3 waitLocation in waitLocations) {
				Gizmos.DrawSphere (waitLocation, 0.1f);
			}
		}
	}
}
