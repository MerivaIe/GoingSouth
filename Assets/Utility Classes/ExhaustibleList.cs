using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System.Linq;

/// <summary>
/// Exhaustible list: wen an option is chosen it becomes unavailable. At the moment the onus is on the caller to exhaust the option just chosen- would be nice to extend List and then override the index method so that when something is retrieved this class removes it.
/// </summary>
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
	public void ExhaustOption(int index) {
		if (availableOptions.Count > 0) {
			exhaustedOptions.Add (availableOptions[index]);
			availableOptions.RemoveAt (index);
		} else {
			Debug.LogWarning ("Caller was attempting to exhaust an option but none are in the available list.");
		}
	}

	public void RestoreOption(MyType item) {
		if (exhaustedOptions.Contains (item)) {
			availableOptions.Add (item);
			exhaustedOptions.Remove (item);
		} else {
			Debug.LogWarning ("Caller was attempting to restore an option that was not available.");
		}
	}
}
