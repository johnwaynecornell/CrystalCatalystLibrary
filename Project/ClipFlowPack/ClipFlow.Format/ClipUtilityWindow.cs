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
        Thread runner = new Thread(() =>
        {
            Application.Init(new string[0]);
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
                            context.Output.WriteLine(provider.CommandName);
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
    
    public static void Paste(ClipContext context, ClipType type, ClipEndpoint endpoint)
    {
        Thread runner = new Thread(() =>
        {
            Application.Init(new string[0]);
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
                
                Console.WriteLine(Marshal.PtrToStringUTF8(data));
                wnd.PostClose();
              
            };
            
            string[] format_prec = new string[] { "text/plain", "TEXT", "STRING", "UTF8_STRING" };
            
            work_queue.Enqueue((wnd) =>
            {
                DataInterchange di = wnd.ClipboardPaste();
                
                string? format = null;

                bool hasNode = false;
                for (var node = di.FormatEnum(); node != IntPtr.Zero; node = DataInterchange.FormatEnumNext(node)) {
                    if (!hasNode)
                    {
                        Console.WriteLine("Advertised formats:");
                    }
                    
                    DataInterchange.FormatEnumText(node, out var drop_format);
                    Console.WriteLine(drop_format);
                    
                    hasNode = true;
                }

                if (!hasNode)
                {
                    Console.WriteLine("No advertised formats");
                }
                
                for (var node = di.FormatEnum(); node != IntPtr.Zero; node = DataInterchange.FormatEnumNext(node)) {
                    
                    DataInterchange.FormatEnumText(node, out var drop_format);
                    
                    foreach (string known in format_prec)
                    {
                        if (known == drop_format) {
                            format = known;
                            break;
                        }
                    }
                    if (format != null)
                        break;
                }

                if (format == null)
                {
                    context.ErrorOutput.WriteLine("Clipboard paste error no known format found");
                    context.Status = 1;
                    wnd.PostClose();
                }
                else
                {
                    context.ErrorOutput.WriteLine($"Clipboard paste format {format}");
                    di.Select(format);
                }
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
    
    public static void Copy(ClipContext context, ClipType type, ClipEndpoint endpoint)
    {
        Thread runner = new Thread(() =>
        {
            Application.Init(new string[0]);
            CrystalWindow wnd = CrystalWindow.CreateSimple(1, 1, "Clip Utility");
            wnd.ApplicationRetain();

            Queue<Action<CrystalWindow>> work_queue = new Queue<Action<CrystalWindow>>();
            
            wnd.OnClose = (wnd) =>
            {
                wnd.ApplicationRelease();
            };
            
            wnd.OnClipboardProvideChosen = (handle, data, format) =>
            {
                if (format == "text/plain" || format == "TEXT")
                {
                    string message = "Hello, World! From the clipboard";
            
                    byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(message);
                    IntPtr _data = Marshal.AllocHGlobal(utf8Bytes.Length);
                    Marshal.Copy(utf8Bytes, 0, _data, utf8Bytes.Length);
                    IntPtr _size = (IntPtr)utf8Bytes.Length;
            
                    data.SelectionSet(format, _data, _size);

                    work_queue.Enqueue((wnd) => { wnd.PostClose(); });
                }
                
            };
            
            work_queue.Enqueue((wnd) =>
            {
                DataInterchange dataInterchange = DataInterchange.Create();
                dataInterchange.FormatAdd("text/plain");

                wnd.ClipboardCopy(dataInterchange);
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
}