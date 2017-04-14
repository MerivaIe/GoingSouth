using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System.Linq;

/// <summary>
/// Exhaustible list: when an option is chosen it becomes unavailable. At the moment the onus is on the caller to exhaust the option just chosen- would be nice to extend List and then override the index method so that when something is retrieved this class removes it.
/// </summary>
public class ExhaustibleList<T> {
	private SortedDictionary<string,T> availableOptions = new SortedDictionary<string, T> ();
	private List<T> exhaustedOptions = new List<T>();

	public ReadOnlyCollection<T> AvailableOptions{
		get{
			return availableOptions.Values.ToList().AsReadOnly ();
		}
	}

	public ReadOnlyCollection<T> AllOptions{
		get{
			return exhaustedOptions.Concat (availableOptions.Values).ToList ().AsReadOnly();
		}
	}

	public void Add(string keyString, T item) {
		availableOptions.Add (keyString, item);
	}

	public T UseItem(string keyString) {
		T myItem;
		availableOptions.TryGetValue (keyString,out myItem);
		if (myItem != null) {
			availableOptions.Remove (keyString);
			exhaustedOptions.Add (myItem);
			return myItem;
		} else {
			Debug.LogWarning ("Caller was attempting to use an item that was not available.");
			return default(T);
		}
	}

	public void RestoreItem(string keyString, T item) {
		if (exhaustedOptions.Remove (item)) {
			availableOptions.Add (keyString, item);
		} else {
			Debug.LogWarning ("Caller was attempting to restore an option that was not available.");
		}
	}
}
