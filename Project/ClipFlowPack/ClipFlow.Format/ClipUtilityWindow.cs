using System.Reflection;
using System.Runtime.InteropServices;
using CrystalCatalystLibrary.net;

namespace ClipFlow.Format;

public class ClipUtilityWindow
{
    /*
        DONE / substantially proven
       ---------------------------
       thread-affine CrystalApplication
       multiple UI threads
       Windows + X11 clipboard plumbing
       raw X11 format preservation
       DataInterchange error callback
       ClipFlow fluent grammar
       utility-window execution isolation
       
       NOW
       ---
       ClipType specialization
       ClipEndpoint read/write behavior
       context-owned input/output
       format selection by ClipType
       real copy/paste payloads
    */
    public static void ShowAvail(ClipContext context)
    {
        ShowAvail(
            context,
            header => context.Output.WriteLine(header.CommandName));
    }
    
    public static void ShowAvail(ClipContext context, Action<ClipTypeHeader> available)
    {
        Thread runner = new Thread(() =>
        {
            Application.Init(new string[0]);
            Application.SetDiagnosticsCallback(context._Diagnostic);
            
            CrystalWindow wnd = CrystalWindow.CreateSimple(1, 1, "Clip Utility");
            wnd.ApplicationRetain();

            Queue<Action<CrystalWindow>> work_queue = new Queue<Action<CrystalWindow>>();
            
            wnd.OnClose = (wnd) =>
            {
                wnd.ApplicationRelease();
            };
            
            wnd.OnDataInterchangeError = (wnd, di, message) =>
            {
                context.ErrorOutput.WriteLine($"Clipboard data interchange error: {message}");
                if (message != null && message.Contains("no clipboard manager running"))
                {
                    context.ErrorOutput.WriteLine("Notice: No X11 clipboard manager is running. Start a clipboard daemon (e.g. xfce4-clipman, parcellite, clipman) to retain clipboard contents after ClipFlow exits.");
                }
                context.Status = 1;
                wnd.PostClose();
            };
            
            List<ClipTypeHeader> providers = new List<ClipTypeHeader>();

            foreach (Type t in typeof(ClipType).GetNestedTypes().Where(t => t.IsSubclassOf(typeof(ClipType))))
            {
                providers.Add((ClipTypeHeader)t.GetField("ClipTypeHeader", BindingFlags.Public | BindingFlags.Static).GetValue(null));
            }

            List<string> seen = new List<string>();
            
            work_queue.Enqueue((wnd) =>
            {
                DataInterchange di = wnd.ClipboardPaste();

                bool found = false;
                for (var node = di.FormatEnum(); node != IntPtr.Zero; node = DataInterchange.FormatEnumNext(node)) {
                    
                    DataInterchange.FormatEnumText(node, out var drop_format);

                    foreach (var provider in providers)
                    {
                        if (seen.Contains(provider.CommandName)) continue;

                        if (provider.Formats.Contains(drop_format))
                        {
                            available(provider);
                            seen.Add(provider.CommandName);
                            found = true;
                            break;
                        }
                    }
                }

                
                if (!found)
                {
                    context.ErrorOutput.WriteLine("Clipboard contains no known format");
                }
                
                wnd.PostClose();
            });

            wnd.OnIdle = (wnd) =>
            {
                if (work_queue.Count > 0)
                {
                    Action<CrystalWindow> action = work_queue.Dequeue();
                    action(wnd);
                }
            };
            
            
            Application.Run();
        });

        if (OperatingSystem.IsWindows())
        {
            runner.SetApartmentState(ApartmentState.STA);
        }
        
        runner.Start();
        runner.Join();
    }
    
    public static void Paste(ClipContext context, ClipType type)
    {
        PasteCore(context, type, null);
    }

    public static void Paste(ClipContext context, ClipType type, ClipEndpoint endpoint)
    {
        PasteCore(context, type, () => endpoint.Write(context, type));
    }

