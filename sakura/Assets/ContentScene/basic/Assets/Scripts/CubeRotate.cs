/****************************************************************************
 *
 * CRI Middleware SDK
 *
 * Copyright (c) 2011-2012 CRI Middleware Co.,Ltd.
 *
 * Library  : CRI Mana
 * Module   : CRI Mana Sample for Unity
 * File     : CubeRotate.cs
 *
 ****************************************************************************/
using UnityEngine;
using System.Collections;

public class CubeRotate : MonoBehaviour {
	// Update is called once per frame
	void Update () {
		/* ゲームオブジェクトの回転 */
		transform.Rotate(Vector3.down * Time.deltaTime * 10);
		transform.Rotate(Vector3.right * Time.deltaTime * 1);
		transform.Rotate(Vector3.forward * Time.deltaTime * 8);
	}
}
