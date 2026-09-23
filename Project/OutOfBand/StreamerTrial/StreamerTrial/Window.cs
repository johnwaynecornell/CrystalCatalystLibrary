using System;
using System.Collections.Generic;
using CrystalCatalystLibrary.net;
using CrystalOpenAL;
using SkiaSharp;

namespace StreamerTrial;

public class Window
{
    public CrystalWindow wnd;
    public PixData pixelBacking;

    public AudioEngine Audio { get; } = new AudioEngine();
    private AudioStreamer? _streamer;
    private AudioCapture? _capture;
    private readonly AudioCyclicBuffer _cyclicBuffer;
    private readonly OscilloscopeModel _model = new();

    // UI Controls
    private readonly List<SliderControl> _sliders = new();
    private readonly List<ButtonControl> _buttons = new();
    private SliderControl? _activeSlider;
    private float _sourceHeaderY;
    private float _sigGenHeaderY;

    // Oscilloscope snapshot buffer for drawing
    private double[] _renderSampleBuffer = new double[4096];
    private double[] _frozenSampleBuffer = new double[4096];
    private int _frozenSampleCount = 0;

    // Telemetry & FPS measurement
    public int sample_count = 0;
    public int sample_index = 0;
    public double[] sample_deltas = new double[128];
    public double last_time = 0;

    // Cached Skia paints
    private readonly SKPaint bgPaint = new()
    {
        Color = new SKColor(11, 15, 25), // Ultra-dark slate
        Style = SKPaintStyle.Fill,
    };

