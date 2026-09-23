using System;
using Silk.NET.OpenAL;

namespace CrystalOpenAL;

/// <summary>
/// Cross-platform OpenAL audio engine for UI and game sound effects using Silk.NET.OpenAL and <see cref="ALBridges"/>.
/// Provides managed, pointerless audio initialization and playback with 3D audio sources and in-memory synthesized PCM data.
/// </summary>
public class AudioEngine : IDisposable
{
    public readonly AL? AL;
    public readonly ALContext? ALContext;
    public IntPtr Device;
    public IntPtr Context;

    /// <summary>
    /// OpenAL ALC API alias matching C naming conventions.
    /// </summary>
    public ALContext? ALC => ALContext;

    private uint _victoryBuffer;
    private uint _gameStartBuffer;

    private uint _victorySource;
    private uint _gameStartSource;

    private bool _isInitialized;
    private bool _disposed;

    /// <summary>
    /// Indicates whether the OpenAL hardware/driver device was successfully opened and initialized.
    /// If false, playback operations will safely no-op without raising exceptions.
    /// </summary>
    public bool IsAvailable => _isInitialized && !_disposed;

    /// <summary>
    /// Gets or sets the master volume/gain for all OpenAL playback (0.0 to 1.0+).
    /// </summary>
    public float MasterVolume
    {
        get
        {
            if (!IsAvailable || AL == null) return 0f;
            AL.GetListenerProperty(ListenerFloat.Gain, out float gain);
            return gain;
        }
        set
        {
            if (!IsAvailable || AL == null) return;
            AL.SetListenerProperty(ListenerFloat.Gain, Math.Max(0f, value));
        }
    }

    /// <summary>
    /// Sets the master volume/gain for all OpenAL playback.
    /// </summary>
    public void SetVolume(float volume)
    {
        MasterVolume = volume;
    }

    public AudioEngine()
    {
        try
        {
            ALContext = ALContext.GetApi();
            AL = AL.GetApi();

            Device = ALBridges.OpenDevice(ALContext, null);
            if (Device == IntPtr.Zero)
            {
                return;
            }

            Context = ALBridges.CreateContext(ALContext, Device);
            if (Context == IntPtr.Zero)
            {
                ALBridges.CloseDevice(ALContext, Device);
                Device = IntPtr.Zero;
                return;
            }

            ALBridges.MakeContextCurrent(ALContext, Context);

            // Configure listener properties at origin
            AL.SetListenerProperty(ListenerVector3.Position, 0f, 0f, 0f);
            AL.SetListenerProperty(ListenerVector3.Velocity, 0f, 0f, 0f);
            AL.SetListenerProperty(ListenerFloat.Gain, 1.0f);

            // Generate and upload synthesized chime buffers using GCHandle pinned PCM arrays
            short[] victoryPcm = SoundSynthesizer.GenerateVictoryChime(SoundSynthesizer.DefaultSampleRate);
            _victoryBuffer = CreateBufferFromPcm(victoryPcm, SoundSynthesizer.DefaultSampleRate);

            short[] gameStartPcm = SoundSynthesizer.GenerateGameStartChime(SoundSynthesizer.DefaultSampleRate);
            _gameStartBuffer = CreateBufferFromPcm(gameStartPcm, SoundSynthesizer.DefaultSampleRate);

            // Create and configure 3D sources at the listener's origin
            _victorySource = CreateSourceForBuffer(_victoryBuffer);
            _gameStartSource = CreateSourceForBuffer(_gameStartBuffer);

            _isInitialized = true;
        }
        catch (Exception)
        {
            // Soft-fail: audio subsystem failure should not prevent the application from running
            CleanupResources();
            _isInitialized = false;
        }
    }

    /// <summary>
    /// Uploads 16-bit PCM mono samples into an OpenAL buffer using managed GCHandle pinning via <see cref="ALBridges"/>.
    /// </summary>
    public uint CreateBufferFromPcm(short[] samples, int sampleRate)
    {
        if (AL == null || samples == null || samples.Length == 0) return 0;

        uint buffer = AL.GenBuffer();
        ALBridges.BufferData(AL, buffer, BufferFormat.Mono16, samples, sampleRate);
        return buffer;
    }

    /// <summary>
    /// Uploads arbitrary PCM array data into an OpenAL buffer using managed GCHandle pinning via <see cref="ALBridges"/>.
    /// </summary>
    public uint CreateBufferFromArray(Array data, BufferFormat format, int sizeInBytes, int sampleRate)
    {
        if (AL == null || data == null) return 0;

        uint buffer = AL.GenBuffer();
        ALBridges.BufferData(AL, buffer, format, data, sizeInBytes, sampleRate);
        return buffer;
    }

