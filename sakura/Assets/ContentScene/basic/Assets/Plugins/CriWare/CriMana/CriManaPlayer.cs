/****************************************************************************
  *
  * CRI Middleware SDK
  *
  * Copyright (c) 2011-2014 CRI Middleware Co., Ltd.
  *
  * Library  : CRI Mana
  * Module   : CRI Mana for Unity
  * File     : CriManaPlayer.cs
  *
  ****************************************************************************/

/*==========================================================================
 *	  Plugin Defines 
 *=========================================================================*/

/*--------------------
 * Rendering Defines
 *--------------------*/
#if (UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5)
	// For Unity 3 *No support for Texture.GetNativeTexturePtr()  prior to Unity 4.0*
	#if UNITY_EDITOR || UNITY_STANDALONE_WIN  || UNITY_STANDALONE_OSX
		#define CRIMANAPLAYER_SHADER_TYPE_FORWARD_RGB
	#elif UNITY_ANDROID || UNITY_IPHONE
		#define CRIMANAPLAYER_SHADER_TYPE_YUV2RGB
	#else
		#error unsupported platform
	#endif
#else
	// For Unity 4 or later version
	#if UNITY_EDITOR || UNITY_STANDALONE_WIN  || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_IPHONE || UNITY_PSP2
		#define CRIMANAPLAYER_SHADER_TYPE_YUV2RGB
	#elif UNITY_WIIU || UNITY_PS3 || UNITY_PS4
		#define CRIMANAPLAYER_SHADER_TYPE_FORWARD_RGB
	#else
		#error unsupported platform
	#endif
#endif

/*---------------------------
 * Cueopint Callback Defines
 *---------------------------*/
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_IPHONE
	#define CRIMANAPLAYER_SUPPORT_CUEPOINT_CALLBACK
	/* Callback Implentation Defines */
	#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_ANDROID
		#define CRIMANAPLAYER_CALLBACK_IMPL_NATIVE2MANAGED
	#elif UNITY_IPHONE
		#define CRIMANAPLAYER_CALLBACK_IMPL_UNITYSENDMESSAGE
	#else
		#error supported platform must select one of callback implementations
	#endif
#elif UNITY_WIIU || UNITY_PSP2 || UNITY_PS3 || UNITY_PS4
	#define CRIMANAPLAYER_UNSUPPORT_CUEPOINT_CALLBACK
#else
	#error undefined platform if supporting cuepoint callback
#endif

#if !UNITY_EDITOR && UNITY_4_6_2 && UNITY_IPHONE
	#define CRI_MANA_PLAYER_IL2CPP_WORKAROUND
#endif

/*==========================================================================
 *	  CRI Mana Player
 *=========================================================================*/
using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;

/**
 * \addtogroup CRIMANA_UNITY_COMPONENT
 * @{
 */

/**
 * <summary>ムービ再生制御を行うモジュールです。</summary>
 * \par 説明:
 * ムービ再生制御を行うモジュールです。GameObjectに貼り付けて使用してください。<br/>
 */
[AddComponentMenu("CRIWARE/CRI Mana Player")]
public class CriManaPlayer : MonoBehaviour
{
	#region DataType
	/**
	 * <summary>使用するシェーダの種類を示す値です。</summary>
	 */
	public enum ShaderType
	{
		Yuv2Rgb,		/**< Y, U, V, (A)チャネルのテクスチャを用いて色変換を行う種類のシェーダを使用 */
		ForwardRgb,		/**< 色変換済みのテクスチャを使用する種類のシェーダを使用 */
	}
    
	/**
	 * <summary>ムービの合成モードを示す値です。</summary>
	 */
	public enum AlphaType
	{
		CompoOpaq		= 0,	/**< 不透明、アルファ情報なし */
		CompoAlphaFull	= 1,	/**< フルAlpha合成（アルファ用データが8ビット) */
		CompoAlpha3Step	= 2,	/**< 3値アルファ */
		CompoAlpha32Bit = 3,	/**< フルAlpha、（カラーとアルファデータで32ビット） */
	}

	/**
	 * <summary>プレーヤの状態を示す値です。</summary>
	 * \sa CriManaPlayer::GetStatus
	 */
	public enum Status
	{
		 Stop,		/**< 停止中		*/
		 Dechead,	/**< ヘッダ解析中	*/
		 WaitPrep,	/**< バッファリング開始停止中 */
		 Prep,		/**< 再生準備中	*/
		 Ready,		/**< 再生準備完了	*/
		 Playing,	/**< 再生中		*/
		 PlayEnd,	/**< 再生終了		*/
		 Error		/**< エラー		*/
	}

	/**
	 * <summary>ファイルをセットする際のモードです。</summary>
	 * \sa CriManaPlayer::SetFile, CriManaPlayer::SetContentId, CriManaPlayer::SetFileRange
	 */
	public enum SetMode
	{
		 New,				/**< 次回の再生で利用するムービを指定	*/
		 Append,			/**< 連結再生キューに追加する	*/
		 AppendRepeatedly	/**< 連結再生キューに次のムービが登録されるまで繰り返し追加する	*/
	}
	
	/**
	 * <summary>オーディオトラックを設定する際のモードです。</summary>
	 * \sa CriManaPlayer::SetAudioTrack, CriManaPlayer::SetSubAudioTrack
	 */
	public enum AudioTrack
	{
		Off,	/**< オーディオ再生OFFの指定値	*/
		Auto,	/**< オーディオトラックのデフォルト値	*/
	}
	
	/**
	 * <summary>ムービファイル内のオーディオ解析情報です。</summary>
	 */
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct AudioInfo
	{
		public UInt32 sampling_rate;	/**< サンプリング周波数 */
		public UInt32 num_channels;		/**< オーディオチャネル数 */
		public UInt32 total_samples;	/**< 総サンプル数 */
	}