    private readonly SKPaint scopeBezelPaint = new()
    {
        Color = new SKColor(20, 27, 45),
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private readonly SKPaint scopeBezelBorderPaint = new()
    {
        Color = new SKColor(51, 65, 85),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2,
        IsAntialias = true,
    };

    private readonly SKPaint scopeScreenBgPaint = new()
    {
        Color = new SKColor(6, 11, 20), // CRT deep dark
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private readonly SKPaint gridMajorPaint = new()
    {
        Color = new SKColor(30, 58, 95, 160), // Subdued cyan-blue grid
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.0f,
        IsAntialias = true,
    };

    private readonly SKPaint gridMinorPaint = new()
    {
        Color = new SKColor(30, 58, 95, 70),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 0.5f,
        IsAntialias = true,
    };

    private readonly SKPaint centerAxisPaint = new()
    {
        Color = new SKColor(56, 189, 248, 120), // Sky center axis
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.2f,
        IsAntialias = true,
    };

    private readonly SKPaint triggerLinePaint = new()
    {
        Color = new SKColor(245, 158, 11, 140), // Amber trigger level
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.0f,
        PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0),
        IsAntialias = true,
    };

    private readonly SKPaint triggerMarkerPaint = new()
    {
        Color = new SKColor(245, 158, 11),
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    // Waveform glow and core paints
    private readonly SKPaint waveGlowWidePaint = new()
    {
        Color = new SKColor(34, 197, 94, 45), // Phosphor Green Wide Glow
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 6.0f,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round,
        IsAntialias = true,
    };

    private readonly SKPaint waveGlowMedPaint = new()
    {
        Color = new SKColor(74, 222, 128, 140), // Medium Glow
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 3.2f,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round,
        IsAntialias = true,
    };

    private readonly SKPaint waveCorePaint = new()
    {
        Color = new SKColor(240, 253, 244), // Bright Core
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.6f,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round,
        IsAntialias = true,
    };

    // Panel & HUD paints
    private readonly SKPaint panelBgPaint = new()
    {
        Color = new SKColor(17, 24, 39, 230),
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private readonly SKPaint panelBorderPaint = new()
    {
        Color = new SKColor(55, 65, 81),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.5f,
        IsAntialias = true,
    };

    private readonly SKPaint hudBadgeBgPaint = new()
    {
        Color = new SKColor(15, 23, 42, 200),
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
    };

    private readonly SKPaint hudBadgeBorderPaint = new()
    {
        Color = new SKColor(51, 65, 85),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.0f,
        IsAntialias = true,
    };

    private readonly SKPaint textPrimaryPaint = new()
    {
        Color = new SKColor(241, 245, 249),
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private readonly SKPaint textMutedPaint = new()
    {
        Color = new SKColor(148, 163, 184),
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private readonly SKPaint textAccentPaint = new()
    {
        Color = new SKColor(56, 189, 248),
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private readonly SKPaint textGreenPaint = new()
    {
        Color = new SKColor(34, 197, 94),
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private readonly SKPaint textAmberPaint = new()
    {
        Color = new SKColor(245, 158, 11),
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private readonly SKPaint textPinkPaint = new()
    {
        Color = new SKColor(236, 72, 153),
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    // Fonts
    private readonly SKFont titleFont = new(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default, 17);
    private readonly SKFont sectionFont = new(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default, 13);
    private readonly SKFont bodyFont = new(SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal) ?? SKTypeface.Default, 12);
    private readonly SKFont bodyBoldFont = new(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default, 12);
    private readonly SKFont monoFont = new(SKTypeface.FromFamilyName("Monospace", SKFontStyle.Bold) ?? SKTypeface.Default, 12);
    private readonly SKFont smallMonoFont = new(SKTypeface.FromFamilyName("Monospace", SKFontStyle.Normal) ?? SKTypeface.Default, 10.5f);

    public Window()
    {
        wnd = CrystalWindow.Create(1020, 720, "CrystalCatalyst - Audio Feed Oscilloscope Showcase");
        wnd.ApplicationRetain();

        // 1-second oversized cyclic buffer
        int sampleRate = 44100;
        _cyclicBuffer = new AudioCyclicBuffer(sampleRate, durationSeconds: 1.0);

        // Audio Input Capture device
        _capture = new AudioCapture(sampleRate: sampleRate);

        // Start OpenAL Audio Streamer
        _streamer = Audio.CreateStreamer(bufferCount: 4, bufferSize: 4096, sampleRate: sampleRate);
        if (_streamer != null)
        {
            _streamer.Start((doubles, offset, count) =>
            {
                return _model.GenerateAudioBlock(doubles, offset, count, _streamer.SampleRate, _cyclicBuffer);
            });
        }

        InitializeUI();

        wnd.OnDraw = OnDraw;
        wnd.OnIdle = OnIdle;
        wnd.OnClose = (w) =>
        {
            _capture?.Stop();
            _capture?.Dispose();
            _streamer?.Stop();
            _streamer?.Dispose();
            Audio.Dispose();
            if (pixelBacking)
            {
                pixelBacking.Dispose();
            }
            w.ApplicationRelease();
        };

        wnd.OnMouseDown = OnMouseDown;
        wnd.OnMouseUp = OnMouseUp;
        wnd.OnMouseMove = OnMouseMove;
        wnd.OnKeyDown = OnKeyDown;
    }

    private void InitializeUI()
    {
        // Scope Sliders
        _sliders.Add(new SliderControl(
            "timebase",
            "Timebase / Sweep",
            0.5f,
            20.0f,
            _model.TimebaseMs,
            v => $"{v:F1} ms ({v / 10f:F2} ms/div)",
            v => _model.TimebaseMs = v
        ) { AccentColor = new SKColor(56, 189, 248) });

        _sliders.Add(new SliderControl(
            "gain",
            "Vertical Scale / Gain",
            0.2f,
            4.0f,
            _model.VerticalGain,
            v => $"{v:F2}x ({0.25f / v:F2} V/div)",
            v => _model.VerticalGain = v
        ) { AccentColor = new SKColor(168, 85, 247) }); // Purple

        _sliders.Add(new SliderControl(
            "trigger",
            "Trigger Level",
            -1.0f,
            1.0f,
            _model.TriggerLevel,
            v => $"{v:+0.00;-0.00;0.00} V",
            v => _model.TriggerLevel = v
        ) { AccentColor = new SKColor(245, 158, 11) }); // Amber

        // Signal Generator Sliders
        _sliders.Add(new SliderControl(
            "freq",
            "Oscillator Frequency",
            40.0f,
            2000.0f,
            _model.Frequency,
            v => $"{v:F0} Hz",
            v => _model.Frequency = v
        ) { AccentColor = new SKColor(34, 197, 94) }); // Green

        _sliders.Add(new SliderControl(
            "volume",
            "Output Volume",
            0.0f,
            1.0f,
            _model.Volume,
            v => $"{(int)(v * 100)}%",
            v => _model.Volume = v
        ) { AccentColor = new SKColor(236, 72, 153) }); // Pink

        // Trigger Mode Buttons
        _buttons.Add(new ButtonControl("trig_auto", "Auto", () => _model.TriggerMode = TriggerSlope.Auto) { ActiveColor = new SKColor(56, 189, 248) });
        _buttons.Add(new ButtonControl("trig_rise", "Rising ▲", () => _model.TriggerMode = TriggerSlope.Rising) { ActiveColor = new SKColor(34, 197, 94) });
        _buttons.Add(new ButtonControl("trig_fall", "Falling ▼", () => _model.TriggerMode = TriggerSlope.Falling) { ActiveColor = new SKColor(245, 158, 11) });
        _buttons.Add(new ButtonControl("trig_free", "Free Run", () => _model.TriggerMode = TriggerSlope.FreeRun) { ActiveColor = new SKColor(148, 163, 184) });

        // Waveform Buttons
        _buttons.Add(new ButtonControl("wave_sine", "Sine", () => _model.Waveform = WaveformType.Sine));
        _buttons.Add(new ButtonControl("wave_square", "Square", () => _model.Waveform = WaveformType.Square));
        _buttons.Add(new ButtonControl("wave_tri", "Triangle", () => _model.Waveform = WaveformType.Triangle));
        _buttons.Add(new ButtonControl("wave_saw", "Sawtooth", () => _model.Waveform = WaveformType.Sawtooth));
        _buttons.Add(new ButtonControl("wave_fm", "FM Chirp", () => _model.Waveform = WaveformType.ChirpFM));

        // State Action Buttons
        _buttons.Add(new ButtonControl("btn_freeze", "Freeze (Space)", () => _model.IsFrozen = !_model.IsFrozen) { ActiveColor = new SKColor(239, 68, 68) });
        _buttons.Add(new ButtonControl("btn_mute", "Mute Audio", () => _model.IsMuted = !_model.IsMuted) { ActiveColor = new SKColor(239, 68, 68) });
        _buttons.Add(new ButtonControl("btn_glow", "Phosphor Glow", () => _model.PhosphorGlow = !_model.PhosphorGlow) { ActiveColor = new SKColor(34, 197, 94) });

        // Signal Source Mode Buttons (Generator vs Silent Live Input)
        _buttons.Add(new ButtonControl("src_gen", "Signal Gen", () => SetSourceMode(SignalSourceMode.Generator)) { ActiveColor = new SKColor(56, 189, 248) });
        _buttons.Add(new ButtonControl("src_input", "Live Mic / In", () => SetSourceMode(SignalSourceMode.AudioInput)) { ActiveColor = new SKColor(236, 72, 153) });
    }

    private void SetSourceMode(SignalSourceMode mode)
    {
        _model.SourceMode = mode;
        if (mode == SignalSourceMode.AudioInput)
        {
            _capture?.Start(_cyclicBuffer);
        }
        else
        {
            _capture?.Stop();
        }
    }

    private void UpdateLayout(int width, int height)
    {
        float padding = 16f;
        float rightPanelWidth = 320f;
        float topHeaderHeight = 44f;

        float scopeX = padding;
        float scopeY = topHeaderHeight + 8f;
        float scopeW = Math.Max(200f, width - rightPanelWidth - padding * 3f);
        float scopeH = Math.Max(200f, height - scopeY - padding);

        float panelX = width - rightPanelWidth - padding;
        float panelY = scopeY;
        float panelW = rightPanelWidth;
        float panelH = scopeH;

        // Position sliders & buttons inside right panel
        float curY = panelY + 36f;
        float sliderWidth = panelW - 32f;
        float sliderLeft = panelX + 16f;

        // Scope group sliders
        foreach (var s in _sliders)
        {
            if (s.Id is "timebase" or "gain" or "trigger")
            {
                s.Bounds = new SKRect(sliderLeft, curY, sliderLeft + sliderWidth, curY + 34f);
                curY += 46f;
            }
        }

        // Trigger Mode buttons row
        float btnW = (sliderWidth - 12f) / 4f;
        float trigBtnY = curY + 4f;
        int tIdx = 0;
        foreach (var b in _buttons)
        {
            if (b.Id.StartsWith("trig_"))
            {
                b.Bounds = new SKRect(sliderLeft + tIdx * (btnW + 4f), trigBtnY, sliderLeft + tIdx * (btnW + 4f) + btnW, trigBtnY + 28f);
                tIdx++;
            }
        }
        curY = trigBtnY + 28f + 16f;

        // Signal Source Mode Header & Buttons
        _sourceHeaderY = curY + 12f;
        curY += 24f;

        float srcBtnW = (sliderWidth - 6f) / 2f;
        float srcBtnY = curY + 2f;
        int sIdx = 0;
        foreach (var b in _buttons)
        {
            if (b.Id.StartsWith("src_"))
            {
                b.Bounds = new SKRect(sliderLeft + sIdx * (srcBtnW + 6f), srcBtnY, sliderLeft + sIdx * (srcBtnW + 6f) + srcBtnW, srcBtnY + 28f);
                sIdx++;
            }
        }
        curY = srcBtnY + 28f + 16f;

        // Signal Generator Section Header
        _sigGenHeaderY = curY + 12f;
        curY += 26f;

        // Signal Generator Sliders
        foreach (var s in _sliders)
        {
            if (s.Id is "freq" or "volume")
            {
                s.Bounds = new SKRect(sliderLeft, curY, sliderLeft + sliderWidth, curY + 34f);
                curY += 46f;
            }
        }

        // Waveform Buttons (2 rows)
        float waveBtnW = (sliderWidth - 8f) / 3f;
        float waveBtnY1 = curY + 4f;
        float waveBtnY2 = waveBtnY1 + 32f;

        int wIdx = 0;
        foreach (var b in _buttons)
        {
            if (b.Id.StartsWith("wave_"))
            {
                float rowY = wIdx < 3 ? waveBtnY1 : waveBtnY2;
                float colX = (wIdx % 3) * (waveBtnW + 4f);
                b.Bounds = new SKRect(sliderLeft + colX, rowY, sliderLeft + colX + waveBtnW, rowY + 28f);
                wIdx++;
            }
        }
        curY += 76f;

        // Action Buttons Row
        float actBtnW = (sliderWidth - 8f) / 3f;
        float actBtnY = curY + 8f;
        int aIdx = 0;
        foreach (var b in _buttons)
        {
            if (b.Id is "btn_freeze" or "btn_mute" or "btn_glow")
            {
                b.Bounds = new SKRect(sliderLeft + aIdx * (actBtnW + 4f), actBtnY, sliderLeft + aIdx * (actBtnW + 4f) + actBtnW, actBtnY + 32f);
                aIdx++;
            }
        }
    }

    private void OnMouseMove(CrystalWindow windowHandle, int x, int y)
    {
        if (_activeSlider != null)
        {
            _activeSlider.HandleMouseMove(x, y);
        }
    }

    private void OnMouseUp(CrystalWindow windowHandle, int button, int x, int y)
    {
        if (button == (int)CrystalMouseButton.Left)
        {
            _activeSlider?.HandleMouseUp();
            _activeSlider = null;
        }
    }

    private void OnMouseDown(CrystalWindow windowHandle, int button, int x, int y)
    {
        if (button != (int)CrystalMouseButton.Left) return;

        // Check sliders
        foreach (var slider in _sliders)
        {
            if (slider.HandleMouseDown(x, y))
            {
                _activeSlider = slider;
                return;
            }
        }

        // Check buttons
        foreach (var btn in _buttons)
        {
            if (btn.HandleClick(x, y))
            {
                return;
            }
        }
    }

    private void OnKeyDown(CrystalWindow windowHandle, int keycode)
    {
        if (keycode == (int)KeyCode.Escape)
        {
            return;
        }

        if (keycode == 32) // Space bar
        {
            _model.IsFrozen = !_model.IsFrozen;
            return;
        }

        if (keycode is (int)'M' or (int)'m')
        {
            _model.IsMuted = !_model.IsMuted;
            return;
        }

        if (keycode is (int)'I' or (int)'i')
        {
            SetSourceMode(_model.SourceMode == SignalSourceMode.Generator ? SignalSourceMode.AudioInput : SignalSourceMode.Generator);
            return;
        }

        if (keycode is (int)'1') _model.Waveform = WaveformType.Sine;
        if (keycode is (int)'2') _model.Waveform = WaveformType.Square;
        if (keycode is (int)'3') _model.Waveform = WaveformType.Triangle;
        if (keycode is (int)'4') _model.Waveform = WaveformType.Sawtooth;
        if (keycode is (int)'5') _model.Waveform = WaveformType.ChirpFM;

        if (keycode is (int)'T' or (int)'t')
        {
            _model.TriggerMode = _model.TriggerMode switch
            {
                TriggerSlope.Auto => TriggerSlope.Rising,
                TriggerSlope.Rising => TriggerSlope.Falling,
                TriggerSlope.Falling => TriggerSlope.FreeRun,
                _ => TriggerSlope.Auto
            };
        }
    }

    private void OnIdle(CrystalWindow windowHandle)
    {
        windowHandle.QueueRedraw();
    }

    private void OnDraw(CrystalWindow windowHandle)
    {
        windowHandle.GetSize(out int width, out int height);

        // FPS Calculation
        double time = wnd.uptimeSeconds();
        double time_delta = time - last_time;
        last_time = time;

        sample_deltas[sample_index] = time_delta;
        sample_index = (sample_index + 1) % sample_deltas.Length;
        if (sample_count < sample_deltas.Length) sample_count++;

        double total_delta = 0;
        for (int i = 0; i < sample_count; i++)
        {
            total_delta += sample_deltas[i];
        }
        string fpsText = $"{sample_count / Math.Max(0.0001, total_delta):0.0} FPS";

        // Layout update
        UpdateLayout(width, height);

        // Synchronize Button Active States
        foreach (var b in _buttons)
        {
            b.IsActive = b.Id switch
            {
                "src_gen" => _model.SourceMode == SignalSourceMode.Generator,
                "src_input" => _model.SourceMode == SignalSourceMode.AudioInput,
                "trig_auto" => _model.TriggerMode == TriggerSlope.Auto,
                "trig_rise" => _model.TriggerMode == TriggerSlope.Rising,
                "trig_fall" => _model.TriggerMode == TriggerSlope.Falling,
                "trig_free" => _model.TriggerMode == TriggerSlope.FreeRun,
                "wave_sine" => _model.Waveform == WaveformType.Sine,
                "wave_square" => _model.Waveform == WaveformType.Square,
                "wave_tri" => _model.Waveform == WaveformType.Triangle,
                "wave_saw" => _model.Waveform == WaveformType.Sawtooth,
                "wave_fm" => _model.Waveform == WaveformType.ChirpFM,
                "btn_freeze" => _model.IsFrozen,
                "btn_mute" => _model.IsMuted,
                "btn_glow" => _model.PhosphorGlow,
                _ => false
            };
        }

        // Backing PixData management
        if (!pixelBacking || pixelBacking.width != width || pixelBacking.height != height)
        {
            if (pixelBacking)
            {
                pixelBacking.Dispose();
            }
            pixelBacking = CrystalSkia.net.FixedPixDataRenderer.CreateFixed(width, height, (canvas, info) => { });
        }

        CrystalSkia.net.PixDataSkia.WithCanvasView(pixelBacking, (bitmap, canvas) =>
        {
            // Clear entire window background
            canvas.DrawRect(new SKRect(0, 0, width, height), bgPaint);

            // Draw Top Application Header Bar
            DrawTopHeader(canvas, width, fpsText);

            // Calculate scope screen rect
            float padding = 16f;
            float rightPanelWidth = 320f;
            float topHeaderHeight = 44f;
            float scopeX = padding;
            float scopeY = topHeaderHeight + 8f;
            float scopeW = Math.Max(200f, width - rightPanelWidth - padding * 3f);
            float scopeH = Math.Max(200f, height - scopeY - padding);
            var scopeScreenRect = new SKRect(scopeX, scopeY, scopeX + scopeW, scopeY + scopeH);

            // Draw Oscilloscope Screen & Grid
            DrawOscilloscope(canvas, scopeScreenRect);

            // Draw Right Control Panel
            float panelX = width - rightPanelWidth - padding;
            var panelRect = new SKRect(panelX, scopeY, panelX + rightPanelWidth, scopeY + scopeH);
            DrawControlPanel(canvas, panelRect);
        });

        windowHandle.PresentPix(ref pixelBacking);
    }

    private void DrawTopHeader(SKCanvas canvas, int width, string fpsText)
    {
        var headerRect = new SKRect(16, 8, width - 16, 44);
        var rrect = new SKRoundRect(headerRect, 6, 6);
        canvas.DrawRoundRect(rrect, panelBgPaint);
        canvas.DrawRoundRect(rrect, panelBorderPaint);

        // Title and Subtitle
        string title = "CRYSTALCATALYST OSCILLOSCOPE";
        canvas.DrawText(title, 28, 30, titleFont, textPrimaryPaint);
        titleFont.MeasureText(title, out SKRect titleBounds, textPrimaryPaint);

        float subtitleX = 28 + titleBounds.Width + 16f;
        string subtitle = "•  1-SEC CYCLIC AUDIO BUFFER  •  REAL-TIME HARDWARE SKIA ACCELERATED";
        canvas.DrawText(subtitle, subtitleX, 30, smallMonoFont, textAccentPaint);

        // Status Indicators on the Right
        string audioState;
        SKPaint audioPaint;

        if (_model.SourceMode == SignalSourceMode.AudioInput)
        {
            if (_capture?.IsCapturing == true)
            {
                audioState = "MIC IN (SILENT)";
                audioPaint = textPinkPaint;
            }
            else if (_capture?.IsAvailable == false)
            {
                audioState = "INPUT (NO DEV)";
                audioPaint = textAmberPaint;
            }
            else
            {
                audioState = "INPUT READY";
                audioPaint = textAccentPaint;
            }
        }
        else
        {
            audioState = _streamer?.IsStreaming == true ? (_model.IsMuted ? "MUTED" : "LIVE AUDIO") : "NO AUDIO";
            audioPaint = _model.IsMuted ? textAmberPaint : (_streamer?.IsStreaming == true ? textGreenPaint : textMutedPaint);
        }

        monoFont.MeasureText(audioState, out SKRect audioBounds, audioPaint);
        float audioX = width - 36 - audioBounds.Width - 110;
        canvas.DrawText(audioState, audioX, 30, monoFont, audioPaint);

        canvas.DrawText(fpsText, width - 110, 30, monoFont, textMutedPaint);
    }

    private void DrawOscilloscope(SKCanvas canvas, SKRect bounds)
    {
        // Draw Outer Bezel
        var bezelRRect = new SKRoundRect(bounds, 8, 8);
        canvas.DrawRoundRect(bezelRRect, scopeBezelPaint);
        canvas.DrawRoundRect(bezelRRect, scopeBezelBorderPaint);

        // Screen area with inner margin
        float margin = 8f;
        var screenRect = new SKRect(bounds.Left + margin, bounds.Top + margin, bounds.Right - margin, bounds.Bottom - margin);
        var screenRRect = new SKRoundRect(screenRect, 6, 6);

        canvas.Save();
        canvas.ClipRoundRect(screenRRect, antialias: true);

        // Fill CRT Screen
        canvas.DrawRoundRect(screenRRect, scopeScreenBgPaint);

        // Draw Graticule Grid (10 horizontal divisions, 8 vertical divisions)
        int hDivs = 10;
        int vDivs = 8;
        float divW = screenRect.Width / hDivs;
        float divH = screenRect.Height / vDivs;

        // Minor grid lines / subdivisions
        if (_model.ShowGridSubdivisions)
        {
            for (int i = 0; i < hDivs * 5; i++)
            {
                float x = screenRect.Left + i * (divW / 5f);
                canvas.DrawLine(x, screenRect.Top, x, screenRect.Bottom, gridMinorPaint);
            }
            for (int j = 0; j < vDivs * 5; j++)
            {
                float y = screenRect.Top + j * (divH / 5f);
                canvas.DrawLine(screenRect.Left, y, screenRect.Right, y, gridMinorPaint);
            }
        }

        // Major grid lines
        for (int i = 1; i < hDivs; i++)
        {
            float x = screenRect.Left + i * divW;
            canvas.DrawLine(x, screenRect.Top, x, screenRect.Bottom, gridMajorPaint);
        }
        for (int j = 1; j < vDivs; j++)
        {
            float y = screenRect.Top + j * divH;
            canvas.DrawLine(screenRect.Left, y, screenRect.Right, y, gridMajorPaint);
        }

        // Center Crosshair Axis
        float centerX = screenRect.Left + screenRect.Width / 2f;
        float centerY = screenRect.Top + screenRect.Height / 2f;
        canvas.DrawLine(screenRect.Left, centerY, screenRect.Right, centerY, centerAxisPaint);
        canvas.DrawLine(centerX, screenRect.Top, centerX, screenRect.Bottom, centerAxisPaint);

        // Center Axis Calibration Ticks
        for (int i = 0; i <= hDivs * 5; i++)
        {
            float x = screenRect.Left + i * (divW / 5f);
            float tickH = (i % 5 == 0) ? 6f : 3f;
            canvas.DrawLine(x, centerY - tickH, x, centerY + tickH, centerAxisPaint);
        }
        for (int j = 0; j <= vDivs * 5; j++)
        {
            float y = screenRect.Top + j * (divH / 5f);
            float tickW = (j % 5 == 0) ? 6f : 3f;
            canvas.DrawLine(centerX - tickW, y, centerX + tickW, y, centerAxisPaint);
        }

        // Trigger Level Line & Arrow Marker
        float triggerY = centerY - (_model.TriggerLevel * _model.VerticalGain * (screenRect.Height / 2f) * 0.8f);
        if (triggerY >= screenRect.Top && triggerY <= screenRect.Bottom)
        {
            canvas.DrawLine(screenRect.Left, triggerY, screenRect.Right, triggerY, triggerLinePaint);

            // Left Trigger Marker Arrow
            using var markerPath = new SKPath();
            markerPath.MoveTo(screenRect.Left + 1, triggerY);
            markerPath.LineTo(screenRect.Left + 8, triggerY - 5);
            markerPath.LineTo(screenRect.Left + 8, triggerY + 5);
            markerPath.Close();
            canvas.DrawPath(markerPath, triggerMarkerPaint);
        }

        // Capture Samples from 1-Sec Cyclic Buffer
        int sampleRate = _cyclicBuffer.SampleRate;
        int samplesNeeded = (int)Math.Clamp(sampleRate * (_model.TimebaseMs / 1000.0f), 32, 4096);

        if (_renderSampleBuffer.Length < samplesNeeded)
        {
            _renderSampleBuffer = new double[samplesNeeded];
        }

        bool isTriggered = false;
        int samplesRead = 0;

        if (_model.IsFrozen)
        {
            // Retain frozen snapshot
            samplesRead = _frozenSampleCount;
            Array.Copy(_frozenSampleBuffer, _renderSampleBuffer, samplesRead);
        }
        else
        {
            // Live capture from circular buffer
            samplesRead = _cyclicBuffer.ReadTriggeredWindow(
                _renderSampleBuffer,
                samplesNeeded,
                _model.TriggerLevel,
                _model.TriggerMode,
                out isTriggered
            );

            // Save to freeze buffer
            if (_frozenSampleBuffer.Length < samplesRead)
            {
                _frozenSampleBuffer = new double[samplesRead];
            }
            Array.Copy(_renderSampleBuffer, _frozenSampleBuffer, samplesRead);
            _frozenSampleCount = samplesRead;
        }

        // Compute Telemetry Metrics
        _cyclicBuffer.ComputeTelemetry(samplesNeeded, out double vpp, out double vrms, out double estimatedFreq);

        // Draw Waveform Trace
        if (samplesRead > 1)
        {
            using var wavePath = new SKPath();
            float halfH = (screenRect.Height / 2f) * 0.8f;

            for (int i = 0; i < samplesRead; i++)
            {
                float px = screenRect.Left + (float)i / (samplesRead - 1) * screenRect.Width;
                double sampleVal = _renderSampleBuffer[i];
                float py = centerY - (float)(sampleVal * _model.VerticalGain * halfH);

                if (i == 0)
                {
                    wavePath.MoveTo(px, py);
                }
                else
                {
                    wavePath.LineTo(px, py);
                }
            }

            // Multi-pass Phosphor Glow
            if (_model.PhosphorGlow)
            {
                canvas.DrawPath(wavePath, waveGlowWidePaint);
                canvas.DrawPath(wavePath, waveGlowMedPaint);
            }
            canvas.DrawPath(wavePath, waveCorePaint);
        }

        // Scope Top Overlay HUD
        DrawScopeHud(canvas, screenRect, isTriggered, vpp, vrms, estimatedFreq);

        canvas.Restore();
    }

    private void DrawScopeHud(SKCanvas canvas, SKRect screenRect, bool isTriggered, double vpp, double vrms, double estimatedFreq)
    {
        float hudY = screenRect.Top + 8f;

        // Top Left: Channel 1 Settings Badge
        string srcTag = _model.SourceMode == SignalSourceMode.AudioInput ? "MIC/IN" : "GEN";
        var ch1Rect = new SKRect(screenRect.Left + 10, hudY, screenRect.Left + 225, hudY + 24);
        canvas.DrawRoundRect(new SKRoundRect(ch1Rect, 4, 4), hudBadgeBgPaint);
        canvas.DrawRoundRect(new SKRoundRect(ch1Rect, 4, 4), hudBadgeBorderPaint);
        string ch1Text = $"CH1 [{srcTag}]: {0.25f / _model.VerticalGain:F2}V/div  {_model.TimebaseMs / 10f:F2}ms/div";
        canvas.DrawText(ch1Text, ch1Rect.Left + 8, ch1Rect.Top + 16, smallMonoFont, textAccentPaint);

        // Top Center: Trigger Status Badge
        string trigText = _model.IsFrozen ? "● STOP / FROZEN" : (isTriggered ? "● TRIG'D" : (_model.TriggerMode == TriggerSlope.FreeRun ? "● FREE RUN" : "● AUTO"));
        SKPaint trigPaint = _model.IsFrozen ? textAmberPaint : (isTriggered ? textGreenPaint : textAccentPaint);
        monoFont.MeasureText(trigText, out SKRect trigBounds, trigPaint);

        var trigRect = new SKRect(screenRect.Left + screenRect.Width / 2f - trigBounds.Width / 2f - 12, hudY, screenRect.Left + screenRect.Width / 2f + trigBounds.Width / 2f + 12, hudY + 24);
        canvas.DrawRoundRect(new SKRoundRect(trigRect, 4, 4), hudBadgeBgPaint);
        canvas.DrawRoundRect(new SKRoundRect(trigRect, 4, 4), hudBadgeBorderPaint);
        canvas.DrawText(trigText, trigRect.Left + 12, trigRect.Top + 16, monoFont, trigPaint);

        // Bottom Measurement Bar
        float bottomHudY = screenRect.Bottom - 30f;
        var measRect = new SKRect(screenRect.Left + 10, bottomHudY, screenRect.Right - 10, bottomHudY + 24);
        canvas.DrawRoundRect(new SKRoundRect(measRect, 4, 4), hudBadgeBgPaint);
        canvas.DrawRoundRect(new SKRoundRect(measRect, 4, 4), hudBadgeBorderPaint);

        string meas1 = $"Vpp: {vpp:F2}V";
        string meas2 = $"Vrms: {vrms:F2}V";
        string meas3 = $"Freq: {(estimatedFreq > 10 ? $"{estimatedFreq:F0} Hz" : $"{_model.Frequency:F0} Hz (calc)")}";
        string meas4 = $"Buffer: {_cyclicBuffer.DurationSeconds:F1}s ({_cyclicBuffer.Capacity:N0} smp)";

        float spacing = measRect.Width / 4f;
        canvas.DrawText(meas1, measRect.Left + 10, measRect.Top + 16, smallMonoFont, textPrimaryPaint);
        canvas.DrawText(meas2, measRect.Left + spacing + 10, measRect.Top + 16, smallMonoFont, textPrimaryPaint);
        canvas.DrawText(meas3, measRect.Left + spacing * 2 + 10, measRect.Top + 16, smallMonoFont, textGreenPaint);
        canvas.DrawText(meas4, measRect.Left + spacing * 3 + 10, measRect.Top + 16, smallMonoFont, textAccentPaint);
    }

    private void DrawControlPanel(SKCanvas canvas, SKRect panelRect)
    {
        var rrect = new SKRoundRect(panelRect, 8, 8);
        canvas.DrawRoundRect(rrect, panelBgPaint);
        canvas.DrawRoundRect(rrect, panelBorderPaint);

        float left = panelRect.Left + 16f;

        // Section 1 Header: Oscilloscope Parameters
        canvas.DrawText("OSCILLOSCOPE CONTROLS", left, panelRect.Top + 24f, sectionFont, textAccentPaint);

        // Section 2 Header: Signal Source
        canvas.DrawText("SIGNAL SOURCE", left, _sourceHeaderY, sectionFont, textPinkPaint);

        // Section 3 Header: Signal Generator
        SKPaint genHeaderPaint = _model.SourceMode == SignalSourceMode.Generator ? textGreenPaint : textMutedPaint;
        string genTitle = _model.SourceMode == SignalSourceMode.Generator ? "SIGNAL GENERATOR (ACTIVE)" : "SIGNAL GENERATOR (STANDBY)";
        canvas.DrawText(genTitle, left, _sigGenHeaderY, sectionFont, genHeaderPaint);

        // Sliders
        foreach (var s in _sliders)
        {
            s.Draw(canvas, bodyFont, monoFont);
        }

        // Buttons
        foreach (var b in _buttons)
        {
            b.Draw(canvas, bodyBoldFont);
        }

        // Shortcuts Footer Guide
        float footerY = panelRect.Bottom - 38f;
        canvas.DrawText("Shortcuts: [Space] Freeze  [M] Mute  [I] Source (Mic/Gen)", left, footerY, smallMonoFont, textMutedPaint);
        canvas.DrawText("[1-5] Waves  [T] Trigger Mode  [Esc] Close Showcase", left, footerY + 16f, smallMonoFont, textMutedPaint);
    }

    public void Show(bool visible = true)
    {
        wnd.Show(visible);
    }
}
