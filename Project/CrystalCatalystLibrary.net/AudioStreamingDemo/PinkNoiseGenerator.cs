using System;

namespace AudioStreamingDemo;

/// <summary>
/// Mathematically generates pink noise (1/f power spectral density) and related acoustic noise textures.
/// Uses Paul Kellet's refined multi-pole filter network to produce accurate pink noise (-3 dB/octave slope)
/// with low computational overhead and natural acoustic characteristics.
/// </summary>
public class PinkNoiseGenerator
{
    // Filter state variables for Kellet's algorithm
    private double _b0;
    private double _b1;
    private double _b2;
    private double _b3;
    private double _b4;
    private double _b5;
    private double _b6;

    // Filter state for Brownian / red noise (1/f^2)
    private double _brown;

    private readonly Random _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="PinkNoiseGenerator"/> class.
    /// </summary>
    /// <param name="seed">Optional random seed for reproducible noise generation.</param>
    public PinkNoiseGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// Generates the next single pink noise sample normalized in approximately [-1.0, 1.0].
    /// </summary>
    public double NextPinkSample()
    {
        // White noise uniform in [-1.0, 1.0]
        double white = _random.NextDouble() * 2.0 - 1.0;

        // Paul Kellet's 6-pole filter network simulating 1/f spectral roll-off
        _b0 = 0.99886 * _b0 + white * 0.0555179;
        _b1 = 0.99332 * _b1 + white * 0.0750759;
        _b2 = 0.96900 * _b2 + white * 0.1538520;
        _b3 = 0.86650 * _b3 + white * 0.3104856;
        _b4 = 0.55000 * _b4 + white * 0.5329522;
        _b5 = -0.7616 * _b5 - white * 0.0168980;

        double pink = _b0 + _b1 + _b2 + _b3 + _b4 + _b5 + _b6 + white * 0.5362;
        _b6 = white * 0.115926;

        // Scale by 0.11 to normalize nominal peak range to [-1.0, 1.0]
        return pink * 0.11;
    }

    /// <summary>
    /// Generates the next single white noise sample in [-1.0, 1.0].
    /// </summary>
    public double NextWhiteSample()
    {
        return _random.NextDouble() * 2.0 - 1.0;
    }

    /// <summary>
    /// Generates the next single brownian / red noise (1/f^2) sample in [-1.0, 1.0].
    /// </summary>
    public double NextBrownSample()
    {
        double white = _random.NextDouble() * 2.0 - 1.0;
        _brown = (_brown * 0.96) + (white * 0.08);
        return Math.Clamp(_brown, -1.0, 1.0);
    }

    /// <summary>
    /// Fills an array slice with 16-bit PCM pink noise samples.
    /// </summary>
    /// <param name="buffer">Target array of short PCM samples.</param>
    /// <param name="offset">Starting index within the buffer.</param>
    /// <param name="count">Number of samples to generate.</param>
    /// <param name="gain">Gain multiplier (default 1.0).</param>
    public void FillPinkPcm(short[] buffer, int offset, int count, double gain = 1.0)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (offset < 0 || count < 0 || offset + count > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(count), "Offset and count exceed buffer bounds.");

        for (int i = 0; i < count; i++)
        {
            double sample = NextPinkSample() * gain;
            int pcm = (int)Math.Round(sample * short.MaxValue);
            buffer[offset + i] = (short)Math.Clamp(pcm, short.MinValue, short.MaxValue);
        }
    }

    /// <summary>
    /// Generates a standalone in-memory array of 16-bit PCM pink noise.
    /// </summary>
    /// <param name="durationSeconds">Duration of pink noise in seconds.</param>
    /// <param name="sampleRate">Sample rate in Hertz (default 44100).</param>
    /// <param name="gain">Gain factor (default 0.8).</param>
    /// <param name="seed">Optional random seed.</param>
    /// <returns>Array of 16-bit signed PCM audio samples.</returns>
    public static short[] GeneratePinkPcm(double durationSeconds, int sampleRate = 44100, double gain = 0.8, int? seed = null)
    {
        if (durationSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Duration must be positive.");
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive.");

        int totalSamples = (int)(durationSeconds * sampleRate);
        short[] buffer = new short[totalSamples];
        var gen = new PinkNoiseGenerator(seed);
        gen.FillPinkPcm(buffer, 0, totalSamples, gain);
        return buffer;
    }
}
