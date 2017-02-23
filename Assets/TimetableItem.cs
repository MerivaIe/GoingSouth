using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ATM: centralised data repository.
/// </summary>
public class TimetableItem {
	public float scheduledArrivalTime;
	public Platform platform;
	public Train train;
	public string destination;
	public TimetableItem(float _schedArrivalTime, Platform _platform, Train _train, string _destination) {
		scheduledArrivalTime = _schedArrivalTime;
		platform = _platform;
		train = _train;
		destination = _destination;
	}
}
