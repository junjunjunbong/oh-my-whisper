using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace OhMyWhisper.Audio;

public class AudioCaptureService : IDisposable
{
    private WasapiCapture? _capture;
    private WaveFormat? _captureFormat;

    public RingBuffer Buffer { get; } = new();

    private const int TargetSampleRate = 16000;

    public void StartCapture()
    {
        Buffer.Clear();

        _capture = new WasapiCapture();
        _captureFormat = _capture.WaveFormat;
        _capture.DataAvailable += OnDataAvailable;
        _capture.StartRecording();
    }

    public void StopCapture()
    {
        if (_capture != null)
        {
            _capture.StopRecording();
            _capture.DataAvailable -= OnDataAvailable;
            _capture.Dispose();
            _capture = null;
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_captureFormat == null || e.BytesRecorded == 0) return;

        // PCM bytes → float samples (입력 포맷에 맞게)
        var inputSamples = ConvertToFloatMono(e.Buffer, e.BytesRecorded, _captureFormat);

        // 리샘플링: 원본 sample rate → 16kHz
        if (_captureFormat.SampleRate != TargetSampleRate)
        {
            inputSamples = Resample(inputSamples, _captureFormat.SampleRate, TargetSampleRate);
        }

        Buffer.Write(inputSamples, 0, inputSamples.Length);
    }

    private static float[] ConvertToFloatMono(byte[] buffer, int bytesRecorded, WaveFormat format)
    {
        int channels = format.Channels;
        int bitsPerSample = format.BitsPerSample;
        int bytesPerSample = bitsPerSample / 8;
        int totalSamples = bytesRecorded / bytesPerSample;
        int frames = totalSamples / channels;

        var mono = new float[frames];

        if (bitsPerSample == 32 && format.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            for (int i = 0; i < frames; i++)
            {
                float sum = 0;
                for (int ch = 0; ch < channels; ch++)
                {
                    int offset = (i * channels + ch) * 4;
                    sum += BitConverter.ToSingle(buffer, offset);
                }
                mono[i] = sum / channels;
            }
        }
        else if (bitsPerSample == 16)
        {
            for (int i = 0; i < frames; i++)
            {
                float sum = 0;
                for (int ch = 0; ch < channels; ch++)
                {
                    int offset = (i * channels + ch) * 2;
                    short sample = BitConverter.ToInt16(buffer, offset);
                    sum += sample / 32768f;
                }
                mono[i] = sum / channels;
            }
        }
        else if (bitsPerSample == 32 && format.Encoding == WaveFormatEncoding.Pcm)
        {
            for (int i = 0; i < frames; i++)
            {
                float sum = 0;
                for (int ch = 0; ch < channels; ch++)
                {
                    int offset = (i * channels + ch) * 4;
                    int sample = BitConverter.ToInt32(buffer, offset);
                    sum += sample / (float)int.MaxValue;
                }
                mono[i] = sum / channels;
            }
        }

        return mono;
    }

    /// <summary>간단한 선형 보간 리샘플링</summary>
    private static float[] Resample(float[] input, int fromRate, int toRate)
    {
        double ratio = (double)fromRate / toRate;
        int outputLength = (int)(input.Length / ratio);
        var output = new float[outputLength];

        for (int i = 0; i < outputLength; i++)
        {
            double srcIndex = i * ratio;
            int idx = (int)srcIndex;
            double frac = srcIndex - idx;

            if (idx + 1 < input.Length)
                output[i] = (float)(input[idx] * (1 - frac) + input[idx + 1] * frac);
            else if (idx < input.Length)
                output[i] = input[idx];
        }

        return output;
    }

    public void Dispose()
    {
        StopCapture();
    }
}
