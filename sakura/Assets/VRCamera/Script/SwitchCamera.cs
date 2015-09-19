using UnityEngine;
using System.Collections;

public class SwitchCamera : MonoBehaviour {

	[SerializeField]
	private Camera SingleCamera;
	[SerializeField]
	private Camera[] SideBySideCamera = new Camera[2];
	[SerializeField]
	private float IPD;
	[SerializeField]
	private MultiGyroCamera multiGyro;

	void Start(){
		multiGyro = GetComponentInChildren<MultiGyroCamera>();

		InitCamera();
		SetCameraMode(CameraMode.SideBySide);
	}

	private void InitCamera(){
		if(SideBySideCamera.Length != 2){
			return;
		}
		SideBySideCamera[0].fieldOfView = 52;
		SideBySideCamera[0].rect = new Rect(0,0,0.5f,1);
		SideBySideCamera[1].fieldOfView = 52;
		SideBySideCamera[1].rect = new Rect(0.5f,0,0.5f,1);
		if(IPD > 0){
			SideBySideCamera[0].transform.localPosition = Vector3.left * IPD / 2;
			SideBySideCamera[1].transform.localPosition = Vector3.right * IPD / 2;
		}
	}



	public enum CameraMode{
		Single,SideBySide
	}

	public void SetCameraMode(CameraMode mode){
		SingleCamera.enabled = mode == CameraMode.Single;
		
		for(int i = 0; i < SideBySideCamera.Length;i++){
			SideBySideCamera[i].enabled = mode == CameraMode.SideBySide;
		}
	}
	public void SetCameraMode(bool isSingle){
		SetCameraMode(isSingle?CameraMode.Single:CameraMode.SideBySide);
	}

	public void ResetCameraRotate(){
		multiGyro.ResetBaseGyro();
	}
}
