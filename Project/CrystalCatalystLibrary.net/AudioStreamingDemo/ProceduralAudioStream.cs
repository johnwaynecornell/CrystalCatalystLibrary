using System;
using CrystalOpenAL;

namespace AudioStreamingDemo;

/// <summary>
/// Real-time procedural audio generator combining a soothing pink noise background bed with
/// distinct, mathematically synthesized sound layers (sonar pings, sci-fi sweeps, melodic arpeggios, and rhythmic pulses).
/// </summary>
public class ProceduralAudioStream
{
    private readonly int _sampleRate;
    private readonly PinkNoiseGenerator _pinkNoise;

    private long _totalSamples;
    private double _timeSeconds;

    // Layer toggle flags
    public bool EnablePinkNoise { get; set; } = true;
    public bool EnableSonarPing { get; set; } = true;
    public bool EnableSciFiSweep { get; set; } = true;
    public bool EnableMelodicArpeggio { get; set; } = true;
    public bool EnableRhythmicPulse { get; set; } = true;

    // Gain properties
    public double MasterGain { get; set; } = 0.8;
    public double PinkNoiseGain { get; set; } = 0.35;
    public double SonarPingGain { get; set; } = 0.55;
    public double SciFiSweepGain { get; set; } = 0.50;
    public double MelodicArpeggioGain { get; set; } = 0.45;
    public double RhythmicPulseGain { get; set; } = 0.40;

    // Intervals & timing (in seconds)
    public double SonarPingInterval { get; set; } = 1.8;
    public double SciFiSweepInterval { get; set; } = 2.6;
    public double ArpeggioStepInterval { get; set; } = 0.25;
    public double PulseInterval { get; set; } = 0.50;

    // Sonar ping state
    private double _lastSonarPingTime = -10.0;
    private double _manualSonarPingTime = -10.0;
    private double _sonarPingFreq = 1200.0;

    // Sci-Fi sweep state
    private double _lastSciFiSweepTime = -10.0;
    private double _manualSciFiSweepTime = -10.0;
    private double _sciFiSweepPhase;

    // Melodic arpeggio state
    private static readonly double[] ArpeggioFrequencies = new[]
    {
        523.25, // C5
        659.25, // E5
        783.99, // G5
        987.77, // B5
        1046.50, // C6
        987.77, // B5
        783.99, // G5
        659.25  // E5
    };
    private int _currentArpeggioStep;
    private double _lastArpeggioStepTime = -10.0;
    private double _currentArpeggioFreq = 523.25;

    // Telemetry & diagnostics
    public double CurrentPeakLevel { get; private set; }
    public double CurrentRmsLevel { get; private set; }
    public long TotalSamplesGenerated => _totalSamples;
    public double ElapsedTimeSeconds => _timeSeconds;
    public int SampleRate => _sampleRate;

    public ProceduralAudioStream(int sampleRate = SoundSynthesizer.DefaultSampleRate, int? seed = null)
    {
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive.");
        _sampleRate = sampleRate;
        _pinkNoise = new PinkNoiseGenerator(seed);
    }

    /// <summary>
    /// Manually triggers an immediate resonant sonar ping.
    /// </summary>
    public void TriggerSonarPing(double frequency = 1200.0)
    {
        _sonarPingFreq = frequency;
        _manualSonarPingTime = _timeSeconds;
    }

    /// <summary>
    /// Manually triggers an immediate sci-fi frequency modulation chirp.
    /// </summary>
    public void TriggerSciFiSweep()
    {
        _manualSciFiSweepTime = _timeSeconds;
        _sciFiSweepPhase = 0.0;
    }

