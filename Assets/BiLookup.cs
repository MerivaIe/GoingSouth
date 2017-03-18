using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiLookup<TFirst, TSecond> {
	public IDictionary<TFirst, TSecond> firstToSecond = new Dictionary<TFirst, TSecond>();
	public IDictionary<TSecond, TFirst> secondToFirst = new Dictionary<TSecond, TFirst>();

	public void Add(TFirst first, TSecond second)
	{
		if (firstToSecond.ContainsKey (first) || secondToFirst.ContainsKey (second)) {
			Debug.LogWarning ("Key/s already found in BiDictionary for entries being added. Will not add them.");
		} else {
			firstToSecond.Add (first, second);
			secondToFirst.Add (second, first);
		}
	}

	public bool TryGetValueByFirst(TFirst first, out TSecond second)
	{
		return firstToSecond.TryGetValue(first, out second);
	}

	public bool TryGetValueBySecond(TSecond second, out TFirst first)
	{
		return secondToFirst.TryGetValue(second, out first);
	}
}
