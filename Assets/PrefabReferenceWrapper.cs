using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// To be used if multiple children need to reference a prefab but assigning it individually to each would be inefficient. They should use FindComponentInParent to retrieve this reference.
/// </summary>
public class PrefabReferenceWrapper : MonoBehaviour {

	public GameObject prefab;

}
