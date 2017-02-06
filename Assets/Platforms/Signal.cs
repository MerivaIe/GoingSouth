using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Signal : MonoBehaviour {

	public enum SignalType {Brake,Accelerate}
	public SignalType signalType;

	void Start () {
		if (signalType == SignalType.Brake) {
		}
	}


}
