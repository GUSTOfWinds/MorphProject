﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MorphController : MonoBehaviour
{
	public GameObject startObject;

	private bool morphed = false;
	private GameObject currentObject;

	void Start()
    {
		currentObject = Instantiate(startObject, gameObject.transform.position, gameObject.transform.rotation, gameObject.transform);
	}

	public bool isMorphed() {
		return morphed;
	}


	public void morphObject(GameObject newObj) {
		newObj = Instantiate(newObj, gameObject.transform.position, gameObject.transform.rotation, gameObject.transform);
		morphed = morphed ? morphed = false : morphed = true;
		
		Destroy(currentObject);
		currentObject = newObj;
		Debug.Log("You are now a: " + currentObject.name);
		FindObjectOfType<AudioManager>().Play("Transform");
	}
	public void morphObject() {
		morphObject(startObject);
	}
}