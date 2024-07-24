// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lightning;
using Newtonsoft.Json;

namespace BuildDLLImports
{
    public static class Snapshot
    {
        public static string directoryPath = "../../../";
        public static void SaveSnapshot(QuickList<ParsedFunction> functions)
        {
            //string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            //string filePath = Path.Combine(directoryPath, $"snapshot_{timestamp}.json");
            string filePath = Path.Combine(directoryPath, "snapshot_fresh.json");
            string previousPath = Path.Combine(directoryPath, "snapshot_stable.json");

            string json = JsonConvert.SerializeObject(functions, Formatting.Indented);
            File.WriteAllText(filePath, json);
            
            List<ParsedFunction> previousOutput;
                
            previousOutput = File.Exists(previousPath) ? JsonConvert.DeserializeObject<List<ParsedFunction>>(File.ReadAllText(previousPath)) : new List<ParsedFunction>();
            
// Implement comparison logic to identify changes
            var addedFunctions = functions.Except(previousOutput).ToList();
            var removedFunctions = previousOutput.Except(functions).ToList();
            var modifiedFunctions = functions.Intersect(previousOutput).Where(f => !f.Equals(previousOutput.Single(p => p.Name == f.Name))).ToList();
            
            var report = new StringBuilder();
            report.AppendLine("Added Functions:");
            foreach (var func in addedFunctions) report.AppendLine("\t"+func.Name);

            report.AppendLine("\nRemoved Functions:");
            foreach (var func in removedFunctions) report.AppendLine("\t"+func.Name);

            report.AppendLine("\nModified Functions:");
            foreach (var func in modifiedFunctions) report.AppendLine("\t"+func.Name);
            
            Console.WriteLine("***************************************************************");
            Console.Write(report.ToString());
            Console.WriteLine("***************************************************************");
        }
    }
}
