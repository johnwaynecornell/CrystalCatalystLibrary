using System;
using CrystalOpenAL;
using StreamerTrial;
using Xunit;

namespace StreamerTrial.Tests;

public class OscilloscopeTests
{
    [Fact]
    public void OscilloscopeModel_GeneratesAudioAcrossAllWaveforms()
    {
        var model = new OscilloscopeModel();
        var buffer = new AudioCyclicBuffer(44100, 1.0);
        double[] sampleBuffer = new double[512];

        foreach (WaveformType wave in Enum.GetValues<WaveformType>())
        {
            model.Waveform = wave;
            model.Frequency = 440.0f;
            model.Volume = 0.8f;
            model.IsMuted = false;

            int generated = model.GenerateAudioBlock(sampleBuffer, 0, 512, 44100, buffer);
            Assert.Equal(512, generated);

            // Compute peak amplitude
            double maxAmp = 0.0;
            for (int i = 0; i < 512; i++)
            {
                maxAmp = Math.Max(maxAmp, Math.Abs(sampleBuffer[i]));
            }
            Assert.True(maxAmp > 0.1, $"Waveform {wave} failed to generate expected amplitude, max was {maxAmp}");
        }
    }

    [Fact]
    public void OscilloscopeModel_RespectsMute()
    {
        var model = new OscilloscopeModel();
        var buffer = new AudioCyclicBuffer(44100, 1.0);
        double[] sampleBuffer = new double[256];

        model.IsMuted = true;
        int generated = model.GenerateAudioBlock(sampleBuffer, 0, 256, 44100, buffer);

        Assert.Equal(256, generated);
        for (int i = 0; i < 256; i++)
        {
            Assert.Equal(0.0, sampleBuffer[i]);
        }
    }

    [Fact]
    public void OscilloscopeModel_InitializesWithSensibleDefaults()
    {
        var model = new OscilloscopeModel();

        Assert.Equal(WaveformType.Sine, model.Waveform);
        Assert.Equal(440.0f, model.Frequency);
        Assert.Equal(0.70f, model.Volume);
        Assert.False(model.IsMuted);
        Assert.Equal(5.0f, model.TimebaseMs);
        Assert.Equal(1.0f, model.VerticalGain);
        Assert.Equal(0.0f, model.TriggerLevel);
        Assert.Equal(TriggerSlope.Rising, model.TriggerMode);
        Assert.False(model.IsFrozen);
        Assert.True(model.PhosphorGlow);
    }

    [Fact]
    public void OscilloscopeModel_VolumeZeroGeneratesSilence()
    {
        var model = new OscilloscopeModel();
        var buffer = new AudioCyclicBuffer(44100, 1.0);
        double[] sampleBuffer = new double[256];

        model.Volume = 0.0f;
        int generated = model.GenerateAudioBlock(sampleBuffer, 0, 256, 44100, buffer);

        Assert.Equal(256, generated);
        for (int i = 0; i < 256; i++)
        {
            Assert.Equal(0.0, sampleBuffer[i]);
        }
    }
    [Fact]
    public void OscilloscopeModel_AudioInputModeOutputsSilence()
    {
        var model = new OscilloscopeModel
        {
            SourceMode = SignalSourceMode.AudioInput,
            Volume = 1.0f,
            Frequency = 440.0f
        };
        var buffer = new AudioCyclicBuffer(44100, 1.0);
        double[] sampleBuffer = new double[256];
        // Populate sample buffer with dummy values
        for (int i = 0; i < sampleBuffer.Length; i++) sampleBuffer[i] = 1.0;

        int generated = model.GenerateAudioBlock(sampleBuffer, 0, 256, 44100, buffer);

        Assert.Equal(256, generated);
        for (int i = 0; i < 256; i++)
        {
            Assert.Equal(0.0, sampleBuffer[i]);
        }
    }

    [Fact]
    public void OscilloscopeModel_DefaultsToGeneratorMode()
    {
        var model = new OscilloscopeModel();
        Assert.Equal(SignalSourceMode.Generator, model.SourceMode);
    }

    [Fact]
    public void AudioCapture_InitializesAndDisposesSafely()
    {
        using var capture = new AudioCapture(44100, 4096);
        Assert.Equal(44100, capture.SampleRate);
        Assert.False(capture.IsCapturing);

        var cyclicBuffer = new AudioCyclicBuffer(44100, 1.0);
        if (capture.IsAvailable)
        {
            bool started = capture.Start(cyclicBuffer);
            Assert.True(started);
            Assert.True(capture.IsCapturing);

            System.Threading.Thread.Sleep(50);

            capture.Stop();
            Assert.False(capture.IsCapturing);
        }
        else
        {
            // Graceful handling when no capture device hardware is present in testing environment
            Assert.False(capture.IsCapturing);
        }
    }
}
