/****************************************************************************
 *
 * CRI Middleware SDK
 *
 * Copyright (c) 2011-2014 CRI Middleware Co., Ltd.
 *
 * Library  : CRI Mana
 * Module   : CRI Mana for Unity
 * File     : CriMana.cs
 *
 ****************************************************************************/

/*==========================================================================
 *	  CRI Mana
 *=========================================================================*/
using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;

public class CriManaPlugin
{
	#region Native API Definition (DLL)

	private enum GraphicsApi {
		Unknown = 0,
		OpenGLES_2_0 = 1,
		OpenGLES_3_0 = 2,
		Metal = 3
	};
	
	/* 初期化カウンタ */
	private static int initializationCount = 0;
	
	public static bool isInitialized { get { return initializationCount > 0; } }
	
	public static void SetConfigParameters(int num_decoders, int max_num_of_entries, bool enable_cue_point)
	{
		GraphicsApi graphicsApi = GraphicsApi.Unknown;
#if !UNITY_EDITOR && UNITY_IPHONE
		if (SystemInfo.graphicsDeviceVersion.IndexOf("OpenGL ES 2.") > -1) {
			graphicsApi = GraphicsApi.OpenGLES_2_0;
		} else if (SystemInfo.graphicsDeviceVersion.IndexOf("OpenGL ES 3.") > -1) {
			graphicsApi = GraphicsApi.OpenGLES_3_0;
		} else if (SystemInfo.graphicsDeviceVersion.IndexOf("Metal") > -1) {
			graphicsApi = GraphicsApi.Metal;
		}
#endif
		CriManaPlugin.criManaUnity_SetConfigParameters((int)graphicsApi, num_decoders, max_num_of_entries, enable_cue_point);
	}
	
	public static void InitializeLibrary()
	{
		/* 初期化カウンタの更新 */
		CriManaPlugin.initializationCount++;
		if (CriManaPlugin.initializationCount != 1) {
			return;
		}
		
		/* CriWareInitializerが実行済みかどうかを確認 */
		bool initializerWorking = CriWareInitializer.IsInitialized();
		if (initializerWorking == false) {
			Debug.Log("[CRIWARE] CriWareInitializer is not working. "
				+ "Initializes Mana by default parameters.");
		}
		
		/* 依存ライブラリの初期化 */
		CriFsPlugin.InitializeLibrary();
		CriAtomPlugin.InitializeLibrary();

		/* ライブラリの初期化 */
		CriManaPlugin.criManaUnity_Initialize();
	}
	
	public static void FinalizeLibrary()
	{
		/* 初期化カウンタの更新 */
		CriManaPlugin.initializationCount--;
		if (CriManaPlugin.initializationCount < 0) {
			Debug.LogError("[CRIWARE] ERROR: Mana library is already finalized.");
			return;
		}
		if (CriManaPlugin.initializationCount != 0) {
			return;
		}
		
		/* ライブラリの終了 */
		CriManaPlugin.criManaUnity_Finalize();
		
		/* 依存ライブラリの終了 */
		CriAtomPlugin.FinalizeLibrary();
		CriFsPlugin.FinalizeLibrary();
	}
	
	/**
	 * <summary>キューポイントコールバックのイベントパラメタ区切り文字列を指定します。</summary>
	 * <param name="eventParamsString">イベントパラメタ区切り文字列</param>
	 * \par 説明:
	 * キューポイントコールバック関数に渡される文字列の各情報の区切り文字列を指定します。<br/>
	 * 区切り文字列を明示的に設定しない場合はタブ文字"\t"が使用されます。<br/>
	 * \par 注意:
	 * 区切り文字列は15文字以下である必要があります。
	 * \sa CriManaPlayer::CuePointCbFunc, CriManaPlayer::SetCuePointCallback
	 */
	public static void SetCuePointFormatDelimiter(string delimiter)
	{
		criManaUnity_SetCuePointFormatDelimiter(delimiter);
	}
	
	// CRI Mana Unity
	[DllImport(CriWare.pluginName)]
	private static extern void criManaUnity_SetConfigParameters(int graphics_api, int num_decoders, int num_of_max_entries, bool enable_cue_point);
	[DllImport(CriWare.pluginName)]
	private static extern void criManaUnity_Initialize();
	[DllImport(CriWare.pluginName)]
	private static extern void criManaUnity_Finalize();
	
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnity_ExecuteMain();
	[DllImport(CriWare.pluginName)]
	public static extern uint criManaUnity_GetAllocatedHeapSize();
	
	
	[DllImport(CriWare.pluginName)]
	private static extern void criManaUnity_SetCuePointFormatDelimiter(string delimite_string);

