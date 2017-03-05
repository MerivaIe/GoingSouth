using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Platform : MonoBehaviour {

	[Tooltip("MUST SET THIS MANUALLY AS NAVMESH API IS LACKING")]
	public float nmAgentRadius = 0.5f;
	public float waitSpacing = 1f;
	public string nextDeparture { get; private set; }
	public Train incomingTrain { get; private set; }

	private List<WaitLocation> waitLocations = new List<WaitLocation> ();
	private Bounds platformTriggerBounds;

	void Start () {
		platformTriggerBounds = GetComponentInChildren<PlatformTrigger>().gameObject.GetComponent <BoxCollider>().bounds;
		nextDeparture = "Bristol"; //hard coding
		incomingTrain = GameManager.GetNextTrain (this);

		CalculateNewWaitLocations ();
	}
		
	void CalculateNewWaitLocations(){	// a nice way of doing this would be to store old locations generated and workaround them
		PoissonDiscSampler sampler = new PoissonDiscSampler (platformTriggerBounds.size.x - 2*nmAgentRadius, platformTriggerBounds.size.z - 2*nmAgentRadius, waitSpacing);
		foreach (Vector2 sample in sampler.Samples()) {
			Vector3 waitLocation = platformTriggerBounds.min;	//place at the '0,0' location for the generated grid
			waitLocation.x += sample.x + nmAgentRadius;
			waitLocation.z += sample.y + nmAgentRadius;
			NavMeshHit hit;										//using SamplePosition to make absolutely sure the wait location we have generated ends up on the navmesh
			if (NavMesh.SamplePosition (waitLocation, out hit, 0.5f, NavMesh.AllAreas)) {
				waitLocations.Add (new WaitLocation(hit.position,null));
			}
		}
	}

	public Vector3 RegisterPerson(Person person) {
		List<WaitLocation> freeWaitLocations = waitLocations.Where (a => a.person == null).ToList ();
		if (freeWaitLocations.Count == 0) {	//no more wait locations, calculate some more
			CalculateNewWaitLocations ();
			freeWaitLocations = waitLocations;
		}
		WaitLocation waitLocation = freeWaitLocations [Random.Range (0, freeWaitLocations.Count ())];
		waitLocation.person = person;
		return waitLocation.position;
	}

	public void UnregisterPerson(Person person) {
		try {
			waitLocations.First(a => a.person == person).person = null;	//set person = null for this waitLocation
		} catch {
			Debug.LogError ("Could not find expected person in platform list.");
		}
	}
		
	private class WaitLocation {
		public Vector3 position;
		public Person person;
		public WaitLocation(Vector3 _pos, Person _person) {
			position = _pos;
			person = _person;
		}
	}

	void OnDrawGizmos() {
		if (waitLocations.Count > 0) {
			Gizmos.color = Color.yellow;
			foreach (WaitLocation waitLocation in waitLocations.Where (a => a.person == null)) {
				Gizmos.DrawSphere (waitLocation.position, 0.1f);
			}
		}
	}
}
