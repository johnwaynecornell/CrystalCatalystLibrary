using FluentCommandLine;

namespace ClipFlow.Format;

[KV_FA(FluentAttribute.Help, "type of clipboard content")]
public class ClipType
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
    
    public class Text : ClipType
    {
        public static ClipTypeHeader ClipTypeHeader =
            new ClipTypeHeader("text", new[] { "text/plain", "TEXT", "STRING", "UTF8_STRING" });

    }
    
    public class Html : ClipType
    {
        public static ClipTypeHeader ClipTypeHeader =
            new ClipTypeHeader("html", new[] { "text/html", "HTML", "HTML_TEXT" });

    }

    public class Image : ClipType
    {
        public static ClipTypeHeader ClipTypeHeader =
            new ClipTypeHeader("image", new[] { "image/png", "image/bmp" });

    }
    
    public class Files : ClipType
    {
        public static ClipTypeHeader ClipTypeHeader =
            new ClipTypeHeader("files", new[] { "text/file-uri" });
    }
    
}