	// CRI Mana Unity Player
	[DllImport(CriWare.pluginName)]
	public static extern int criManaUnityPlayer_Create();
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_Destroy(int player_id);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_SetFile(int player_id, IntPtr binder, string path);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_SetContentId(int player_id, IntPtr binder, int content_id);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_SetFileRange(int player_id, string path, UInt64 offset, Int64 range);
	[DllImport(CriWare.pluginName)]
	public static extern bool criManaUnityPlayer_EntryFile(int player_id, IntPtr binder, string path, bool repeat);
	[DllImport(CriWare.pluginName)]
	public static extern bool criManaUnityPlayer_EntryContentId(int player_id, IntPtr binder, int content_id, bool repeat);
	[DllImport(CriWare.pluginName)]
	public static extern bool criManaUnityPlayer_EntryFileRange(int player_id, string path, UInt64 offset, Int64 range, bool repeat);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_ClearEntry(int player_id);
	[DllImport(CriWare.pluginName)]
	public static extern Int32 criManaUnityPlayer_GetNumberOfEntry(int player_id);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_SetCuePointCallback(int player_id,
		IntPtr cbfunc,
		//[MarshalAs(UnmanagedType.FunctionPtr)]CuePointCbFunc cbfunc,
		string gameobj_name, string func_name);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_GetMovieInfo(int player_id, [Out] out CriManaPlayer.MovieInfo movie_info);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_Update(int player_id);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_Prepare(int player_id);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_Start(int player_id);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_Stop(int player_id);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_SetSeekPosition(int player_id, int seek_frame_no);
	[DllImport(CriWare.pluginName)]
	public static extern bool criManaUnityPlayer_UpdateTexture(int player_id, IntPtr texbuf, [Out] out CriManaPlayer.FrameInfo frame_info);
	[DllImport(CriWare.pluginName)]
	public static extern bool criManaUnityPlayer_UpdateTextureYuvByID(
		int player_id,
		uint texid_y,
		uint texid_u,
		uint texid_v,
		[Out] out CriManaPlayer.FrameInfo frame_info
	);
	[DllImport(CriWare.pluginName)]
	public static extern bool criManaUnityPlayer_UpdateTextureYuvaByID(
		int player_id,
		uint texid_y,
		uint texid_u,
		uint texid_v,
		uint texid_a,
		[Out] out CriManaPlayer.FrameInfo frame_info
	);
	[DllImport(CriWare.pluginName)]
	public static extern bool criManaUnityPlayer_UpdateTextureYuvByPtr(
		int player_id,
		IntPtr texid_y,
		IntPtr texid_u,
		IntPtr texid_v,
		[Out] out CriManaPlayer.FrameInfo frame_info
	);
	[DllImport(CriWare.pluginName)]
	public static extern bool criManaUnityPlayer_UpdateTextureYuvaByPtr(
		int player_id,
		IntPtr texid_y,
		IntPtr texid_u,
		IntPtr texid_v,
		IntPtr texid_a,
		[Out] out CriManaPlayer.FrameInfo frame_info
	);	
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_Pause(int player_id, int sw);
	[DllImport(CriWare.pluginName)]
	public static extern bool criManaUnityPlayer_IsPaused(int player_id);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_Loop(int player_id, int sw);
	[DllImport(CriWare.pluginName)]
	public static extern long criManaUnityPlayer_GetTime(int player_id);
	[DllImport(CriWare.pluginName)]
	public static extern int criManaUnityPlayer_GetStatus(int player_id);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_SetAudioTrack(int player_id, int track);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_SetVolume(int player_id, float vol);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_SetSubAudioTrack(int player_id, int track);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_SetSubAudioVolume(int player_id, float vol);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_SetSpeed(int player_id, float speed);
	[DllImport(CriWare.pluginName)]
	public static extern void criManaUnityPlayer_SetDeviceSendLevel(int player_id, int device_id, float level);
	[DllImport(CriWare.pluginName)]
	public static extern bool criManaUnityPlayer_HasAlpha(int player_id);
	#endregion
}


/* end of file */