    /// <summary>
    /// Creates and configures an OpenAL 3D source positioned at the listener's origin.
    /// </summary>
    public uint CreateSourceForBuffer(uint buffer)
    {
        if (AL == null || buffer == 0) return 0;

        uint source = AL.GenSource();
        AL.SetSourceProperty(source, SourceInteger.Buffer, (int)buffer);
        AL.SetSourceProperty(source, SourceVector3.Position, 0f, 0f, 0f);
        AL.SetSourceProperty(source, SourceVector3.Velocity, 0f, 0f, 0f);
        AL.SetSourceProperty(source, SourceFloat.Gain, 0.9f);
        AL.SetSourceProperty(source, SourceFloat.Pitch, 1.0f);
        AL.SetSourceProperty(source, SourceBoolean.Looping, false);
        return source;
    }

    /// <summary>
    /// Plays the synthesized celebratory victory chime bell.
    /// </summary>
    public void PlayVictoryChime()
    {
        if (!IsAvailable || AL == null || _victorySource == 0) return;

        try
        {
            AL.SourceStop(_victorySource);
            AL.SetSourceProperty(_victorySource, SourceInteger.Buffer, (int)_victoryBuffer);
            AL.SourcePlay(_victorySource);
        }
        catch (Exception)
        {
            // Ignored to avoid throwing during UI rendering/events
        }
    }

    /// <summary>
    /// Plays the synthesized game start chime bell.
    /// </summary>
    public void PlayGameStartChime()
    {
        if (!IsAvailable || AL == null || _gameStartSource == 0) return;

        try
        {
            AL.SourceStop(_gameStartSource);
            AL.SetSourceProperty(_gameStartSource, SourceInteger.Buffer, (int)_gameStartBuffer);
            AL.SourcePlay(_gameStartSource);
        }
        catch (Exception)
        {
            // Ignored to avoid throwing during UI rendering/events
        }
    }

    /// <summary>
    /// Creates a new <see cref="AudioStreamer"/> for real-time continuous audio streaming using this engine's OpenAL instance.
    /// </summary>
    /// <param name="bufferCount">Number of ring buffers in queue (default 4).</param>
    /// <param name="bufferSize">Size of each buffer in samples (default 4096).</param>
    /// <param name="sampleRate">Audio sample rate (default 44100 Hz).</param>
    /// <returns>A new <see cref="AudioStreamer"/> instance, or null if audio is unavailable.</returns>
    public AudioStreamer? CreateStreamer(int bufferCount = 4, int bufferSize = 4096, int sampleRate = SoundSynthesizer.DefaultSampleRate)
    {
        if (!IsAvailable || AL == null) return null;
        return new AudioStreamer(AL, sampleRate, bufferCount, bufferSize);
    }

    /// <summary>
    /// Creates and immediately starts an <see cref="AudioStreamer"/> using the provided sample provider delegate.
    /// </summary>
    /// <param name="sampleProvider">Managed sample generator delegate: (buffer, offset, count) => samplesWritten.</param>
    /// <param name="bufferCount">Number of ring buffers in queue (default 4).</param>
    /// <param name="bufferSize">Size of each buffer in samples (default 4096).</param>
    /// <param name="sampleRate">Audio sample rate (default 44100 Hz).</param>
    /// <returns>The running <see cref="AudioStreamer"/> instance, or null if audio is unavailable.</returns>
    public AudioStreamer? StartStream(Func<short[], int, int, int> sampleProvider, int bufferCount = 4, int bufferSize = 4096, int sampleRate = SoundSynthesizer.DefaultSampleRate)
    {
        ArgumentNullException.ThrowIfNull(sampleProvider);
        var streamer = CreateStreamer(bufferCount, bufferSize, sampleRate);
        streamer?.Start(sampleProvider);
        return streamer;
    }

