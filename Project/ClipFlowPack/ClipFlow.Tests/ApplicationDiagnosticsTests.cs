using System;
using System.Collections.Generic;
using System.Threading;
using CrystalCatalystLibrary.net;
using Xunit;

namespace ClipFlow.Tests;

public class ApplicationDiagnosticsTests
{
    [Fact]
    public void SetDiagnosticsCallback_AcceptsNullableVariable()
    {
        // Must compile and execute cleanly with nullable variable typed as Action<string>?
        Action<string>? nullCallback = null;
        Application.SetDiagnosticsCallback(nullCallback);

        var captured = new List<string>();
        Action<string>? nonNullCallback = message => captured.Add(message);
        Application.SetDiagnosticsCallback(nonNullCallback);

        // Reset to null
        Application.SetDiagnosticsCallback(null);
    }

    [Fact]
    public void SetDiagnosticsCallback_ThreadIsolation()
    {
        var thread1Messages = new List<string>();
        var thread2Messages = new List<string>();

        var t1 = new Thread(() =>
        {
            Action<string>? cb1 = msg => thread1Messages.Add("T1:" + msg);
            Application.SetDiagnosticsCallback(cb1);
            Application.SetDiagnosticsCallback(null);
        });

        var t2 = new Thread(() =>
        {
            Action<string>? cb2 = msg => thread2Messages.Add("T2:" + msg);
            Application.SetDiagnosticsCallback(cb2);
            Application.SetDiagnosticsCallback(null);
        });

        t1.Start();
        t2.Start();
        t1.Join();
        t2.Join();
    }

    [Fact]
    public void DiagnosticMessage_DirectCallWithoutInit_DoesNotCrash()
    {
        // When TheApplication is null, DiagnosticMessage falls back to stderr
        Application.DiagnosticMessage("Fallback diagnostic message");
    }

    [Fact]
    public void DiagnosticMessage_WithCallback_InvokesHandler()
    {
        var captured = new List<string>();
        var t = new Thread(() =>
        {
            Application.Init(Array.Empty<string>());
            Application.SetDiagnosticsCallback(msg => captured.Add(msg));

            Application.DiagnosticMessage("Managed diagnostic test");

            Application.SetDiagnosticsCallback(null);
        });
        t.Start();
        t.Join();

        Assert.Single(captured);
        Assert.Equal("Managed diagnostic test", captured[0]);
    }

    [Fact]
    public void DiagnosticMessage_ThreadIsolation()
    {
        var thread1Messages = new List<string>();
        var thread2Messages = new List<string>();

        var t1 = new Thread(() =>
        {
            Application.Init(Array.Empty<string>());
            Application.SetDiagnosticsCallback(msg => thread1Messages.Add("T1:" + msg));
            Application.DiagnosticMessage("Message from T1");
            Application.SetDiagnosticsCallback(null);
        });

        var t2 = new Thread(() =>
        {
            Application.Init(Array.Empty<string>());
            Application.SetDiagnosticsCallback(msg => thread2Messages.Add("T2:" + msg));
            Application.DiagnosticMessage("Message from T2");
            Application.SetDiagnosticsCallback(null);
        });

        t1.Start();
        t2.Start();
        t1.Join();
        t2.Join();

        Assert.Equal(new[] { "T1:Message from T1" }, thread1Messages);
        Assert.Equal(new[] { "T2:Message from T2" }, thread2Messages);
    }
}
