using UnityEngine;

[RequireComponent(typeof(WaitingArea))]
public class Platform : MonoBehaviour {

	public int platformNumber;
	public BoxCollider platformSignalTrigger { get; private set; }
	[Tooltip("On train approach, does it stop to the left or to the right of the platform.")]
	public bool isLeftHanded = true;
	public WaitingArea waitingArea {get; private set;}

	void Start () {
		GameObject signalGO = GetComponentInChildren<Signal> ().gameObject;
		platformSignalTrigger = signalGO.GetComponent <BoxCollider> ();

		Vector3 signalPosition = signalGO.transform.localPosition;
		signalPosition.z = isLeftHanded ? 3.58f : -3.58f;
		signalGO.transform.localPosition = signalPosition;

		waitingArea = GetComponent <WaitingArea> ();
	}
		
	public void OnAssignedToTimetableItem(TimetableItem timetableItem) {
		//TODO: MEDIUM PRIORITY change colour of text being displayed and also the name of destination
	}

	public void OnRemovedFromTimetableItem(TimetableItem timetableItem) {
		//empty for now
	}

	public void OnTrainBoardingTime(Train train) {
		foreach (Person person in waitingArea.PeopleInWaitingArea) {
			person.OnTrainBoardingTime (train);
		}
	}
}
