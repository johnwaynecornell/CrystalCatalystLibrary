using System;
using System.Threading;
using CrystalOpenAL;

namespace AudioStreamingDemo;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("================================================================");
        Console.WriteLine("      CrystalCatalystLibrary - OpenAL Streaming Playback Demo   ");
        Console.WriteLine("================================================================");
        Console.WriteLine("Initializing OpenAL Audio Subsystem...");

        using var audioEngine = new AudioEngine();
        if (!audioEngine.IsAvailable)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[Warning] OpenAL audio device/context could not be initialized.");
            Console.WriteLine("Please ensure OpenAL Soft or an audio device driver is available.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[OK] OpenAL subsystem initialized successfully.");
        Console.ResetColor();

        var stream = new ProceduralAudioStream(sampleRate: 44100);
        using var streamer = audioEngine.StartStream(stream.GenerateSamples, bufferCount: 4, bufferSize: 4096);

        if (streamer == null || !streamer.IsStreaming)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[Error] Failed to start OpenAL audio streamer.");
            Console.ResetColor();
            return;
        }

        // Check if run in non-interactive/automated test mode
        bool isAutomatedTest = Array.Exists(args, a => a.Equals("--test", StringComparison.OrdinalIgnoreCase));
        if (isAutomatedTest || Console.IsInputRedirected)
        {
            Console.WriteLine("Running in non-interactive mode for 1.5 seconds...");
            Thread.Sleep(1500);
            streamer.Stop();
            Console.WriteLine("Streaming demo completed successfully.");
            return;
        }

        Console.CursorVisible = false;
        try
        {
            RunInteractiveLoop(stream, streamer);
        }
        finally
        {
            Console.CursorVisible = true;
            streamer.Stop();
            Console.WriteLine("\nAudio streaming stopped. Goodbye!");
        }
    }

    private static void RunInteractiveLoop(ProceduralAudioStream stream, AudioStreamer streamer)
    {
        bool running = true;
        DateTime startTime = DateTime.UtcNow;
        DateTime lastUiUpdate = DateTime.MinValue;

        while (running)
        {
            // Process user input
            while (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                switch (key.Key)
                {
                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1:
                        stream.EnablePinkNoise = !stream.EnablePinkNoise;
                        break;
                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        stream.EnableSonarPing = !stream.EnableSonarPing;
                        break;
                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        stream.EnableSciFiSweep = !stream.EnableSciFiSweep;
                        break;
                    case ConsoleKey.D4:
                    case ConsoleKey.NumPad4:
                        stream.EnableMelodicArpeggio = !stream.EnableMelodicArpeggio;
                        break;
                    case ConsoleKey.D5:
                    case ConsoleKey.NumPad5:
                        stream.EnableRhythmicPulse = !stream.EnableRhythmicPulse;
                        break;

                    case ConsoleKey.P:
                        stream.TriggerSonarPing(1200.0);
                        break;
                    case ConsoleKey.L:
                        stream.TriggerSciFiSweep();
                        break;

                    case ConsoleKey.Spacebar:
                        if (streamer.IsPaused)
                            streamer.Resume();
                        else
                            streamer.Pause();
                        break;

                    case ConsoleKey.OemPlus:
                    case ConsoleKey.Add:
                    case ConsoleKey.UpArrow:
                        stream.MasterGain = Math.Clamp(stream.MasterGain + 0.05, 0.0, 1.0);
                        break;

                    case ConsoleKey.OemMinus:
                    case ConsoleKey.Subtract:
                    case ConsoleKey.DownArrow:
                        stream.MasterGain = Math.Clamp(stream.MasterGain - 0.05, 0.0, 1.0);
                        break;

                    case ConsoleKey.Q:
                    case ConsoleKey.Escape:
                        running = false;
                        break;
                }
            }

            // Refresh UI at ~15 FPS
            if ((DateTime.UtcNow - lastUiUpdate).TotalMilliseconds >= 65)
            {
                lastUiUpdate = DateTime.UtcNow;
                RenderDashboard(stream, streamer, DateTime.UtcNow - startTime);
            }

            Thread.Sleep(15);
        }
    }

    private static void RenderDashboard(ProceduralAudioStream stream, AudioStreamer streamer, TimeSpan elapsed)
    {
        Console.SetCursorPosition(0, 0);

        Console.WriteLine("================================================================");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("    CRYSTALCATALYST - OPENAL STREAMING PLAYBACK & NOISE DEMO   ");
        Console.ResetColor();
        Console.WriteLine("================================================================");

        // State indicator
        Console.Write(" Status: ");
        if (streamer.IsPaused)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[ PAUSED  ]");
        }
        else if (streamer.IsStreaming)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[ PLAYING ]");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[ STOPPED ]");
        }
        Console.ResetColor();

        Console.Write($"  | Elapsed: {elapsed:mm\\:ss\\.f}  | Rate: {streamer.SampleRate} Hz\n");
        Console.WriteLine($" Buffers: {streamer.GetQueuedBufferCount()}/{streamer.BufferCount} queued | Underruns: {streamer.UnderrunCount} | Samples: {streamer.TotalSamplesStreamed:N0}");
        Console.WriteLine("----------------------------------------------------------------");

        // VU Meter
        double peak = stream.CurrentPeakLevel;
        int barLength = 28;
        int filled = (int)Math.Round(Math.Clamp(peak, 0.0, 1.0) * barLength);
        string bar = new string('█', filled) + new string('░', barLength - filled);

        double db = peak > 0.0001 ? 20.0 * Math.Log10(peak) : -60.0;
        Console.Write(" Peak Level : [");
        if (filled > 22) Console.ForegroundColor = ConsoleColor.Red;
        else if (filled > 14) Console.ForegroundColor = ConsoleColor.Yellow;
        else Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(bar);
        Console.ResetColor();
        Console.WriteLine($"] {db,5:F1} dB ({peak * 100,3:F0}%)");

        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine(" Active Audio Layers (Press 1-5 to toggle):                     ");

        PrintLayerToggle("1", "Pink Noise Bed (1/f Kellet Filter)  ", stream.EnablePinkNoise);
        PrintLayerToggle("2", "Resonant Sonar Ping (1200 Hz Chime) ", stream.EnableSonarPing);
        PrintLayerToggle("3", "Sci-Fi FM Laser Sweep (350-2600 Hz) ", stream.EnableSciFiSweep);
        PrintLayerToggle("4", "Melodic Pentatonic Arpeggio Chimes  ", stream.EnableMelodicArpeggio);
        PrintLayerToggle("5", "Rhythmic Sync Pulse (880/1760 Hz)   ", stream.EnableRhythmicPulse);

        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine($" Master Volume  : [{(int)(stream.MasterGain * 100),3}%]  (+/- or Up/Down to adjust)      ");
        Console.WriteLine(" Interactive Commands:                                          ");
        Console.WriteLine("   [P] Trigger Sonar Ping Now      [L] Trigger Sci-Fi Laser Chirp");
        Console.WriteLine("   [Space] Pause / Resume          [Q / Esc] Stop and Exit       ");
        Console.WriteLine("================================================================");
    }

    private static void PrintLayerToggle(string key, string label, bool enabled)
    {
        Console.Write($"  [{key}] {label} : ");
        if (enabled)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[ ENABLED  ]");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("[ DISABLED ]");
        }
        Console.ResetColor();
    }
}
