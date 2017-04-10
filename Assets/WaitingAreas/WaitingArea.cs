using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class WaitingArea : MonoBehaviour {

	public float waitSpacing = 1f;
	public Bounds waitAreaTriggerBounds { get; private set; }

	private List<WaitLocation> waitingPeople = new List<WaitLocation> ();
	private List<Person> nonWaitingPeople = new List<Person> ();	//could be people passing through, could be people that are kicked back on to platform whilst boarding

	void Start () {
		waitAreaTriggerBounds = GetComponentInChildren<WaitingAreaTrigger>().gameObject.GetComponent <BoxCollider>().bounds;
	}

	public IEnumerable<Person> PeopleInWaitingArea {
		get {
			return nonWaitingPeople.Concat (waitingPeople.Where (w => w.person != null).Select (w => w.person));
		}
	}

	void CalculateNewWaitLocations(){	// a nice way of doing this would be to store old locations generated and workaround them
		PoissonDiscSampler sampler = new PoissonDiscSampler (waitAreaTriggerBounds.size.x - 2*Person.nmAgentRadius, waitAreaTriggerBounds.size.z - 2*Person.nmAgentRadius, waitSpacing);
		foreach (Vector2 sample in sampler.Samples()) {
			Vector3 waitLocation = waitAreaTriggerBounds.min;	//place at the '0,0' location for the generated grid
			waitLocation.x += sample.x + Person.nmAgentRadius;
			waitLocation.z += sample.y + Person.nmAgentRadius;
			NavMeshHit hit;										//using SamplePosition to make absolutely sure the wait location we have generated ends up on the navmesh
			if (NavMesh.SamplePosition (waitLocation, out hit, 0.5f, NavMesh.AllAreas)) {
				waitingPeople.Add (new WaitLocation(hit.position,null));
			}
		}
	}

	public Vector3 RegisterPersonForWaiting(Person person) {
		List<WaitLocation> freeWaitLocations = waitingPeople.Where (a => a.person == null).ToList ();
		if (freeWaitLocations.Count == 0) {	//no more wait locations, calculate some more
			CalculateNewWaitLocations ();
			freeWaitLocations = waitingPeople;
		}
		WaitLocation waitLocation = freeWaitLocations [Random.Range (0, freeWaitLocations.Count ())];
		waitLocation.person = person;
		return waitLocation.position;
	}

	public void RegisterNonWaitingPerson(Person person) {
		nonWaitingPeople.Add (person);
	}

	public void UnregisterPerson(Person person) {
		WaitLocation waitLocation = waitingPeople.FirstOrDefault(w => w.person == person);
		if (waitLocation != null) {
			waitLocation.person = null;	//set person = null for this waitLocation
		} else {
			if (!nonWaitingPeople.Remove (person)) {
				Debug.LogWarning ("A person could not be found to unregister.");
			}
		}
	}

	public bool IsPersonRegistered(Person personToSearchFor) {
		return nonWaitingPeople.Contains (personToSearchFor) || waitingPeople.Any (w => w.person == personToSearchFor) ;
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
		if (waitingPeople.Count > 0) {
			Gizmos.color = Color.yellow;
			foreach (WaitLocation waitLocation in waitingPeople.Where (a => a.person == null)) {
				Gizmos.DrawSphere (waitLocation.position, 0.1f);
			}
		}
	}
}
