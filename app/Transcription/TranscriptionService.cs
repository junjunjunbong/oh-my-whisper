using System.Runtime.InteropServices;

namespace OhMyWhisper.Transcription;

public class TranscriptionService : IDisposable
{
    private IntPtr _handle = IntPtr.Zero;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public string ModelPath { get; set; } = "models/ggml-base.bin";
    public string Language { get; set; } = "ko";
    public int Threads { get; set; } = 4;

    public bool IsInitialized => _handle != IntPtr.Zero;

    public Task InitializeAsync()
    {
        return Task.Run(() =>
        {
            if (_handle != IntPtr.Zero)
            {
                WhisperBridgeInterop.bridge_free(_handle);
                _handle = IntPtr.Zero;
            }

            _handle = WhisperBridgeInterop.bridge_init(ModelPath, Language, Threads);
            if (_handle == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to load whisper model: {ModelPath}");
        });
    }

    /// <summary>
    /// 전사 수행. skipIfBusy=true이면 이전 추론 중일 때 null 반환 (부분 전사용).
    /// </summary>
    public async Task<string?> TranscribeAsync(float[] samples, bool skipIfBusy = false)
    {
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("TranscriptionService not initialized");

        if (skipIfBusy && _semaphore.CurrentCount == 0)
            return null;

        await _semaphore.WaitAsync();
        try
        {
            return await Task.Run(() =>
            {
                IntPtr resultPtr = WhisperBridgeInterop.bridge_transcribe(_handle, samples, samples.Length);
                if (resultPtr == IntPtr.Zero)
                    return string.Empty;

                string result = Marshal.PtrToStringUTF8(resultPtr) ?? string.Empty;
                WhisperBridgeInterop.bridge_string_free(resultPtr);
                return result;
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void ReconfigureLanguage(string language)
    {
        Language = language;
        if (_handle != IntPtr.Zero)
            WhisperBridgeInterop.bridge_set_language(_handle, language);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_handle != IntPtr.Zero)
        {
            WhisperBridgeInterop.bridge_free(_handle);
            _handle = IntPtr.Zero;
        }
        _semaphore.Dispose();
    }
}
