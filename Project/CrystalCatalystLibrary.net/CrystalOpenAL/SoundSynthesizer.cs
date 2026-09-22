using System;

namespace CrystalOpenAL;

/// <summary>
/// Mathematically generates 16-bit PCM audio samples for bell and chime sound effects in memory.
/// </summary>
public static class SoundSynthesizer
{
    public const int DefaultSampleRate = 44100;

    public struct Overtone
    {
        public double FrequencyRatio;
        public double AmplitudeRatio;
        public double DecayMultiplier;

        public Overtone(double freqRatio, double ampRatio, double decayMult = 1.0)
        {
            FrequencyRatio = freqRatio;
            AmplitudeRatio = ampRatio;
            DecayMultiplier = decayMult;
        }
    }

    /// <summary>
    /// Default harmonic and inharmonic partials characteristic of acoustic chimes and bells.
    /// Combines fundamental with overtone partials (octave, tierce, and high chime partials).
    /// </summary>
    public static readonly Overtone[] DefaultBellOvertones = new[]
    {
        new Overtone(1.00, 1.00, 1.00), // Fundamental
        new Overtone(2.00, 0.55, 1.30), // Octave overtone
        new Overtone(3.01, 0.30, 1.70), // Tierce / 12th
        new Overtone(4.15, 0.18, 2.20), // High strike chime
        new Overtone(5.42, 0.10, 3.00), // Shimmer overtone
    };

