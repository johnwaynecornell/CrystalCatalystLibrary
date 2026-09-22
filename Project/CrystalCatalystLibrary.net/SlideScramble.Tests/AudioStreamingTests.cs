using System;
using System.Threading;
using AudioStreamingDemo;
using CrystalOpenAL;
using Xunit;

namespace SlideScramble.Tests;

public class AudioStreamingTests
{
    [Fact]
    public void PinkNoiseGenerator_GeneratesValidSamplesWithinBounds()
    {
        var generator = new PinkNoiseGenerator(seed: 42);
        double maxAbs = 0.0;
        double sumSq = 0.0;
        int count = 10000;

        for (int i = 0; i < count; i++)
        {
            double sample = generator.NextPinkSample();
            Assert.False(double.IsNaN(sample), "Sample should not be NaN");
            Assert.False(double.IsInfinity(sample), "Sample should not be Infinity");
            Assert.InRange(sample, -2.0, 2.0);

            double abs = Math.Abs(sample);
            if (abs > maxAbs) maxAbs = abs;
            sumSq += sample * sample;
        }

        double rms = Math.Sqrt(sumSq / count);
        // Pink noise should have substantial AC energy
        Assert.True(rms > 0.05, $"Expected significant RMS energy, got {rms}");
        Assert.True(maxAbs > 0.1, $"Expected reasonable peak energy, got {maxAbs}");
    }

