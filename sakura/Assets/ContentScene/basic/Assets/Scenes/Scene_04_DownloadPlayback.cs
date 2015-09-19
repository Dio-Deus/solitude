using UnityEngine;
using System.Collections;
using System.IO;

public class Scene_04_DownloadPlayback : MonoBehaviour 
{
	#region Variables
	/* CriManaPlayer を AddComponent するオブジェクト */
	public  GameObject movieObject = null;

	/* movieObject に AddComponent した CriManaPlayer を参照するためのオブジェクト */
	private CriManaPlayer moviePlayer = null;

	/* 読み込み元のディレクトリ */
	/* 各プラットフォームに合ったAssetBundleファイルを読み込むように定義 */
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
	const string AssetDir = "WIN_OSX/";
#elif UNITY_IPHONE
	const string AssetDir = "iOS/";
#elif UNITY_ANDROID
	const string AssetDir = "Android/";
#endif
	
	private const string RemoteUrl = "http://crimot.com/sdk/sampledata/crimana/";
	private const string CpkFilename = "sample.cpk";
	private const string AssetFilename = "CharaMw.unity3d";
	
	private string localCpkPath;
	private string remoteCpkPath;
	
	/* CPK内からロードするファイル名 */
	private const string movieFilename = "sample_256x256.usm";
	
	/* インストールリクエストの保存領域 */
	private CriFsInstallRequest[] installRequest = new CriFsInstallRequest[2];
	
	/* CPKバインドに使うパラメータ */
	private CriFsBinder binder = null;
	private uint bindId = 0;

	/* GUIボタンのロックフラグ */
	private bool lockInstallButton = false;
	private bool lockBindCpkButton = false;
	private bool showBindCpkButton = false;
	#endregion

	#region Functions
	/* シーン初期化処理 */
	void OnEnable()
	{
		/* バインダの作成 */
		/* ファイルバインド、CPKバインド、ディレクトリバインド等のバインド機能を使う場合、*/
		/* アプリケーションが明示的にバインダを作成する必要があります。 */
		this.binder = new CriFsBinder();

		/* パスの作成 */
		/* インストール先のパスを作成 */
		this.localCpkPath = Path.Combine(CriWare.installTargetPath, CpkFilename);

		/* インストール元のパスを作成 */
		this.remoteCpkPath = Path.Combine(RemoteUrl, CpkFilename);
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
	
	private IEnumerator Install(string src_path, string install_path, int request_index)
	{
		this.showBindCpkButton = false;
		/* インストール要求 */
		this.installRequest[request_index] = CriFsUtility.Install(null, src_path, install_path);
		/* インストール完了待ち */
		yield return this.installRequest[request_index].WaitForDone(this);
		/* 備考）インストール要求時のリクエストをアプリ側で保持し */
		/* 多重インストール防止のためのフラグとして転用しています。 */
		
		if (this.installRequest[request_index].error == null) {
			Debug.Log("Completed installation." + src_path);
			this.showBindCpkButton = true;
		} else {
			Debug.LogWarning("Failed installation." + src_path);
		}
		/* インストール処理完了後はリクエストを解放 */
		this.installRequest[request_index] = null;
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
		
		lockInstallButton = false;
		lockBindCpkButton = false;
		showBindCpkButton = false;
	}

	void OnGUI()
	{
		if (Scene_00_SampleList.ShowList == true) {
			return;
		}
		
		Scene_00_GUI.BeginGui("01/SampleMain");

		/* UIスキンの設定 */
		GUI.skin = Scene_00_SampleList.uiSkin;
		
		GUILayout.BeginArea(new Rect(Screen.width - 416, 70 , 400, 32 * 6));
		
		if (this.showBindCpkButton == false) {
			if (this.installRequest[0] == null) {
				if (this.lockInstallButton == false) {
					if (Scene_00_GUI.Button("Install CPK File")) {
						/* CPKファイルを上書きするのでインストールの前にCPKをアンバインドしておく */
						this.UnbindCpk();
						/* CPKファイルのインストール処理開始 */
						StartCoroutine(this.Install(this.remoteCpkPath, this.localCpkPath, 0));
					}
				}
			} else {
				/* インストールのリクエストから進捗状況を取得してアプリの画面表示に反映 */
				float progress = this.installRequest[0].progress;
				GUILayout.Label("Installing... " + (int)(progress * 100) + "%");
				GUILayout.Box("", GUILayout.Width(400 * progress + 10), GUILayout.Height(8));
			}
		}
		
		if (this.showBindCpkButton == true) {
			if (this.lockBindCpkButton == false) {
				GUILayout.Label("Loaded CPK File");
				if (Scene_00_GUI.Button(((this.bindId > 0) ? "*" : " ") + " Bind CPK File")) {
					/* ローカルCPKファイルのバインド処理を開始 */
					StartCoroutine(this.BindCpk(this.localCpkPath, 1));
				}
				if (this.bindId <= 0) {
					GUI.enabled = false;
					Scene_00_GUI.Button("Play");
				}
				GUI.enabled = true;

			} else {
				Scene_00_GUI.Button("Binding...");
			}
		}

		if (this.bindId > 0) {
			/* 本サンプルではムービーの再生開始時に CriManaPlayer を AddComponent して、 */
			/* ムービーの再生終了時か Stop ボタンが押された際に Destroy します。 */
			if (moviePlayer == null) {
				/* Play ボタン処理 */
				if (Scene_00_GUI.Button("Play")) {
					this.lockInstallButton = true;
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
					this.lockInstallButton = false;
				break;
				}
			}
		}

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
