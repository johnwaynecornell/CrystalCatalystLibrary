using System;
using System.Threading;
using Silk.NET.OpenAL;

namespace CrystalOpenAL;

/// <summary>
/// Provides continuous, low-latency streaming audio playback using OpenAL buffer queues
/// via <see cref="ALBridges.SourceQueueBuffers"/> and <see cref="ALBridges.SourceUnqueueBuffers"/>.
/// Dynamically streams audio samples from a managed generator delegate function.
/// </summary>
public class AudioStreamer : IDisposable
{
    private readonly AL _al;
    private readonly uint _source;
    private readonly uint[] _buffers;
    private readonly int _bufferCount;
    private readonly int _bufferSize;
    private readonly int _sampleRate;
    private readonly short[] _transferBuffer;

    private Func<short[], int, int, int>? _sampleProvider;
    private Thread? _streamingThread;
    private readonly object _lock = new();

    private volatile bool _isRunning;
    private volatile bool _isPaused;
    private bool _disposed;

    /// <summary>
    /// Sample rate of the streaming audio in Hertz (e.g. 44100).
    /// </summary>
    public int SampleRate => _sampleRate;

    /// <summary>
    /// Number of ring buffers used in the OpenAL streaming queue.
    /// </summary>
    public int BufferCount => _bufferCount;

    /// <summary>
    /// Size of each buffer in samples.
    /// </summary>
    public int BufferSize => _bufferSize;

    /// <summary>
    /// Indicates whether audio is actively streaming.
    /// </summary>
    public bool IsStreaming => _isRunning && !_isPaused && !_disposed;

    /// <summary>
    /// Indicates whether streaming is currently paused.
    /// </summary>
    public bool IsPaused => _isPaused && !_disposed;

    /// <summary>
    /// OpenAL Source ID handle.
    /// </summary>
    public uint SourceId => _source;

    /// <summary>
    /// Total number of audio samples delivered into the OpenAL queue since streaming started.
    /// </summary>
    public long TotalSamplesStreamed { get; private set; }

    /// <summary>
    /// Total count of buffer underruns detected and recovered.
    /// </summary>
    public int UnderrunCount { get; private set; }

    /// <summary>
    /// Creates a new <see cref="AudioStreamer"/> instance.
    /// </summary>
    /// <param name="al">OpenAL API instance.</param>
    /// <param name="sampleRate">Audio sample rate (default 44100 Hz).</param>
    /// <param name="bufferCount">Number of ring buffers in queue (default 4).</param>
    /// <param name="bufferSize">Size of each buffer in samples (default 4096 = ~92ms latency at 44.1kHz).</param>
    public AudioStreamer(
        AL al,
        int sampleRate = SoundSynthesizer.DefaultSampleRate,
        int bufferCount = 4,
        int bufferSize = 4096)
    {
        ArgumentNullException.ThrowIfNull(al);
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive.");
        _al = al;
        _sampleRate = sampleRate;
        _bufferCount = Math.Max(2, bufferCount);
        _bufferSize = Math.Max(512, bufferSize);
        _transferBuffer = new short[_bufferSize];

        _source = _al.GenSource();
        _al.SetSourceProperty(_source, SourceVector3.Position, 0f, 0f, 0f);
        _al.SetSourceProperty(_source, SourceVector3.Velocity, 0f, 0f, 0f);
        _al.SetSourceProperty(_source, SourceFloat.Gain, 1.0f);
        _al.SetSourceProperty(_source, SourceFloat.Pitch, 1.0f);
        _al.SetSourceProperty(_source, SourceBoolean.Looping, false);

        _buffers = new uint[_bufferCount];
        for (int i = 0; i < _bufferCount; i++)
        {
            _buffers[i] = _al.GenBuffer();
        }
    }


    /// <summary>
    /// Converts a normalized float sample generator (-1.0f to 1.0f) into a 16-bit PCM short provider delegate using a zero-allocation closure.
    /// </summary>
    /// <param name="floatProvider">Normalized float sample provider delegate: (buffer, offset, count) => samplesWritten.</param>
    /// <returns>A 16-bit PCM short provider delegate.</returns>
    public static Func<short[], int, int, int> FromFloat(Func<float[], int, int, int> floatProvider)
    {
        ArgumentNullException.ThrowIfNull(floatProvider);
        float[] scratch = Array.Empty<float>();

        return (shortBuffer, offset, count) =>
        {
            if (scratch.Length < count)
            {
                scratch = new float[count];
            }

            int samplesGenerated = floatProvider(scratch, 0, count);

            for (int i = 0; i < samplesGenerated; i++)
            {
                float sample = scratch[i];
                int pcm = (int)Math.Round(sample * short.MaxValue);
                shortBuffer[offset + i] = (short)Math.Clamp(pcm, short.MinValue, short.MaxValue);
            }

            return samplesGenerated;
        };
    }

