using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace CrystalOpenAL;

/// <summary>
/// Managed, cross-platform audio capture service utilizing OpenAL ALC capture functionality.
/// Records live microphone/line-in audio input and routes normalized samples into a destination cyclic buffer or callback.
/// </summary>
public class AudioCapture : IDisposable
{
    private const int AL_FORMAT_MONO16 = 0x1101;
    private const int ALC_CAPTURE_SAMPLES = 0x312;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr AlcCaptureOpenDeviceDelegate(string? deviceName, uint frequency, int format, int bufferSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool AlcCaptureCloseDeviceDelegate(IntPtr device);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void AlcCaptureStartDelegate(IntPtr device);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void AlcCaptureStopDelegate(IntPtr device);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void AlcCaptureSamplesDelegate(IntPtr device, IntPtr buffer, int samples);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void AlcGetIntegervDelegate(IntPtr device, int param, int size, IntPtr data);

    private readonly AlcCaptureOpenDeviceDelegate? _alcCaptureOpenDevice;
    private readonly AlcCaptureCloseDeviceDelegate? _alcCaptureCloseDevice;
    private readonly AlcCaptureStartDelegate? _alcCaptureStart;
    private readonly AlcCaptureStopDelegate? _alcCaptureStop;
    private readonly AlcCaptureSamplesDelegate? _alcCaptureSamples;
    private readonly AlcGetIntegervDelegate? _alcGetIntegerv;

    private IntPtr _captureDevice = IntPtr.Zero;
    private readonly int _sampleRate;
    private readonly int _captureBufferSize;
    private Thread? _captureThread;
    private volatile bool _isCapturing;
    private bool _disposed;
    private readonly object _lock = new();

    private AudioCyclicBuffer? _targetCyclicBuffer;
    private Action<double[], int>? _sampleCallback;
    private Action<short[], int>? _pcmCallback;

    /// <summary>
    /// Indicates whether the OpenAL capture subsystem and hardware capture device are available.
    /// </summary>
    public bool IsAvailable { get; }

    /// <summary>
    /// Indicates whether audio is currently actively capturing.
    /// </summary>
    public bool IsCapturing => _isCapturing && !_disposed;

    /// <summary>
    /// Audio sample rate in Hertz.
    /// </summary>
    public int SampleRate => _sampleRate;

    /// <summary>
    /// Capture ring buffer capacity in samples.
    /// </summary>
    public int CaptureBufferSize => _captureBufferSize;

    /// <summary>
    /// Native OpenAL ALC capture device pointer.
    /// </summary>
    public IntPtr DeviceHandle => _captureDevice;

    public AudioCapture(int sampleRate = 44100, int bufferSize = 8192, string? deviceName = null)
    {
        _sampleRate = sampleRate > 0 ? sampleRate : 44100;
        _captureBufferSize = bufferSize > 0 ? bufferSize : 8192;

        try
        {
            IntPtr libHandle = IntPtr.Zero;
            bool loaded = NativeLibrary.TryLoad("openal", typeof(AudioCapture).Assembly, null, out libHandle)
                       || NativeLibrary.TryLoad("libopenal.so.1", out libHandle)
                       || NativeLibrary.TryLoad("soft_oal", out libHandle)
                       || NativeLibrary.TryLoad("openal32.dll", out libHandle);

            if (!loaded || libHandle == IntPtr.Zero)
            {
                IsAvailable = false;
                return;
            }

            if (NativeLibrary.TryGetExport(libHandle, "alcCaptureOpenDevice", out IntPtr pOpen) &&
                NativeLibrary.TryGetExport(libHandle, "alcCaptureCloseDevice", out IntPtr pClose) &&
                NativeLibrary.TryGetExport(libHandle, "alcCaptureStart", out IntPtr pStart) &&
                NativeLibrary.TryGetExport(libHandle, "alcCaptureStop", out IntPtr pStop) &&
                NativeLibrary.TryGetExport(libHandle, "alcCaptureSamples", out IntPtr pSamples) &&
                NativeLibrary.TryGetExport(libHandle, "alcGetIntegerv", out IntPtr pGetIntegerv))
            {
                _alcCaptureOpenDevice = Marshal.GetDelegateForFunctionPointer<AlcCaptureOpenDeviceDelegate>(pOpen);
                _alcCaptureCloseDevice = Marshal.GetDelegateForFunctionPointer<AlcCaptureCloseDeviceDelegate>(pClose);
                _alcCaptureStart = Marshal.GetDelegateForFunctionPointer<AlcCaptureStartDelegate>(pStart);
                _alcCaptureStop = Marshal.GetDelegateForFunctionPointer<AlcCaptureStopDelegate>(pStop);
                _alcCaptureSamples = Marshal.GetDelegateForFunctionPointer<AlcCaptureSamplesDelegate>(pSamples);
                _alcGetIntegerv = Marshal.GetDelegateForFunctionPointer<AlcGetIntegervDelegate>(pGetIntegerv);

                _captureDevice = _alcCaptureOpenDevice(deviceName, (uint)_sampleRate, AL_FORMAT_MONO16, _captureBufferSize);
                IsAvailable = _captureDevice != IntPtr.Zero;
            }
            else
            {
                IsAvailable = false;
            }
        }
        catch
        {
            IsAvailable = false;
        }
    }

    /// <summary>
    /// Starts capturing audio input and delivering samples into the specified <see cref="AudioCyclicBuffer"/>.
    /// </summary>
    public bool Start(AudioCyclicBuffer cyclicBuffer)
    {
        ArgumentNullException.ThrowIfNull(cyclicBuffer);
        lock (_lock)
        {
            _targetCyclicBuffer = cyclicBuffer;
            _sampleCallback = null;
            _pcmCallback = null;
            return StartInternal();
        }
    }

    /// <summary>
    /// Starts capturing audio input and delivering normalized double samples (-1.0 to 1.0) via callback.
    /// </summary>
    public bool Start(Action<double[], int> sampleCallback)
    {
        ArgumentNullException.ThrowIfNull(sampleCallback);
        lock (_lock)
        {
            _sampleCallback = sampleCallback;
            _targetCyclicBuffer = null;
            _pcmCallback = null;
            return StartInternal();
        }
    }

    /// <summary>
    /// Starts capturing audio input and delivering raw 16-bit PCM samples via callback.
    /// </summary>
    public bool Start(Action<short[], int> pcmCallback)
    {
        ArgumentNullException.ThrowIfNull(pcmCallback);
        lock (_lock)
        {
            _pcmCallback = pcmCallback;
            _targetCyclicBuffer = null;
            _sampleCallback = null;
            return StartInternal();
        }
    }

    private bool StartInternal()
    {
        if (!IsAvailable || _disposed || _captureDevice == IntPtr.Zero || _alcCaptureStart == null)
        {
            return false;
        }

        if (_isCapturing) return true;

        try
        {
            _alcCaptureStart(_captureDevice);
            _isCapturing = true;

            _captureThread = new Thread(CaptureLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = "OpenAL-AudioCapture-Thread"
            };
            _captureThread.Start();
            return true;
        }
        catch
        {
            _isCapturing = false;
            return false;
        }
    }

    /// <summary>
    /// Stops audio capture without closing the device.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            if (!_isCapturing) return;
            _isCapturing = false;

            try
            {
                if (_captureDevice != IntPtr.Zero && _alcCaptureStop != null)
                {
                    _alcCaptureStop(_captureDevice);
                }
            }
            catch
            {
                // Ignored
            }

            if (_captureThread != null && _captureThread.IsAlive)
            {
                _captureThread.Join(100);
                _captureThread = null;
            }
        }
    }

    private void CaptureLoop()
    {
        short[] pcmBuffer = new short[4096];
        double[] doubleBuffer = new double[4096];
        int[] countBox = new int[1];

        GCHandle pcmHandle = GCHandle.Alloc(pcmBuffer, GCHandleType.Pinned);
        GCHandle countHandle = GCHandle.Alloc(countBox, GCHandleType.Pinned);

        try
        {
            IntPtr pPcm = pcmHandle.AddrOfPinnedObject();
            IntPtr pCount = countHandle.AddrOfPinnedObject();

            while (_isCapturing && !_disposed)
            {
                try
                {
                    if (_captureDevice == IntPtr.Zero || _alcGetIntegerv == null || _alcCaptureSamples == null)
                    {
                        break;
                    }

                    countBox[0] = 0;
                    _alcGetIntegerv(_captureDevice, ALC_CAPTURE_SAMPLES, 1, pCount);
                    int sampleCount = countBox[0];

                    if (sampleCount > 0)
                    {
                        int samplesToRead = Math.Min(sampleCount, pcmBuffer.Length);

                        _alcCaptureSamples(_captureDevice, pPcm, samplesToRead);

                        if (_pcmCallback != null)
                        {
                            _pcmCallback(pcmBuffer, samplesToRead);
                        }

                        if (_targetCyclicBuffer != null || _sampleCallback != null)
                        {
                            const double inv32768 = 1.0 / 32768.0;
                            for (int i = 0; i < samplesToRead; i++)
                            {
                                doubleBuffer[i] = pcmBuffer[i] * inv32768;
                            }

                            if (_targetCyclicBuffer != null)
                            {
                                _targetCyclicBuffer.WriteBlock(doubleBuffer, 0, samplesToRead);
                            }

                            if (_sampleCallback != null)
                            {
                                _sampleCallback(doubleBuffer, samplesToRead);
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(5);
                    }
                }
                catch
                {
                    break;
                }
            }
        }
        finally
        {
            if (pcmHandle.IsAllocated) pcmHandle.Free();
            if (countHandle.IsAllocated) countHandle.Free();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;

            Stop();

            if (_captureDevice != IntPtr.Zero && _alcCaptureCloseDevice != null)
            {
                try
                {
                    _alcCaptureCloseDevice(_captureDevice);
                }
                catch
                {
                    // Ignored
                }
                _captureDevice = IntPtr.Zero;
            }
        }
        GC.SuppressFinalize(this);
    }
}
