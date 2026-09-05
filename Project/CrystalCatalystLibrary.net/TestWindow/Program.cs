// See https://aka.ms/new-console-template for more information

using System;
using System.Runtime.InteropServices;
using CrystalCatalystLibrary.net;
using JWCEssentials.net;
using TestWindow;

namespace TestWindow
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Pre init Application.Peek() = " + Application.Peek());
            Console.WriteLine(JWCEssentials.net.Essentials.feffect("fg_red(\"Hello\")"));

            /*
            utf8_string_struct[] tmp = (from s in args select (utf8_string_struct)s).ToArray();

            struct_array_struct<utf8_string_struct> _args =
                (struct_array_struct<utf8_string_struct>)tmp; */
            Application.Init(args);
            Console.WriteLine("Post init Application.Peek() = " + Application.Peek());
            Window wnd = new Window();
            wnd.Run();
        }
    }
}
