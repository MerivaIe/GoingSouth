using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System.Linq;

/// <summary>
/// Exhaustible list: wen an option is chosen it becomes unavailable. At the moment the onus is on the caller to exhaust the option just chosen- would be nice to extend List and then override the index method so that when something is retrieved this class removes it.
/// </summary>
public class ExhaustibleList<T> {
	private List<T> availableOptions = new List<T> ();
	private List<T> exhaustedOptions = new List<T>();

	public ReadOnlyCollection<T> AvailableOptions{
		get{
			return availableOptions.AsReadOnly ();
		}
	}

	public ReadOnlyCollection<T> AllOptions{
		get{
			return availableOptions.Concat (exhaustedOptions).ToList ().AsReadOnly ();
		}
	}

	public void Add(T item) {
		availableOptions.Add (item);
	}

	public void AddRange(List<T> listToAdd) {
		availableOptions.AddRange (listToAdd);
	}

	public void ExhaustOption(T item) {
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
			Debug.LogWarning ("Caller was attempting to exhaust an option using index but none are in the available list.");
		}
	}

	public void RestoreOption(T item) {
		if (exhaustedOptions.Contains (item)) {
			availableOptions.Add (item);
			exhaustedOptions.Remove (item);
		} else {
			Debug.LogWarning ("Caller was attempting to restore an option that was not available.");
		}
	}
}
