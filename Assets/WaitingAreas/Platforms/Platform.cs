using UnityEngine;

public class Platform : MonoBehaviour {

	public int platformNumber;
	public Bounds platformSignalBounds { get; private set; }

	void Start () {
		platformSignalBounds = GetComponentInChildren<Signal> ().gameObject.GetComponent <BoxCollider> ().bounds;
	}
		
	public void OnAssignedToTimetableItem(TimetableItem timetableItem) {
		//TODO change colour of text being displayed and also the name of destination
	}
}
