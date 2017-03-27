using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExhaustibleList<MyType> {
	public List<MyType> availableOptions { get; private set; }
	private List<MyType> exhaustedOptions;

	public void Add(MyType item) {
		availableOptions.Add (item);
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
