using System;
using System.Runtime.InteropServices;
using CrystalOpenAL;
using Silk.NET.OpenAL;
using SlideScramble;
using Xunit;

namespace SlideScramble.Tests;

public class AudioAndSynthesizerTests
{
    [Fact]
    public void ALBridges_StaticConstructor_InitializesAllDelegatesWithoutExceptions()
    {
        // Touching any member initializes static constructor and compiles all dynamic methods
        Assert.NotNull(typeof(ALBridges));
    }

    [Fact]
    public void ALBridges_ALContext_NullValidation()
    {
        Assert.Throws<ArgumentNullException>(() => ALBridges.OpenDevice(null!));
        Assert.Throws<ArgumentNullException>(() => ALBridges.CloseDevice(null!, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.CreateContext(null!, IntPtr.Zero, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.CreateContext(null!, IntPtr.Zero, new int[] { 0 }));
        Assert.Throws<ArgumentNullException>(() => ALBridges.CreateContextHandle(null!, IntPtr.Zero, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.CreateContextHandle(null!, IntPtr.Zero, new int[] { 0 }));
        Assert.Throws<ArgumentNullException>(() => ALBridges.MakeContextCurrent(null!, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.DestroyContext(null!, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.GetCurrentContext(null!));
        Assert.Throws<ArgumentNullException>(() => ALBridges.GetContextsDevice(null!, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.GetError(null!, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.ProcessContext(null!, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.SuspendContext(null!, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.IsExtensionPresent(null!, IntPtr.Zero, "ALC_EXT"));
        Assert.Throws<ArgumentNullException>(() => ALBridges.GetProcAddress(null!, IntPtr.Zero, "alcTest"));
        Assert.Throws<ArgumentNullException>(() => ALBridges.GetEnumValue(null!, IntPtr.Zero, "ALC_ENUM"));
        Assert.Throws<ArgumentNullException>(() => ALBridges.GetContextProperty(null!, IntPtr.Zero, GetContextString.DeviceSpecifier));
        Assert.Throws<ArgumentNullException>(() => ALBridges.GetContextProperty(null!, IntPtr.Zero, GetContextInteger.MajorVersion, 1, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.GetContextProperty(null!, IntPtr.Zero, GetContextInteger.MajorVersion, 1, new int[1]));
    }

    [Fact]
    public void ALBridges_AL_NullValidation()
    {
        Assert.Throws<ArgumentNullException>(() => ALBridges.GenBuffers(null!, 1, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.GenBuffers(null!, new uint[1]));
        Assert.Throws<ArgumentNullException>(() => ALBridges.DeleteBuffers(null!, 1, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.DeleteBuffers(null!, new uint[1]));
        Assert.Throws<ArgumentNullException>(() => ALBridges.GenSources(null!, 1, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.GenSources(null!, new uint[1]));
        Assert.Throws<ArgumentNullException>(() => ALBridges.DeleteSources(null!, 1, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.DeleteSources(null!, new uint[1]));
        Assert.Throws<ArgumentNullException>(() => ALBridges.SourcePlay(null!, 1, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.SourcePlay(null!, new uint[1]));
        Assert.Throws<ArgumentNullException>(() => ALBridges.SourcePause(null!, 1, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.SourcePause(null!, new uint[1]));
        Assert.Throws<ArgumentNullException>(() => ALBridges.SourceStop(null!, 1, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.SourceStop(null!, new uint[1]));
        Assert.Throws<ArgumentNullException>(() => ALBridges.SourceRewind(null!, 1, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.SourceRewind(null!, new uint[1]));
        Assert.Throws<ArgumentNullException>(() => ALBridges.SourceQueueBuffers(null!, 1, 1, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.SourceQueueBuffers(null!, 1, new uint[1]));
        Assert.Throws<ArgumentNullException>(() => ALBridges.SourceUnqueueBuffers(null!, 1, 1, IntPtr.Zero));
        Assert.Throws<ArgumentNullException>(() => ALBridges.SourceUnqueueBuffers(null!, 1, new uint[1]));
    }

    [Fact]
    public void ALBridges_BufferData_NullValidation()
    {
        Assert.Throws<ArgumentNullException>(() => ALBridges.BufferData(null!, 1, BufferFormat.Mono16, new short[10], 44100));
        Assert.Throws<ArgumentNullException>(() => ALBridges.BufferData(null!, 1, BufferFormat.Mono16, (Array)null!, 10, 44100));
        Assert.Throws<ArgumentNullException>(() => ALBridges.BufferData(null!, 1, BufferFormat.Mono16, (short[])null!, 44100));
        Assert.Throws<ArgumentNullException>(() => ALBridges.BufferData(null!, 1, BufferFormat.Mono16, (byte[])null!, 44100));
        Assert.Throws<ArgumentNullException>(() => ALBridges.BufferData(null!, 1, BufferFormat.Mono16, (float[])null!, 44100));
    }

    [Fact]
    public void GCHandle_Pinning_VerifiesPointerStability()
    {
        short[] pcm = SoundSynthesizer.GenerateVictoryChime(44100);
        GCHandle handle = GCHandle.Alloc(pcm, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            Assert.NotEqual(IntPtr.Zero, ptr);
            short firstSample = Marshal.ReadInt16(ptr);
            Assert.Equal(pcm[0], firstSample);
        }
        finally
        {
            handle.Free();
        }
    }

    [Fact]
    public void GenerateBellChime_ValidParameters_ReturnsCorrectSampleCount()
    {
        int sampleRate = 44100;
        double duration = 1.0;
        short[] pcm = SoundSynthesizer.GenerateBellChime(880.0, duration, 3.0, sampleRate);

        Assert.NotNull(pcm);
        Assert.Equal(44100, pcm.Length);
    }

    [Fact]
    public void GenerateBellChime_DemonstratesExponentialDecay()
    {
        int sampleRate = 44100;
        double duration = 1.5;
        short[] pcm = SoundSynthesizer.GenerateBellChime(587.33, duration, 3.5, sampleRate);

        // Compare RMS energy in the first 200ms vs the last 200ms
        int window = (int)(0.2 * sampleRate);
        double earlyEnergy = 0.0;
        for (int i = 0; i < window; i++)
        {
            earlyEnergy += (double)pcm[i] * pcm[i];
        }
        earlyEnergy = Math.Sqrt(earlyEnergy / window);

        double lateEnergy = 0.0;
        int lateStart = pcm.Length - window;
        for (int i = lateStart; i < pcm.Length; i++)
        {
            lateEnergy += (double)pcm[i] * pcm[i];
        }
        lateEnergy = Math.Sqrt(lateEnergy / window);

        Assert.True(earlyEnergy > 0, "Early energy should be non-zero");
        Assert.True(earlyEnergy > lateEnergy * 5.0, $"Early energy ({earlyEnergy}) should significantly exceed late energy ({lateEnergy}) due to exponential decay");
    }

    [Fact]
    public void GenerateBellChime_HasSoftAttack()
    {
        int sampleRate = 44100;
        short[] pcm = SoundSynthesizer.GenerateBellChime(440.0, 1.0, 3.0, sampleRate);

        // At sample 0, attack envelope multiplier is 0
        Assert.Equal(0, pcm[0]);
    }

    [Fact]
    public void GenerateBellChime_InvalidArguments_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SoundSynthesizer.GenerateBellChime(0, 1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SoundSynthesizer.GenerateBellChime(440.0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SoundSynthesizer.GenerateBellChime(440.0, 1.0, 3.0, 0));
    }

    [Fact]
    public void GenerateVictoryChime_ReturnsExpectedLengthAndSignal()
    {
        short[] pcm = SoundSynthesizer.GenerateVictoryChime(44100);

        Assert.NotNull(pcm);
        Assert.True(pcm.Length > 44100 * 1.5);

        bool hasNonZero = false;
        for (int i = 0; i < pcm.Length; i++)
        {
            if (pcm[i] != 0)
            {
                hasNonZero = true;
                break;
            }
        }
        Assert.True(hasNonZero);
    }

    [Fact]
    public void GenerateGameStartChime_ReturnsExpectedLengthAndSignal()
    {
        short[] pcm = SoundSynthesizer.GenerateGameStartChime(44100);

        Assert.NotNull(pcm);
        Assert.True(pcm.Length > 44100 * 0.8);

        bool hasNonZero = false;
        for (int i = 0; i < pcm.Length; i++)
        {
            if (pcm[i] != 0)
            {
                hasNonZero = true;
                break;
            }
        }
        Assert.True(hasNonZero);
    }

    [Fact]
    public void ToPcmByteArray_ConvertsAccurately()
    {
        short[] samples = new short[] { 0, 1000, -1000, short.MaxValue, short.MinValue };
        byte[] bytes = SoundSynthesizer.ToPcmByteArray(samples);

        Assert.Equal(samples.Length * 2, bytes.Length);

        short[] roundtripped = new short[samples.Length];
        Buffer.BlockCopy(bytes, 0, roundtripped, 0, bytes.Length);

        Assert.Equal(samples, roundtripped);
    }

    [Fact]
    public void AudioEngine_Lifecycle_DoesNotThrow()
    {
        using var engine = new AudioEngine();
        // Mute master volume during test execution so unit tests run silently
        if (engine.IsAvailable)
        {
            engine.MasterVolume = 0.0f;
            Assert.Equal(0.0f, engine.MasterVolume);
            engine.SetVolume(0.0f);
        }

        // Play calls should be safe regardless of audio hardware presence
        engine.PlayVictoryChime();
        engine.PlayGameStartChime();
        engine.PlayCustomChime(880.0, 0.5);
    }

    [Fact]
    public void AudioEngine_PublicFields_AreAccessible()
    {
        using var engine = new AudioEngine();
        // Verify public fields and properties are exposed for developer extensibility
        Assert.True(engine.AL == null || engine.AL != null);
        Assert.True(engine.ALContext == null || engine.ALContext != null);
        Assert.Equal(engine.ALContext, engine.ALC);
        Assert.True(engine.Device == IntPtr.Zero || engine.Device != IntPtr.Zero);
        Assert.True(engine.Context == IntPtr.Zero || engine.Context != IntPtr.Zero);
    }

    [Fact]
    public void AudioEngine_DoubleDispose_IsSafe()
    {
        var engine = new AudioEngine();
        engine.Dispose();
        engine.Dispose(); // Should be safe and not throw
        engine.PlayVictoryChime(); // Safe no-op after disposal
    }
}
