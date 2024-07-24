// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
using System.Diagnostics;
using System.Text;
using Lightning;

namespace BuildDLLImports
{ 
    public class HeaderParser
    {
        public class Entry
        {
            public string Type { get; set; }
            public QuickList<object> Contents { get; set; } = new QuickList<object>();

            public Entry(string type, params object[] contents)
            {
                Type = type;
                foreach (var content in contents)
                {
                    Contents.Add(content);
                }
            }

            public QuickList<object>.Cursor NewCursor() => Contents.NewCursor();
            public QuickList<object>.Cursor NewCursor(QuickList<object>.Node startingNode) => Contents.NewCursor(startingNode);

            public override string ToString()
            {
                var builder = new StringBuilder();
                for (QuickList<object>.Node node = Contents.Head; node != null; node = node.Next)
                {
                    builder.Append(node.Value);
                }
                return builder.ToString();
            }
        }

        private readonly Type _constructType;

        public HeaderParser(Type constructType)
        {
            _constructType = constructType;
        }

        public void Whitespace(StringCursor cursor)
        {
            while (!cursor.IsEnd && char.IsWhiteSpace(cursor.Current))
            {
                cursor.Take();
            }
        }

        public Entry? ParseMatch(StringCursor cursor, string construct)
        {
            var matchedConstruct = ConstructMatcher.Match(cursor, "Match" + construct, _constructType);
            return matchedConstruct != null ? new Entry(construct, matchedConstruct) : null;
        }

        public Entry? ParseIdentifier(StringCursor cursor) => ParseMatch(cursor, "Identifier");

        public Entry? ParseQuote(StringCursor cursor) => ParseMatch(cursor, "Quote");

        public Entry? ConsumeParen(QuickList<object>.Cursor cursor)
        {
            if (cursor.IsEnd)
            {
                return null;
            }

            if (cursor.Current is char currentChar && currentChar == '(')
            {
                var entry = new Entry("Paren", cursor.Take());
                while (!cursor.IsEnd)
                {
                    if (IsChar(cursor.Current, ')'))
                    {
                        entry.Contents.Add(cursor.Take());
                        return entry;
                    }
                    else
                    {
                        var parenEntry = ConsumeParen(cursor);
                        if (parenEntry != null)
                        {
                            entry.Contents.Add(parenEntry);
                        }
                        else
                        {
                            entry.Contents.Add(cursor.Take());
                        }
                    }
                }

                Console.Error.WriteLine($"ERROR: open parenthesis in \"{entry}\"");
            }

            return null;
        }

        public Entry Parenthesize(Entry entry)
        {
            var result = new Entry(entry.Type);
            var cursor = entry.NewCursor();

            while (!cursor.IsEnd)
            {
                var parenEntry = ConsumeParen(cursor);
                if (parenEntry != null)
                {
                    result.Contents.Add(parenEntry);
                }
                else
                {
                    result.Contents.Add(cursor.Take());
                }
            }

            return result;
        }

        public Entry? ParseLine(StringCursor cursor)
        {
            Whitespace(cursor);
            if (cursor.IsEnd)
            {
                return null;
            }

            var entry = new Entry("Line");
            while (!cursor.IsEnd)
            {
                var parsed = ParseIdentifier(cursor) ?? ParseQuote(cursor);

                if (parsed != null)
                {
                    entry.Contents.Add(parsed);
                }
                else
                {
                    entry.Contents.Add(cursor.Take());
                }
            }

            return Parenthesize(entry);
        }

        private readonly string[] _directions = { "P_ELEMENTS", "P_INSTANCE", "P_OUT", "P_IN_OUT" };

        public bool IsChar(object obj, char c) => obj is char ch && ch == c;

