using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCube : MonoBehaviour {

	private GameObject directionalLight;

	void Start() {
		directionalLight = GetComponentInParent <PrefabReferenceWrapper>().prefab;
	}
		
	void OnMouseDown() {
		Instantiate (directionalLight,transform.position,Quaternion.identity);
		Destroy (gameObject);
	}

}
