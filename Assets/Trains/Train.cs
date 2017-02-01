using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Train : MonoBehaviour {

	public float speed = 10f;
	private Rigidbody rb;
	private bool travelRight = true;
	private Vector3 newPos;

	// Use this for initialization
	void Start () {
		rb = GetComponentInChildren <Rigidbody> ();
		newPos = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		if (transform.position.x < -50f) {
			travelRight = true;
		} else if (transform.position.x > 50f) {
			travelRight = false;
		}

		if (travelRight) {
			print ("Setting speed right" + Vector3.right * speed);
			newPos.x = transform.position.x + speed * Time.deltaTime;
			transform.position = newPos;
			//rb.velocity = Vector3.right * speed;
		} else {
			print ("Setting speed left" + Vector3.left * speed);
			newPos.x = transform.position.x - speed * Time.deltaTime;
			transform.position = newPos;
			//rb.velocity = Vector3.left * speed;
		}
	}
}
