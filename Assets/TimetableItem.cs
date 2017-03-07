using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ATM: centralised data repository.
/// </summary>
public class TimetableItem {
	public float scheduledArrivalTime;
	public Platform platform;
	public Train train;
	public Destination destination;

	public TimetableItem(Destination _destination, float _schedArrivalTime) {
		destination = _destination;
		scheduledArrivalTime = _schedArrivalTime;
	}

	public void SetTrain(Train _train) {
		train = _train;
		//let train know that this item is assigned to it
		train.myTimetableItem = this;
	}

	public void SetPlatform (Platform _platform) {
		platform = _platform;
		//TODO: let platform know this item is assigned to it???????????
	}
}
