using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Train : MonoBehaviour {

	public float brakeTime = 1f;
	public float speed = 10f;

	private TravelDirection travelDirection;
	private enum TravelDirection {Idle,Right,Left,Brake}
	private Rigidbody rb;
	private Vector3 newPos;


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
			rb.velocity = Vector3.right * speed;
			travelDirection = TravelDirection.Idle;
		}
		else if (travelDirection == TravelDirection.Left) {
			print ("Setting speed left" + Vector3.left * speed);
			rb.velocity = Vector3.left * speed;
			travelDirection = TravelDirection.Idle;
		} else if (travelDirection == TravelDirection.Brake) {
			//reduce velocity gradually to 0
			Vector3 reducedVelocity = Vector3.Lerp (rb.velocity,Vector3.zero,brakeTime*Time.deltaTime);
			print ("Reducing velocity to: " + reducedVelocity);
			rb.velocity = reducedVelocity;
		}
	}

	public void Brake(float stopPointX) {
		travelDirection = TravelDirection.Brake;
	}

}
