/****************************************************************************
 *
 * CRI Middleware SDK
 *
 * Copyright (c) 2011-2012 CRI Middleware Co.,Ltd.
 *
 * Library  : CRI Mana
 * Module   : CRI Mana Sample for Unity
 * File     : Scene_05_Seek.cs
 *
 ****************************************************************************/
/*
 * 本サンプルは、スライダーで指定された位置からのシーク再生を行います。n
 */

using UnityEngine;

public class Scene_05_Seek : MonoBehaviour {
	/* movieObject に AddComponent した CriManaPlayer を参照するためのオブジェクト */
	public CriManaPlayer player = null;

	private int  seekFrameNumber = 0;
	private uint totalFrames = 0;

	void OnGUI()
	{
		if (Scene_00_SampleList.ShowList == true) {
			return;
		}
		
		Scene_00_GUI.BeginGui("01/SampleMain");
		
		/* UIスキンの設定 */
		GUI.skin = Scene_00_SampleList.uiSkin;
		
		Rect playbackBarRect = new Rect(32, Screen.height - 80, Screen.width - 64,  32);
		Rect seekBarRect     = new Rect(32, Screen.height - 64, Screen.width - 64,  32);
		
		if ((player.status == CriManaPlayer.Status.Playing) || (player.status == CriManaPlayer.Status.PlayEnd)) {
			totalFrames = player.movieInfo.total_frames;
			GUI.enabled = false;
			Scene_00_GUI.HorizontalSlider(playbackBarRect, player.frameInfo.frame_no, 0, totalFrames);
			GUI.enabled = true;
			seekFrameNumber = (int)Scene_00_GUI.HorizontalSlider(seekBarRect, seekFrameNumber, 0, totalFrames);
			if (GUI.changed) {
				/* 再生を停止します。(再生中にシークを行うことはできません。) */
				player.Stop();
				/* シークを行い、再生を開始します。 */
				player.SetSeekPosition(seekFrameNumber);
				player.Play();
			}
		} else {
			GUI.enabled = false;
			Scene_00_GUI.HorizontalSlider(playbackBarRect, 0, 0, totalFrames);
			GUI.enabled = true;
			Scene_00_GUI.HorizontalSlider(seekBarRect, seekFrameNumber, 0, totalFrames);
		}
		
		Scene_00_GUI.EndGui();
	}
}
