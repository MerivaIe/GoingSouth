using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExhaustibleList<MyType>{
	private List<MyType> availableOptions = new List<MyType> ();
	private List<MyType> exhaustedOptions = new List<MyType>();

	public List<MyType> AvailableOptions{
		get{
			return availableOptions;
		}
	}

	public void Add(MyType item) {
		AvailableOptions.Add (item);
	}

	public void AddRange(List<MyType> listToAdd) {
		AvailableOptions.AddRange (listToAdd);
	}

	public void ExhaustOption(MyType item) {
		if (AvailableOptions.Contains (item)) {
			exhaustedOptions.Add (item);
			AvailableOptions.Remove (item);
		} else {
			Debug.LogWarning ("Caller was attempting to exhaust an option that was not available.");
		}
	}

	public void RestoreOption(MyType item) {
		if (exhaustedOptions.Contains (item)) {
			AvailableOptions.Add (item);
			exhaustedOptions.Remove (item);
		} else {
			Debug.LogWarning ("Caller was attempting to exhaust an option that was not available.");
		}
	}
}
