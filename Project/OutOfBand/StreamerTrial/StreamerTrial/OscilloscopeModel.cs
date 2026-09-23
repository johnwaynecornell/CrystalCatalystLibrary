using System;
using CrystalOpenAL;

namespace StreamerTrial;

public enum SignalSourceMode
{
    Generator,
    AudioInput
}

public enum WaveformType
{
    Sine,
    Square,
    Triangle,
    Sawtooth,
    ChirpFM
}

/// <summary>
/// State model and signal generator for the Oscilloscope and audio stream.
/// </summary>
public class OscilloscopeModel
{
    // Signal Source Mode
    public SignalSourceMode SourceMode { get; set; } = SignalSourceMode.Generator;

    // Signal Generator Parameters
    public WaveformType Waveform { get; set; } = WaveformType.Sine;
    public float Frequency { get; set; } = 440.0f; // Hz
    public float Volume { get; set; } = 0.70f;
    public bool IsMuted { get; set; } = false;

    // Oscilloscope Display Parameters
    public float TimebaseMs { get; set; } = 5.0f; // ms visible window
    public float VerticalGain { get; set; } = 1.0f; // vertical scaling multiplier
    public float TriggerLevel { get; set; } = 0.0f; // voltage trigger threshold (-1.0 to 1.0)
    public TriggerSlope TriggerMode { get; set; } = TriggerSlope.Rising;
    public bool IsFrozen { get; set; } = false;
    public bool PhosphorGlow { get; set; } = true;
    public bool ShowGridSubdivisions { get; set; } = true;

    // Internal Phase Accumulator for clickless continuous audio synthesis
    private double _phase = 0.0;
    private double _modPhase = 0.0;

    /// <summary>
    /// Generates audio samples into the provided buffer and writes them to the cyclic buffer.
    /// When in AudioInput mode, outputs silence to prevent speaker feedback while external capture feeds the cyclic buffer.
    /// </summary>
    public int GenerateAudioBlock(double[] buffer, int offset, int count, int sampleRate, AudioCyclicBuffer cyclicBuffer)
    {
        if (buffer == null || count <= 0) return 0;

        if (SourceMode == SignalSourceMode.AudioInput)
        {
            // Silent playback during live audio input mode
            Array.Clear(buffer, offset, count);
            return count;
        }

        double sampleDelta = 1.0 / sampleRate;

        for (int i = 0; i < count; i++)
        {
            double sample = 0.0;
            if (!IsMuted && Volume > 0.001f)
            {
                double freq = Math.Max(10.0, (double)Frequency);
                double vol = Math.Clamp((double)Volume, 0.0, 1.0);

                switch (Waveform)
                {
                    case WaveformType.Sine:
                        sample = Math.Sin(2.0 * Math.PI * _phase) * vol;
                        break;

                    case WaveformType.Square:
                        sample = (Math.Sin(2.0 * Math.PI * _phase) >= 0.0 ? 0.9 : -0.9) * vol;
                        break;

                    case WaveformType.Triangle:
                        double normP = _phase - Math.Floor(_phase);
                        sample = (4.0 * Math.Abs(normP - 0.5) - 1.0) * vol;
                        break;

                    case WaveformType.Sawtooth:
                        double sawP = _phase - Math.Floor(_phase);
                        sample = (2.0 * sawP - 1.0) * vol;
                        break;

                    case WaveformType.ChirpFM:
                        // Frequency modulation: carrier + modulator
                        double mod = Math.Sin(2.0 * Math.PI * _modPhase) * (freq * 0.4);
                        sample = Math.Sin(2.0 * Math.PI * _phase + (mod / freq)) * vol;
                        _modPhase += (freq * 0.08) * sampleDelta;
                        if (_modPhase >= 1.0) _modPhase -= Math.Floor(_modPhase);
                        break;
                }

                _phase += freq * sampleDelta;
                if (_phase >= 1.0) _phase -= Math.Floor(_phase);
            }

            buffer[offset + i] = sample;
        }

        // Push newly generated samples to the 1-second cyclic buffer
        cyclicBuffer.WriteBlock(buffer, offset, count);

        return count;
    }
}
