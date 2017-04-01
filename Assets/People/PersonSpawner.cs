using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PersonSpawner : MonoBehaviour {

	public GameObject personPrefab;

	private Spawnpoint[] spawnPoints;
	private GameObject organisingParent;

	void Start () {
		spawnPoints = GameObject.FindObjectsOfType <Spawnpoint> ();
		organisingParent = GameObject.Find ("People");
		if (!organisingParent) {Debug.LogWarning ("No 'People' organising game object found. Please create it.");}
	}
	
	void Update () {
		foreach (Destination dest in GameManager.instance.destinations) {
			if (IsTimeToSpawn (dest)) {
				GameObject personGO = Instantiate (personPrefab, spawnPoints[Random.Range (0,spawnPoints.GetUpperBound (0))].transform.position, Quaternion.identity, organisingParent.transform) as GameObject;
				Person person = personGO.GetComponent <Person> ();
				person.SetMovingToWaitingArea (false,GameManager.instance.foyerGO.transform.position);
				person.desiredDestination = dest;
			}
		}
	}

	bool IsTimeToSpawn(Destination destination) {
		float spawnsPerSecond = 1f / destination.meanSpawnDelay;

		if(Time.deltaTime > destination.meanSpawnDelay) {
			Debug.LogWarning("Spawn rate capped by frame rate (time between frames lower than spawn delay) ");
		}

		//arbitrary threshold based on frame rate
		float threshold = spawnsPerSecond * Time.deltaTime;
		float randomValue = Random.value;
		return (randomValue < threshold);
	}
}
