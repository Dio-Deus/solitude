using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button)),RequireComponent(typeof(BoxCollider))]
public class VRButton : MonoBehaviour{
	private Image gaugeImage;
	private float lastGazeTime;

	[SerializeField]
	private bool initialState = false;

	void Start(){
		gaugeImage = this.transform.FindChild("Gauge").GetComponent<Image>();
		gaugeImage.fillAmount = 0.0f;

		RectTransform rect = GetComponent<RectTransform>();
		GetComponent<BoxCollider>().size = new Vector3(rect.rect.width,rect.rect.height,0.1f);


		this.gameObject.SetActive(initialState);
	}

	void Update(){
		if(Time.time - lastGazeTime > 0.2f){
			gaugeImage.fillAmount = 0.0f;
		}
	}

	public void OnGaze(){
		lastGazeTime = Time.time;
		gaugeImage.fillAmount = gaugeImage.fillAmount + 0.5f * Time.deltaTime;
		if(gaugeImage.fillAmount >= 1.0f){
			gaugeImage.fillAmount = 0.0f;
			this.GetComponent<Button>().onClick.Invoke();
		}
	}
}
