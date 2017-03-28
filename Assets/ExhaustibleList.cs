using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System.Linq;

public class ExhaustibleList<MyType> {
	private List<MyType> availableOptions = new List<MyType> ();
	private List<MyType> exhaustedOptions = new List<MyType>();

	public ReadOnlyCollection<MyType> AvailableOptions{
		get{
			return availableOptions.AsReadOnly ();
		}
	}
	public ReadOnlyCollection<MyType> AllOptions{
		get{
			return availableOptions.Concat (exhaustedOptions).ToList ().AsReadOnly ();
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
