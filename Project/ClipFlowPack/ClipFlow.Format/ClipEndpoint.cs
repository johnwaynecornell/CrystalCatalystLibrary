using System.Diagnostics;
using FluentCommandLine;
using SkiaSharp;

namespace ClipFlow.Format;

[KV_FA(FluentAttribute.Help, "a copy/paste endpoint")]
public abstract class ClipEndpoint
{
    [FluentMethod]
    public static ClipEndpoint file(string path)
    {
        return new File(path);
    }
    
    [FluentMethod]
    public static ClipEndpoint console()
    {
        return new Console();
    }

    [FluentMethod]
    public static ClipEndpoint directory(string path)
    {
        return new Directory(path);
    }

    [FluentMethod("string")]
    public static ClipEndpoint stringVal(string value)
    {
        return new String(value);
    }
    
    public abstract void Write(ClipContext context, ClipType type);
    
    public class File : ClipEndpoint
    {
        public File(string path)
        {
            this.path = path;
        }

        public string path;
        public override void Write(ClipContext context, ClipType type)
        {
            throw new NotImplementedException();
        }
    }
    
    public class String : ClipEndpoint
    {
        public String(string value)
        {
            this.value = value;
        }

        public string value;
        public override void Write(ClipContext context, ClipType type)
        {
            throw new NotImplementedException();
        }
        
    }
    
    public class Console : ClipEndpoint
    {
        public Console()
        {
            
        }

        public override void Write(ClipContext context, ClipType type)
        {
            switch (type)
            {
                case ClipType.Text text:
                    if (text.Identity != null)
                        context.Output.Write(text.Identity);
                    break;

                case ClipType.Html html:
                    if (html.Identity != null)
                        context.Output.Write(html.Identity);
                    break;

                case ClipType.Files files:
                    if (files.Identity != null)
                    {
                        foreach (string file in files.Identity)
                            context.Output.WriteLine(file);
                    }
                    break;

                case ClipType.Image image:
                    WriteImage(context, image);
                    break;

                default:
                    context.ErrorOutput.WriteLine(
                        $"Console endpoint does not support {type.GetType().Name}");
                    context.Status = 1;
                    break;
            }
        }
        
        private static void WriteImage(ClipContext context, ClipType.Image image)
        {
            if (image.Identity == null)
            {
                context.ErrorOutput.WriteLine("Image data is not available.");
                context.Status = 1;
                return;
            }

            string path = Path.Combine(
                Path.GetTempPath(),
                $"clipflow-{Guid.NewGuid():N}.png");

            using (SKData data =
                   image.Identity.Encode(SKEncodedImageFormat.Png, 100))
            using (FileStream stream = System.IO.File.Create(path))
            {
                data.SaveTo(stream);
            }

            ProcessStartInfo psi;

            if (OperatingSystem.IsWindows())
            {
                psi = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                };
            }
            else
            {
                psi = new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    ArgumentList = { path },
                    UseShellExecute = false
                };
            }

            Process.Start(psi);
        }
    }
    
    public class Directory : ClipEndpoint
    {
        public Directory(string path)
        {
            this.path = path;
        }

        public string path;
        public override void Write(ClipContext context, ClipType type)
        {
            throw new NotImplementedException();
        }
    }
}