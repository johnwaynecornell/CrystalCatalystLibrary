using FluentCommandLine;

namespace ClipFlow.Format;

[KV_FA(FluentAttribute.Help, "type of clipboard content")]
public class ClipType
{
    public string? type;
    
    //public abstract byte[] Read(...);
    //public abstract void Write(..., byte[] data);

    public ClipType(string type)
    {
        this.type = type;
    }
    
    [FluentMethod]
    [KV_FA(FluentAttribute.Help, "regular clipboard text")]
    public static ClipType text()
    {
        return new ClipType("text");
    }
    
    [FluentMethod]
    [KV_FA(FluentAttribute.Help, "html text")]
    public static ClipType html()
    {
        return new ClipType("html");
    }
    
    [FluentMethod]
    [KV_FA(FluentAttribute.Help, "A 2d image")]
    public static ClipType image()
    {
        return new ClipType("image");
    }
    
    [FluentMethod]
    [KV_FA(FluentAttribute.Help, "A set of files")]
    public static ClipType files()
    {
        return new ClipType("files");
    }
}