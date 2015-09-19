using UnityEngine;
using System.Collections;

public class BillBoadScript : MonoBehaviour {
	public Transform Center;

	[ContextMenu("Look")]
	public void Look(){
		this.transform.LookAt(Center.position);
	}

}
