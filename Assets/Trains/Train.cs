using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Train : MonoBehaviour {

	public float speed = 10f;
	private Rigidbody rb;
	private enum TravelDirection {Idle,Right,Left}
	private Vector3 newPos;
	private TravelDirection travelDirection;

	// Use this for initialization
	void Start () {
		rb = GetComponentInChildren <Rigidbody> ();
		newPos = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		if (transform.position.x < -50f) {
			travelDirection = TravelDirection.Right;
		} else if (transform.position.x > 50f) {
			travelDirection = TravelDirection.Left;
		}

		SetTravelVelocity ();
	}

	void SetTravelVelocity ()
	{
		if (travelDirection == TravelDirection.Right) {
			print ("Setting speed right" + Vector3.right * speed);
			//			newPos.x = transform.position.x + speed * Time.deltaTime;
			//			transform.position = newPos;
			rb.velocity = Vector3.right * speed;
			travelDirection = TravelDirection.Idle;
		}
		else if (travelDirection == TravelDirection.Left) {
			print ("Setting speed left" + Vector3.left * speed);
			//			newPos.x = transform.position.x - speed * Time.deltaTime;
			//			transform.position = newPos;
			rb.velocity = Vector3.left * speed;
			travelDirection = TravelDirection.Idle;
		}
	}

}