	/**
	 * <summary>ムービファイルのヘッダ解析情報です。</summary>
	 */
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct MovieInfo
	{
		public UInt32 is_playable;		/**< 再生可能フラグ（1: 再生可能、0: 再生不可） */
		public UInt32 has_alpha;		/**< アルファムービーかどうか（1: アルファ有、0: アルファ無） */
		public UInt32 width;			/**< ムービ最大幅（８の倍数） */
		public UInt32 height;			/**< ムービ最大高さ（８の倍数） */
		public UInt32 disp_width;		/**< 表示したい映像の横ピクセル数（左端から） */
		public UInt32 disp_height;		/**< 表示したい映像の縦ピクセル数（上端から） */
		public UInt32 framerate_n;		/**< 有理数形式フレームレート(分子) framerate [x1000] = framerate_n / framerate_d */
		public UInt32 framerate_d;		/**< 有理数形式フレームレート(分母) framerate [x1000] = framerate_n / framerate_d */
		public UInt32 total_frames;		/**< 総フレーム数 */
		public UInt32 num_audio_streams;	/**< オーディオストリームの数 */
#if CRI_MANA_PLAYER_IL2CPP_WORKAROUND
		UInt32 _audio_prm_sampling_rate_0;	UInt32 _audio_prm_num_channels_0;	UInt32 _audio_prm_total_samples_0;
		UInt32 _audio_prm_sampling_rate_1;	UInt32 _audio_prm_num_channels_1;	UInt32 _audio_prm_total_samples_1;
		UInt32 _audio_prm_sampling_rate_2;	UInt32 _audio_prm_num_channels_2;	UInt32 _audio_prm_total_samples_2;
		UInt32 _audio_prm_sampling_rate_3;	UInt32 _audio_prm_num_channels_3;	UInt32 _audio_prm_total_samples_3;
		UInt32 _audio_prm_sampling_rate_4;	UInt32 _audio_prm_num_channels_4;	UInt32 _audio_prm_total_samples_4;
		UInt32 _audio_prm_sampling_rate_5;	UInt32 _audio_prm_num_channels_5;	UInt32 _audio_prm_total_samples_5;
		UInt32 _audio_prm_sampling_rate_6;	UInt32 _audio_prm_num_channels_6;	UInt32 _audio_prm_total_samples_6;
		UInt32 _audio_prm_sampling_rate_7;	UInt32 _audio_prm_num_channels_7;	UInt32 _audio_prm_total_samples_7;
		UInt32 _audio_prm_sampling_rate_8;	UInt32 _audio_prm_num_channels_8;	UInt32 _audio_prm_total_samples_8;
		UInt32 _audio_prm_sampling_rate_9;	UInt32 _audio_prm_num_channels_9;	UInt32 _audio_prm_total_samples_9;
		UInt32 _audio_prm_sampling_rate_10;	UInt32 _audio_prm_num_channels_10;	UInt32 _audio_prm_total_samples_10;
		UInt32 _audio_prm_sampling_rate_11;	UInt32 _audio_prm_num_channels_11;	UInt32 _audio_prm_total_samples_11;
		UInt32 _audio_prm_sampling_rate_12;	UInt32 _audio_prm_num_channels_12;	UInt32 _audio_prm_total_samples_12;
		UInt32 _audio_prm_sampling_rate_13;	UInt32 _audio_prm_num_channels_13;	UInt32 _audio_prm_total_samples_13;
		UInt32 _audio_prm_sampling_rate_14;	UInt32 _audio_prm_num_channels_14;	UInt32 _audio_prm_total_samples_14;
		UInt32 _audio_prm_sampling_rate_15;	UInt32 _audio_prm_num_channels_15;	UInt32 _audio_prm_total_samples_15;
		UInt32 _audio_prm_sampling_rate_16;	UInt32 _audio_prm_num_channels_16;	UInt32 _audio_prm_total_samples_16;
		UInt32 _audio_prm_sampling_rate_17;	UInt32 _audio_prm_num_channels_17;	UInt32 _audio_prm_total_samples_17;
		UInt32 _audio_prm_sampling_rate_18;	UInt32 _audio_prm_num_channels_18;	UInt32 _audio_prm_total_samples_18;
		UInt32 _audio_prm_sampling_rate_19;	UInt32 _audio_prm_num_channels_19;	UInt32 _audio_prm_total_samples_19;
		UInt32 _audio_prm_sampling_rate_20;	UInt32 _audio_prm_num_channels_20;	UInt32 _audio_prm_total_samples_20;
		UInt32 _audio_prm_sampling_rate_21;	UInt32 _audio_prm_num_channels_21;	UInt32 _audio_prm_total_samples_21;
		UInt32 _audio_prm_sampling_rate_22;	UInt32 _audio_prm_num_channels_22;	UInt32 _audio_prm_total_samples_22;
		UInt32 _audio_prm_sampling_rate_23;	UInt32 _audio_prm_num_channels_23;	UInt32 _audio_prm_total_samples_23;
		UInt32 _audio_prm_sampling_rate_24;	UInt32 _audio_prm_num_channels_24;	UInt32 _audio_prm_total_samples_24;
		UInt32 _audio_prm_sampling_rate_25;	UInt32 _audio_prm_num_channels_25;	UInt32 _audio_prm_total_samples_25;
		UInt32 _audio_prm_sampling_rate_26;	UInt32 _audio_prm_num_channels_26;	UInt32 _audio_prm_total_samples_26;
		UInt32 _audio_prm_sampling_rate_27;	UInt32 _audio_prm_num_channels_27;	UInt32 _audio_prm_total_samples_27;
		UInt32 _audio_prm_sampling_rate_28;	UInt32 _audio_prm_num_channels_28;	UInt32 _audio_prm_total_samples_28;
		UInt32 _audio_prm_sampling_rate_29;	UInt32 _audio_prm_num_channels_29;	UInt32 _audio_prm_total_samples_29;
		UInt32 _audio_prm_sampling_rate_30;	UInt32 _audio_prm_num_channels_30;	UInt32 _audio_prm_total_samples_30;
		UInt32 _audio_prm_sampling_rate_31;	UInt32 _audio_prm_num_channels_31;	UInt32 _audio_prm_total_samples_31;
		
		public AudioInfo[] audio_prm	/**< オーディオパラメータ */
		{
			get {
				return new []{
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_0, num_channels = _audio_prm_num_channels_0, total_samples = _audio_prm_total_samples_0},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_1, num_channels = _audio_prm_num_channels_1, total_samples = _audio_prm_total_samples_1},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_2, num_channels = _audio_prm_num_channels_2, total_samples = _audio_prm_total_samples_2},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_3, num_channels = _audio_prm_num_channels_3, total_samples = _audio_prm_total_samples_3},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_4, num_channels = _audio_prm_num_channels_4, total_samples = _audio_prm_total_samples_4},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_5, num_channels = _audio_prm_num_channels_5, total_samples = _audio_prm_total_samples_5},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_6, num_channels = _audio_prm_num_channels_6, total_samples = _audio_prm_total_samples_6},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_7, num_channels = _audio_prm_num_channels_7, total_samples = _audio_prm_total_samples_7},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_8, num_channels = _audio_prm_num_channels_8, total_samples = _audio_prm_total_samples_8},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_9, num_channels = _audio_prm_num_channels_9, total_samples = _audio_prm_total_samples_9},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_10, num_channels = _audio_prm_num_channels_10, total_samples = _audio_prm_total_samples_10},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_11, num_channels = _audio_prm_num_channels_11, total_samples = _audio_prm_total_samples_11},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_12, num_channels = _audio_prm_num_channels_12, total_samples = _audio_prm_total_samples_12},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_13, num_channels = _audio_prm_num_channels_13, total_samples = _audio_prm_total_samples_13},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_14, num_channels = _audio_prm_num_channels_14, total_samples = _audio_prm_total_samples_14},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_15, num_channels = _audio_prm_num_channels_15, total_samples = _audio_prm_total_samples_15},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_16, num_channels = _audio_prm_num_channels_16, total_samples = _audio_prm_total_samples_16},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_17, num_channels = _audio_prm_num_channels_17, total_samples = _audio_prm_total_samples_17},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_18, num_channels = _audio_prm_num_channels_18, total_samples = _audio_prm_total_samples_18},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_19, num_channels = _audio_prm_num_channels_19, total_samples = _audio_prm_total_samples_19},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_20, num_channels = _audio_prm_num_channels_20, total_samples = _audio_prm_total_samples_20},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_21, num_channels = _audio_prm_num_channels_21, total_samples = _audio_prm_total_samples_21},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_22, num_channels = _audio_prm_num_channels_22, total_samples = _audio_prm_total_samples_22},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_23, num_channels = _audio_prm_num_channels_23, total_samples = _audio_prm_total_samples_23},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_24, num_channels = _audio_prm_num_channels_24, total_samples = _audio_prm_total_samples_24},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_25, num_channels = _audio_prm_num_channels_25, total_samples = _audio_prm_total_samples_25},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_26, num_channels = _audio_prm_num_channels_26, total_samples = _audio_prm_total_samples_26},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_27, num_channels = _audio_prm_num_channels_27, total_samples = _audio_prm_total_samples_27},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_28, num_channels = _audio_prm_num_channels_28, total_samples = _audio_prm_total_samples_28},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_29, num_channels = _audio_prm_num_channels_29, total_samples = _audio_prm_total_samples_29},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_30, num_channels = _audio_prm_num_channels_30, total_samples = _audio_prm_total_samples_30},
					new AudioInfo{sampling_rate = _audio_prm_sampling_rate_31, num_channels = _audio_prm_num_channels_31, total_samples = _audio_prm_total_samples_31},
				};
			}
		}
