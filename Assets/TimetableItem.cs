using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ATM: centralised data repository. The idea being that there may be several journeys planned ahead of time without trains/platforms yet assigned to them
/// </summary>
public class TimetableItem {
	public float scheduledArrivalTime;
	public Platform platform;
	public Train train;
	public Destination destination;
	public bool journeyInProgress;	//required?

	public TimetableItem(Destination _destination, float _schedArrivalTime) {
		destination = _destination;
		scheduledArrivalTime = _schedArrivalTime;
	}

	public void SetTrain(Train _train) {
		train = _train;
		//TODO: need to decide how something checks which current timetable the train is on
		train.SetCurrentTimetableItem (this);
	}

	public void SetPlatform (Platform _platform) {
		platform = _platform;
		//TODO: let platform know this item is assigned to it???????????
	}

	public void SetJourneyActive(bool _journeyInProg) {
		journeyInProgress = _journeyInProg;
	}
}