    /// <summary>
    /// Creates and immediately starts an <see cref="AudioStreamer"/> using the provided normalized float sample generator delegate.
    /// </summary>
    /// <param name="floatProvider">Normalized float sample provider delegate: (buffer, offset, count) => samplesWritten.</param>
    /// <param name="bufferCount">Number of ring buffers in queue (default 4).</param>
    /// <param name="bufferSize">Size of each buffer in samples (default 4096).</param>
    /// <param name="sampleRate">Audio sample rate (default 44100 Hz).</param>
    /// <returns>The running <see cref="AudioStreamer"/> instance, or null if audio is unavailable.</returns>
    public AudioStreamer? StartStream(Func<float[], int, int, int> floatProvider, int bufferCount = 4, int bufferSize = 4096, int sampleRate = SoundSynthesizer.DefaultSampleRate)
    {
        ArgumentNullException.ThrowIfNull(floatProvider);
        var streamer = CreateStreamer(bufferCount, bufferSize, sampleRate);
        streamer?.Start(floatProvider);
        return streamer;
    }

    /// <summary>
    /// Creates and immediately starts an <see cref="AudioStreamer"/> using the provided normalized double sample generator delegate.
    /// </summary>
    /// <param name="doubleProvider">Normalized double sample provider delegate: (buffer, offset, count) => samplesWritten.</param>
    /// <param name="bufferCount">Number of ring buffers in queue (default 4).</param>
    /// <param name="bufferSize">Size of each buffer in samples (default 4096).</param>
    /// <param name="sampleRate">Audio sample rate (default 44100 Hz).</param>
    /// <returns>The running <see cref="AudioStreamer"/> instance, or null if audio is unavailable.</returns>
    public AudioStreamer? StartStream(Func<double[], int, int, int> doubleProvider, int bufferCount = 4, int bufferSize = 4096, int sampleRate = SoundSynthesizer.DefaultSampleRate)
    {
        ArgumentNullException.ThrowIfNull(doubleProvider);
        var streamer = CreateStreamer(bufferCount, bufferSize, sampleRate);
        streamer?.Start(doubleProvider);
        return streamer;
    }

    /// <summary>
    /// Creates and immediately starts an <see cref="AudioStreamer"/> using the provided raw PCM byte generator delegate.
    /// </summary>
    /// <param name="byteProvider">Raw PCM byte provider delegate: (buffer, offset, count) => bytesWritten.</param>
    /// <param name="bufferCount">Number of ring buffers in queue (default 4).</param>
    /// <param name="bufferSize">Size of each buffer in samples (default 4096).</param>
    /// <param name="sampleRate">Audio sample rate (default 44100 Hz).</param>
    /// <returns>The running <see cref="AudioStreamer"/> instance, or null if audio is unavailable.</returns>
    public AudioStreamer? StartStream(Func<byte[], int, int, int> byteProvider, int bufferCount = 4, int bufferSize = 4096, int sampleRate = SoundSynthesizer.DefaultSampleRate)
    {
        ArgumentNullException.ThrowIfNull(byteProvider);
        var streamer = CreateStreamer(bufferCount, bufferSize, sampleRate);
        streamer?.Start(byteProvider);
        return streamer;
    }

