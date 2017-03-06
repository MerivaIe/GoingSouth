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

	public TimetableItem(Destination _destination, float _schedArrivalTime, Platform _platform, Train _train) {
		destination = _destination;
		scheduledArrivalTime = _schedArrivalTime;
		platform = _platform;
		train = _train;
	}
}
