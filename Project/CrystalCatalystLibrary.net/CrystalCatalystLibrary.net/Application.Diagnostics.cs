using System;
using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary.net;

public partial class Application
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NativeDiagnosticCallback(ref utf8_string_struct message);

    [ThreadStatic]
    private static NativeDiagnosticCallback? _nativeDiagnosticCallback;

    [ThreadStatic]
    private static Action<string>? _diagnosticCallback;

    public static void SetDiagnosticsCallback(Action<string>? callback)
    {
        if (callback == null)
        {
            Imports.Application_SetDiagnosticsCallback(IntPtr.Zero);
            _nativeDiagnosticCallback = null;
            _diagnosticCallback = null;
            return;
        }

        _diagnosticCallback = callback;
        _nativeDiagnosticCallback = (ref utf8_string_struct message) =>
        {
            _diagnosticCallback?.Invoke((string)message);
        };

        IntPtr pointerToNative = Marshal.GetFunctionPointerForDelegate(_nativeDiagnosticCallback);
        Imports.Application_SetDiagnosticsCallback(pointerToNative);
    }
}
