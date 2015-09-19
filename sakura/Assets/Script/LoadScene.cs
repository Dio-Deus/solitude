using UnityEngine;
using System.Collections;

public class LoadScene : MonoBehaviour {
	void Start(){
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
	}


	public void LoadLevel(int sceneId){
		Application.LoadLevel(sceneId);
	}
}
