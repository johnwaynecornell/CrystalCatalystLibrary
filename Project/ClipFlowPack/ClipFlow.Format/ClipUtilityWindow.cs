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
            
                type.Receive(context, di, format, data, size);
                if (context.Status == 0) endpoint.Write(context, type);
                
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
                endpoint.Read(context, type);
                
                byte[]? bytes = type.Provide(context, data, format);

                if (context.Status != 0)
                {
                    wnd.PostClose();
                    return;
                }

                IntPtr _data = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, _data, bytes.Length);
                IntPtr _size = (IntPtr)bytes.Length;
        
                data.SelectionSet(format, _data, _size);

                work_queue.Enqueue((wnd) => { wnd.PostClose(); });
            };
            
            work_queue.Enqueue((wnd) =>
            {
                DataInterchange dataInterchange = DataInterchange.Create();
                type.Advertise(context, dataInterchange);

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