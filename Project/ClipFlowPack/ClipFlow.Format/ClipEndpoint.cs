using FluentCommandLine;

namespace ClipFlow.Format;

[KV_FA(FluentAttribute.Help, "a copy/paste endpoint")]
public class ClipEndpoint
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
    
    public class File : ClipEndpoint
    {
        public File(string path)
        {
            this.path = path;
        }

        public string path;
    }
    
    public class String : ClipEndpoint
    {
        public String(string value)
        {
            this.value = value;
        }

        public string value;
    }
    
    public class Console : ClipEndpoint
    {
        public Console()
        {
        }
    }
    
    public class Directory : ClipEndpoint
    {
        public Directory(string path)
        {
            this.path = path;
        }

        public string path;
    }
}