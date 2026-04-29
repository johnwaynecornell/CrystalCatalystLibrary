// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;
using CrystalCatalystLibrary;
using JWCEssentials.net;

Console.WriteLine(JWCEssentials.net.Essentials.feffect("fg_red(\"Hello\")"));

/*
utf8_string_struct[] tmp = (from s in args select (utf8_string_struct)s).ToArray();

struct_array_struct<utf8_string_struct> _args =
    (struct_array_struct<utf8_string_struct>)tmp; */
Application.Init(args);

CrystalWindow wnd = CrystalWindow.Create(800, 600, "test Window");
wnd.ApplicationRetain();

wnd.Show(true);

Application.Run();
