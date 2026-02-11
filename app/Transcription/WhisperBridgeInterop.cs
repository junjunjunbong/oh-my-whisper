using System.Runtime.InteropServices;

namespace OhMyWhisper.Transcription;

/// <summary>
/// whisper_bridge.dll P/Invoke 바인딩.
/// 복잡한 whisper.cpp 구조체를 직접 마샬링하지 않고,
/// 브릿지 DLL의 간단한 API만 사용.
/// </summary>
public static class WhisperBridgeInterop
{
    private const string DllName = "whisper_bridge";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr bridge_init(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string model_path,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string language,
        int n_threads);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr bridge_transcribe(
        IntPtr handle,
        [In] float[] samples,
        int n_samples);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void bridge_set_language(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string language);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void bridge_free(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void bridge_string_free(IntPtr p);
}
