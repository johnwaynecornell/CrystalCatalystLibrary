using System;
using System.Threading;
using CrystalOpenAL;
using Xunit;

namespace SlideScramble.Tests;

public class AudioCaptureAndCyclicBufferTests
{
    [Fact]
    public void AudioCyclicBuffer_InitializesWithPowerOfTwoCapacity()
    {
        var buffer = new AudioCyclicBuffer(sampleRate: 44100, durationSeconds: 1.0);
        Assert.Equal(44100, buffer.SampleRate);
        Assert.True(buffer.Capacity >= 44100);
        // Capacity must be a power of two
        Assert.Equal(0, buffer.Capacity & (buffer.Capacity - 1));
        Assert.True(buffer.DurationSeconds >= 1.0);
    }

    [Fact]
    public void AudioCyclicBuffer_WritesAndReadsSampleBlock()
    {
        var buffer = new AudioCyclicBuffer(sampleRate: 44100, durationSeconds: 1.0);
        double[] writeData = [0.1, 0.2, 0.3, 0.4, 0.5];

        buffer.WriteBlock(writeData, 0, writeData.Length);

        double[] readData = new double[5];
        int readCount = buffer.ReadLatest(readData, 5);

        Assert.Equal(5, readCount);
        for (int i = 0; i < writeData.Length; i++)
        {
            Assert.Equal(writeData[i], readData[i], precision: 5);
        }
    }

    [Fact]
    public void AudioCyclicBuffer_HandlesRingBufferWrapAround()
    {
        var buffer = new AudioCyclicBuffer(sampleRate: 1000, durationSeconds: 1.0);
        int totalToWrite = buffer.Capacity * 2 + 100;

        for (int i = 0; i < totalToWrite; i++)
        {
            buffer.WriteSample(i);
        }

        double[] snapshot = new double[100];
        int read = buffer.ReadLatest(snapshot, 100);

        Assert.Equal(100, read);
        for (int i = 0; i < 100; i++)
        {
            double expected = totalToWrite - 100 + i;
            Assert.Equal(expected, snapshot[i]);
        }
    }

    [Fact]
    public void AudioCyclicBuffer_TriggerRisingSlope_DetectsEdgeCrossing()
    {
        var buffer = new AudioCyclicBuffer(sampleRate: 1000, durationSeconds: 1.0);

        // Feed negative values then positive values crossing 0.0
        double[] signal = [-0.5, -0.4, -0.3, -0.1, 0.2, 0.4, 0.5, 0.3, 0.1, -0.2];
        for (int repeat = 0; repeat < 20; repeat++)
        {
            buffer.WriteBlock(signal, 0, signal.Length);
        }

        double[] window = new double[5];
        int read = buffer.ReadTriggeredWindow(window, 5, triggerLevel: 0.0, slope: TriggerSlope.Rising, out bool isTriggered);

        Assert.Equal(5, read);
        Assert.True(isTriggered);
        Assert.True(window[0] >= 0.0, "Triggered window start should be at or above trigger level");
    }

    [Fact]
    public void AudioCyclicBuffer_TriggerFallingSlope_DetectsEdgeCrossing()
    {
        var buffer = new AudioCyclicBuffer(sampleRate: 1000, durationSeconds: 1.0);

        double[] signal = [0.5, 0.4, 0.3, 0.1, -0.2, -0.4, -0.5, -0.3, -0.1, 0.2];
        for (int repeat = 0; repeat < 20; repeat++)
        {
            buffer.WriteBlock(signal, 0, signal.Length);
        }

        double[] window = new double[5];
        int read = buffer.ReadTriggeredWindow(window, 5, triggerLevel: 0.0, slope: TriggerSlope.Falling, out bool isTriggered);

        Assert.Equal(5, read);
        Assert.True(isTriggered);
        Assert.True(window[0] <= 0.0, "Triggered window start should be at or below trigger level");
    }

    [Fact]
    public void AudioCyclicBuffer_ComputeTelemetry_CalculatesVppVrmsFreqCorrectly()
    {
        int sampleRate = 44100;
        var buffer = new AudioCyclicBuffer(sampleRate: sampleRate, durationSeconds: 1.0);

        // Generate a 440 Hz pure sine wave of amplitude 1.0 (Vpp = 2.0, Vrms ~= 0.707)
        int numSamples = 2000;
        double freq = 440.0;
        double[] wave = new double[numSamples];
        for (int i = 0; i < numSamples; i++)
        {
            wave[i] = Math.Sin(2.0 * Math.PI * freq * i / sampleRate);
        }

        buffer.WriteBlock(wave, 0, numSamples);

        buffer.ComputeTelemetry(numSamples, out double vpp, out double vrms, out double estimatedFreq);

        Assert.InRange(vpp, 1.9, 2.05);
        Assert.InRange(vrms, 0.68, 0.73);
        Assert.InRange(estimatedFreq, 420.0, 460.0);
    }

    [Fact]
    public void AudioCapture_InitializesAndDisposesSafely()
    {
        using var capture = new AudioCapture(sampleRate: 44100, bufferSize: 4096);
        Assert.Equal(44100, capture.SampleRate);
        Assert.Equal(4096, capture.CaptureBufferSize);
        Assert.False(capture.IsCapturing);

        var cyclicBuffer = new AudioCyclicBuffer(44100, 1.0);
        if (capture.IsAvailable)
        {
            bool started = capture.Start(cyclicBuffer);
            Assert.True(started);
            Assert.True(capture.IsCapturing);

            Thread.Sleep(50);

            capture.Stop();
            Assert.False(capture.IsCapturing);
        }
        else
        {
            Assert.False(capture.IsCapturing);
        }
    }

    [Fact]
    public void AudioEngine_CaptureFactoryMethods_CreateAndStartSafely()
    {
        using var engine = new AudioEngine();
        var buffer = new AudioCyclicBuffer(44100, 1.0);

        using var capture = engine.CreateCapture(sampleRate: 44100, bufferSize: 4096);
        if (capture != null)
        {
            Assert.True(capture.IsAvailable);
            Assert.Equal(44100, capture.SampleRate);
        }

        var runningCapture = engine.StartCapture(buffer, sampleRate: 44100, bufferSize: 4096);
        if (runningCapture != null)
        {
            Assert.True(runningCapture.IsCapturing);
            runningCapture.Stop();
            runningCapture.Dispose();
        }
    }
}
