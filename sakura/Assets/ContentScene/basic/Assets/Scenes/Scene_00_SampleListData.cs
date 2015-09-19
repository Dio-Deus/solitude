/****************************************************************************
 *
 * CRI Middleware SDK
 *
 * Copyright (c) 2014 CRI Middleware Co.,Ltd.
 *
 * Library  : CRIWARE
 * Module   : CRIWARE Sample for Unity
 * File     : Scene_00_SampleListData.cs
 *
 ****************************************************************************/

public static class Scene_00_SampleListData {
    public const string Title = "< CRI Mana Sample >";
	
	public static readonly string[,] SceneList = new string[,]{
		{"Scene_01_SimplePlayback",
			"シンプルなムービ再生です。\nムービはキューブへテクスチャとして\n貼り付けられています。"
		},
		{"Scene_02_AlphaMovie",
			"アルファムービを利用したサンプルです。\nステージの開始などをイメージした\nアルファムービが再生されます。"
		},
		{"Scene_03_PlaybackCpk",
			"CPKファイルからのムービ再生を行うサンプルです。\nCRI File Systemのバインダを利用した\n再生を行います。"
		},
		{"Scene_04_DownloadPlayback",
			"ダウンロードしたムービを再生するサンプルです。\nCPKファイルをダウンロードし、ローカルに保存した\nムービファイルを再生します。"
		},
		{"Scene_05_Seek",
			"シークを行うサンプルです。\nスクリプトでシーク再生を行います。"
		},
		{"Scene_06_SeamlessSequencePlayback",
			"シームレス連結再生を行うサンプルです。\nスクリプトでシームレス連結再生を行います。"
		},
	};
}
