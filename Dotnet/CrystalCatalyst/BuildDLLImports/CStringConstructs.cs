// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
using System.Text;

namespace BuildDLLImports;

public static class CStringConstructs
{
    public static string? MatchIdentifier(StringCursor cursor)
    {
        cursor.SkipWhitespace();
        if (cursor.IsEnd || !(cursor.Current == '_' || char.IsLetter(cursor.Current)))
        {
            return null;
        }

        var builder = new StringBuilder();
        while (!cursor.IsEnd && (cursor.Current == '_' || char.IsLetterOrDigit(cursor.Current)))
        {
            builder.Append(cursor.Take());
        }

        return builder.ToString();
    }

    public static string? MatchQuote(StringCursor cursor)
    {
        cursor.SkipWhitespace();
        if (cursor.IsEnd || (cursor.Current != '\'' && cursor.Current != '\"'))
        {
            return null;
        }

        var builder = new StringBuilder();
        char quoteChar = cursor.Take();
        builder.Append(quoteChar);

        bool inEscape = false;

        while (!cursor.IsEnd)
        {
            char currentChar = cursor.Take();
            builder.Append(currentChar);

            if (!inEscape)
            {
                if (currentChar == quoteChar)
                {
                    return builder.ToString();
                }

                if (currentChar == '\\')
                {
                    inEscape = true;
                }
            }
            else
            {
                inEscape = false;
            }
        }

        return null;
    }

    public static string? MatchNumber(StringCursor cursor)
    {
        cursor.SkipWhitespace();
        if (cursor.IsEnd || !char.IsDigit(cursor.Current))
        {
            return null;
        }

        var builder = new StringBuilder();
        while (!cursor.IsEnd && (char.IsDigit(cursor.Current) || cursor.Current == '.'))
        {
            builder.Append(cursor.Take());
        }

        return builder.ToString();
    }
}
