using System;
using System.Threading;

namespace CrystalOpenAL;

/// <summary>
/// Trigger slope modes for edge-synchronized oscilloscope display capture.
/// </summary>
public enum TriggerSlope
{
    Auto,
    Rising,
    Falling,
    FreeRun
}

/// <summary>
/// Thread-safe cyclic (ring) buffer designed for audio sample ingestion and oscilloscope capture.
/// Sized to hold at least 1 second of audio samples with lock-free write and snapshot reads.
/// </summary>
public class AudioCyclicBuffer
{
    private readonly double[] _buffer;
    private readonly int _capacity;
    private long _writeIndex; // Monotonically increasing 64-bit write position
    private readonly int _sampleRate;

    public int Capacity => _capacity;
    public int SampleRate => _sampleRate;
    public double DurationSeconds => (double)_capacity / _sampleRate;

    public AudioCyclicBuffer(int sampleRate = 44100, double durationSeconds = 1.0)
    {
        _sampleRate = sampleRate > 0 ? sampleRate : 44100;
        int minSamples = (int)Math.Ceiling(_sampleRate * Math.Max(1.0, durationSeconds));
        // Allocate oversized buffer to nearest power of two for performance
        _capacity = 1;
        while (_capacity < minSamples)
        {
            _capacity <<= 1;
        }
        _buffer = new double[_capacity];
        _writeIndex = 0;
    }

    /// <summary>
    /// Writes a single sample into the cyclic buffer.
    /// </summary>
    public void WriteSample(double sample)
    {
        long index = Interlocked.Increment(ref _writeIndex) - 1;
        int pos = (int)(index & (_capacity - 1));
        _buffer[pos] = sample;
    }

    /// <summary>
    /// Writes an array block of samples into the cyclic buffer.
    /// </summary>
    public void WriteBlock(double[] samples, int offset, int count)
    {
        if (samples == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            long index = Interlocked.Increment(ref _writeIndex) - 1;
            int pos = (int)(index & (_capacity - 1));
            _buffer[pos] = samples[offset + i];
        }
    }

    /// <summary>
    /// Captures the most recent <paramref name="count"/> samples into the destination array.
    /// </summary>
    public int ReadLatest(double[] destination, int count)
    {
        if (destination == null || count <= 0) return 0;
        int samplesToCopy = Math.Min(count, Math.Min(destination.Length, _capacity));

        long currentHead = Interlocked.Read(ref _writeIndex);
        long startPos = currentHead - samplesToCopy;

        for (int i = 0; i < samplesToCopy; i++)
        {
            int bufferIdx = (int)((startPos + i) & (_capacity - 1));
            destination[i] = _buffer[bufferIdx];
        }

        return samplesToCopy;
    }

    /// <summary>
    /// Captures a synchronized window of samples based on edge triggering.
    /// </summary>
    public int ReadTriggeredWindow(
        double[] destination,
        int count,
        double triggerLevel,
        TriggerSlope slope,
        out bool isTriggered)
    {
        isTriggered = false;
        if (destination == null || count <= 0) return 0;

        int samplesNeeded = Math.Min(count, destination.Length);
        long currentHead = Interlocked.Read(ref _writeIndex);

        if (currentHead < samplesNeeded)
        {
            return ReadLatest(destination, count);
        }

        if (slope == TriggerSlope.FreeRun)
        {
            return ReadLatest(destination, count);
        }

        // Search backward from head for a trigger crossing
        // Search window covers up to 1/3 of the buffer or 2x the requested window
        int searchWindow = Math.Min(_capacity - samplesNeeded - 1, Math.Max(samplesNeeded * 2, _sampleRate / 20));
        long searchStart = currentHead - samplesNeeded;
        long foundTriggerHead = -1;

        for (int s = 0; s < searchWindow; s++)
        {
            long currPos = searchStart - s;
            long prevPos = currPos - 1;

            if (prevPos < 0) break;

            double currVal = _buffer[(int)(currPos & (_capacity - 1))];
            double prevVal = _buffer[(int)(prevPos & (_capacity - 1))];

            bool matched = false;
            if (slope == TriggerSlope.Rising || slope == TriggerSlope.Auto)
            {
                matched = prevVal < triggerLevel && currVal >= triggerLevel;
            }
            else if (slope == TriggerSlope.Falling)
            {
                matched = prevVal > triggerLevel && currVal <= triggerLevel;
            }

            if (matched)
            {
                foundTriggerHead = currPos;
                isTriggered = true;
                break;
            }
        }

        if (!isTriggered)
        {
            // Auto trigger fallback: just read latest
            return ReadLatest(destination, count);
        }

        // Copy triggered window
        for (int i = 0; i < samplesNeeded; i++)
        {
            int bufferIdx = (int)((foundTriggerHead + i) & (_capacity - 1));
            destination[i] = _buffer[bufferIdx];
        }

        return samplesNeeded;
    }

    /// <summary>
    /// Computes signal telemetry from the latest samples (Peak-to-Peak, RMS, Estimated Frequency).
    /// </summary>
    public void ComputeTelemetry(int sampleCount, out double vpp, out double vrms, out double estimatedFreq)
    {
        vpp = 0.0;
        vrms = 0.0;
        estimatedFreq = 0.0;

        int count = Math.Min(sampleCount, Math.Min(_capacity, 4096));
        if (count <= 2) return;

        long currentHead = Interlocked.Read(ref _writeIndex);
        long startPos = currentHead - count;

        double min = double.MaxValue;
        double max = double.MinValue;
        double sumSquares = 0.0;
        int zeroCrossings = 0;
        double lastSample = 0.0;

        for (int i = 0; i < count; i++)
        {
            int idx = (int)((startPos + i) & (_capacity - 1));
            double s = _buffer[idx];

            if (s < min) min = s;
            if (s > max) max = s;
            sumSquares += s * s;

            if (i > 0)
            {
                if ((lastSample <= 0.0 && s > 0.0) || (lastSample >= 0.0 && s < 0.0))
                {
                    zeroCrossings++;
                }
            }
            lastSample = s;
        }

        vpp = max > min ? max - min : 0.0;
        vrms = Math.Sqrt(sumSquares / count);

        // Estimate frequency from zero crossings
        double durationSec = (double)count / _sampleRate;
        if (durationSec > 0 && zeroCrossings > 1)
        {
            estimatedFreq = (zeroCrossings / 2.0) / durationSec;
        }
    }
}
