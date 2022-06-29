// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class SpelTypeCode
{
    private static readonly Dictionary<string, SpelTypeCode> _names = new ();
    private static readonly Dictionary<Type, SpelTypeCode> _types = new ();

    public static readonly SpelTypeCode OBJECT = new ("OBJECT", typeof(object));
    public static readonly SpelTypeCode BOOLEAN = new ("BOOLEAN", typeof(bool));
    public static readonly SpelTypeCode BYTE = new ("BYTE", typeof(byte));
    public static readonly SpelTypeCode SBYTE = new ("SBYTE", typeof(sbyte));
    public static readonly SpelTypeCode CHAR = new ("CHAR", typeof(char));
    public static readonly SpelTypeCode DOUBLE = new ("DOUBLE", typeof(double));
    public static readonly SpelTypeCode FLOAT = new ("FLOAT", typeof(float));
    public static readonly SpelTypeCode INT = new ("INT", typeof(int));
    public static readonly SpelTypeCode UINT = new ("UINT", typeof(uint));
    public static readonly SpelTypeCode LONG = new ("LONG", typeof(long));
    public static readonly SpelTypeCode ULONG = new ("ULONG", typeof(ulong));
    public static readonly SpelTypeCode SHORT = new ("SHORT", typeof(short));
    public static readonly SpelTypeCode USHORT = new ("USHORT", typeof(ushort));

    private SpelTypeCode(string name, Type type)
    {
        Type = type;
        Name = name;
        _names.Add(Name, this);
        _types.Add(Type, this);
    }

    public Type Type { get; }

    public string Name { get; }

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
