namespace ClipFlow.Format;

public class ClipTypeHeader
{
    public string CommandName { get; }
    public string[] Formats { get; }

    public ClipTypeHeader(
        string commandName,
        IEnumerable<string> supportedFormats)
    {
        CommandName = commandName;
        Formats = supportedFormats.ToArray();
    }
}