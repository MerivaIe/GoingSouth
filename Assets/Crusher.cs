using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crusher : MonoBehaviour {

	public Vector3 crushingForce;

	private Rigidbody rb;

	void Start () {
		rb = GetComponent <Rigidbody> ();
	}
	
	void FixedUpdate () {
		rb.AddForce (crushingForce);
	}
}
