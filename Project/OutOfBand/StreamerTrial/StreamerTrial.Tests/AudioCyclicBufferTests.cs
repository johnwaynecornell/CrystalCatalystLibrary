using System;
using CrystalOpenAL;
using StreamerTrial;
using Xunit;

namespace StreamerTrial.Tests;

public class AudioCyclicBufferTests
{
    [Fact]
    public void AudioCyclicBuffer_InitializesWithAtLeastOneSecondCapacity()
    {
        int sampleRate = 44100;
        var buffer = new AudioCyclicBuffer(sampleRate, durationSeconds: 1.0);

        Assert.True(buffer.Capacity >= 44100, $"Expected capacity >= 44100, but got {buffer.Capacity}");
        Assert.True(buffer.DurationSeconds >= 1.0, $"Expected duration >= 1.0s, but got {buffer.DurationSeconds}");
        Assert.Equal(44100, buffer.SampleRate);
    }

    [Fact]
    public void AudioCyclicBuffer_WritesAndReadsLatestCorrectly()
    {
        var buffer = new AudioCyclicBuffer(1000, 1.0); // Capacity will be 1024
        double[] testSamples = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5 };

        buffer.WriteBlock(testSamples, 0, testSamples.Length);

        double[] dest = new double[5];
        int read = buffer.ReadLatest(dest, 5);

        Assert.Equal(5, read);
        Assert.Equal(0.1, dest[0], precision: 4);
        Assert.Equal(0.2, dest[1], precision: 4);
        Assert.Equal(0.3, dest[2], precision: 4);
        Assert.Equal(0.4, dest[3], precision: 4);
        Assert.Equal(0.5, dest[4], precision: 4);
    }

    [Fact]
    public void AudioCyclicBuffer_WrapsAroundSeamlessly()
    {
        var buffer = new AudioCyclicBuffer(100, 1.0); // Capacity 128
        int capacity = buffer.Capacity;

        // Write 3x capacity worth of samples
        for (int i = 0; i < capacity * 3; i++)
        {
            buffer.WriteSample(i);
        }

        double[] dest = new double[10];
        int read = buffer.ReadLatest(dest, 10);

        Assert.Equal(10, read);
        // The last 10 samples written were (capacity*3 - 10) to (capacity*3 - 1)
        for (int i = 0; i < 10; i++)
        {
            double expected = (capacity * 3 - 10) + i;
            Assert.Equal(expected, dest[i]);
        }
    }

    [Fact]
    public void AudioCyclicBuffer_DetectsRisingEdgeTrigger()
    {
        var buffer = new AudioCyclicBuffer(1000, 1.0);
        // Generate a sine wave with known zero-crossings
        int count = 200;
        double[] sine = new double[count];
        for (int i = 0; i < count; i++)
        {
            sine[i] = Math.Sin(2.0 * Math.PI * i / 20.0); // Period = 20 samples
        }

        buffer.WriteBlock(sine, 0, count);

        double[] window = new double[30];
        int read = buffer.ReadTriggeredWindow(window, 30, triggerLevel: 0.0, slope: TriggerSlope.Rising, out bool triggered);

        Assert.True(triggered);
        Assert.Equal(30, read);
        // Triggered window should start at or immediately after rising edge crossing 0.0
        Assert.True(window[0] >= -0.01);
        Assert.True(window[1] > window[0]); // Rising
    }

    [Fact]
    public void AudioCyclicBuffer_DetectsFallingEdgeTrigger()
    {
        var buffer = new AudioCyclicBuffer(1000, 1.0);
        int count = 200;
        double[] sine = new double[count];
        for (int i = 0; i < count; i++)
        {
            sine[i] = Math.Sin(2.0 * Math.PI * i / 20.0);
        }

        buffer.WriteBlock(sine, 0, count);

        double[] window = new double[30];
        int read = buffer.ReadTriggeredWindow(window, 30, triggerLevel: 0.0, slope: TriggerSlope.Falling, out bool triggered);

        Assert.True(triggered);
        Assert.Equal(30, read);
        Assert.True(window[1] < window[0]); // Falling
    }

    [Fact]
    public void AudioCyclicBuffer_CalculatesTelemetryMetricsAccurately()
    {
        var buffer = new AudioCyclicBuffer(44100, 1.0);
        int count = 4410; // 0.1s at 44100 Hz
        double[] sine = new double[count];
        double freq = 440.0;
        double amplitude = 0.75;

        for (int i = 0; i < count; i++)
        {
            sine[i] = amplitude * Math.Sin(2.0 * Math.PI * freq * i / 44100.0);
        }

        buffer.WriteBlock(sine, 0, count);
        buffer.ComputeTelemetry(count, out double vpp, out double vrms, out double estFreq);

        // Vpp should be ~ 2 * 0.75 = 1.50V
        Assert.InRange(vpp, 1.45, 1.55);
        // Vrms of sine is Amp / sqrt(2) = 0.75 / 1.414 = 0.53V
        Assert.InRange(vrms, 0.50, 0.56);
        // Estimated frequency should be near 440 Hz
        Assert.InRange(estFreq, 420.0, 460.0);
    }
}
