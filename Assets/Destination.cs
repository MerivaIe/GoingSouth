using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Stores info against each destination
/// </summary>
public class Destination {
	string destination;
	float avgTimeBetweenTrains;	//player decided
	float routeLength;
	int noTrainsAssigned;		//player decided
	int estimatedDailyFootfall;

	public Destination(string _destination, float _routeLength, int _estimatedDailyFootfall) {
		destination = _destination;
		routeLength = _routeLength;
		estimatedDailyFootfall = _estimatedDailyFootfall;
	}
}
