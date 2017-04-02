using UnityEngine;

public class Platform : MonoBehaviour {

	public int platformNumber;
	public BoxCollider platformSignalTrigger { get; private set; }
	[Tooltip("On train approach, does it stop to the left or to the right of the platform.")]
	public bool isLeftHanded = true;

	void Start () {
		GameObject signalGO = GetComponentInChildren<Signal> ().gameObject;
		platformSignalTrigger = signalGO.GetComponent <BoxCollider> ();

		Vector3 signalPosition = signalGO.transform.localPosition;
		signalPosition.z = isLeftHanded ? 3.58f : -3.58f;
		signalGO.transform.localPosition = signalPosition;
	}
		
	public void OnAssignedToTimetableItem(TimetableItem timetableItem) {
		//TODO: MEDIUM PRIORITY change colour of text being displayed and also the name of destination
	}
}
