using System;
using System.Collections.Generic;
using System.IO;
using CrystalCatalystLibrary.net;

namespace MessageWindow;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.Init(args);
        string? inputFile = null;
        bool useStdin = false;
        string? outputImage = null;
        List<string> buttons = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-stdin")
            {
                useStdin = true;
            }
            else if (args[i] == "--Button" || args[i] == "-button")
            {
                if (i + 1 < args.Length)
                {
                    buttons.Add(args[++i]);
                }
            }
            else if (args[i] == "-output-image")
            {
                if (i + 1 < args.Length)
                {
                    outputImage = args[++i];
                }
            }
            else if (File.Exists(args[i]))
            {
                inputFile = args[i];
            }
        }

        byte[] inputData;
        if (useStdin)
        {
            using var ms = new MemoryStream();
            Console.OpenStandardInput().CopyTo(ms);
            inputData = ms.ToArray();
        }
        else if (inputFile != null)
        {
            inputData = File.ReadAllBytes(inputFile);
        }
        else
        {
            Console.WriteLine("Usage: MessageWindow [-stdin | file] [--Button \"Text|Action\"] [-output-image filename.png]");
            return;
        }

        var renderer = new CrystalSkia.net.AnsiSkiaRenderer();
        var content = renderer.Parse(inputData);

        var window = new Window(content, buttons, outputImage);
        window.Run();
    }
}
