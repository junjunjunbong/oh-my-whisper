namespace OhMyWhisper.Audio;

/// <summary>
/// Thread-safe 순환 버퍼. 16kHz mono float samples 저장.
/// 기본 용량: 16000 * 20 = 320,000 samples (20초)
/// </summary>
public class RingBuffer
{
    private readonly float[] _buffer;
    private readonly object _lock = new();
    private int _writePos;
    private int _count;

    public int Capacity => _buffer.Length;
    public int Count { get { lock (_lock) return _count; } }

    public RingBuffer(int capacity = 16000 * 20)
    {
        _buffer = new float[capacity];
    }

    public void Write(float[] data, int offset, int count)
    {
        lock (_lock)
        {
            for (int i = 0; i < count; i++)
            {
                _buffer[_writePos] = data[offset + i];
                _writePos = (_writePos + 1) % _buffer.Length;
            }
            _count = Math.Min(_count + count, _buffer.Length);
        }
    }

    /// <summary>최근 n개 샘플을 읽어 반환</summary>
    public float[] ReadLast(int n)
    {
        lock (_lock)
        {
            int toRead = Math.Min(n, _count);
            var result = new float[toRead];
            int start = (_writePos - toRead + _buffer.Length) % _buffer.Length;
            for (int i = 0; i < toRead; i++)
            {
                result[i] = _buffer[(start + i) % _buffer.Length];
            }
            return result;
        }
    }

    /// <summary>버퍼에 있는 모든 샘플을 읽어 반환</summary>
    public float[] ReadAll()
    {
        return ReadLast(Count);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _writePos = 0;
            _count = 0;
        }
    }
}