    /// <summary>
    /// Generates the next block of 16-bit PCM audio samples.
    /// </summary>
    /// <param name="buffer">Target PCM sample array.</param>
    /// <param name="offset">Starting array index.</param>
    /// <param name="count">Number of samples to generate.</param>
    /// <returns>Number of samples written.</returns>
    public int GenerateSamples(short[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (offset < 0 || count < 0 || offset + count > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(count), "Offset and count exceed buffer bounds.");

        double peakAcc = 0.0;
        double sumSqAcc = 0.0;

        double dt = 1.0 / _sampleRate;

        for (int i = 0; i < count; i++)
        {
            double mixedSample = 0.0;

            // 1. Pink Noise Bed
            if (EnablePinkNoise)
            {
                mixedSample += _pinkNoise.NextPinkSample() * PinkNoiseGain;
            }

            // 2. Resonant Sonar Ping (1200 Hz with harmonic at 2400 Hz and slow ring-down)
            if (EnableSonarPing)
            {
                if (_timeSeconds - _lastSonarPingTime >= SonarPingInterval)
                {
                    _lastSonarPingTime = _timeSeconds;
                    _sonarPingFreq = 1200.0;
                }

                double pingAge = Math.Min(_timeSeconds - _lastSonarPingTime, _timeSeconds - _manualSonarPingTime);
                if (pingAge >= 0.0 && pingAge < 0.9)
                {
                    double env = Math.Exp(-pingAge * 4.5);
                    double ping = Math.Sin(2.0 * Math.PI * _sonarPingFreq * pingAge)
                                  + 0.35 * Math.Sin(2.0 * Math.PI * (_sonarPingFreq * 2.0) * pingAge);
                    mixedSample += ping * env * SonarPingGain;
                }
            }

            // 3. Sci-Fi Frequency Modulation Laser / Space Chirp
            if (EnableSciFiSweep)
            {
                if (_timeSeconds - _lastSciFiSweepTime >= SciFiSweepInterval)
                {
                    _lastSciFiSweepTime = _timeSeconds;
                }

                double sweepAge = Math.Min(_timeSeconds - _lastSciFiSweepTime, _timeSeconds - _manualSciFiSweepTime);
                if (sweepAge >= 0.0 && sweepAge < 0.35)
                {
                    double tNorm = sweepAge / 0.35;
                    // Exponential downward frequency sweep from 2600 Hz down to 350 Hz
                    double instFreq = 350.0 + 2250.0 * Math.Pow(1.0 - tNorm, 2.5);
                    // Add subtle FM modulation
                    instFreq += 150.0 * Math.Sin(2.0 * Math.PI * 45.0 * sweepAge);

                    _sciFiSweepPhase += 2.0 * Math.PI * instFreq * dt;
                    double env = Math.Sin(Math.PI * tNorm); // Hanning window envelope
                    double chirp = Math.Sin(_sciFiSweepPhase);
                    mixedSample += chirp * env * SciFiSweepGain;
                }
            }

            // 4. Melodic Pentatonic Arpeggio Chimes
            if (EnableMelodicArpeggio)
            {
                if (_timeSeconds - _lastArpeggioStepTime >= ArpeggioStepInterval)
                {
                    _lastArpeggioStepTime = _timeSeconds;
                    _currentArpeggioStep = (_currentArpeggioStep + 1) % ArpeggioFrequencies.Length;
                    _currentArpeggioFreq = ArpeggioFrequencies[_currentArpeggioStep];
                }

                double arpAge = _timeSeconds - _lastArpeggioStepTime;
                if (arpAge >= 0.0 && arpAge < 0.28)
                {
                    double env = Math.Exp(-arpAge * 9.0);
                    // Fundamental + 2nd overtone chime
                    double note = Math.Sin(2.0 * Math.PI * _currentArpeggioFreq * arpAge)
                                  + 0.25 * Math.Sin(2.0 * Math.PI * (_currentArpeggioFreq * 3.0) * arpAge);
                    mixedSample += note * env * MelodicArpeggioGain;
                }
            }

            // 5. Rhythmic Sync Pulse (subtle stereo-like beat pulse)
            if (EnableRhythmicPulse)
            {
                double pulsePhase = (_timeSeconds % PulseInterval) / PulseInterval;
                if (pulsePhase < 0.12)
                {
                    double pAge = pulsePhase * PulseInterval;
                    double env = Math.Sin(Math.PI * (pulsePhase / 0.12));
                    double pulse = Math.Sin(2.0 * Math.PI * 880.0 * pAge)
                                   + 0.5 * Math.Sin(2.0 * Math.PI * 1760.0 * pAge);
                    mixedSample += pulse * env * RhythmicPulseGain;
                }
            }

            // Apply Master Gain
            mixedSample *= MasterGain;

            // Soft-knee saturation / soft limiting to prevent harsh digital clipping
            double limitedSample = Math.Tanh(mixedSample);

            // Telemetry accumulation
            double abs = Math.Abs(limitedSample);
            if (abs > peakAcc) peakAcc = abs;
            sumSqAcc += limitedSample * limitedSample;

            // Convert to 16-bit PCM short
            int pcm = (int)Math.Round(limitedSample * short.MaxValue);
            buffer[offset + i] = (short)Math.Clamp(pcm, short.MinValue, short.MaxValue);

            _timeSeconds += dt;
            _totalSamples++;
        }

        CurrentPeakLevel = peakAcc;
        CurrentRmsLevel = Math.Sqrt(sumSqAcc / count);

        return count;
    }
}
