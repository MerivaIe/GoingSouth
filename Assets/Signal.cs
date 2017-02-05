using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Signal : MonoBehaviour {

	public enum SignalType {Brake,Accelerate}
	public SignalType signalType;
	public float stoppingPointX;

	void Start () {
		if (signalType == SignalType.Brake) {
			stoppingPointX = transform.position.x - 0.5f * GetComponent <BoxCollider>().size.x;
		}
	}


}
