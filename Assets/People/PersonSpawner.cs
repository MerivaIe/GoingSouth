﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PersonSpawner : MonoBehaviour {

	[Tooltip ("Time between people spawns (0.2 for 5 per second)")]
	public float meanSpawnDelay = 1f;
	public GameObject personPrefab;
	public GameObject[] platforms;	//TODO: in future this should be a list in game manager and should be found using ObjectsOfType on Start

	private Spawnpoint[] spawnPoints;
	private GameObject organisingParent;

	void Start () {
		spawnPoints = GameObject.FindObjectsOfType <Spawnpoint> ();
		organisingParent = GameObject.Find ("People");
		if (!organisingParent) {Debug.LogWarning ("No 'People' organising game object found. Please create it.");}
	}
	
	void Update () {
		foreach (Spawnpoint spawnPoint in spawnPoints) {
			if (IsTimeToSpawn ()) {
				GameObject person = Instantiate (personPrefab, spawnPoint.transform.position, Quaternion.identity) as GameObject;
				person.transform.parent = organisingParent.transform;
				Vector3 platformPos = platforms[Random.Range (0, platforms.Length)].transform.position;	//set platform destination randomly from publically exposed platforms
				person.GetComponent <Person>().SetMovingToPlatform (platformPos);
			}
		}
	}

	bool IsTimeToSpawn() {
		float spawnsPerSecond = 1f /meanSpawnDelay;

		if(Time.deltaTime > meanSpawnDelay) {
			Debug.LogWarning("Spawn rate capped by frame rate (time between frames lower than spawn delay) ");
		}

		//arbitrary threshold based on frame rate (/ spawnPoints.Length because we are spawning at multiple sites at the moment)
		float threshold = spawnsPerSecond * Time.deltaTime / spawnPoints.Length;
		float randomValue = Random.value;
		return (randomValue < threshold);
	}
}
