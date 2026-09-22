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
