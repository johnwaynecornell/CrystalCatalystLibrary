using JWCEssentials.net;

namespace ClipFlow.Format;

public class ClipContext : StandardContext
{
    public Action<string> _Diagnostic { get; private set; }

    private Action<ClipContext, string> diagnostic;
    
    public Action<ClipContext, string> Diagnostic
    {
        set
        {
            diagnostic = value;
            if (diagnostic == null) _Diagnostic = null;
            else _Diagnostic = (message) => diagnostic(this, message);
        }
        
        get => diagnostic;
    }
    
    public void DiagnosticOut(string message)
    {
        if (Diagnostic != null) Diagnostic(this, message);
        else ErrorOutput.WriteLine(message);
    }
    
    public void Paste(ClipType type)
    {
        ClipUtilityWindow.Paste(this, type);
    }

    public void Paste(ClipType type, ClipEndpoint endpoint)
    {
        ClipUtilityWindow.Paste(this, type, endpoint);
    }
    
    public void Copy(ClipType type)
    {
        ClipUtilityWindow.Copy(this, type);
    }

    public void Copy(ClipType type, ClipEndpoint endpoint)
    {
        ClipUtilityWindow.Copy(this, type, endpoint);
    }


}