    /// <summary>
    /// Generates a single bell chime with an exponential decay envelope and natural overtones.
    /// </summary>
    /// <param name="frequency">Fundamental frequency in Hertz (e.g., 880.0 for A5).</param>
    /// <param name="durationSeconds">Duration of the generated chime in seconds.</param>
    /// <param name="decayRate">Base exponential decay rate (higher values decay faster).</param>
    /// <param name="sampleRate">Sample rate in samples per second (default 44100 Hz).</param>
    /// <param name="overtones">Custom overtone definitions, or null to use default bell harmonics.</param>
    /// <returns>Array of 16-bit signed PCM audio samples (mono).</returns>
    public static short[] GenerateBellChime(
        double frequency,
        double durationSeconds,
        double decayRate = 3.2,
        int sampleRate = DefaultSampleRate,
        Overtone[]? overtones = null)
    {
        if (frequency <= 0) throw new ArgumentOutOfRangeException(nameof(frequency), "Frequency must be positive.");
        if (durationSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Duration must be positive.");
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive.");

        overtones ??= DefaultBellOvertones;
        int totalSamples = (int)(durationSeconds * sampleRate);
        short[] buffer = new short[totalSamples];
        double[] rawSamples = new double[totalSamples];

        double attackSeconds = Math.Min(0.004, durationSeconds * 0.05); // 4ms attack to prevent pop/click
        int attackSamples = Math.Max(1, (int)(attackSeconds * sampleRate));

        double maxPeak = 0.0;

        for (int i = 0; i < totalSamples; i++)
        {
            double t = (double)i / sampleRate;

            // Attack envelope: linear fade-in
            double attack = (i < attackSamples) ? ((double)i / attackSamples) : 1.0;

            double sampleSum = 0.0;
            foreach (var ot in overtones)
            {
                double partialFreq = frequency * ot.FrequencyRatio;
                // Avoid Nyquist aliasing
                if (partialFreq >= sampleRate * 0.49) continue;

                double decay = Math.Exp(-decayRate * ot.DecayMultiplier * t);
                double sine = Math.Sin(2.0 * Math.PI * partialFreq * t);
                sampleSum += ot.AmplitudeRatio * decay * sine;
            }

            double val = attack * sampleSum;
            rawSamples[i] = val;
            double absVal = Math.Abs(val);
            if (absVal > maxPeak)
            {
                maxPeak = absVal;
            }
        }

        // Normalize with 95% full-scale headroom to prevent any digital clipping
        double normFactor = (maxPeak > 1e-6) ? (0.95 / maxPeak) : 1.0;

        for (int i = 0; i < totalSamples; i++)
        {
            double normalized = rawSamples[i] * normFactor;
            int pcmValue = (int)Math.Round(normalized * short.MaxValue);
            buffer[i] = (short)Math.Clamp(pcmValue, short.MinValue, short.MaxValue);
        }

        return buffer;
    }

    /// <summary>
    /// Generates a celebratory multi-note bell chime chord (e.g., major arpeggio) for puzzle victory.
    /// </summary>
    public static short[] GenerateVictoryChime(int sampleRate = DefaultSampleRate)
    {
        // Major chord arpeggio: C6 (1046.50 Hz), E6 (1318.51 Hz), G6 (1567.98 Hz), C7 (2093.00 Hz)
        double[] notes = { 1046.50, 1318.51, 1567.98, 2093.00 };
        double noteSpacing = 0.10; // 100ms stagger between strikes
        double noteDuration = 1.60;
        double totalDuration = (notes.Length - 1) * noteSpacing + noteDuration;

        int totalSamples = (int)(totalDuration * sampleRate);
        double[] mixed = new double[totalSamples];
        double maxPeak = 0.0;

        for (int n = 0; n < notes.Length; n++)
        {
            double freq = notes[n];
            int startSample = (int)(n * noteSpacing * sampleRate);
            short[] notePcm = GenerateBellChime(freq, noteDuration, decayRate: 2.8, sampleRate: sampleRate);

            for (int i = 0; i < notePcm.Length && (startSample + i) < totalSamples; i++)
            {
                int destIndex = startSample + i;
                mixed[destIndex] += notePcm[i] / (double)short.MaxValue;
                double absVal = Math.Abs(mixed[destIndex]);
                if (absVal > maxPeak)
                {
                    maxPeak = absVal;
                }
            }
        }

        short[] finalBuffer = new short[totalSamples];
        double normFactor = (maxPeak > 1e-6) ? (0.95 / maxPeak) : 1.0;

        for (int i = 0; i < totalSamples; i++)
        {
            double normalized = mixed[i] * normFactor;
            int pcmValue = (int)Math.Round(normalized * short.MaxValue);
            finalBuffer[i] = (short)Math.Clamp(pcmValue, short.MinValue, short.MaxValue);
        }

        return finalBuffer;
    }

    /// <summary>
    /// Generates a crisp, bright bell chime for game/scramble start.
    /// </summary>
    public static short[] GenerateGameStartChime(int sampleRate = DefaultSampleRate)
    {
        // Double-bell tap: G5 (783.99 Hz) -> C6 (1046.50 Hz)
        double[] notes = { 783.99, 1046.50 };
        double noteSpacing = 0.08;
        double noteDuration = 0.95;
        double totalDuration = (notes.Length - 1) * noteSpacing + noteDuration;

        int totalSamples = (int)(totalDuration * sampleRate);
        double[] mixed = new double[totalSamples];
        double maxPeak = 0.0;

        for (int n = 0; n < notes.Length; n++)
        {
            double freq = notes[n];
            int startSample = (int)(n * noteSpacing * sampleRate);
            short[] notePcm = GenerateBellChime(freq, noteDuration, decayRate: 3.5, sampleRate: sampleRate);

            for (int i = 0; i < notePcm.Length && (startSample + i) < totalSamples; i++)
            {
                int destIndex = startSample + i;
                mixed[destIndex] += notePcm[i] / (double)short.MaxValue;
                double absVal = Math.Abs(mixed[destIndex]);
                if (absVal > maxPeak)
                {
                    maxPeak = absVal;
                }
            }
        }

        short[] finalBuffer = new short[totalSamples];
        double normFactor = (maxPeak > 1e-6) ? (0.95 / maxPeak) : 1.0;

        for (int i = 0; i < totalSamples; i++)
        {
            double normalized = mixed[i] * normFactor;
            int pcmValue = (int)Math.Round(normalized * short.MaxValue);
            finalBuffer[i] = (short)Math.Clamp(pcmValue, short.MinValue, short.MaxValue);
        }

        return finalBuffer;
    }

    /// <summary>
    /// Converts a 16-bit short PCM array into a raw byte array suitable for OpenAL buffer uploads.
    /// </summary>
    public static byte[] ToPcmByteArray(short[] samples)
    {
        byte[] bytes = new byte[samples.Length * sizeof(short)];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}
