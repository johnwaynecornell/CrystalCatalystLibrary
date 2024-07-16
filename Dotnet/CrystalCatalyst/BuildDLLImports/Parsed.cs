// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildDLLImports
{
    public class TypeInfo
    {
        public List<string> Directions { get; set; } = new List<string>();
        public string? Type { get; set; } = null;

        public ParsedFunction? Declaration { get; set; } = null;

        public override bool Equals(object obj)
        {
            if (obj is TypeInfo other)
            {
                return Directions.SequenceEqual(other.Directions) &&
                       Type == other.Type &&
                       EqualityComparer<ParsedFunction>.Default.Equals(Declaration, other.Declaration);
            }
            return false;
        }

        public override string ToString()
        {
            var directionStr = Directions.Count > 0 ? $"{string.Join(" ", Directions)} " : "";
            
            if (Type != null) return $"{directionStr}{Type}";

            return $"{directionStr}{Declaration}";

        }

        public override int GetHashCode()
        {
            int hashCode = 153293416;
            hashCode = hashCode * -1521134295 + Directions.Aggregate(0, (hash, dir) => hash * -1521134295 + dir.GetHashCode());
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
            hashCode = hashCode * -1521134295 + EqualityComparer<ParsedFunction>.Default.GetHashCode(Declaration);
            return hashCode;
        }
    }

    public class ParsedFunction
    {
        public TypeInfo ReturnType { get; set; }
        public string Name { get; set; }
        public List<FunctionParameter> Parameters { get; set; } = new List<FunctionParameter>();

        public override string ToString()
        {
            string r = ReturnType + " " + Name + "(";

            if (Parameters.Count > 0)
            {
                r += Parameters[0].ToString();

                for (int i = 1; i < Parameters.Count; i++)
                {
                    r += ", " + Parameters[i].ToString();
                }
            }
            
            r += ")";
            return r;
        }

        public override bool Equals(object obj)
        {
            if (obj is ParsedFunction other)
            {
                return ReturnType == other.ReturnType &&
                       Name == other.Name &&
                       Parameters.SequenceEqual(other.Parameters);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hashCode = -1205601023;
            hashCode = hashCode * -1521134295 + EqualityComparer<TypeInfo>.Default.GetHashCode(ReturnType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + Parameters.Aggregate(0, (hash, param) => hash * -1521134295 + param.GetHashCode());
            return hashCode;
        }
    }
    public class FunctionParameter
    {
        public TypeInfo Type { get; set; } 
        public string Name { get; set; }
        
        public override bool Equals(object obj)
        {
            if (obj is FunctionParameter other)
            {
                return Type == other.Type &&
                       Name == other.Name;
            }
            return false;
        }

        public override string ToString()
        {   
            return $"{Type.ToString()} {Name}";
        }

        public override int GetHashCode()
        {
            int hashCode = 958347798;
            hashCode = hashCode * -1521134295 + EqualityComparer<TypeInfo>.Default.GetHashCode(Type);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }
    }
}