    private static void PasteCore(ClipContext context, ClipType type, Action? complete)
    {
        Thread runner = new Thread(() =>
        {
            Application.Init(new string[0]);
            Application.SetDiagnosticsCallback(context._Diagnostic);
            
            CrystalWindow wnd = CrystalWindow.CreateSimple(1, 1, "Clip Utility");
            wnd.ApplicationRetain();

            Queue<Action<CrystalWindow>> work_queue = new Queue<Action<CrystalWindow>>();
            
            wnd.OnClose = (wnd) =>
            {
                wnd.ApplicationRelease();
            };
            
            wnd.OnDataInterchangeError = (wnd, di, message) =>
            {
                context.ErrorOutput.WriteLine($"Clipboard data interchange error: {message}");
                if (message != null && message.Contains("no clipboard manager running"))
                {
                    context.ErrorOutput.WriteLine("Notice: No X11 clipboard manager is running. Start a clipboard daemon (e.g. xfce4-clipman, parcellite, clipman) to retain clipboard contents after ClipFlow exits.");
                }
                context.Status = 1;
                wnd.PostClose();
            };
            
            wnd.OnClipboardReceiveData = (wnd, di) =>
            {
                di.SelectionReveal(out string format, out IntPtr data, out IntPtr size);
                if (data == IntPtr.Zero)
                {
                    context.ErrorOutput.WriteLine($"Clipboard paste data is null");
                    context.Status = 1;
                    wnd.PostClose();
                    return;
                }
            
                type.Receive(context, di, format, data, size);
                if (context.Status == 0) complete?.Invoke();
                
                wnd.PostClose();
            };
            
            work_queue.Enqueue((wnd) =>
            {
                DataInterchange di = wnd.ClipboardPaste();
                type.Select(context, di);
                
                if (context.Status != 0) wnd.PostClose();
            });

            wnd.OnIdle = (wnd) =>
            {
                if (work_queue.Count > 0)
                {
                    Action<CrystalWindow> action = work_queue.Dequeue();
                    action(wnd);
                }
            };
            
            Application.Run();
        });

        if (OperatingSystem.IsWindows())
        {
            runner.SetApartmentState(ApartmentState.STA);
        }
        
        runner.Start();
        runner.Join();
    }
    
    public static void Copy(ClipContext context, ClipType type)
    {
        CopyCore(context, type, null);
    }

    public static void Copy(ClipContext context, ClipType type, ClipEndpoint endpoint)
    {
        CopyCore(context, type, () => endpoint.Read(context, type));
    }

    private static void CopyCore(ClipContext context, ClipType type, Action? prepare)
    {
        Thread runner = new Thread(() =>
        {
            Application.Init([]);
            Application.SetDiagnosticsCallback(context._Diagnostic);

            CrystalWindow wnd =
                CrystalWindow.CreateSimple(1, 1, "Clip Utility");

            wnd.ApplicationRetain();

            wnd.OnClose = wnd =>
            {
                wnd.ApplicationRelease();
            };

            wnd.OnDataInterchangeError = (wnd, di, message) =>
            {
                context.ErrorOutput.WriteLine(
                    $"Clipboard data interchange error: {message}");
                if (message != null && message.Contains("no clipboard manager running"))
                {
                    context.ErrorOutput.WriteLine(
                        "Notice: No X11 clipboard manager is running. Start a clipboard daemon (e.g. xfce4-clipman, parcellite, clipman) to retain clipboard contents after ClipFlow exits.");
                }

                context.Status = 1;
                wnd.PostClose();
            };

            wnd.OnClipboardProvideChosen = (handle, data, format) =>
            {
                byte[]? bytes =
                    type.Provide(context, data, format);

                if (context.Status != 0 || bytes == null)
                    return;

                IntPtr ptr =
                    Marshal.AllocHGlobal(bytes.Length);

                try
                {
                    Marshal.Copy(
                        bytes,
                        0,
                        ptr,
                        bytes.Length);

                    data.SelectionSet(
                        format,
                        ptr,
                        (IntPtr)bytes.Length);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            };

            bool executed = false;
            wnd.OnIdle = wnd =>
            {
                if (executed) return;
                executed = true;

                prepare?.Invoke();

                if (context.Status != 0)
                {
                    wnd.PostClose();
                    return;
                }

                DataInterchange di =
                    DataInterchange.Create();

                type.Advertise(context, di);

                wnd.ClipboardCopyPersist(di);

                // Persist owns completion semantics.
                wnd.PostClose();
            };

            Application.Run();
        });
        
        if (OperatingSystem.IsWindows())
        {
            runner.SetApartmentState(ApartmentState.STA);
        }
        
        runner.Start();
        runner.Join();
    }
}