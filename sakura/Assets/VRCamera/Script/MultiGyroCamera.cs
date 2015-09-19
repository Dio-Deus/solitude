using UnityEngine;
using System.Collections;

public class MultiGyroCamera : MonoBehaviour {
	private Quaternion baseYaw;

	private Quaternion baseGyro;
	private float rotDelta;

	[SerializeField]
	private Transform[] followObjects; 

	void Start () {
		//		Time.timeScale = 10.0f;
		if(Application.platform == RuntimePlatform.Android){
			Input.gyro.enabled = true;
		}

		baseYaw = this.transform.localRotation;
			
		ResetBaseGyro();
	}

	// Update is called once per frame
	void Update () {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IPHONE)
		Quaternion gyro = Input.gyro.attitude;

		//A
		gyro.x *= -1.0f;
		gyro.y *= -1.0f;
		//		Quaternion temp = Quaternion.Euler(90,0,0) * gyro * baseGyro;

		Quaternion temp = Quaternion.Euler(90,0,0) * gyro;
		this.transform.localRotation = temp;
		this.transform.Rotate(0,rotDelta,0,Space.World);

#endif

		foreach(Transform tr in followObjects){
			tr.transform.localRotation = this.transform.localRotation;
		}
		

		if(Input.GetKeyDown (KeyCode.Escape)){
			ResetBaseGyro ();
		}
	}

	public void ResetBaseGyro(){
		Quaternion gyro = Input.gyro.attitude;
#if UNITY_EDITOR
		gyro = this.transform.localRotation;
#endif

		//A
		gyro.x *= -1.0f;
		gyro.y *= -1.0f;
		gyro = Quaternion.Euler(90,0,0) * gyro;
		float yGyro = gyro.eulerAngles.y;
		float yDefault = baseYaw.eulerAngles.y;
		rotDelta = yDefault - yGyro;
		baseGyro = Quaternion.Euler(0,rotDelta,0);
	}

}
