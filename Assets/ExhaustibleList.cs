using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExhaustibleList<MyType> {
	private List<MyType> availableOptions = new List<MyType> ();
	private List<MyType> exhaustedOptions = new List<MyType>();

	public System.Collections.ObjectModel.ReadOnlyCollection<MyType> AvailableOptions{
		get{
			return availableOptions.AsReadOnly ();
		}
	}

	public void Add(MyType item) {
		availableOptions.Add (item);
	}

	public void AddRange(List<MyType> listToAdd) {
		availableOptions.AddRange (listToAdd);
	}

	public void ExhaustOption(MyType item) {
		if (availableOptions.Contains (item)) {
			exhaustedOptions.Add (item);
			availableOptions.Remove (item);
		} else {
			Debug.LogWarning ("Caller was attempting to exhaust an option that was not available.");
		}
	}

	public void RestoreOption(MyType item) {
		if (exhaustedOptions.Contains (item)) {
			availableOptions.Add (item);
			exhaustedOptions.Remove (item);
		} else {
			Debug.LogWarning ("Caller was attempting to exhaust an option that was not available.");
		}
	}
}
