using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;

public class CriPcmData
{
	public float[]		data;
	public int			numSamples;
	public int			numChannels;
	public int			samplingRate;
	public int			loopStart;
	public int			loopLength;
}

public static class CriHcaDecoder
{
	public static CriPcmData Decode(byte[] data)
	{
		GCHandle igch = GCHandle.Alloc(data, GCHandleType.Pinned);
		IntPtr inPtr = igch.AddrOfPinnedObject();
		
		CriPcmData pcmData = new CriPcmData();
		bool result = criAtomDecHca_GetInfo(inPtr, data.Length, out pcmData.samplingRate, 
			out pcmData.numChannels, out pcmData.numSamples, out pcmData.loopStart, out pcmData.loopLength);
		if (result == false) {
			igch.Free();
			return null;
		}

		pcmData.data = new float[pcmData.numSamples * pcmData.numChannels];
		GCHandle ogch = GCHandle.Alloc(pcmData.data, GCHandleType.Pinned);
		IntPtr outPtr = ogch.AddrOfPinnedObject();
		criAtomDecHca_DecodeFloatInterleaved(inPtr, data.Length, outPtr, pcmData.data.Length * 4);
		
		ogch.Free();
		igch.Free();

		return pcmData;
	}

	public static CriPcmData Decode(string path)
	{
		byte[] data = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, path));
		return CriHcaDecoder.Decode(data);
	}

	public static AudioClip CreateAudioClip(string name, string path)
	{
		CriPcmData pcmData = CriHcaDecoder.Decode(path);
		if (pcmData == null) {
			return null;
		}
#if (UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6)
		AudioClip clip = AudioClip.Create(name, pcmData.numSamples, 
			pcmData.numChannels, pcmData.samplingRate, false, false);
#else
		AudioClip clip = AudioClip.Create(name, pcmData.numSamples, 
		    pcmData.numChannels, pcmData.samplingRate, false);
#endif
		clip.SetData(pcmData.data, 0);
		return clip;
	}

	[DllImport(CriWare.pluginName)]
	private static extern bool criAtomDecHca_GetInfo(IntPtr data, int nbyte,
		out int sampling_rate, out int num_channels, out int num_samples,
		out int loop_start, out int loop_length);

	[DllImport(CriWare.pluginName)]
	private static extern int criAtomDecHca_DecodeShortInterleaved(IntPtr in_data, int inbyte, IntPtr out_buf, int out_nbyte);
	
	[DllImport(CriWare.pluginName)]
	private static extern int criAtomDecHca_DecodeFloatInterleaved(IntPtr in_data, int in_nbyte, IntPtr out_buf, int out_nbyte);
}
