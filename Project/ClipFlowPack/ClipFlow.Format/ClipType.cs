using System.Reflection;
using System.Runtime.InteropServices;
using CrystalCatalystLibrary.net;
using FluentCommandLine;
using SkiaSharp;

namespace ClipFlow.Format;

[KV_FA(FluentAttribute.Help, "type of clipboard content")]
public abstract class ClipType
{
    //public abstract byte[] Read(...);
    //public abstract void Write(..., byte[] data);
    
    [FluentMethod]
    [KV_FA(FluentAttribute.Help, "regular clipboard text")]
    public static ClipType text()
    {
        return new ClipType.Text();
    }
    
    [FluentMethod]
    [KV_FA(FluentAttribute.Help, "html text")]
    public static ClipType html()
    {
        return new ClipType.Html();
    }
    
    [FluentMethod]
    [KV_FA(FluentAttribute.Help, "A 2d image")]
    public static ClipType image()
    {
        return new ClipType.Image();
    }
    
    [FluentMethod]
    [KV_FA(FluentAttribute.Help, "A set of files")]
    public static ClipType files()
    {
        return new ClipType.Files();
    }
    
    public virtual void Select(ClipContext context, DataInterchange di)
    {
        string? format = null;

        var fi = GetType().GetField("ClipTypeHeader", BindingFlags.Public | BindingFlags.Static);
        if (fi == null) throw new Exception("Programmer error: ClipTypeHeader field not found");
        
        ClipTypeHeader header = (ClipTypeHeader)fi.GetValue(null);
        
        foreach (string known in header.Formats)
        {
            for (var node = di.FormatEnum();
                 node != IntPtr.Zero;
                 node = DataInterchange.FormatEnumNext(node))
            {
                DataInterchange.FormatEnumText(node, out var advertised);

                if (known == advertised)
                {
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
        }
        else
        {
            context.ErrorOutput.WriteLine($"Clipboard paste format {format}");
            di.Select(format);
        }
    }

    public abstract void Receive(ClipContext context, DataInterchange di, string format, IntPtr data, IntPtr size);
    
    public class Text : ClipType
    {
        public static ClipTypeHeader ClipTypeHeader =
            new ClipTypeHeader("text", new[] { "text/plain", "TEXT", "STRING", "UTF8_STRING" });
        
        public string? Identity { get; set; }
        
        public override void Receive(ClipContext context, DataInterchange di, string format, IntPtr data, IntPtr size)
        {
            Identity = Marshal.PtrToStringUTF8(data, checked((int)size));
        }
    }
    
    public class Html : ClipType
    {
        public static ClipTypeHeader ClipTypeHeader =
            new ClipTypeHeader("html", new[] { "text/html", "HTML", "HTML_TEXT" });
        
        public string? Identity { get; set; }
        
        public override void Receive(ClipContext context, DataInterchange di, string format, IntPtr data, IntPtr size)
        {
            Identity = Marshal.PtrToStringUTF8(data, checked((int)size));
        }
    }

    public class Image : ClipType
    {
        public static ClipTypeHeader ClipTypeHeader =
            new ClipTypeHeader("image", new[] { "image/png", "image/bmp" });

        public SKImage? Identity { get; private set; }

        public override void Receive(
            ClipContext context,
            DataInterchange di,
            string format,
            IntPtr data,
            IntPtr size)
        {
            int length = checked((int)size);

            byte[] bytes = new byte[length];
            Marshal.Copy(data, bytes, 0, length);

            using SKData skData = SKData.CreateCopy(bytes);
            Identity = SKImage.FromEncodedData(skData);

            if (Identity == null)
            {
                context.ErrorOutput.WriteLine(
                    $"Unable to decode clipboard image format {format}");
                context.Status = 1;
            }
        }
    }
    public class Files : ClipType
    {
        public static ClipTypeHeader ClipTypeHeader =
            new ClipTypeHeader("files", new[] { "text/file-uri" });
        
        public List<string>? Identity { get; set; }
        
        public override void Receive(ClipContext context, DataInterchange di, string format, IntPtr data, IntPtr size)
        {
            StringReader reader = new StringReader(Marshal.PtrToStringUTF8(data));
            
            Identity = new List<string>();

            string? line;
            do
            {
                line = reader.ReadLine();
                if (line != null) Identity.Add(line);
                
            } while (line != null);
        }
    }
    
}