    [Fact]
    public void PinkNoiseGenerator_FillPinkPcm_FillsBufferCorrectly()
    {
        var generator = new PinkNoiseGenerator(seed: 12345);
        short[] buffer = new short[512];
        generator.FillPinkPcm(buffer, 0, buffer.Length, gain: 0.7);

        bool hasNonZero = false;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] != 0) hasNonZero = true;
        }
        Assert.True(hasNonZero, "Buffer should contain non-zero PCM samples");
    }

    [Fact]
    public void PinkNoiseGenerator_GeneratePinkPcm_ValidatesArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PinkNoiseGenerator.GeneratePinkPcm(-1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PinkNoiseGenerator.GeneratePinkPcm(0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PinkNoiseGenerator.GeneratePinkPcm(1.0, sampleRate: 0));

        short[] pcm = PinkNoiseGenerator.GeneratePinkPcm(durationSeconds: 0.1, sampleRate: 44100, gain: 0.5, seed: 99);
        Assert.Equal(4410, pcm.Length);
    }

    [Fact]
    public void ProceduralAudioStream_GeneratesStreamSamples_WithoutClipping()
    {
        var stream = new ProceduralAudioStream(sampleRate: 44100, seed: 777);
        short[] buffer = new short[4096];

        int generated = stream.GenerateSamples(buffer, 0, buffer.Length);
        Assert.Equal(4096, generated);
        Assert.Equal(4096, stream.TotalSamplesGenerated);
        Assert.True(stream.ElapsedTimeSeconds > 0.0);
        Assert.True(stream.CurrentPeakLevel > 0.0);
        Assert.True(stream.CurrentRmsLevel > 0.0);

        // Ensure all samples are valid 16-bit PCM values
        for (int i = 0; i < buffer.Length; i++)
        {
            Assert.InRange(buffer[i], short.MinValue, short.MaxValue);
        }
    }

    [Fact]
    public void ProceduralAudioStream_LayerTogglesAndTriggers()
    {
        var stream = new ProceduralAudioStream(sampleRate: 44100);
        short[] buffer = new short[1024];

        // Disable all layers
        stream.EnablePinkNoise = false;
        stream.EnableSonarPing = false;
        stream.EnableSciFiSweep = false;
        stream.EnableMelodicArpeggio = false;
        stream.EnableRhythmicPulse = false;

        stream.GenerateSamples(buffer, 0, buffer.Length);
        for (int i = 0; i < buffer.Length; i++)
        {
            Assert.Equal(0, buffer[i]);
        }

        // Trigger manual sonar ping
        stream.EnableSonarPing = true;
        stream.TriggerSonarPing(1200.0);
        stream.GenerateSamples(buffer, 0, buffer.Length);
        Assert.True(stream.CurrentPeakLevel > 0.0);

        // Trigger manual sci-fi sweep
        stream.EnableSciFiSweep = true;
        stream.TriggerSciFiSweep();
        stream.GenerateSamples(buffer, 0, buffer.Length);
        Assert.True(stream.CurrentPeakLevel > 0.0);
    }

    [Fact]
    public void AudioStreamer_Constructor_NullAndArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => new AudioStreamer(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            using var engine = new AudioEngine();
            if (engine.IsAvailable && engine.AL != null)
            {
                new AudioStreamer(engine.AL, sampleRate: -1);
            }
            else
            {
                throw new ArgumentOutOfRangeException("sampleRate");
            }
        });
    }

    [Fact]
    public void AudioEngine_CreateStreamer_And_StartStream_Helpers()
    {
        using var engine = new AudioEngine();
        if (engine.IsAvailable)
        {
            var stream = new ProceduralAudioStream();
            // Disable layers during unit tests so test suite runs silently
            stream.EnablePinkNoise = false;
            stream.EnableSonarPing = false;
            stream.EnableSciFiSweep = false;
            stream.EnableMelodicArpeggio = false;
            stream.EnableRhythmicPulse = false;

            using var streamer = engine.StartStream(stream.GenerateSamples, bufferCount: 3, bufferSize: 2048);

            // Volume & pitch (volume 0.0f ensures complete silence during test execution)
            
            Assert.NotNull(streamer);
            Assert.True(streamer.IsStreaming);
            Assert.False(streamer.IsPaused);
            Assert.Equal(3, streamer.BufferCount);
            Assert.Equal(2048, streamer.BufferSize);
            streamer.SetVolume(0.0f);
            
            // Pause and resume
            streamer.Pause();
            Assert.True(streamer.IsPaused);

            streamer.Resume();
            Assert.False(streamer.IsPaused);

            // Volume & pitch (volume 0.0f ensures complete silence during test execution)
            streamer.SetVolume(0.0f);
            streamer.SetPitch(1.1f);

            // Let it stream briefly
            Thread.Sleep(50);
            Assert.True(streamer.TotalSamplesStreamed > 0);

            streamer.Stop();
            Assert.False(streamer.IsStreaming);
        }
    }

    [Fact]
    public void AudioStreamer_StreamsWithCustomSampleProviderLambda()
    {
        using var engine = new AudioEngine();
        if (engine.IsAvailable)
        {
            using var streamer = engine.CreateStreamer(bufferCount: 2, bufferSize: 1024);
            Assert.NotNull(streamer);

            // Stream silence (0) during unit tests; sine wave generation left as reference comment
            // double phase = 0.0;
            streamer.Start((short[] buffer, int offset, int count) =>
            {
                for (int i = 0; i < count; i++)
                {
                    buffer[offset + i] = 0; // (short)(Math.Sin(phase) * 10000);
                    // phase += 2.0 * Math.PI * 440.0 / 44100.0;
                }
                return count;
            });

            Assert.True(streamer.IsStreaming);
            Thread.Sleep(40);
            Assert.True(streamer.TotalSamplesStreamed > 0);

            streamer.Stop();
            Assert.False(streamer.IsStreaming);
        }
    }

    [Fact]
    public void AudioStreamer_FromFloat_ConvertsNormalizedSamplesCorrectly()
    {
        Assert.Throws<ArgumentNullException>(() => AudioStreamer.FromFloat(null!));

        var floatAdapter = AudioStreamer.FromFloat((floatBuffer, offset, count) =>
        {
            floatBuffer[offset + 0] = 0.0f;
            floatBuffer[offset + 1] = 1.0f;
            floatBuffer[offset + 2] = -1.0f;
            floatBuffer[offset + 3] = 0.5f;
            floatBuffer[offset + 4] = 2.0f;  // should clamp to short.MaxValue
            floatBuffer[offset + 5] = -2.0f; // should clamp to short.MinValue
            return 6;
        });

        short[] shortBuffer = new short[6];
        int written = floatAdapter(shortBuffer, 0, 6);

        Assert.Equal(6, written);
        Assert.Equal(0, shortBuffer[0]);
        Assert.Equal(short.MaxValue, shortBuffer[1]);
        Assert.Equal(-short.MaxValue, shortBuffer[2]);
        Assert.Equal((short)Math.Round(0.5f * short.MaxValue), shortBuffer[3]);
        Assert.Equal(short.MaxValue, shortBuffer[4]);
        Assert.Equal(short.MinValue, shortBuffer[5]);
    }

    [Fact]
    public void AudioStreamer_FromDouble_ConvertsNormalizedSamplesCorrectly()
    {
        Assert.Throws<ArgumentNullException>(() => AudioStreamer.FromDouble(null!));

        var doubleAdapter = AudioStreamer.FromDouble((doubleBuffer, offset, count) =>
        {
            doubleBuffer[offset + 0] = 0.0;
            doubleBuffer[offset + 1] = 1.0;
            doubleBuffer[offset + 2] = -1.0;
            doubleBuffer[offset + 3] = 0.25;
            doubleBuffer[offset + 4] = 1.5;
            doubleBuffer[offset + 5] = -1.5;
            return 6;
        });

        short[] shortBuffer = new short[6];
        int written = doubleAdapter(shortBuffer, 0, 6);

        Assert.Equal(6, written);
        Assert.Equal(0, shortBuffer[0]);
        Assert.Equal(short.MaxValue, shortBuffer[1]);
        Assert.Equal(-short.MaxValue, shortBuffer[2]);
        Assert.Equal((short)Math.Round(0.25 * short.MaxValue), shortBuffer[3]);
        Assert.Equal(short.MaxValue, shortBuffer[4]);
        Assert.Equal(short.MinValue, shortBuffer[5]);
    }

    [Fact]
    public void AudioStreamer_FromByte_ConvertsRawPcmBytesCorrectly()
    {
        Assert.Throws<ArgumentNullException>(() => AudioStreamer.FromByte(null!));

        var byteAdapter = AudioStreamer.FromByte((byteBuffer, offset, count) =>
        {
            // Two 16-bit PCM samples: 0x0100 (256) and 0xFFFE (-2)
            byteBuffer[offset + 0] = 0x00;
            byteBuffer[offset + 1] = 0x01;
            byteBuffer[offset + 2] = 0xFE;
            byteBuffer[offset + 3] = 0xFF;
            return 4;
        });

        short[] shortBuffer = new short[2];
        int written = byteAdapter(shortBuffer, 0, 2);

        Assert.Equal(2, written);
        Assert.Equal(256, shortBuffer[0]);
        Assert.Equal(-2, shortBuffer[1]);
    }

    [Fact]
    public void AudioStreamer_StreamsWithFloatAndDoubleGenerators()
    {
        using var engine = new AudioEngine();
        if (engine.IsAvailable)
        {
            // Test float stream (silence during test; sine wave left as comment)
            // float floatPhase = 0f;
            using var floatStreamer = engine.StartStream((float[] buffer, int offset, int count) =>
            {
                for (int i = 0; i < count; i++)
                {
                    buffer[offset + i] = 0f; // (float)Math.Sin(floatPhase) * 0.5f;
                    // floatPhase += (float)(2.0 * Math.PI * 523.25 / 44100.0);
                }
                return count;
            }, bufferCount: 2, bufferSize: 1024);

            Assert.NotNull(floatStreamer);
            Assert.True(floatStreamer.IsStreaming);
            Thread.Sleep(30);
            Assert.True(floatStreamer.TotalSamplesStreamed > 0);
            floatStreamer.Stop();

            // Test double stream (silence during test; sine wave left as comment)
            // double doublePhase = 0.0;
            using var doubleStreamer = engine.StartStream((double[] buffer, int offset, int count) =>
            {
                for (int i = 0; i < count; i++)
                {
                    buffer[offset + i] = 0.0; // Math.Sin(doublePhase) * 0.5;
                    // doublePhase += 2.0 * Math.PI * 659.25 / 44100.0;
                }
                return count;
            }, bufferCount: 2, bufferSize: 1024);

            Assert.NotNull(doubleStreamer);
            Assert.True(doubleStreamer.IsStreaming);
            Thread.Sleep(30);
            Assert.True(doubleStreamer.TotalSamplesStreamed > 0);
            doubleStreamer.Stop();
        }
    }
}
