// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;
using CrystalCatalystLibrary.net;
using JWCEssentials.net;
using TestWindow;

Console.WriteLine(JWCEssentials.net.Essentials.feffect("fg_red(\"Hello\")"));

/*
utf8_string_struct[] tmp = (from s in args select (utf8_string_struct)s).ToArray();

struct_array_struct<utf8_string_struct> _args =
    (struct_array_struct<utf8_string_struct>)tmp; */
Application.Init(args);
Window wnd = new Window();
Application.Run();