        public TypeInfo GetTypeInfo(QuickList<object>.Cursor cursor)
        {
            var typeInfo = new TypeInfo();
            string value = cursor.Take().ToString();

            while (_directions.Contains(value))
            {
                typeInfo.Directions.Add(value);
                var nextEntry = (Entry)cursor.Take();
                cursor = nextEntry.NewCursor(nextEntry.Contents.Head.Next);
                value = cursor.Take().ToString();
            }

            typeInfo.Type = value;

            if (value == "const")
            {
                typeInfo.Type += " " + cursor.Take().ToString();
            }

            while (cursor.AtLeast(2) && IsChar(cursor.Current, ':') && IsChar(cursor.CurrentNode.Next.Value, ':'))
            {
                cursor.CurrentNode = cursor.CurrentNode.Next.Next;
                typeInfo.Type += "::" + cursor.Take();
            }

            if (!cursor.IsEnd && cursor.Current.ToString() == "const")
            {
                value = "const " + value;
                cursor.CurrentNode = cursor.CurrentNode.Next;
            }

            return typeInfo;
        }

        public ParsedFunction LineToFunction(QuickList<object>.Cursor cursor)
        {
            var function = new ParsedFunction
            {
                ReturnType = GetTypeInfo(cursor),
                Name = cursor.Take().ToString()
            };

            if (!(cursor.Current is Entry parameters))
            {
                Console.Error.WriteLine($"ERROR: {cursor.List} has invalid parameters");
                Debugger.Break();
            }
            else
            {
                function.Parameters.AddRange(ParseParameters(parameters));
            }

            return function;
        }

        public QuickList<ParsedFunction> ParseHeaderFile(string filePath)
        {
            var functions = new QuickList<ParsedFunction>();

            foreach (var line in File.ReadLines(filePath))
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Length == 0) continue;

                var cursor = new StringCursor(trimmedLine);

                if (trimmedLine.StartsWith("_EXPORT_"))
                {
                    cursor.Advance("_EXPORT_".Length);
                    var parsedLine = ParseLine(cursor);
                    if (parsedLine != null)
                    {
                        var parsedCursor = parsedLine.NewCursor();
                        functions.Add(LineToFunction(parsedCursor));
                    }
                }
                else
                {
                    var parsedLine = ParseLine(cursor);
                    if (parsedLine != null)
                    {
                        var parsedCursor = parsedLine.NewCursor();
                        functions.Add(LineToFunction(parsedCursor));
                    }
                }
            }

            return functions;
        }

        public string GetEntryType(object obj) => obj is Entry entry ? entry.Type : "";

        public IEnumerable<Entry> ParenSplit(Entry entry)
        {
            if (entry.Type != "Paren")
            {
                Console.Error.WriteLine("ERROR: expecting Paren entry");
                yield break;
            }

            var parameterEntry = new Entry("Parameter");
            for (QuickList<object>.Node node = entry.Contents.Head.Next; node != null && node.Next != null; node = node.Next)
            {
                if (node.Value is char ch && ch == ',')
                {
                    yield return parameterEntry;
                    parameterEntry = new Entry("Parameter");
                }
                else
                {
                    parameterEntry.Contents.Add(node.Value);
                }
            }

            yield return parameterEntry;
        }

        public IEnumerable<FunctionParameter> ParseParameters(Entry parameters)
        {
            var parsedParameters = new List<FunctionParameter>();
            string parameterString = parameters.ToString();

            if (parameterString == "(void)" || parameterString == "()")
            {
                return parsedParameters;
            }

            Console.WriteLine($"Debug: Parsing parameters: '{parameters}'");

            foreach (var parameter in ParenSplit(parameters))
            {
                if (parameter.Contents.Count >= 3 && GetEntryType(parameter.Contents[1]) == "Paren" &&
                    ((Entry)parameter.Contents[1]).Contents[1].ToString() == "*")
                {
                    var functionParameter = new FunctionParameter();
                    var cursor = parameter.NewCursor();

                    var nestedFunction = LineToFunction(cursor);
                    int length = nestedFunction.Name.Length;

                    functionParameter.Name = nestedFunction.Name.Substring(2, length - 3);
                    nestedFunction.Name = "(*)";
                    functionParameter.Type = new TypeInfo { Declaration = nestedFunction };

                    parsedParameters.Add(functionParameter);
                }
                else
                {
                    var functionParameter = new FunctionParameter();
                    var cursor = parameter.NewCursor();

                    functionParameter.Type = GetTypeInfo(cursor);
                    functionParameter.Name = cursor.Take().ToString();

                    parsedParameters.Add(functionParameter);
                }
            }

            return parsedParameters;
        }
    }
}
