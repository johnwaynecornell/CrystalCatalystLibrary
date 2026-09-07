using System;
using System.IO;
using ClipFlow.Format;
using Xunit;

namespace ClipFlow.Tests;

public class ContextHelperTests
{
    [Fact]
    public void TestContextStreamsCanBeAssigned()
    {
        var input = new StringReader("hello input");
        var output = new StringWriter();
        var errorOutput = new StringWriter();

        var ctx = new ClipContext
        {
            Input = input,
            Output = output,
            ErrorOutput = errorOutput,
            Status = 0
        };

        Assert.Same(input, ctx.Input);
        Assert.Same(output, ctx.Output);
        Assert.Same(errorOutput, ctx.ErrorOutput);
        Assert.Equal(0, ctx.Status);

        ctx.Output.WriteLine("test out");
        ctx.ErrorOutput.WriteLine("test err");
        ctx.Status = 42;

        Assert.Equal("test out" + Environment.NewLine, output.ToString());
        Assert.Equal("test err" + Environment.NewLine, errorOutput.ToString());
        Assert.Equal(42, ctx.Status);
    }
}
