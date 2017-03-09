using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Signal : MonoBehaviour {

	public enum SignalType {Brake,Accelerate}

	public SignalType signalType;

	//TODO maybe on a signal change though you should reset triggers so that the TrainController here starts accelerating
}