    /// <summary>
    /// Converts a normalized double sample generator (-1.0 to 1.0) into a 16-bit PCM short provider delegate using a zero-allocation closure.
    /// </summary>
    /// <param name="doubleProvider">Normalized double sample provider delegate: (buffer, offset, count) => samplesWritten.</param>
    /// <returns>A 16-bit PCM short provider delegate.</returns>
    public static Func<short[], int, int, int> FromDouble(Func<double[], int, int, int> doubleProvider)
    {
        ArgumentNullException.ThrowIfNull(doubleProvider);
        double[] scratch = Array.Empty<double>();

        return (shortBuffer, offset, count) =>
        {
            if (scratch.Length < count)
            {
                scratch = new double[count];
            }

            int samplesGenerated = doubleProvider(scratch, 0, count);

            for (int i = 0; i < samplesGenerated; i++)
            {
                double sample = scratch[i];
                int pcm = (int)Math.Round(sample * short.MaxValue);
                shortBuffer[offset + i] = (short)Math.Clamp(pcm, short.MinValue, short.MaxValue);
            }

            return samplesGenerated;
        };
    }

    /// <summary>
    /// Converts a raw 16-bit PCM byte generator into a 16-bit PCM short provider delegate using a zero-allocation closure.
    /// </summary>
    /// <param name="byteProvider">Raw PCM byte provider delegate: (buffer, offset, count) => bytesWritten.</param>
    /// <returns>A 16-bit PCM short provider delegate.</returns>
    public static Func<short[], int, int, int> FromByte(Func<byte[], int, int, int> byteProvider)
    {
        ArgumentNullException.ThrowIfNull(byteProvider);
        byte[] scratch = Array.Empty<byte>();

        return (shortBuffer, offset, count) =>
        {
            if (scratch.Length < count * 2)
            {
                scratch = new byte[count * 2];
            }

            int bytesRead = byteProvider(scratch, 0, count * 2);
            int samples = bytesRead / 2;

            for (int i = 0; i < samples; i++)
            {
                shortBuffer[offset + i] = unchecked((short)(scratch[i * 2] | (scratch[i * 2 + 1] << 8)));
            }

            return samples;
        };
    }

    /// <summary>
    /// Starts streaming audio using a custom sample generator function: (buffer, offset, count) => samplesWritten.
    /// </summary>
    public void Start(Func<short[], int, int, int> sampleProvider)
    {
        ArgumentNullException.ThrowIfNull(sampleProvider);
        if (_disposed) throw new ObjectDisposedException(nameof(AudioStreamer));

        lock (_lock)
        {
            Stop();

            _sampleProvider = sampleProvider;
            _isRunning = true;
            _isPaused = false;
            TotalSamplesStreamed = 0;
            UnderrunCount = 0;

            // Pre-fill and queue all initial buffers
            for (int i = 0; i < _bufferCount; i++)
            {
                int samples = _sampleProvider(_transferBuffer, 0, _bufferSize);
                TotalSamplesStreamed += samples;

                ALBridges.BufferData(_al, _buffers[i], BufferFormat.Mono16, _transferBuffer, _sampleRate);
                ALBridges.SourceQueueBuffers(_al, _source, _buffers[i]);
            }

            _al.SourcePlay(_source);

            // Launch streaming background worker thread
            _streamingThread = new Thread(StreamingLoop)
            {
                IsBackground = true,
                Name = "CrystalOpenAL_StreamingWorker",
                Priority = ThreadPriority.AboveNormal
            };
            _streamingThread.Start();
        }
    }

    /// <summary>
    /// Starts streaming audio using a normalized float sample generator function: (buffer, offset, count) => samplesWritten.
    /// Uses an internal zero-allocation closure to adapt float samples into 16-bit PCM shorts.
    /// </summary>
    public void Start(Func<float[], int, int, int> floatProvider)
    {
        ArgumentNullException.ThrowIfNull(floatProvider);
        Start(FromFloat(floatProvider));
    }

    /// <summary>
    /// Starts streaming audio using a normalized double sample generator function: (buffer, offset, count) => samplesWritten.
    /// Uses an internal zero-allocation closure to adapt double samples into 16-bit PCM shorts.
    /// </summary>
    public void Start(Func<double[], int, int, int> doubleProvider)
    {
        ArgumentNullException.ThrowIfNull(doubleProvider);
        Start(FromDouble(doubleProvider));
    }

    /// <summary>
    /// Starts streaming audio using a raw 16-bit PCM byte generator function: (buffer, offset, count) => bytesWritten.
    /// Uses an internal zero-allocation closure to adapt byte buffers into 16-bit PCM shorts.
    /// </summary>
    public void Start(Func<byte[], int, int, int> byteProvider)
    {
        ArgumentNullException.ThrowIfNull(byteProvider);
        Start(FromByte(byteProvider));
    }

    /// <summary>
    /// Pauses audio streaming playback.
    /// </summary>
    public void Pause()
    {
        if (_disposed || !_isRunning) return;

        lock (_lock)
        {
            _isPaused = true;
            try
            {
                _al.SourcePause(_source);
            }
            catch (Exception)
            {
                // Soft-fail
            }
        }
    }