#else
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public AudioInfo[] audio_prm;	/**< オーディオパラメータ */
#endif
		public UInt32 num_subtitle_channels;	/**< 字幕チャネル数 */
	}

	/**
	 * <summary>ビデオフレーム情報です。</summary>
	 */
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct FrameInfo
	{
		public Int32	frame_no;				/**< フレーム識別番号(0からの通し番号) */
		public Int32	frame_no_per_file;		/**< フレーム識別番号(ムービファイル固有の識別番号) */
		public UInt32	width;					/**< ムービの横幅[pixel] (８の倍数) */
		public UInt32	height;					/**< ムービの高さ[pixel] (８の倍数) */
		public UInt32	disp_width;				/**< 表示したい映像の横ピクセル数（左端から） */
		public UInt32	disp_height;			/**< 表示したい映像の縦ピクセル数（上端から） */
		public UInt32	framerate_n;			/**< 有理数形式フレームレート(分子) framerate [x1000] = framerate_n / framerate_d */
		public UInt32	framerate_d;			/**< 有理数形式フレームレート(分子) framerate [x1000] = framerate_n / framerate_d */
		public UInt64	time;					/**< 時刻。time / tunit で秒を表す。ループ再生や連結再生時には継続加算。 */
		public UInt64	tunit;					/**< 時刻単位 */
		public UInt32	cnt_concatenated_movie;	/**< ムービの連結回数 */
		AlphaType		alpha_type;				/**< アルファの合成モード */
		public UInt32	cnt_skipped_frames;		/**< デコードスキップされたフレーム数 */
	}
	
	/**
	 * <summary>キューポイントコールバック</summary>
	 * <param name="eventParamsString">イベントパラメタ文字列</param>	 
	 * \par 説明:
	 * キューポイントコールバック関数型です。<br/>
	 * 引数の文字列には以下の情報が含まれます。<br/>
	 *  -# タイマカウント
	 *  -# 1秒あたりのタイマカウント数
	 *  -# イベントタイプ
	 *  -# イベントポイント名
	 *  -# ユーザパラメータ文字列
     *  .
	 * 各情報は指定した区切り文字列を挟みながら一つの文字列として連結されて渡ってきます。<br/>
	 * 必要なパラメタを文字列からパースしてご利用ください。<br/>
	 * \sa CriManaPlayer::SetCuePointCallback, CriMana::SetCuePointFormatDelimiter
	 */
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void CuePointCbFunc(string eventParamsString);
	#endregion
	
	/**
	 * <summary>要求するシェーダの種類です。</summary>
	 * \par 説明:
	 * プラットフォームとUnityのバージョンによって要求されるシェーダの種類が異なります。<br/>
	 * CriManaPlayer::shaderDispatchFunction でユーザ定義のシェーダを使用する場合
	 * に適切なシェーダを返すために使用してください。
	 * \sa CriManaPlayer::DefaultShaderDispatchFunction, CriManaPlayer::shaderDispatchFunction 
	 */
	public const ShaderType RequiredShaderType =
#if CRIMANAPLAYER_SHADER_TYPE_YUV2RGB
		ShaderType.Yuv2Rgb;
#elif CRIMANAPLAYER_SHADER_TYPE_FORWARD_RGB
		ShaderType.ForwardRgb;
#endif

	/**
	 * <summary>デフォルトのシェーダ割り当て関数です。</summary>
	 * <param name="alpha">アルファムービかどうか</param>
	 * <param name="additive">加算合成かどうか</param>
	 * <returns>ムービ描画に使用するシェーダ</returns>
	 * \par 説明:
	 * デフォルトのシェーダ割り当て関数です。
	 * CriManaライブラリが提供するデフォルトのムービ用シェーダを返します。<br/>
	 * ユーザ定義のシェーダを使用する場合は CriManaPlayer::shaderDispatchFunction 
	 * にシェーダ割り当て関数を設定してください。
	 * \sa CriManaPlayer::RequiredShaderType, CriManaPlayer::shaderDispatchFunction
	 */
	static public Shader DefaultShaderDispatchFunction(bool alpha, bool additive)
	{
		Shader shader;
		#if CRIMANAPLAYER_SHADER_TYPE_FORWARD_RGB
		if (alpha) {
			shader = Shader.Find(additive ? "CriMana/ForwardRgbaAdditive" : "CriMana/ForwardRgba");
		}
		else {
			shader = Shader.Find(additive ? "CriMana/ForwardRgbAdditive" : "CriMana/ForwardRgb");
		}
		#elif CRIMANAPLAYER_SHADER_TYPE_YUV2RGB
		if (alpha) {
			shader = Shader.Find(additive ? "CriMana/Yuva2RgbaAdditive" : "CriMana/Yuva2Rgba");
		}
		else {
			shader = Shader.Find(additive ? "CriMana/Yuv2RgbAdditive" :"CriMana/Yuv2Rgb");
		}
		#endif
		if (shader == null) {
			Debug.LogError(
				"Can't find CriMana Sharder. " +
				"Probably, the link from a material to shader has broken. " +
				"Please reimport 'CRIWARE SDK for Unity'.");
			shader = Shader.Find("Diffuse");
		}
		return shader;
	}

	#region Internal Variables
	private const int  InvalidPlayerId   = -1;

	Action          setFileDelegate;
	private int		playerId		= InvalidPlayerId;
	private CriManaPlayerDetail.TextureHolder texture_holder = null;
	private bool    setupOnceFlag   = false;
	private bool	prepareRequire  = false;
	private bool	playRequire		= false;
	private bool	stopRequire		= false;
	private bool	gotFirstFrame	= false;
	private bool	gotMovieInfo	= false;

	private bool	isSeeking		= false;
	private Int32	seekFrameNumber	= 0;

	private bool    unpauseOnApplicationUnpause = false;

	private UnityEngine.Material originalMaterial = null;
	private UnityEngine.Material createdMaterial = null;
	[SerializeField]
	private UnityEngine.Material _movieMaterial = null;
	
    private Shader movieShader  = null;

	private CriFsBinder _binder;
	private Func<bool, bool, Shader> _shaderDispatchFunction = DefaultShaderDispatchFunction;
	
#if !CRIMANAPLAYER_UNSUPPORT_CUEPOINT_CALLBACK
	private CuePointCbFunc cuePointUserCbFunc = null;
#endif

#if CRIMANAPLAYER_CALLBACK_IMPL_NATIVE2MANAGED
	private GCHandle cuePointUserCbFuncPinHandle;
