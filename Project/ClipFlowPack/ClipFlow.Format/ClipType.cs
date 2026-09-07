using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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
    
    public abstract void Advertise(ClipContext context, DataInterchange di);
    public abstract byte[]? Provide(ClipContext context, DataInterchange di, string format);
    
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
            new ClipTypeHeader("text", new[] { "text/plain", "UTF8_STRING", "STRING", "TEXT" });
        
        public string? Identity { get; set; }
        
        public override void Advertise(ClipContext context, DataInterchange di)
        {
            di.FormatAdd("text/plain");
        }
        
        public override byte[]? Provide(ClipContext context, DataInterchange di, string format)
        {
            if (Identity == null)
            {
                context.ErrorOutput.WriteLine("ClipType Identity is null");
                context.Status = 1;
                return null;
            }
            
            return System.Text.Encoding.UTF8.GetBytes(Identity);
        }
        
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
        
        public override void Advertise(ClipContext context, DataInterchange di)
        {
            di.FormatAdd("text/html");
        }

        public override byte[]? Provide(ClipContext context, DataInterchange di, string format)
        {
            if (Identity == null)
            {
                context.ErrorOutput.WriteLine("ClipType Identity is null");
                context.Status = 1;
                return null;
            }
            
            return System.Text.Encoding.UTF8.GetBytes(Identity);
        }

        public override void Receive(ClipContext context, DataInterchange di, string format, IntPtr data, IntPtr size)
        {
            Identity = Marshal.PtrToStringUTF8(data, checked((int)size));
        }
    }

    public class Image : ClipType
    {
        public static ClipTypeHeader ClipTypeHeader =
            new ClipTypeHeader("image", new[] { "image/png", "image/bmp" });

        public SKImage? Identity { get; set; }

        public override void Advertise(ClipContext context, DataInterchange di)
        {
            di.FormatAdd("image/png");
            di.FormatAdd("image/bmp");
        }

        public override byte[]? Provide(ClipContext context, DataInterchange di, string format)
        {
            if (Identity == null)
            {
                context.ErrorOutput.WriteLine("ClipType Identity is null");
                context.Status = 1;
                return null;
            }

            if (format == "image/bmp")
            {
                using SKData encodedData = Identity.Encode(SKEncodedImageFormat.Bmp, 100);
                return encodedData.ToArray();
            }
            
            if (format == "image/png")
            {
                using SKData encodedData = Identity.Encode(SKEncodedImageFormat.Png, 100);
                return encodedData.ToArray();
            }
            
            return null;
        }

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
        
        public override void Advertise(ClipContext context, DataInterchange di)
        {
            di.FormatAdd("text/file-uri");
        }

        public override byte[]? Provide(ClipContext context, DataInterchange di, string format)
        {
            if (Identity == null)
            {
                context.ErrorOutput.WriteLine("ClipType Identity is null");
                context.Status = 1;
                return null;
            }

            StringWriter writer = new StringWriter();
            foreach (string s in Identity)
            {
                writer.WriteLine(s);
            }

            return Encoding.UTF8.GetBytes(writer.ToString());
        }

        public override void Receive(ClipContext context, DataInterchange di, string format, IntPtr data, IntPtr size)
        {
            string value =
                Marshal.PtrToStringUTF8(data, checked((int)size))
                ?? string.Empty;

            using StringReader reader = new(value);
            
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