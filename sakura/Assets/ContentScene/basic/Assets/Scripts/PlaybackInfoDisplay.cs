using UnityEngine;
using System.Collections;

public class PlaybackInfoDisplay : MonoBehaviour 
{
	#region Variables
	public GameObject pastedMovieObject = null;
	
	private string timestr = "";
	private string durstr = "";
	#endregion

	void Update () 
	{
		this.timestr = "";
		this.durstr  = "";
		if (this.pastedMovieObject != null) {
			CriManaPlayer cri_mana_player = pastedMovieObject.GetComponent<CriManaPlayer>();
			if (cri_mana_player != null) {
				if (cri_mana_player.status == CriManaPlayer.Status.Playing) {
                	/* 再生情報取得 */
                	/* 注意:これらの情報はプレーヤステータスがPlaying時のみ有効な値が取得できます。 */
					long time         = cri_mana_player.GetTime();	/* usec */
					uint total_frames = cri_mana_player.movieInfo.total_frames;
					uint framerate    = cri_mana_player.movieInfo.framerate_n;
					if (framerate == 0) {
						framerate = 1; // To avoide zero division
					}
				
					/* 再生情報表示 */
					this.timestr = string.Format(
						"Playback Time: {0}:{1:00}.{2:000}" ,
						(time / 1000 / 1000 / 60),
						((time / 1000 / 1000) % 60), (time % 1000)
						);
					this.durstr = "Duration     : " + total_frames * 1000 / framerate + " [sec]";
				}
			}
		}
	}
	
	void OnGUI() 
	{
		if (Scene_00_SampleList.ShowList == true) {
			return;
		}

		/* UIスキンの設定 */
		GUI.skin = Scene_00_SampleList.uiSkin;
		
		GUILayout.BeginArea(new Rect(16, 80, 320, 140), "", Scene_00_SampleList.TextStyle);
		GUILayout.Label("Playback Information");
		GUILayout.Label(this.timestr);
		GUILayout.Label(this.durstr);
		GUILayout.EndArea();
	}
}

/* end of file */
