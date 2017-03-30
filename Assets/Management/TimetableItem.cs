using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ATM: centralised data repository. The idea being that there may be several journeys planned ahead of time without trains/platforms yet assigned to them
/// </summary>
public class TimetableItem {
	public float scheduledDepartureTime;
	public Platform platform { get; private set; }
	public Train train { get; private set; }
	public Destination destination { get; private set; }

	public TimetableItem(float _schedDepartureTime) {
		scheduledDepartureTime = _schedDepartureTime;
	}

	public void SetDestination(Destination _destination) {
		destination = _destination;
	}

	public void SetTrain(Train _train) {
		train = _train;
		//TODO: need to decide how something checks which current timetable the train is on
		train.OnAssignedToTimetableItem (this);
	}

	public void SetPlatform (Platform _platform) {
		platform = _platform;
		platform.OnAssignedToTimetableItem (this);
	}
}
