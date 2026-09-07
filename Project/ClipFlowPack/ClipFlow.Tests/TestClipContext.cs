using System;
using System.IO;
using ClipFlow.Format;

namespace ClipFlow.Tests;

public class TestClipContext : IDisposable
{
    public ClipContext Context { get; }
    public StringReader InputReader { get; }
    public StringWriter OutputWriter { get; }
    public StringWriter ErrorWriter { get; }

    public string OutputText => OutputWriter.ToString();
    public string ErrorText => ErrorWriter.ToString();
    public int Status => Context.Status;

    public TestClipContext(string initialInput = "")
    {
        InputReader = new StringReader(initialInput);
        OutputWriter = new StringWriter();
        ErrorWriter = new StringWriter();

        Context = new ClipContext
        {
            Input = InputReader,
            Output = OutputWriter,
            ErrorOutput = ErrorWriter,
            Status = 0
        };
    }

    public void Dispose()
    {
        InputReader.Dispose();
        OutputWriter.Dispose();
        ErrorWriter.Dispose();
    }
}
