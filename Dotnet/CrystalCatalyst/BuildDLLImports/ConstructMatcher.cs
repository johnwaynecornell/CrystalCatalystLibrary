// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
using System.Reflection;
using Lightning;

namespace BuildDLLImports
{
    public static class ConstructMatcher
    {
        public delegate string? MatchDelegate(StringCursor cursor);

        private static readonly Mutex threadGuard = new Mutex();
        private static readonly Dictionary<Type, Dictionary<string, MatchDelegate>> matchers = new Dictionary<Type, Dictionary<string, MatchDelegate>>();

        public static string? Match(StringCursor cursor, string constructName, Type constructType)
        {
            MatchDelegate matchDelegate;

            threadGuard.WaitOne();
            try
            {
                if (!matchers.TryGetValue(constructType, out var t))
                {
                    matchers[constructType] = t = new Dictionary<string, MatchDelegate>();
                }

                if (!t.TryGetValue(constructName, out matchDelegate))
                {
                    var method = constructType.GetMethod(constructName, BindingFlags.Public | BindingFlags.Static);
                    if (method != null)
                    {
                        matchDelegate = (MatchDelegate)Delegate.CreateDelegate(typeof(MatchDelegate), method);
                        t[constructName] = matchDelegate;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Method {constructName} not found in {constructType.Name}");
                    }
                }
            }
            finally
            {
                threadGuard.ReleaseMutex();
            }

            return matchDelegate(cursor);
        }
    }
}