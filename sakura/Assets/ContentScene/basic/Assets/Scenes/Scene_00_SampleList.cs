/****************************************************************************
 *
 * CRI Middleware SDK
 *
 * Copyright (c) 2011-2014 CRI Middleware Co.,Ltd.
 *
 * Library  : CRIWARE
 * Module   : CRIWARE Sample for Unity
 * File     : Scene_00_SampleList.cs
 *
 ****************************************************************************/
/*
 * 本プログラムはサンプルシーンの切り替えを行います。
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Scene_00_SampleList : MonoBehaviour
{
    #region Variables
	public static GUISkin uiSkin;
    public static bool ShowList = false;
    public static GUIStyle TextStyle = null;
	
	Texture2D bgTexture = null;
	bool      enableGui = true;

    public static GUIStyle ButtonStyle
    {
        get
        {
            GUIStyle style = new GUIStyle("button");
            style.normal.textColor = Color.white;
            style.margin.left = 16;
            style.alignment = TextAnchor.MiddleLeft;
            return style;
        }
    }

    public static GUIStyle BoxStyle
    {
        get
        {
            GUIStyle style = new GUIStyle("box");
            style.normal.textColor = Color.white;
            style.margin.left = 16;
            style.alignment = TextAnchor.MiddleCenter;
            style.fixedHeight = 120;
			style.fixedWidth = 600;
            return style;
        }
    }
	#endregion

    #region Functions
    /// <summary>
    /// バックグランドテクスチャとスタイルの作成
    /// </summary>
    void InitializeBgTexture()
    {
        if (this.bgTexture == null) {
            this.bgTexture = new Texture2D(128, 128);
            TextStyle = new GUIStyle();
            Color col = new Color(0, 0, 0, 0.7f);
            for (int y = 0; y < this.bgTexture.height; ++y) {
                for (int x = 0; x < this.bgTexture.width; ++x) {
                    this.bgTexture.SetPixel(x, y, col);
                }
            }
            this.bgTexture.Apply();

            TextStyle.normal.textColor = Color.white;
            TextStyle.normal.background = this.bgTexture;
            TextStyle.alignment = TextAnchor.MiddleCenter;
        }
    }
    /// <summary>
    /// Awake this instance.
    /// </summary>
    void Awake()
    {
		ShowList = false;

		/* カスタムスキンの読み込み */
		uiSkin = (GUISkin)Resources.Load("SU3DJPFont/SU3DJPFontSkinMid", typeof(GUISkin));

        /* バックグランドテクスチャとスタイルの作成 */
        InitializeBgTexture();
    }

    /// <summary>
    /// Raises the GUI event.
    /// </summary>
    void OnGUI()
	{
		if (!enableGui) {
			return;
		}

		Scene_00_GUI.BeginGui("00/SceneMenu");

		/* UIスキンの設定 */
		GUI.skin = Scene_00_SampleList.uiSkin;

		GUILayout.Label(Scene_00_SampleListData.Title + " \"" + Application.loadedLevelName + "\"");
		ShowList = Scene_00_GUI.Toggle(ShowList, "Change to Other Sample");
        if (ShowList) {
			var sceneList = Scene_00_SampleListData.SceneList;
			for (int i = 0; i < sceneList.GetLength(0); i++) {
				string sceneName        = sceneList[i, 0];
				string sceneDescription = sceneList[i, 1];
				if (Scene_00_GUI.Button(new GUIContent(sceneName, sceneDescription), ButtonStyle)) {
					/* 現在のシーンのゲームオブジェクトを破棄してシーンを読み込む */
					StartCoroutine(DestroyAllGameObjectsAndLoadLevel(sceneName));
					enableGui = false;
                }
			}
            /* サンプル解説の表示 */
	        GUILayout.Space(8);
            GUILayout.Box(GUI.tooltip, BoxStyle);
        }

		Scene_00_GUI.EndGui();
    }

	IEnumerator DestroyAllGameObjectsAndLoadLevel(string sceneName)
	{
		/* 現在のシーンのゲームオブジェクトを破棄 */
		foreach (GameObject go in GameObject.FindObjectsOfType(typeof(GameObject))) {
			if (go != gameObject) {
				Destroy(go);
			}
		}
		yield return true;
		Destroy(gameObject);
		/* シーンの読み込み */
		Application.LoadLevel(sceneName);
	}
    #endregion
}

/* end of file */
