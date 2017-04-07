using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ATM: centralised data repository.
/// There may be several journeys planned ahead of time without trains/platforms yet assigned to them.
/// If this class ever has to have more control over getting and setting then make all fields accessible through properties.
/// </summary>
public class TimetableItem {
	public float scheduledDepartureTime;
	public Platform platform;
	public Train train;
	public Destination destination;
	public bool modificationFlag;

	public TimetableItem(float _schedDepartureTime) {
		scheduledDepartureTime = _schedDepartureTime;
	}
}
