// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
using System.Xml;

namespace BuildDLLImports;

using System;

public class Program
{
    public static void Main(string[] args)
    {
        var headerFilePath = "input.txt"; // Path to your header file
        var parser = new HeaderParser(typeof(CStringConstructs));
        var functions = parser.ParseHeaderFile(headerFilePath);
        
        Snapshot.SaveSnapshot(functions);
        
        foreach (var function in functions)
        {
            Console.WriteLine($"Function: {function.Name}");
            Console.WriteLine($"  Return Type: {function.ReturnType}");
            Console.WriteLine($"  Parameters:");
            foreach (var param in function.Parameters)
            {
                Console.WriteLine("    "+param.ToString());
            }
            Console.WriteLine();
        }
    }
}