    /// <summary>
    /// Resumes audio streaming playback from paused state.
    /// </summary>
    public void Resume()
    {
        if (_disposed || !_isRunning) return;

        lock (_lock)
        {
            _isPaused = false;
            try
            {
                _al.SourcePlay(_source);
            }
            catch (Exception)
            {
                // Soft-fail
            }
        }
    }

    /// <summary>
    /// Stops audio streaming and clears queued buffers.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            _isRunning = false;
            _isPaused = false;
        }

        if (_streamingThread != null && _streamingThread.IsAlive)
        {
            _streamingThread.Join(200);
            _streamingThread = null;
        }

        lock (_lock)
        {
            try
            {
                _al.SourceStop(_source);

                // Unqueue all queued and processed buffers
                _al.GetSourceProperty(_source, GetSourceInteger.BuffersQueued, out int queued);
                while (queued > 0)
                {
                    uint unqueued = ALBridges.SourceUnqueueBuffers(_al, _source);
                    if (unqueued == 0) break;
                    queued--;
                }
            }
            catch (Exception)
            {
                // Soft-fail
            }

            _sampleProvider = null;
        }
    }

    /// <summary>
    /// Sets the playback volume (gain). Range is typically 0.0 to 1.0+.
    /// </summary>
    public void SetVolume(float gain)
    {
        if (_disposed) return;
        try
        {
            _al.SetSourceProperty(_source, SourceFloat.Gain, Math.Max(0f, gain));
        }
        catch (Exception)
        {
            // Soft-fail
        }
    }

    /// <summary>
    /// Sets the playback pitch multiplier. Range is typically 0.5 to 2.0.
    /// </summary>
    public void SetPitch(float pitch)
    {
        if (_disposed) return;
        try
        {
            _al.SetSourceProperty(_source, SourceFloat.Pitch, Math.Clamp(pitch, 0.1f, 4.0f));
        }
        catch (Exception)
        {
            // Soft-fail
        }
    }

    /// <summary>
    /// Queries the number of buffers currently waiting in the OpenAL queue.
    /// </summary>
    public int GetQueuedBufferCount()
    {
        if (_disposed) return 0;
        try
        {
            _al.GetSourceProperty(_source, GetSourceInteger.BuffersQueued, out int queued);
            return queued;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// Queries the number of buffers that have finished playback and are ready to be recycled.
    /// </summary>
    public int GetProcessedBufferCount()
    {
        if (_disposed) return 0;
        try
        {
            _al.GetSourceProperty(_source, GetSourceInteger.BuffersProcessed, out int processed);
            return processed;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// Performs a single streaming pump cycle. Can be called manually or by the background worker.
    /// </summary>
    /// <returns>Number of buffers processed and recycled in this cycle.</returns>
    public int Update()
    {
        if (!_isRunning || _isPaused || _sampleProvider == null || _disposed) return 0;

        lock (_lock)
        {
            if (!_isRunning || _isPaused || _sampleProvider == null || _disposed) return 0;

            int recycled = 0;
            try
            {
                _al.GetSourceProperty(_source, GetSourceInteger.BuffersProcessed, out int processed);

                while (processed > 0)
                {
                    uint buffer = ALBridges.SourceUnqueueBuffers(_al, _source);
                    if (buffer == 0) break;

                    int samples = _sampleProvider(_transferBuffer, 0, _bufferSize);
                    TotalSamplesStreamed += samples;

                    ALBridges.BufferData(_al, buffer, BufferFormat.Mono16, _transferBuffer, _sampleRate);
                    ALBridges.SourceQueueBuffers(_al, _source, buffer);

                    recycled++;
                    processed--;
                }

                // Check for underrun stall
                _al.GetSourceProperty(_source, GetSourceInteger.SourceState, out int state);
                if (state == (int)SourceState.Stopped || state == (int)SourceState.Initial)
                {
                    _al.GetSourceProperty(_source, GetSourceInteger.BuffersQueued, out int queued);
                    if (queued > 0)
                    {
                        UnderrunCount++;
                        _al.SourcePlay(_source);
                    }
                }
            }
            catch (Exception)
            {
                // Soft-fail
            }

            return recycled;
        }
    }

    private void StreamingLoop()
    {
        while (_isRunning && !_disposed)
        {
            if (!_isPaused)
            {
                Update();
            }

            // Sleep ~10ms for smooth buffer pumping with minimal CPU consumption (~0.1%)
            Thread.Sleep(10);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();

            _disposed = true;

            try
            {
                if (_source != 0)
                {
                    _al.DeleteSource(_source);
                }

                for (int i = 0; i < _buffers.Length; i++)
                {
                    if (_buffers[i] != 0)
                    {
                        _al.DeleteBuffer(_buffers[i]);
                        _buffers[i] = 0;
                    }
                }
            }
            catch (Exception)
            {
                // Soft-fail
            }
        }
        GC.SuppressFinalize(this);
    }
}
