/****************************************************************************
 *
 * CRI Middleware SDK
 *
 * Copyright (c) 2015 CRI Middleware Co.,Ltd.
 *
 * Library  : CRIWARE
 * Module   : CRIWARE Sample for Unity
 * File     : Scene_00_GUI.cs
 *
 ****************************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Scene_00_GUI : MonoBehaviour {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN  || UNITY_STANDALONE_OSX
	const string  yAxisName   = "Vertical";
	const string  xAxisName   = "Horizontal";
	const KeyCode fireKeyCode = KeyCode.Return;
#else
	const string  yAxisName   = "Vertical";
	const string  xAxisName   = "Horizontal";
	const KeyCode fireKeyCode = KeyCode.Joystick1Button1;
#endif

	private static Scene_00_GUI _instance;
	private static Scene_00_GUI instance {
		get {
			if (_instance == null) {
				var go = new GameObject("Scene_00_GUI");
				return go.AddComponent<Scene_00_GUI>();
			}
			return _instance;
		}
	}

	List<string> controlNames = new List<string>();
	string controlPrefix      = null;
	int    controlIndex       = 0;
	int    forcusIndex        = 1;
	string focusedControlName = "";
	int    sliderDisplacement = 0;
	bool   fired              = false;
	bool   yAxisDowned        = false;
	bool   xAxisDowned        = false;
	
	void Awake()
	{
		if (_instance != null) {
			Destroy(this);
			return;
		}
		_instance = this;
	}
	
	void OnDestroy ()
	{
		if (_instance != this) {
			return;
		}
		_instance = null;
	}

	void Update ()
	{
		{
			float yAxis = Input.GetAxisRaw(yAxisName);
			if (Mathf.Abs(yAxis) > 0.1) {
				if (!yAxisDowned) {
					if (yAxis > 0.0) {
						--forcusIndex;
					} else if (yAxis < -0.1) {
						++forcusIndex;
					}
				}
				forcusIndex = Mathf.Max(0, Mathf.Min(controlNames.Count - 1, forcusIndex));
				focusedControlName = (controlNames.Count > 0) ? controlNames[forcusIndex] : "";
				yAxisDowned = true;
			} else {
				yAxisDowned = false;
			}
		}

		{
			float xAxis = Input.GetAxisRaw(xAxisName);
			if (Mathf.Abs(xAxis) > 0.1) {
				if (!xAxisDowned) {
					sliderDisplacement = (xAxis > 0.0) ? 1 : -1;
				} else {
					sliderDisplacement = 0;
				}
				xAxisDowned = true;
			} else {
				sliderDisplacement = 0;
				xAxisDowned = false;
			}
		}

		fired = Input.GetKeyDown(fireKeyCode);
		if (focusedControlName.Length == 0) {
			if (controlNames.Count == 1) {
				focusedControlName = controlNames[0];
			} else {
				focusedControlName = (controlNames.Count >= 2) ? controlNames[1] : "";
			}
		}
		controlNames.Clear();
	}
	
	private string GenCurrentControlName()
	{
		return string.Format("{0}/{1:0000}", controlPrefix, controlIndex);
	}

	private void AddControlName(string controlName)
	{
		int index = controlNames.BinarySearch(controlName);
		if (index < 0) {
			controlNames.Insert(~index, controlName);
		}
	}

	static public void BeginGui(string controlPrefix)
	{
		instance.controlPrefix = controlPrefix;
		instance.controlIndex  = 0;
		GUI.FocusControl(instance.focusedControlName);
	}

	static public void EndGui()
	{
		instance.controlPrefix = null;
		instance.controlIndex  = 0;
	}
	
	static public bool Toggle(bool value, string text, params GUILayoutOption[] options)
	{
		if (GUI.enabled) {
			string controlName = instance.GenCurrentControlName();
			GUI.SetNextControlName(controlName);
			bool result = value;
			if (instance.fired && (GUI.GetNameOfFocusedControl() == controlName)) {
				result = !result;
				instance.fired = false;
			} 
			result = GUILayout.Toggle(result, text, options);
			++(instance.controlIndex);
			instance.AddControlName(controlName);
			return result;
		} else {
			GUILayout.Toggle(value, text, options);
			return false;
		}
	}

	static public bool Button(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
	{
		if (GUI.enabled) {
			string controlName = instance.GenCurrentControlName();
			GUI.SetNextControlName(controlName);
			bool result = false;
			if (GUI.GetNameOfFocusedControl() == controlName) {
				result = instance.fired;
				instance.fired = false;
			}
			result |= GUILayout.Button(content, style, options);
			++(instance.controlIndex);
			instance.AddControlName(controlName);
			return result;
		} else {
			GUILayout.Button(content, style, options);
			return false;
		}
	}
	
	static public bool Button(string text, params GUILayoutOption[] options)
	{
		if (GUI.enabled) {
			string controlName = instance.GenCurrentControlName();
			GUI.SetNextControlName(controlName);
			bool result = false;
			if (GUI.GetNameOfFocusedControl() == controlName) {
				result = instance.fired;
				instance.fired = false;
			}
			result |= GUILayout.Button(text, options);
			++(instance.controlIndex);
			instance.AddControlName(controlName);
			return result;
		} else {
			GUILayout.Button(text, options);
			return false;
		}
	}
	
	static public bool Button(Rect position, string text)
	{
		if (GUI.enabled) {
			string controlName = instance.GenCurrentControlName();
			GUI.SetNextControlName(controlName);
			bool result = false;
			if (GUI.GetNameOfFocusedControl() == controlName) {
				result = instance.fired;
				instance.fired = false;
			}
			result |= GUI.Button(position, text);
			++(instance.controlIndex);
			instance.AddControlName(controlName);
			return result;
		} else {
			GUI.Button(position, text);
			return false;
		}
	}

	static public float HorizontalSlider(float value, float leftValue, float rightValue, params GUILayoutOption[] options)
	{
		float result;
		if (GUI.enabled) {
			string controlName = instance.GenCurrentControlName();
			GUI.SetNextControlName(controlName);
			if (GUI.GetNameOfFocusedControl() == controlName) {
				if (instance.sliderDisplacement != 0) {
				    value += instance.sliderDisplacement * (rightValue - leftValue) / 20.0f;
				    instance.sliderDisplacement = 0;
				    GUI.changed = true;
                }
			}
			result = GUILayout.HorizontalSlider(value, leftValue, rightValue, options);
			++(instance.controlIndex);
			instance.AddControlName(controlName);
		} else {
			result = GUILayout.HorizontalSlider(value, leftValue, rightValue, options);
		}
		return result;
	}

	static public float HorizontalSlider(Rect position, float value, float leftValue, float rightValue)
	{
		float result;
		if (GUI.enabled) {
			string controlName = instance.GenCurrentControlName();
			GUI.SetNextControlName(controlName);
			if (GUI.GetNameOfFocusedControl() == controlName) {
				if (instance.sliderDisplacement != 0) {
				    value += instance.sliderDisplacement * (rightValue - leftValue) / 20.0f;
				    instance.sliderDisplacement = 0;
				    GUI.changed = true;
                }
			}
			result = GUI.HorizontalSlider(position, value, leftValue, rightValue);
			++(instance.controlIndex);
			instance.AddControlName(controlName);
		} else {
			result = GUI.HorizontalSlider(position, value, leftValue, rightValue);
		}
		return result;
	}
}
