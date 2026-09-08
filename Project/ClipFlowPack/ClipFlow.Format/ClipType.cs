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
        
        ClipTypeHeader header = (ClipTypeHeader)fi.GetValue(null)!;
        
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
            Application.DiagnosticMessage($"Clipboard paste format {format}");
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
            string raw = Marshal.PtrToStringUTF8(data, checked((int)size)) ?? string.Empty;
            Identity = UnwrapCfHtml(raw);
        }

        public static string UnwrapCfHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            if (!html.StartsWith("Version:", StringComparison.OrdinalIgnoreCase))
            {
                return html;
            }

            int startFrag = FindOffset(html, "StartFragment:");
            int endFrag = FindOffset(html, "EndFragment:");

            if (startFrag >= 0 && endFrag > startFrag)
            {
                byte[] utf8Bytes = Encoding.UTF8.GetBytes(html);
                if (startFrag < utf8Bytes.Length && endFrag <= utf8Bytes.Length && endFrag > startFrag)
                {
                    return Encoding.UTF8.GetString(utf8Bytes, startFrag, endFrag - startFrag);
                }
            }

            int startHtml = FindOffset(html, "StartHTML:");
            int endHtml = FindOffset(html, "EndHTML:");
            if (startHtml >= 0 && endHtml > startHtml)
            {
                byte[] utf8Bytes = Encoding.UTF8.GetBytes(html);
                if (startHtml < utf8Bytes.Length && endHtml <= utf8Bytes.Length && endHtml > startHtml)
                {
                    return Encoding.UTF8.GetString(utf8Bytes, startHtml, endHtml - startHtml);
                }
            }

            const string startMarker = "<!--StartFragment-->";
            const string endMarker = "<!--EndFragment-->";
            int sPos = html.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
            int ePos = html.IndexOf(endMarker, StringComparison.OrdinalIgnoreCase);
            if (sPos >= 0 && ePos > sPos)
            {
                return html.Substring(sPos + startMarker.Length, ePos - (sPos + startMarker.Length));
            }

            return html;
        }

        private static int FindOffset(string text, string field)
        {
            int idx = text.IndexOf(field, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return -1;
            idx += field.Length;
            while (idx < text.Length && (text[idx] == ' ' || text[idx] == '\t')) idx++;
            int end = idx;
            while (end < text.Length && char.IsDigit(text[end])) end++;
            if (end > idx && int.TryParse(text.Substring(idx, end - idx), out int val))
            {
                return val;
            }
            return -1;
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
                byte[]? bmpBytes = BmpEncoder.EncodeToBmp(Identity);
                if (bmpBytes == null)
                {
                    context.ErrorOutput.WriteLine("Failed to encode image as BMP");
                    context.Status = 1;
                    return null;
                }
                return bmpBytes;
            }
            
            if (format == "image/png")
            {
                using SKData? encodedData = Identity.Encode(SKEncodedImageFormat.Png, 100);
                if (encodedData == null)
                {
                    context.ErrorOutput.WriteLine("Failed to encode image as PNG");
                    context.Status = 1;
                    return null;
                }
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
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.TrimStart().StartsWith("#"))
                    continue;

                string normalized = NormalizePathOrUri(line);
                Identity.Add(normalized);
            }
        }

        public static string NormalizePathOrUri(string line)
        {
            string trimmed = line.Trim();

            if (trimmed.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                if (trimmed.StartsWith("file://localhost/", StringComparison.OrdinalIgnoreCase))
                {
                    trimmed = "file:///" + trimmed.Substring("file://localhost/".Length);
                }
                else if (trimmed.StartsWith("file://localhost", StringComparison.OrdinalIgnoreCase))
                {
                    trimmed = "file:///" + trimmed.Substring("file://localhost".Length).TrimStart('/');
                }
                else if (trimmed.StartsWith("file://", StringComparison.OrdinalIgnoreCase) &&
                         !trimmed.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                {
                    string afterScheme = trimmed.Substring("file://".Length);
                    if (afterScheme.Length >= 2 && char.IsLetter(afterScheme[0]) && (afterScheme[1] == ':' || afterScheme[1] == '|'))
                    {
                        trimmed = "file:///" + afterScheme;
                    }
                }

                if (Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri) && uri.IsFile)
                {
                    return Path.GetFullPath(uri.LocalPath);
                }

                string raw = trimmed.Substring(5);
                while (raw.StartsWith("//")) raw = raw.Substring(2);
                if (raw.StartsWith("localhost/", StringComparison.OrdinalIgnoreCase))
                    raw = raw.Substring("localhost/".Length);
                if (raw.StartsWith("/")) raw = raw.Substring(1);
                raw = Uri.UnescapeDataString(raw);
                if (!raw.StartsWith("/") && !raw.Contains(":") && Environment.OSVersion.Platform != PlatformID.Win32NT)
                {
                    raw = "/" + raw;
                }
                return Path.GetFullPath(raw);
            }

            return Path.GetFullPath(line);
        }
    }
    
}