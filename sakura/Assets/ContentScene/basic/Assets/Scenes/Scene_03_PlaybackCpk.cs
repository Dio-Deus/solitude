/****************************************************************************
 *
 * CRI Middleware SDK
 *
 * Copyright (c) 2011-2012 CRI Middleware Co.,Ltd.
 *
 * Library  : CRI Mana
 * Module   : CRI Mana Sample for Unity
 * File     : Scene_03_PlaybackCpk.cs
 *
 ****************************************************************************/
/*
 * 本サンプルは、CRI File SystemのユーティリティAPIを使い、
 * ローカル上のCPKファイルをバインドした後、
 * CPKファイル内のムービーファイルを再生します。
 * 単体ファイルを読み込む際は、CPKをバインドしたバインダハンドルを参照し、
 * CPK内でのファイル名でアクセスします。
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class Scene_03_PlaybackCpk : MonoBehaviour
{
	/* CriManaPlayer を AddComponent するオブジェクト */
	public  GameObject movieObject = null;

	/* movieObject に AddComponent した CriManaPlayer を参照するためのオブジェクト */
	private CriManaPlayer moviePlayer = null;

	#region Variables
	/* ローカルCPKファイルのパス(実行時に作成) */
	private string localCpkFilePath;

	/* CPK内からロードするファイル名 */
	private const string movieFilename = "sample_256x256.usm";

	/* バインダハンドル */
	private CriFsBinder binder = null;
	/* バインドID */
	private uint bindId = 0;

	/* GUIボタンのロックフラグ */
	private bool lockBindCpkButton = false;
	#endregion

	#region Functions
	/* シーン初期化処理 */
	void OnEnable()
	{
		/* バインダの作成 */
		/* ファイルバインド、CPKバインド、ディレクトリバインド等のバインド機能を使う場合、*/
		/* アプリケーションが明示的にバインダを作成する必要があります。 */
		this.binder = new CriFsBinder();

		/* ローカルCPKファイルのパスを作成 */
		this.localCpkFilePath = Path.Combine(CriWare.streamingAssetsPath, "sample.cpk");
	}

	/* シーン終了処理 */
	void OnDisable()
	{
		/* シーンのリセット */
		this.reset();

		/* アンバインド処理の実行 */
		this.UnbindCpk();

		/* バインダの破棄 */
		/* アプリケーションはバインダを使い終わった際に明示的に破棄する必要があります。 */
		this.binder.Dispose();
		this.binder = null;
	}

	/* CPKのバインド処理 */
	/* 本サンプルではコルーチン内でCPKのバインドの完了を待っていますが、 */
	/* Updateメソッド内でバインドの完了をチェックする方法もあります。 */
	/* 詳しくは CriFileSystem のサンプルを参照してください。 */
	private IEnumerator BindCpk(string path, int cursor)
	{
		/* 既にバインド済みCPKがあれば解放する */
		this.UnbindCpk();

		this.lockBindCpkButton = true;

		/* CPKのバインド要求 */
		/* 指定したバインダハンドルにCPKの内容が結び付けられる */
		/*  request は varで宣言して型推論で解決しても問題ない */
		CriFsBindRequest request = CriFsUtility.BindCpk(this.binder, path);

		/* バインド完了待ち */
		yield return request.WaitForDone(this);
		this.lockBindCpkButton = false;

		if (request.error == null) {
			/* 成功したらバインドIDをアプリ側で憶えておく */
			/* バインドIDはファイル（本サンプルではCPK）のバインドを一意に識別するためのID */
			/* 解放処理等に使う */
			this.bindId = request.bindId;
			Debug.Log("Completed to bind CPK. (path=" + path + ")");
		} else {
			/* 失敗した場合はバインドIDとして不正値（-1）を憶えておく */
			this.bindId = 0;
			Debug.LogWarning("Failed to bind CPK. (path=" + path + ")");
		}
	}

	/* バインド済みCPKの解放処理 */
	private void UnbindCpk()
	{
		if (this.bindId > 0) {
			/* バインド済みCPKがあれば解放 */
			CriFsBinder.Unbind(this.bindId);
			this.bindId = 0;
		}
	}

	/* シーンをリセット */
	private void reset()
	{
		/* ムービーの再生を停止 */
		if (moviePlayer != null) {
			moviePlayer.Stop();
			Destroy(moviePlayer);
		}
		/* CPKの解放処理 */
		this.UnbindCpk();
	}

	void OnGUI()
	{
		if (Scene_00_SampleList.ShowList == true) {
			return;
		}
		
		Scene_00_GUI.BeginGui("01/SampleMain");

		/* UIスキンの設定 */
		GUI.skin = Scene_00_SampleList.uiSkin;
		
		GUILayout.BeginArea(new Rect(Screen.width - 466, 70 , 450, 32* 4));
		
		if (this.lockBindCpkButton == false) {
			if (Scene_00_GUI.Button(((this.bindId > 0) ? "*" : " ") + " Bind CPK File")) {
				/* ローカルCPKファイルのバインド処理を開始 */
				StartCoroutine(this.BindCpk(this.localCpkFilePath, 1));
			}
		} else {
			Scene_00_GUI.Button("Binding...");
		}

		if (this.bindId > 0) {
			/* 本サンプルではムービーの再生開始時に CriManaPlayer を AddComponent して、 */
			/* ムービーの再生終了時か Stop ボタンが押された際に Destroy します。 */
			if (moviePlayer == null) {
				/* Play ボタン処理 */
				if (Scene_00_GUI.Button("Play")) {
						/* movieObject に CriManaPlayer を AddComponent */
						moviePlayer = movieObject.AddComponent<CriManaPlayer>();
						/* バインダとファイル名を設定 */
						moviePlayer.SetFile(binder, movieFilename);
						/* 再生開始 */
						moviePlayer.Play();
					}
			}
			else {
				switch (this.moviePlayer.status) {
				case CriManaPlayer.Status.Playing:
					/* Stop ボタン処理 */
					if (Scene_00_GUI.Button("Stop")) {
						moviePlayer.Stop();
					}
				break;
				case CriManaPlayer.Status.PlayEnd:
					/* 再生が終了した場合にも Stop する */
					moviePlayer.Stop();
				break;
				case CriManaPlayer.Status.Stop:
					/* CriManaPlayer を Destroy */
					Destroy(moviePlayer);
				break;
				}
			}
		} else {
			GUI.enabled = false;
			Scene_00_GUI.Button("Play");
		}
		GUI.enabled = true;

		GUILayout.Space(8);

			/* シーンをリセット */
		if (Scene_00_GUI.Button("Reset")) {
			this.reset();
		}
		
		GUILayout.EndArea();
		
		Scene_00_GUI.EndGui();
	}
	#endregion
}

/* end of file */
