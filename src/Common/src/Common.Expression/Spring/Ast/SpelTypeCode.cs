﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class SpelTypeCode
    {
#pragma warning disable S3963 // "static" fields should be initialized inline
#pragma warning disable S4136 // Method overloads should be grouped together
        static SpelTypeCode()
#pragma warning restore S4136 // Method overloads should be grouped together
        {
            _names = new Dictionary<string, SpelTypeCode>();
            _types = new Dictionary<Type, SpelTypeCode>();
            OBJECT = new SpelTypeCode("OBJECT", typeof(object));
            BOOLEAN = new SpelTypeCode("BOOLEAN", typeof(bool));
            BYTE = new SpelTypeCode("BYTE", typeof(byte));
            SBYTE = new SpelTypeCode("SBYTE", typeof(sbyte));
            CHAR = new SpelTypeCode("CHAR", typeof(char));
            DOUBLE = new SpelTypeCode("DOUBLE", typeof(double));
            FLOAT = new SpelTypeCode("FLOAT", typeof(float));
            INT = new SpelTypeCode("INT", typeof(int));
            UINT = new SpelTypeCode("UINT", typeof(uint));
            LONG = new SpelTypeCode("LONG", typeof(long));
            ULONG = new SpelTypeCode("ULONG", typeof(ulong));
            SHORT = new SpelTypeCode("SHORT", typeof(short));
            USHORT = new SpelTypeCode("USHORT", typeof(ushort));
        }
#pragma warning restore S3963 // "static" fields should be initialized inline

        public static readonly SpelTypeCode OBJECT;
        public static readonly SpelTypeCode BOOLEAN;
        public static readonly SpelTypeCode BYTE;
        public static readonly SpelTypeCode SBYTE;
        public static readonly SpelTypeCode CHAR;
        public static readonly SpelTypeCode DOUBLE;
        public static readonly SpelTypeCode FLOAT;
        public static readonly SpelTypeCode INT;
        public static readonly SpelTypeCode UINT;
        public static readonly SpelTypeCode LONG;
        public static readonly SpelTypeCode ULONG;
        public static readonly SpelTypeCode SHORT;
        public static readonly SpelTypeCode USHORT;

        private static readonly Dictionary<string, SpelTypeCode> _names;
        private static readonly Dictionary<Type, SpelTypeCode> _types;

        private readonly Type _type;
        private readonly string _name;

        private SpelTypeCode(string name, Type type)
        {
            _type = type;
            _name = name;
            _names.Add(Name, this);
        }

        public Type Type => _type;

        public string Name => _name;

        public static SpelTypeCode ForName(string name)
        {
            if (!_names.TryGetValue(name.ToUpper(), out var result))
            {
                return OBJECT;
            }

            return result;
        }

        public static SpelTypeCode ForType(Type clazz)
        {
            if (!_types.TryGetValue(clazz, out var result))
            {
                return OBJECT;
            }

            return result;
        }
    }
}
