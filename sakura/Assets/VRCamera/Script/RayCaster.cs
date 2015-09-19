using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class RayCaster : MonoBehaviour {
	[SerializeField]
	private LayerMask layer;

	private Camera mCamera;
	private Ray mRay;
	private RaycastHit mHit;

	private bool isRayCast = true;
	public bool IsRayCast{
		set{isRayCast = value;}
		private get{return isRayCast;}
	}

	void Start(){
		mCamera = GetComponent<Camera>();
	}

	void Update () {
		if(IsRayCast){
			mRay = mCamera.ViewportPointToRay(Vector3.one * 0.5f);
			if(Physics.Raycast(mRay,out mHit,1500.0f,layer.value)){
				mHit.transform.GetComponent<VRButton>().OnGaze();
			}
		}
	}

	void OnDrawGizmos ()
	{
		Debug.DrawLine(mRay.origin,mRay.direction * 1500.0f,Color.red);
	}
}
