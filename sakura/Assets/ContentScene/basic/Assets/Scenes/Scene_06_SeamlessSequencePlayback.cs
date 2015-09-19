using UnityEngine;
using System.Collections.Generic;

public class Scene_06_SeamlessSequencePlayback : MonoBehaviour {
	/* movieObject に AddComponent した CriManaPlayer を参照するためのオブジェクト */
	public CriManaPlayer player = null;
	
	private const int numOfDisplay = 10;
	private Vector2 displayPosition = new Vector3(0.0f, 100.0f);
	
	/* ムービファイルパス */
	private readonly string[] contentsList = {
		"Seamless/seamless_sample_1.usm",
		"Seamless/seamless_sample_2.usm",
		"Seamless/seamless_sample_3.usm",
		"Seamless/seamless_sample_4.usm",
		"Seamless/seamless_sample_5.usm",
		"Seamless/seamless_sample_6.usm",
		"Seamless/seamless_sample_7.usm",
	};
	private List<int>         entryList       = new List<int>();
	private List<int>         loadedEntryList = new List<int>();
	private int               currentIdx      = -1;
	
	
	void Update() {
		if ((entryList.Count > 0) && (currentIdx >= 0)) {
			int currentCntConcat = (player.status == CriManaPlayer.Status.Playing)
					? (int)player.frameInfo.cnt_concatenated_movie
					: 0;
			currentIdx = entryList[currentCntConcat];
		}
	}
	
	
	void OnGUI() {
		if (Scene_00_SampleList.ShowList == true) {
			return;
		}
		
		Scene_00_GUI.BeginGui("01/SampleMain");

		/* UIスキンの設定 */
		GUI.skin = Scene_00_SampleList.uiSkin;
		
		GUILayout.BeginArea(new Rect(displayPosition.x, displayPosition.y, 640, 480));
		{
			CriManaPlayer.Status player_status = player.status;
			
			/* 再生制御ボタン */
			GUILayout.BeginHorizontal();
			{
				GUI.enabled = (currentIdx >= 0) && ((player_status == CriManaPlayer.Status.Stop) || (player_status == CriManaPlayer.Status.Ready));
				if (Scene_00_GUI.Button("Play", GUILayout.MinHeight(40), GUILayout.MinWidth(60))) {
					player.Play();
				}
				GUI.enabled = (player_status == CriManaPlayer.Status.Playing);
				if (Scene_00_GUI.Button("Pause", GUILayout.MinHeight(40), GUILayout.MinWidth(60))) {
					player.Pause(!player.IsPaused());
				}
				GUI.enabled = true;
				if (Scene_00_GUI.Button("Stop", GUILayout.MinHeight(40), GUILayout.MinWidth(60))) {
					player.Stop();
					entryList.Clear();
					loadedEntryList.Clear();
					currentIdx = -1;
				}
			}
			GUILayout.EndHorizontal();
			
			/* ムービ選択ボタン */
			GUILayout.BeginHorizontal();
			{
				GUI.enabled = (player_status != CriManaPlayer.Status.PlayEnd);
				for (int i = 0; i < contentsList.Length ; i++) {
					string button_string = (i == currentIdx) ? ("<" + (i + 1).ToString() + ">") : (i + 1).ToString();
					if(Scene_00_GUI.Button(button_string, GUILayout.MinHeight(40), GUILayout.MinWidth(40))) {
						/* 先頭のムービ登録は CriManaPlayer.SetMode.New でセットします。 */
						if (entryList.Count == 0) {
							player.SetFile(null, contentsList[i], CriManaPlayer.SetMode.New);
							currentIdx = i;
							entryList.Add(i);
						}
						/* 連結するムービは CriManaPlayer.SetMode.Appen でセットすることで、シームレス連結再生を行えます。 */
						else {
							/* プラグイン内部のエントリー数が足りない場合は、falseを返します。 */
							bool set_result = player.SetFile(null, contentsList[i], CriManaPlayer.SetMode.Append);
							if (set_result) {
								entryList.Add(i);
							}
							else {
								Debug.Log("failed SetEntry (entry pool is empty).");
							}
						}
					}
				}
				GUI.enabled = true;
			}
			GUILayout.EndHorizontal();
			
			/* 情報を表示 */
			GUILayout.BeginVertical();
			{
				int currentCntConcat = (player.status == CriManaPlayer.Status.Playing)
					? (int)player.frameInfo.cnt_concatenated_movie
					: 0;
				/* 連結状況を表示 */
				string concat_progress = "Concat Progress: ";
				for (int i = System.Math.Max(0, (entryList.Count - numOfDisplay)); i < entryList.Count; i++) {
					concat_progress +=
						(i == currentCntConcat)
						? ("<" + (entryList[i] + 1).ToString() + "> ")
						: (" " + (entryList[i] + 1).ToString() + "  ");
				}
				GUILayout.Label(concat_progress);
				/* 連結状況を表示 */
				GUILayout.Label(
					"Frame No: " + player.frameInfo.frame_no + "\n" +
					"Frame No Per File: " + player.frameInfo.frame_no_per_file
					);
				/* 連結回数を表示 */
				GUILayout.Label("Concat Count: " + currentCntConcat);
				/* プレーヤの状態を表示 */
				GUILayout.Label("Player Status: " + player.status.ToString());
			}
			GUILayout.EndVertical();
		}
		GUILayout.EndArea ();
		
		Scene_00_GUI.EndGui();
	}
}
