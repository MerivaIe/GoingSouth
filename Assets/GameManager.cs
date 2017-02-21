using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GameManager : MonoBehaviour {

	public GameObject trainPrefab;
	public const float minutesPerSecond = 0.5f;

	private static List<TimetableItem> timetable = new List<TimetableItem>();
	private static List<Train> trainPool = new List<Train>();
	private Text clockText;

	void Awake () {
		clockText = GameObject.Find ("ClockText").GetComponent <Text> ();
		trainPool = GameObject.FindObjectsOfType<Train>().ToList ();	//this would eventually be Instantiating trains at level load based on user decisions
		//savagely hard coded link to just one platform and one train at the moment:
		timetable.Add (new TimetableItem(0f,GameObject.FindObjectOfType <Platform>(),GameObject.Find ("Complex Train (1)").GetComponent <Train>(),"Bristol"));
	}

	void Update() {
		clockText.text = string.Format("{0:#00}:{1:00}", Mathf.Floor(Time.time * minutesPerSecond / 60),Mathf.Floor(Time.time * minutesPerSecond) % 60);
	}

	public static Train GetNextTrain(Platform platform) {
		Train train = timetable.FirstOrDefault (a => a.platform == platform).train;
		if (!train) {
			Debug.LogWarning("Couldn't find train for the requested platform.");
		}
		return train;
	}
		

	/// <summary>
	/// ATM: centralised data repository.
	/// </summary>
	public struct TimetableItem {
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
}
