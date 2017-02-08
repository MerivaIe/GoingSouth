
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Person : MonoBehaviour {

	public enum PersonStatus{ReadyToBoard,Boarded,Moving,Alighted,WrongPlatform,Compromised}
	public PersonStatus status; 
	public string destination{ get; private set; }
	public Platform currentPlatform;
	public float navTargetThreshold = 0.1f, jostlingStrength = 0.25f, centreOfMassYOffset = -0.5f, checkProximityEvery = 1f, randomProximityMod = 0.1f, deathVelocity = 1f, boardingSpeed = 10f;

	private NavMeshAgent nmAgent;
	private Rigidbody rb;
	private Vector3[] proximityDirections = new Vector3[8];
	private float nextProximityCheck, proximityDistance;

	void Start () {
		rb = GetComponent <Rigidbody> ();
		nmAgent = GetComponent <NavMeshAgent>();
		destination = "Bristol";
		rb.centerOfMass = new Vector3(0f,centreOfMassYOffset,0f);
		for (int i=0;i<proximityDirections.Length;i++) {
			proximityDirections [i] = new Vector3 (Mathf.Cos (i*Mathf.PI/4f),0f,Mathf.Sin (i*Mathf.PI/4f));		//using square wave function to calculate 8 directions at compass points
		}
	}

	void FixedUpdate() {
		Vector3 newVelocity = rb.velocity;
		if (currentPlatform) {		
			if (status == PersonStatus.ReadyToBoard && currentPlatform.incomingTrain.status == Train.TrainStatus.BoardingTime) { //if train is at platform
				if (!nmAgent.hasPath) {
					Debug.LogError ("NavMeshAgent has no path: slipped through cracks?");
				} else if (nmAgent.remainingDistance <= navTargetThreshold) {
					ToggleAgentControl (false);
					newVelocity = Vector3.forward * boardingSpeed;
					proximityDistance = currentPlatform.incomingTrain.length;
					status = PersonStatus.Boarded;											//may want a better way of checking to confirm boarded
				}
			} else {return;}
		} else if (!rb.isKinematic && Time.time > nextProximityCheck) {	//removed check on isBoarded
			nextProximityCheck = Time.time + checkProximityEvery;
			Vector3 proximityCorrection = Vector3.zero;
			RaycastHit hit;

//			foreach(Vector3 direction in proximityDirections) {
//				Physics.Raycast (transform.position, direction, out hit, proximityDistance);
//				Vector3 newProxCorr = direction.normalized * hit.distance;
//				proximityCorrection += newProxCorr;											//add it to the accumulating proximityCorrection we are building up
//				Debug.DrawRay (transform.position,newProxCorr,Color.green,0.1f);
//			}
			float longestRayLength = 0f;

			foreach(Vector3 direction in proximityDirections) {
				Physics.Raycast (transform.position, direction, out hit, proximityDistance);
				if (hit.distance < proximityDistance && hit.distance > longestRayLength) {
					longestRayLength = hit.distance;
					proximityCorrection = direction.normalized;
				}
				Debug.DrawRay (transform.position,direction.normalized * hit.distance,Color.green,0.1f);
			}

			newVelocity = proximityCorrection * jostlingStrength;
			//newVelocity += jostlingStrength * proximityCorrection/proximityDirections.Length;
			//Debug.Log ("After proximity checking of: " + jostlingStrength * proximityCorrection/proximityDirections.Length + ", giving person newVelocity: " + newVelocity );

			//Use the following for random movement if anything in overlap
//			newVelocity += new Vector3 (Random.Range(-jostlingStrength,jostlingStrength),0f,Random.Range(-jostlingStrength,jostlingStrength));
		} else {return;}
		rb.velocity = newVelocity;
	}

	public void ToggleAgentControl(bool isTurnOn) {
			rb.isKinematic = isTurnOn;
			nmAgent.enabled = isTurnOn;
	}

	void OnTriggerEnter(Collider coll) {
		Train train = coll.gameObject.GetComponentInParent <Train> ();
		if (train) {					//turn their kinemtic and nmagent off ready for beautiful physics interactions
			if (train.gameObject.GetComponent <Rigidbody> ().velocity.x > deathVelocity) {
				status = PersonStatus.Compromised;
				ToggleAgentControl (false);
				Debug.Log ("Train script found in parent object. Turning person to ragdoll");
			}
		}
		if (status != PersonStatus.Compromised) {
			Platform platform = coll.gameObject.GetComponent <Platform> ();
			if (platform) {
				currentPlatform = platform;
				if (platform.nextDeparture == destination) {
					status = PersonStatus.ReadyToBoard;
					ToggleAgentControl (true);
					nmAgent.SetDestination (platform.GetRandomWaitLocation ());
				} else {
					status = PersonStatus.WrongPlatform;
				}
			}
		}
	}

	void OnTriggerExit(Collider coll) {
		if (status != PersonStatus.Compromised) {
			Platform platform = coll.gameObject.GetComponent <Platform> ();
			if (platform) {
				currentPlatform = null;
			}
		}
	}
}