#endif

	[SerializeField]
	private uint _texNumber = 2; /* Double Texturing */
	[SerializeField]
	private string _moviePath = "";
	[SerializeField]
	private bool _playOnStart = false;
	[SerializeField]
	private bool _loop = false;
	[SerializeField]
	private float _volume = 1.0f;
	[SerializeField]
	private float _subAudioVolume = 1.0f;
	[SerializeField]
	private float _speed = 1.0f;
	[SerializeField]
	private bool _additiveMode = false;
	[SerializeField]
	private bool _flipTopBottom = true;
	[SerializeField]
	private bool _flipLeftRight = true;

	private MovieInfo _movieInfo;
	private FrameInfo _frameInfo;
	#endregion

	#region Properties
	/**
	 * <summary>シェーダの選択関数を設定／取得します。</summary>
	 * \par 説明:
	 * ムービの描画に利用するシェーダを決定する関数のためのプロパティです。<br/>
	 * 「アルファムービかどうか」、「加算合成かどうか」の2つの引数を取ってシェーダを
	 * 返す関数を設定できます。
	 * \par 注意:
	 * ムービの再生前に設定する必要があります。
	 * シェーダの選択がムービの再生開始時でのみ行われるためです。<br/>
	 * \sa CriManaPlayer::RequiredShaderType, CriManaPlayer::DefaultShaderDispatchFunction
	 */
	public Func<bool, bool, Shader> shaderDispatchFunction {
		set { this._shaderDispatchFunction = value; }
		get { return this._shaderDispatchFunction; }
	}
	
	/**
	 * <summary>シェーダの選択関数を設定／取得します。</summary>
	 * \par 説明:
	 * CriManaPlayer::shaderDispatchFunction のスペルミス修正の互換性維持のためのプロパティです。
	 * \sa CriManaPlayer::shaderDispatchFunction
	 */
	[Obsolete("This property is obsolete; use shaderDispatchFunction instead", false)]
	public Func<bool, bool, Shader> shaderDispachFunction {
		set { this.shaderDispatchFunction = value; }
		get { return this.shaderDispatchFunction; }
	}

	/**
	 * <summary>使用するテクスチャの数を設定／取得します。</summary>
	 * \par 説明:
	 * ムービの描画に使用するテクスチャの数を設定／取得するためのプロパティです。<br/>
	 * デフォルト値は2です。
	 * \par 注意:
	 * - 通常は変更しないでください。<br/>
	 * - ムービの再生前に設定する必要があります。<br/>
	 * \par 備考:
	 * 通常、描画中のテクスチャに画像を転送すると、描画が完了するまで転送がブロックされてしまいます。<br/>
	 * そこで、 CriManaPlayer ではムービを描画するためのテクスチャをマルチバッファリングしています。<br/>
	 * このプロパティでは何枚のテクスチャでマルチバッファリングするか設定します。<br/>
	 * なお、実際に内部で確保されるテクスチャの枚数はプラットフォームによって異なります。<br/>
	 */
	public uint texNumber {
		set { this._texNumber = value; }
		get { return this._texNumber; }
	}

	/**
	 * <summary>ストリーミング再生用のバインダを取得します。</summary>
	 * \sa CriManaPlayer::SetFile
	 */
	public CriFsBinder binder {
		private set { this._binder = value; }
		get { return this._binder; }
	}

	/**
	 * <summary>ストリーミング再生用のファイルパスを設定／取得します。</summary>
	 * <param name="filePath">ファイルパス</param>
	 * \par 説明:
	 * ストリーミング再生用のファイルパスを設定／取得するためのプロパティです。<br/>
	 * - 相対パスを指定した場合は StreamingAssets フォルダからの相対でファイルをロードします。
	 * - 絶対パス、あるいはURLを指定した場合には指定したパスでファイルをロードします。
	 * \sa CriManaPlayer::SetFile
	 */
	public string moviePath {
		set
		{
			this._moviePath = value;
		}
		get { return this._moviePath; }
	}

	/**
	 * <summary>オブジェクトの生成時に再生を行うかを設定します。</summary>
	 * <param name="sw">スイッチ(true: 再生を行う, false: 再生を行わない)</param>
	 */
	public bool playOnStart {
		set { this._playOnStart = value; }
		get { return this._playOnStart; }
	}

	/**
	 * <summary>ムービ再生のループ切り替えを行います。</summary>
	 * \par 値の意味:
	 * <param name="sw">ループスイッチ(true: ループモード, false: ループモード解除)</param>
	 * \par 説明:
	 * ループ再生のON/OFFを切り替えます。デフォルトはループOFFです。<br/>
	 * ループ再生をONにした場合は、ムービ終端まで再生しても再生は終了せず先頭に戻って再生を
	 * 繰り返します。<br/>
	 */
	public bool loop {
		set
		{
			this._loop = value;
			if (this.playerId != InvalidPlayerId) {
				CriManaPlugin.criManaUnityPlayer_Loop(this.playerId, (this._loop ? 1 : 0));
			}
		}
		get { return this._loop; }
	}
	
	/**
	 * <summary>ムービ再生の音声ボリューム調整を行います。</summary>
	 * <param name="volume">ボリューム</param>
	 * \par 説明:
	 * ムービのメインオーディオトラックの出力音量ボリュームを指定します。<br/>
	 * ボリューム値には、0.0f〜1.0fの範囲で実数値を指定します。<br/>
	 * ボリューム値は音声データの振幅に対する倍率です（単位はデシベルではありません）。<br/>
	 * 例えば、1.0fを指定した場合、原音はそのままのボリュームで出力されます。<br/>
	 * 0.0fを指定した場合、音声はミュートされます（無音になります）。<br/>
	 * ボリュームのデフォルト値は1.0fです。<br>
	 * \par 備考:
	 * 0.0f〜1.0fの範囲外を指定した場合は、それぞれの最小・最大値にクリップされます。<br/>
	 */
	public float volume {
		set
		{
			this._volume = value;
			if (this.playerId != InvalidPlayerId) {
				CriManaPlugin.criManaUnityPlayer_SetVolume(this.playerId, this._volume);
			}
		}
		get { return this._volume; }
	}
	
	
	/**
	 * <summary>ムービ再生の音声ボリューム調整を行います。（サブオーディオ）</summary>
	 * <param name="volume">ボリューム</param>
	 * \par 説明:
	 * ムービのサブオーディオトラックの出力音量ボリュームを指定します。<br/>
	 * ボリューム値には、0.0f〜1.0fの範囲で実数値を指定します。<br/>
	 * ボリューム値は音声データの振幅に対する倍率です（単位はデシベルではありません）。<br/>
	 * 例えば、1.0fを指定した場合、原音はそのままのボリュームで出力されます。<br/>
	 * 0.0fを指定した場合、音声はミュートされます（無音になります）。<br/>
	 * ボリュームのデフォルト値は1.0fです。<br>
	 * \par 備考:
	 * 0.0f〜1.0fの範囲外を指定した場合は、それぞれの最小・最大値にクリップされます。<br/>
	 */
	public float subAudioVolume {
		set
		{
			this._subAudioVolume = value;
			if (this.playerId != InvalidPlayerId) {
				CriManaPlugin.criManaUnityPlayer_SetSubAudioVolume(this.playerId, this._subAudioVolume);
			}
		}
		get { return this._subAudioVolume; }
	}
	
	/**
	 * <summary>ムービ再生の速度調整を行います。</summary>
	 * <param name="speed">再生速度</param>
	 * \par 説明:
	 * ムービの再生速度を指定します。<br/>
	 * 再生速度は、0.0f〜5.0fの範囲で実数値を指定します。<br/>
	 * 1.0fが指定された場合はデータ本来の速度で再生を行います。<br/>
	 * 例えば、2.0fを指定した場合、ムービはムービデータのフレームレートの２倍の速度で再生されます。<br/>
	 * 再生速度のデフォルト値は1.0fです。<br/>
	 * \par 注意:
	 * 再生中のムービに対する再生速度の変更は、音声がないムービを再生した場合のみ対応しています。<br/>
	 * 音声つきムービに対して再生中の速度変更を行うと正常動作しません。音声つきのムービの場合は、一度再生停止
	 * してから、再生速度を変更し、目的のフレーム番号からシーク再生をしてください。<br>
	 * <br/>
	 */
	public float speed {
		set
		{
			if (value < 0.0f) {
				return;
			}
			this._speed = value;
			if (this.playerId != InvalidPlayerId) {
				CriManaPlugin.criManaUnityPlayer_SetSpeed(this.playerId, this._speed);
			}
		}
		get { return this._speed; }
	}

	/**
	 * <summary>ムービを貼付けるマテリアルを設定します。</summary>
	 * \par 説明:
	 * マテリアルを設定しない場合は、 CriManaPlayer がAddされているオブジェクトに
	 * ムービを貼り付けるマテリアルを追加します。<br/>
	 * \par 注意:
	 * ムービの再生前に設定する必要があります。
	 */
	public UnityEngine.Material movieMaterial {
		set { this._movieMaterial = value; }
		get { return this._movieMaterial; }
	}

	/**
	 * <summary>ムービの上下を反転します。</summary>
	 * \par 説明:
	 * テクスチャのUV座標の上下を反転させることで、ムービの上下を反転させます。<br/>
	 * \par 注意:
	 * ムービの再生前に設定する必要があります。
	 * \sa CriManaPlayer::flipLeftRight
	 */
	public bool flipTopBottom {
		set { this._flipTopBottom = value; SetTextureConfigIfTextureHolderIsNotNull(); }
		get { return this._flipTopBottom; }
	}

	/**
	 * <summary>ムービの左右を反転します。</summary>
	 * \par 説明:
	 * テクスチャのUV座標の左右を反転させることで、ムービの左右を反転させます。<br/>
	 * \par 注意:
	 * ムービの再生前に設定する必要があります。
	 * \sa CriManaPlayer::flipTopBottom
	 */
	public bool flipLeftRight {
		set { this._flipLeftRight = value; SetTextureConfigIfTextureHolderIsNotNull(); }
		get { return this._flipLeftRight; }
	}

	/**
	 * <summary>ムービのブレンドモードを加算合成モードにします。</summary>
	 * \par 説明:
	 * 再生時のシェーダ選択の際に CriManaPlayer::shaderDispatchFunction
	 * に渡される引数になります。<br/>
	 * \par 注意:
	 * ムービの再生前に設定する必要があります。
	 * \sa CriManaPlayer::shaderDispatchFunction
	 */
	public bool additiveMode {
		set { this._additiveMode = value; }
		get { return this._additiveMode; }
	}

	/**
	 * <summary>再生中のムービ情報を取得します。</summary>
	 * <param name="movieInfo">ムービ情報</param>
	 * \par 説明:
	 * 再生を開始したムービのヘッダ解析結果の情報です。<br/>
	 * ムービの解像度、フレームレートなどの情報が参照できます。<br/>
	 * \par 注意:
	 * 再生開始直後は有効な情報が取得できません。プレーヤのステータスが \link CriManaPlayer::Status WaitPrep〜Playing \endlink
	 * 状態の間のみ有効な情報が取得できます。
	 */
	public MovieInfo movieInfo {
		get { return this._movieInfo; }
	}

	/**
	 * <summary>再生中のムービのフレーム情報を取得します。</summary>
	 * <param name="frameInfo">フレーム情報</param>
	 * \par 説明:
	 * 現在描画中のフレーム情報を取得できます。<br/>
	 * \par 備考:
	 * 再生状態のみ有効な情報が取得できます。それ以外の状態では不正な情報となります。<br/>
	 */
	public FrameInfo frameInfo {
		private set { this._frameInfo = value; }
		get { return this._frameInfo; }
	}

	public UInt32 width { get { return this.movieInfo.disp_width; } }
	public UInt32 height { get { return this.movieInfo.disp_height; } }

	/**
	 * <summary>プレーヤのステータスを取得します。</summary>
	 * <returns>ステータス</returns>
	 * \par 説明：
	 * プレーヤのステータスを取得します。<br/>
	 * \sa CriManaPlayer::Status
	 */
	public Status status{
		get {
			if (this.playerId != InvalidPlayerId) {
				Status nativeStatus = (Status)CriManaPlugin.criManaUnityPlayer_GetStatus(playerId);
				if ((nativeStatus == Status.Ready) && (texture_holder == null)) {
					/* ネイティブの状態が Ready かつテクスチャが未生成なら Prep にする */
					return Status.Prep;
				}			
				return nativeStatus;
			}
			return Status.Stop;
		}
	}
	
	/**
	 * <summary>連結再生エントリー数を取得します。</summary>
	 * \par 説明：
	 * 連結再生エントリー数を取得します。<br/>
	 * このプロパティで得られる値は、読み込みが開始されていないエントリー数です。<br/>
	 * 連結する数フレーム前に、エントリーに登録されているムービの読み込みが開始されます。<br/>
	 */
	public Int32 numberOfEntries {
		get {
			 return (this.playerId != InvalidPlayerId)
				? CriManaPlugin.criManaUnityPlayer_GetNumberOfEntry(this.playerId)
				: -1;
		}
	}

	/**
	 * <summary>フレームが更新されたかを取得します。</summary>
	 * \par 説明：
	 * Updateメソッドでフレームが更新されたかを取得します。<br/>
	 */
	public bool frameUpdated { private set; get; }
	#endregion

	/**
	 * <summary>ストリーミング再生用のファイルパスの設定を行います。</summary>
	 * <param name="argBinder">CPKファイルをバインド済みのバインダ</param>
	 * <param name="argMoviePath">CPKファイル内のコンテンツパス</param>
	 * <param name="setMode">ムービの追加モード</param>
	 * <returns>セットに成功したか</returns>
	 * \par 説明:
	 * ストリーミング再生用のファイルパスを設定します。<br/>
	 * 第一引数の argBinder にCPKファイルをバインドしたバインダハンドルを指定することにより、CPKファイルからのムービ再生が行えます。<br/>
	 * CPKからではなく直接ファイルからストリーミングする場合は、argBinder に null を指定ください。<br/>
	 * 第三引数の setMode に CriManaPlayer::SetMode.Append を与えると、連結再生を行います。この場合、関数は false を返す可能性があります。<br/>
	 * 連結再生できるムービファイルには以下の条件があります。<br>
	 * - ビデオの解像度、フレームレート、コーデックが全て同じ
	 * - アルファ情報の有無が一致
	 * - 音声トラックの構成（トラック番号および各チャンネル数）、コーデックが全て同じ
	 * 
	 * \par バインダを指定しない場合(nullを指定した場合):
	 * - moviePath プロパティを設定した場合と等価です。<br/>
	 * - 相対パスを指定した場合は StreamingAssets フォルダからの相対でファイルをロードします。
	 * - 絶対パス、あるいはURLを指定した場合には指定したパスでファイルをロードします。<br/>
	 * 
	 * \par バインダを指定した場合:
	 * バインダ設定後に moviePath プロパティを設定した場合は、バインダにバインドされているCPKファイルからムービ再生が行われます。<br/>
	 * \sa CriManaPlayer::SetContentId, CriManaPlayer::SetFileRange, CriManaPlayer::moviePath
	 */
	public bool SetFile(CriFsBinder argBinder, string argMoviePath, SetMode setMode = SetMode.New)
	{
		System.IntPtr binderPtr = (argBinder == null) ? IntPtr.Zero : argBinder.nativeHandle;
		string trueMoviePath;
		if (argBinder != null) {
			trueMoviePath = argMoviePath;
		} else {
			trueMoviePath = 
				CriWare.IsStreamingAssetsPath(argMoviePath) 
				? Path.Combine(CriWare.streamingAssetsPath, argMoviePath)
				: argMoviePath;
		}
		
		if (setMode == SetMode.New) {
			CriManaPlugin.criManaUnityPlayer_ClearEntry(playerId);
			binder    = argBinder;
			moviePath = argMoviePath;
			setFileDelegate = () => { CriManaPlugin.criManaUnityPlayer_SetFile(playerId, binderPtr, trueMoviePath); };
			return true;
		}
		else {
			bool repeat = (setMode == SetMode.AppendRepeatedly) ? true :false;
			return CriManaPlugin.criManaUnityPlayer_EntryFile(playerId, binderPtr, trueMoviePath, repeat);
		}
	}

	/**
	 * <summary>CPKファイル内のムービファイルの指定を行います。 (コンテンツID指定)</summary>
	 * <param name="arbBinder">CPKファイルをバインド済みのバインダ</param>
	 * <param name="contentId">CPKファイル内のコンテンツID</param>
	 * <param name="setMode">ムービの追加モード</param>
	 * <returns>セットに成功したか</returns>
	 * \par 説明:
	 * ストリーミング再生用のコンテンツIDを設定します。<br/>
	 * 引数にバインダとコンテンツIDを指定することで、CPKファイル内の任意のムービデータを読み込み元にすることが出来ます。
	 * 第三引数の setMode に CriManaPlayer::SetMode.Append を与えると、連結再生を行います。この場合、関数は false を返す可能性があります。<br/>
	 * 連結再生できるムービファイルには以下の条件があります。<br>
	 * - ビデオの解像度、フレームレート、コーデックが全て同じ
	 * - アルファ情報の有無が一致
	 * - 音声トラックの構成（トラック番号および各チャンネル数）、コーデックが全て同じ
	 * \sa CriManaPlayer::SetFile, CriManaPlayer::SetFileRange
	 */
	public bool SetContentId(CriFsBinder argBinder, int contentId, SetMode setMode = SetMode.New)
	{
		if (argBinder == null) {
			Debug.LogError("[CRIWARE] CriFsBinder is null");
		}
		if (setMode == SetMode.New) {
			CriManaPlugin.criManaUnityPlayer_ClearEntry(playerId);
			binder    = argBinder;
			moviePath = "";
			setFileDelegate = () =>
			{
				CriManaPlugin.criManaUnityPlayer_SetContentId(playerId, binder.nativeHandle, contentId);
			};
			return true;
		}
		else {
			bool repeat = (setMode == SetMode.AppendRepeatedly) ? true :false;
			return CriManaPlugin.criManaUnityPlayer_EntryContentId(playerId, argBinder.nativeHandle, contentId, repeat);
		}
	}

	/**
	 * <summary>パックファイル内のムービファイルの指定を行います。 (ファイル範囲指定)</summary>
	 * <param name="filePath">パックファイルへのパス</param>
	 * <param name="offset">パックファイル内のムービファイルのデータ開始位置</param>
	 * <param name="range">パックファイル内のムービファイルのデータ範囲</param>
	 * <param name="setMode">ムービの追加モード</param>
	 * <returns>セットに成功したか</returns>
	 * \par 説明:
	 * ストリーミング再生を行いたいムービファイルをパッキングしたファイルを指定します。<br/>
	 * 引数にオフセット位置とデータ範囲を指定することで、パックファイル内の任意のムービデータを読み込み元にすることが出来ます。
	 * 第二引数の range に -1 を指定した場合はパックファイルの終端までをデータ範囲とします。
	 * 第四引数の setMode に CriManaPlayer::SetMode.Append を与えると、連結再生を行います。この場合、関数は false を返す可能性があります。<br/>
	 * 連結再生できるムービファイルには以下の条件があります。<br>
	 * - ビデオの解像度、フレームレート、コーデックが全て同じ
	 * - アルファ情報の有無が一致
	 * - 音声トラックの構成（トラック番号および各チャンネル数）、コーデックが全て同じ
	 * \sa CriManaPlayer::SetFile, CriManaPlayer::SetContentId, CriManaPlayer::moviePath
	 */
	public bool SetFileRange(string filePath, UInt64 offset, Int64 range, SetMode setMode = SetMode.New)
	{
		if (setMode == SetMode.New) {
			CriManaPlugin.criManaUnityPlayer_ClearEntry(playerId);
			binder    = null;
			moviePath = filePath;
			setFileDelegate = () =>
			{ CriManaPlugin.criManaUnityPlayer_SetFileRange(playerId, filePath, offset, range); };
			return true;
		}
		else {
			bool repeat = (setMode == SetMode.AppendRepeatedly) ? true :false;
			return CriManaPlugin.criManaUnityPlayer_EntryFileRange(playerId, filePath, offset, range, repeat);
		}
	}

	/**
	 * <summary>再生準備処理を行います。</summary>
	 * \par 説明:
	 * ムービ再生は開始せず、ヘッダ解析と再生準備(バッファリング)のみを行って待機する関数です。<br/>
	 * ムービ再生開始の頭出しをそろえたい場合に本関数を使用します。<br/>
	 * <br/>
	 * 再生対象のムービを指定した後、本関数を呼び出してください。
	 * 再生準備が完了すると、プレーヤの状態は \link CriManaPlayer::Status Ready \endlink 状態になり、 CriManaPlayer::Play 関数ですぐに描画開始されます。<br/>
	 * <br/>
	 * 本関数は \link CriManaPlayer::Status Stop \endlink 状態、また \link CriManaPlayer::Status PlayEnd \endlink 状態でのみ呼び出してください。
	 * 
	 * \sa CriManaPlayer::Status
	 */
	public void Prepare()
	{
		this.SetupCallOnce();
		prepareRequire = true;
		this.UpdatePlayer();
	}

	/**
	 * <summary>再生を開始します。</summary>
	 * \par 説明:
	 * ムービ再生を開始します。<br/>
	 * 再生が開始されると、状態は \link CriManaPlayer::Status Playing \endlink になります。
	 * \par 注意:
	 * 本関数を呼び出し後、実際にムービの描画が開始されるまで数フレームかかります。</br>
	 * ムービ再生の頭出しを行いたい場合は、 CriManaPlayer::Prepare 関数で事前に再生準備を行ってください。
	 * \sa CriManaPlayer::Stop, CriManaPlayer::Status
	 */
	public void Play()
	{
		this.SetupCallOnce();
		gotFirstFrame = false;
		playRequire   = true;
		this.UpdatePlayer();
	}

	/**
	 * <summary>ムービ再生の停止要求を行います。</summary>
	 * \par 説明:
	 * ムービ再生の停止要求を出します。本関数は即時復帰関数です。<br/>
	 * 本関数を呼び出すと、描画は即座に終了しますが、再生はすぐには止まりません。<br/>
	 * \sa CriManaPlayer::Status
	 */
	public void Stop()
	{
		if ((originalMaterial != null) && (GetComponent<Renderer>() != null)) {
			GetComponent<Renderer>().material = originalMaterial;
		}
		CriManaPlugin.criManaUnityPlayer_Stop(this.playerId);
		prepareRequire  = false;
		playRequire     = false;
		gotFirstFrame   = false;
		stopRequire     = true;
	}

	/**
	 * <summary>ムービ再生のポーズ切り替えを行います。</summary>
	 * <param name="sw">ポーズスイッチ(true: ポーズ, false: ポーズ解除)</param>
	 * \par 説明:
	 * ポーズのON/OFFを切り替えます。<br/>
	 * 引数にtrueを指定する一時停止、falseを指定すると再生再開です。<br/>
	 * CriManaPlayer::Stop 関数を呼び出すと、ポーズ状態は解除されます <br/>
	 * \sa CriManaPlayer::IsPaused
	 */
	public void Pause(bool sw)
	{
		if (this.playerId != InvalidPlayerId) {
			CriManaPlugin.criManaUnityPlayer_Pause(this.playerId, (sw) ? 1 : 0);
		}
	}

	/**
	 * <summary>ムービ再生のポーズ状態の取得を行います。</summary>
	 * <returns>ポーズ状態</returns>
	 * \par 説明:
	 * ポーズのON/OFFを取得します。<br/>
	 * \sa CriManaPlayer::Pause
	 */
	public bool IsPaused()
	{
		return CriManaPlugin.criManaUnityPlayer_IsPaused(this.playerId);
	}

	/**
	 * <summary>再生時刻の取得を行います。</summary>
	 * <returns>再生時刻 (単位:マイクロ秒)</returns>
	 * \par 説明:
	 * 現在再生中のムービの再生時刻を取得します。<br/>
	 * 単位はマイクロ秒です。<br/>
	 */
	public long GetTime()
	{
		if (this.playerId != InvalidPlayerId) {
			return CriManaPlugin.criManaUnityPlayer_GetTime(this.playerId);
		} else {
			return 0;
		}
	}

	/**
	 * <summary>総フレーム数の取得を行います。</summary>
	 * <returns>再生中のムービの総フレーム数</returns>
	 * \par 説明:
	 * 現在再生中のムービの総フレーム数を取得します。<br/>
	 * \par 注意:
	 * 本関数は再生準備状態または再生状態の間でのみ呼び出してください。<br/>
	 * それ以外の状態の時に本関数を呼び出すと不正な値が返ります。
	 */
	public uint GetTotalFrames()
	{
		if (this.playerId != InvalidPlayerId) {
			return movieInfo.total_frames;
		} else {
			return 0;
		}
	}

	/**
	 * <summary>フレームレートの取得を行います。</summary>
	 * <returns>再生中のムービのフレームレート(fps*1000)</returns>
	 * \par 説明:
	 * 現在再生中のムービのフレームレートを取得します。<br/>
	 * \par 注意:
	 * 本関数は再生準備状態または再生状態の間でのみ呼び出してください。<br/>
	 * それ以外の状態の時に本関数を呼び出すと不正な値が返ります。
	 */
	public uint GetFramerate()
	{
		if (this.playerId != InvalidPlayerId) {
			return movieInfo.framerate_n;
		} else {
			return 0;
		}
	}

	/**
	 * <summary>プレーヤのステータスを取得します。</summary>
	 * <returns>ステータス</returns>
	 * \par 説明：
	 * プレーヤのステータスを取得します。<br/>
	 * \sa CriManaPlayer::Status, CriManaPlayer::status
	 */
	public Status GetStatus()
	{
		return status;
	}

	/**
	 * <summary>キューポイントコールバック関数を登録します。</summary>
	 * <param name="eventParamsString">キューポイントコールバック関数</param>	 
	 * \par 説明:
	 * キューポイントコールバック関数を登録します。<br/>
	 * \par 注意:
	 * 初回の再生でキューポイント機能を使用するには、 CriManaPlayer::Prepare 関数および
	 * CriManaPlayer::Play 関数より先に本関数を呼び出してください。
	 * \sa CriManaPlayer::CuePointCbFunc, CriMana::SetCuePointFormatDelimiter
	 */
	public void SetCuePointCallback(CuePointCbFunc func)
	{
#if CRIMANAPLAYER_SUPPORT_CUEPOINT_CALLBACK		
		/* ネイティブプラグインに関数ポインタを登録 */
		if (func != null) {
			cuePointUserCbFunc = func;
			IntPtr ptr         = IntPtr.Zero;
	#if CRIMANAPLAYER_CALLBACK_IMPL_NATIVE2MANAGED
			if (cuePointUserCbFuncPinHandle.IsAllocated) {
				cuePointUserCbFuncPinHandle.Free();
			}
			CuePointCbFunc delegateInstance = this.CuePointCallbackFromNative;
			cuePointUserCbFuncPinHandle     = GCHandle.Alloc(delegateInstance, GCHandleType.Pinned);
			ptr = Marshal.GetFunctionPointerForDelegate(delegateInstance);
	#endif
			CriManaPlugin.criManaUnityPlayer_SetCuePointCallback(this.playerId, ptr, this.gameObject.name, "CuePointCallbackFromNative");
		}
#elif CRIMANAPLAYER_UNSUPPORT_CUEPOINT_CALLBACK
		Debug.LogError("This platform does not support event callback feature.");
#endif
	}

	/**
	 * <summary>シーク位置の設定を行います。</summary>
	 * <param name="argSeekFrameNumber">シーク先のフレーム番号</param>
	 * \par 説明:
	 * シーク再生を開始するフレーム番号を指定します。<br>
	 * 本関数を実行しなかった場合、またはフレーム番号0を指定した場合はムービの先頭から再生を開始します。
	 * 指定したフレーム番号が、ムービデータの総フレーム数より大きかったり負の値だった場合もムービの先頭から再生します。
	 */
	public void SetSeekPosition(int argSeekFrameNumber)
	{
		if (argSeekFrameNumber > 0) {
			seekFrameNumber = argSeekFrameNumber;
			isSeeking       = true;
		}
	}
	
	/**
	 * <summary>[WiiU] デバイスセンドレベルを設定します。</summary>
	 * 音声出力デバイスが複数存在するプラットフォームにおいて、<br>
	 * ムービに含まれる音声を各デバイスからどれだけ出力するかを設定します。
	 */
	public void SetDeviceSendLevel(int deviceId, float level)
	{
#if !UNITY_EDITOR && UNITY_WIIU
		if (this.playerId != InvalidPlayerId) {
			CriManaPlugin.criManaUnityPlayer_SetDeviceSendLevel(this.playerId, deviceId, level);
		}
#endif
	}
	
	/**
	 * <summary>メインオーディオトラック番号の設定を行います。</summary>
	 * <param name="track">トラック番号</param>
	 * \par 説明:
	 * ムービが複数のオーディオトラックを持っている場合に、再生するオーディオを指定します。<br>
	 * 本関数を実行しなかった場合は、もっとも若い番号のオーディオトラックを再生します。<br>
	 * データが存在しないトラック番号を指定した場合は、オーディオは再生されません。<br>
	 * <br>
	 * トラック番号としてAudioTrack.Offを指定すると、例えムービにオーディオが
	 * 含まれていたとしてもオーディオは再生しません。<br>
	 * <br>
	 * また、デフォルト設定（もっとも若いチャネルのオーディオを再生する）にしたい場合は、
	 * チャネルとしてAudioTrack.Autoを指定してください。<br>
	 *
	 * \par 備考:
	 * 再生中のトラック変更は未対応です。変更前のフレーム番号を記録しておいてシーク再生
	 * してください。
	 */
	public void SetAudioTrack(int track)
	{
		if (this.playerId != InvalidPlayerId) {
			CriManaPlugin.criManaUnityPlayer_SetAudioTrack(this.playerId, track);
		}
	}
	
	public void SetAudioTrack(AudioTrack track)
	{
		if (this.playerId != InvalidPlayerId) {
			if (track == AudioTrack.Off) {
				CriManaPlugin.criManaUnityPlayer_SetAudioTrack(this.playerId, -1);
			} else if (track == AudioTrack.Auto) {
				CriManaPlugin.criManaUnityPlayer_SetAudioTrack(this.playerId, 100);
			}
		}
	}
	
	
	/**
	 * <summary>サブオーディオトラック番号の設定を行います。</summary>
	 * <param name="track">トラック番号</param>
	 * \par 説明:
	 * ムービが複数のオーディオトラックを持っている場合に、再生するオーディオを指定します。<br>
	 * 本関数を実行しなかった場合（デフォルト設定）はサブオーディオからは何も再生されません。
	 * また、データが存在しないトラック番号を指定した場合、サブオーディオトラックとして
	 * メインオーディオと同じトラックを指定した場合もサブオーディオからは何も再生されません。<br>
	 *
	 * \par 備考:
	 * 再生中のトラック変更は未対応です。変更前のフレーム番号を記録しておいてシーク再生
	 * してください。
	 */
	public void SetSubAudioTrack(int track)
	{
		if (this.playerId != InvalidPlayerId) {
			CriManaPlugin.criManaUnityPlayer_SetSubAudioTrack(this.playerId, track);
		}
	}
	
	public void SetSubAudioTrack(AudioTrack track)
	{
		if (this.playerId != InvalidPlayerId) {
			if (track == AudioTrack.Off) {
				CriManaPlugin.criManaUnityPlayer_SetSubAudioTrack(this.playerId, -1);
			} else if (track == AudioTrack.Auto) {
				CriManaPlugin.criManaUnityPlayer_SetSubAudioTrack(this.playerId, 100);
			}
		}
	}
	
	/**
	 * <summary>テクスチャの生成を行います。</summary>
	 * <param name="argWidth">ムービの幅</param>
	 * <param name="argHeight">ムービの高さ</param>
	 * <param name="alphaMode">アルファムービか否か</param>
	 * <param name="coroutineMode">テクスチャの生成をコルーチン化するか</param>
	 * \par 説明:
	 * テクスチャの生成を行います。<br/>
	 * 通常はテクスチャの生成はプレーヤ内部で自動的に行いますが、 CriManaPlayer::Prepare および
	 * CriManaPlayer::Play を呼び出す前に本関数を呼び出すことでテクスチャの生成を事前に行う
	 * ことができます。<br/>
	 * ただし、以下のいずれかの場合は CriManaPlayer::Update 関数内でテクスチャの再生成を行います。<br/>
	 * - argWidth が実際に再生するムービのwidthより小さい
	 * - argHeight が実際に再生するムービのheightより小さい
	 * - alphaMode が実際に再生するムービのアルファの有無と異なる
	 * ただし、argWidth, argHeight, alphaMode のいずれかが実際に再生するムービの情報と違う場合は、
	 * CriManaPlayer::Update 関数内でテクスチャの再生成を行います。<br/>
	 * 第四引数の coroutineMode に true を与えると、テクスチャの生成をコルーチン化します。<br/>
	 * \sa CriManaPlayer::DestroyTexture, CriManaPlayer::Prepare, CriManaPlayer::Play
	 */
	public void CreateTexture(int argWidth, int argHeight, bool alphaMode, bool coroutineMode)
	{
		if (texture_holder != null) {
			if (!texture_holder.IsAvailable(argWidth, argHeight, texNumber, alphaMode)) {
				DestroyTexture();
			}
		}

		if (texture_holder == null) {
			IEnumerator emun_obj = CreateTextureByCoroutine(
				CriManaPlayerDetail.TextureHolder.Create(argWidth, argHeight, texNumber, alphaMode)
				);
			if (coroutineMode) {
				StartCoroutine(emun_obj);
			}
			else {
				while (emun_obj.MoveNext()) {
				}
			}
		}
	}

	/**
	 * <summary>テクスチャの破棄を行います。</summary>
	 * \par 説明:
	 * テクスチャの破棄を行います。<br/>
	 * テクスチャの破棄は CriManaPlayer::OnDisable 時に行われますが、
	 * 本関数で明示的に行うことができます。<br/>
	 * \sa CriManaPlayer::CreateTexture
	 */
	public void DestroyTexture()
	{
		if (texture_holder != null) {
			texture_holder.DestroyTexture();
			texture_holder = null;
		}
	}
	
	#region MonoBehavior inherited functions
	void Awake()
	{
		CriManaPlugin.InitializeLibrary();
		playerId      = CriManaPlugin.criManaUnityPlayer_Create();
		frameUpdated  = false;
	}
	
	void OnEnable()
	{}
	
	void Start()
	{
		SetupCallOnce();
		if (playOnStart) {
			Play();
		}
	}
	
	void OnDisable()
	{
		Stop();
	}
	
	void OnDestroy()
	{
		movieMaterial    = null;
		originalMaterial = null;
		if (createdMaterial != null) {
			DestroyImmediate(createdMaterial);
		}
		createdMaterial = null;
		DestroyTexture();
		if (this.playerId != InvalidPlayerId) {
			CriManaPlugin.criManaUnityPlayer_Destroy(this.playerId);
			this.playerId = InvalidPlayerId;
		}
		#if CRIMANAPLAYER_CALLBACK_IMPL_NATIVE2MANAGED
		if (cuePointUserCbFuncPinHandle.IsAllocated) {
			cuePointUserCbFuncPinHandle.Free();
		}
		#endif
		CriManaPlugin.FinalizeLibrary();
	}
	
	void OnApplicationPause(bool appPause)
	{
		if (appPause) {
			unpauseOnApplicationUnpause = !IsPaused();
			if (unpauseOnApplicationUnpause) {
				Pause(true);
			}
		}
		else {
			if (unpauseOnApplicationUnpause) {
				Pause(false);
			}
			unpauseOnApplicationUnpause = false;
		}
		
	}
	
	public void Update()
	{
		this.UpdatePlayer();
	}
	
	public void OnDrawGizmos()
	{
		if (status == Status.Playing) {
			Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.8f);
		}
		else {
			Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
		}
		
		Gizmos.DrawIcon(this.transform.position, "CriWare/film.png");
		Gizmos.DrawLine(this.transform.position, new Vector3(0, 0, 0));
	}
	#endregion

	#region Private Functions
	void SetupCallOnce()
	{
		if (setupOnceFlag) {
			return;
		}
		
		if (movieMaterial == null) {
			if (GetComponent<Renderer>() != null) {
				originalMaterial = GetComponent<Renderer>().sharedMaterial;
			}
			Shader shader = Shader.Find("CriMana/ForwardRgb");
			if (shader == null) {
				Debug.LogError(
					"Can't find CriMana Sharder. " +
					"Probably, the link from a material to shader has broken. " +
					"Please reimport 'CRIWARE SDK for Unity'.");
				shader = Shader.Find("Diffuse");
			}
			createdMaterial = new UnityEngine.Material(shader);
			movieMaterial   = createdMaterial;
		}
		
		if (setFileDelegate == null) {
			setFileDelegate = () =>
			{
				string usmPath = 
					CriWare.IsStreamingAssetsPath(moviePath) 
					? Path.Combine(CriWare.streamingAssetsPath, moviePath)
					: moviePath;
				CriManaPlugin.criManaUnityPlayer_SetFile(playerId, IntPtr.Zero, usmPath);
			};
		}
		
		frameUpdated = false;
		
		// プロパティの適用
		loop			= _loop;
		volume			= _volume;
		subAudioVolume	= _subAudioVolume;
		playRequire		= _playOnStart;
		
		setupOnceFlag	= true;
	}
	
	private IEnumerator CreateTextureByCoroutine(CriManaPlayerDetail.TextureHolder texture_holder) {
		yield return StartCoroutine(texture_holder.CreateTexture());
		this.texture_holder = texture_holder;
	}
	
	private void GetMovieInfoAndCreateTextureIfNotYet() {
		if (!gotMovieInfo) {
			CriManaPlugin.criManaUnityPlayer_GetMovieInfo(playerId, out _movieInfo);
			movieShader = shaderDispatchFunction((movieInfo.has_alpha == 1), additiveMode);
			CreateTexture((int)movieInfo.disp_width, (int)movieInfo.disp_height, (movieInfo.has_alpha == 1), true);
			gotMovieInfo = true;
		}
	}
	
	private void UpdatePlayer()
	{
		frameUpdated = false;

		if (this.playerId == InvalidPlayerId) {
			return;
		}
		
		CriManaPlugin.criManaUnityPlayer_Update(playerId);
		Status nativeStatus = (Status)CriManaPlugin.criManaUnityPlayer_GetStatus(playerId);
		switch (nativeStatus) {
		case Status.PlayEnd:
			if (numberOfEntries > 0) {
				CriManaPlugin.criManaUnityPlayer_ClearEntry(playerId);
			}
			goto case Status.Stop;
		case Status.Stop:
			if (playRequire || prepareRequire) {
				setFileDelegate();
				if (isSeeking) {
					CriManaPlugin.criManaUnityPlayer_SetSeekPosition(playerId, seekFrameNumber);
				}
				CriManaPlugin.criManaUnityPlayer_Prepare(playerId);
				CriManaPlugin.criManaUnityPlayer_Update(playerId);
				isSeeking      = false;
				stopRequire    = false;
				gotMovieInfo   = false;
				frameUpdated   = false;
				gotFirstFrame  = false;
				prepareRequire = false;
			}
			break;
		case Status.Prep:
			if (!stopRequire) {
				GetMovieInfoAndCreateTextureIfNotYet();
			}
			break;
		case Status.Ready:
			if (!stopRequire) {
				GetMovieInfoAndCreateTextureIfNotYet();
				if ((texture_holder != null) && playRequire) {
					CriManaPlugin.criManaUnityPlayer_Start(playerId);
					CriManaPlugin.criManaUnityPlayer_Update(playerId);
					playRequire = false;
					UpdateFrame();
				}
			}
			break;
		case Status.Playing:
			if (!stopRequire) {
				UpdateFrame();
			}
			break;
		default:
			break;
		}
	}
	
	private void UpdateFrame()
	{
		if (gotFirstFrame) {
			frameUpdated = texture_holder.UpdateTexture(movieMaterial, playerId, out _frameInfo);
		}
		else {
			Shader originalMovieMaterialShader = movieMaterial.shader;
			movieMaterial.shader = movieShader;
			frameUpdated         = texture_holder.UpdateTexture(movieMaterial, playerId, out _frameInfo);
			if (frameUpdated) {
				if ((originalMaterial != null) && (GetComponent<Renderer>() != null)) {
					GetComponent<Renderer>().material = movieMaterial;
				}
				SetTextureConfigIfTextureHolderIsNotNull();
				movieShader   = null;
				gotFirstFrame = true;
			}
			else {
				/* 最初のフレームが取得できなければシェーダを戻す */
				movieMaterial.shader = originalMovieMaterialShader;
			}
		}
	}
	
	private void SetTextureConfigIfTextureHolderIsNotNull() {
		if (texture_holder != null) {
			texture_holder.SetTextureConfig(movieMaterial, (int)movieInfo.disp_width, (int)movieInfo.disp_height, flipTopBottom, flipLeftRight);
		}
	}
	
#if CRIMANAPLAYER_SUPPORT_CUEPOINT_CALLBACK	
	private void CuePointCallbackFromNative(string eventString)
	{
		if (cuePointUserCbFunc != null) {
			cuePointUserCbFunc(eventString);
		}
	}
#endif
	#endregion
}



/**
 * @}
 */


/* end of file */