    /// <summary>
    /// Creates a new <see cref="AudioCapture"/> instance for recording live microphone/line-in audio input.
    /// </summary>
    /// <param name="sampleRate">Audio sample rate (default 44100 Hz).</param>
    /// <param name="bufferSize">Size of the capture ring buffer in samples (default 8192).</param>
    /// <param name="deviceName">Optional specific capture device name, or null for default device.</param>
    /// <returns>A new <see cref="AudioCapture"/> instance, or null if audio capture hardware is unavailable.</returns>
    public AudioCapture? CreateCapture(int sampleRate = SoundSynthesizer.DefaultSampleRate, int bufferSize = 8192, string? deviceName = null)
    {
        try
        {
            var capture = new AudioCapture(sampleRate, bufferSize, deviceName);
            return capture.IsAvailable ? capture : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates and immediately starts an <see cref="AudioCapture"/> instance delivering normalized samples into the specified <see cref="AudioCyclicBuffer"/>.
    /// </summary>
    /// <param name="cyclicBuffer">Target cyclic buffer for captured audio samples.</param>
    /// <param name="sampleRate">Audio sample rate (default 44100 Hz).</param>
    /// <param name="bufferSize">Size of the capture ring buffer in samples (default 8192).</param>
    /// <param name="deviceName">Optional specific capture device name, or null for default device.</param>
    /// <returns>The running <see cref="AudioCapture"/> instance, or null if capture failed to start.</returns>
    public AudioCapture? StartCapture(AudioCyclicBuffer cyclicBuffer, int sampleRate = SoundSynthesizer.DefaultSampleRate, int bufferSize = 8192, string? deviceName = null)
    {
        ArgumentNullException.ThrowIfNull(cyclicBuffer);
        var capture = CreateCapture(sampleRate, bufferSize, deviceName);
        if (capture != null && capture.Start(cyclicBuffer))
        {
            return capture;
        }
        capture?.Dispose();
        return null;
    }

    /// <summary>
    /// Creates and immediately starts an <see cref="AudioCapture"/> instance delivering normalized double samples via callback.
    /// </summary>
    /// <param name="sampleCallback">Callback receiving normalized double samples (-1.0 to 1.0) and sample count.</param>
    /// <param name="sampleRate">Audio sample rate (default 44100 Hz).</param>
    /// <param name="bufferSize">Size of the capture ring buffer in samples (default 8192).</param>
    /// <param name="deviceName">Optional specific capture device name, or null for default device.</param>
    /// <returns>The running <see cref="AudioCapture"/> instance, or null if capture failed to start.</returns>
    public AudioCapture? StartCapture(Action<double[], int> sampleCallback, int sampleRate = SoundSynthesizer.DefaultSampleRate, int bufferSize = 8192, string? deviceName = null)
    {
        ArgumentNullException.ThrowIfNull(sampleCallback);
        var capture = CreateCapture(sampleRate, bufferSize, deviceName);
        if (capture != null && capture.Start(sampleCallback))
        {
            return capture;
        }
        capture?.Dispose();
        return null;
    }

    /// <summary>
    /// Creates and immediately starts an <see cref="AudioCapture"/> instance delivering raw 16-bit PCM samples via callback.
    /// </summary>
    /// <param name="pcmCallback">Callback receiving raw 16-bit PCM samples and sample count.</param>
    /// <param name="sampleRate">Audio sample rate (default 44100 Hz).</param>
    /// <param name="bufferSize">Size of the capture ring buffer in samples (default 8192).</param>
    /// <param name="deviceName">Optional specific capture device name, or null for default device.</param>
    /// <returns>The running <see cref="AudioCapture"/> instance, or null if capture failed to start.</returns>
    public AudioCapture? StartCapture(Action<short[], int> pcmCallback, int sampleRate = SoundSynthesizer.DefaultSampleRate, int bufferSize = 8192, string? deviceName = null)
    {
        ArgumentNullException.ThrowIfNull(pcmCallback);
        var capture = CreateCapture(sampleRate, bufferSize, deviceName);
        if (capture != null && capture.Start(pcmCallback))
        {
            return capture;
        }
        capture?.Dispose();
        return null;
    }

    /// <summary>
    /// Plays a custom synthesized bell chime at a specified fundamental frequency and duration.
    /// </summary>
    public void PlayCustomChime(double frequency, double durationSeconds = 1.2, double decayRate = 3.2)
    {
        if (!IsAvailable || AL == null) return;

        try
        {
            short[] pcm = SoundSynthesizer.GenerateBellChime(frequency, durationSeconds, decayRate);
            uint tempBuffer = CreateBufferFromPcm(pcm, SoundSynthesizer.DefaultSampleRate);
            uint tempSource = CreateSourceForBuffer(tempBuffer);

            AL.SourcePlay(tempSource);
        }
        catch (Exception)
        {
            // Ignored
        }
    }

    private void CleanupResources()
    {
        if (AL != null)
        {
            if (_victorySource != 0)
            {
                AL.SourceStop(_victorySource);
                AL.DeleteSource(_victorySource);
                _victorySource = 0;
            }

            if (_gameStartSource != 0)
            {
                AL.SourceStop(_gameStartSource);
                AL.DeleteSource(_gameStartSource);
                _gameStartSource = 0;
            }

            if (_victoryBuffer != 0)
            {
                AL.DeleteBuffer(_victoryBuffer);
                _victoryBuffer = 0;
            }

            if (_gameStartBuffer != 0)
            {
                AL.DeleteBuffer(_gameStartBuffer);
                _gameStartBuffer = 0;
            }
        }

        if (ALContext != null)
        {
            if (Context != IntPtr.Zero)
            {
                ALBridges.MakeContextCurrent(ALContext, IntPtr.Zero);
                ALBridges.DestroyContext(ALContext, Context);
                Context = IntPtr.Zero;
            }

            if (Device != IntPtr.Zero)
            {
                ALBridges.CloseDevice(ALContext, Device);
                Device = IntPtr.Zero;
            }
        }

        AL?.Dispose();
        ALContext?.Dispose();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            CleanupResources();
            _disposed = true;
            _isInitialized = false;
        }
        GC.SuppressFinalize(this);
    }
}
