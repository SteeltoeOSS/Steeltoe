// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class SpelTypeCode
{
    private static readonly Dictionary<string, SpelTypeCode> Names = new ();
    private static readonly Dictionary<Type, SpelTypeCode> Types = new ();

    public static readonly SpelTypeCode Object = new ("OBJECT", typeof(object));
    public static readonly SpelTypeCode Boolean = new ("BOOLEAN", typeof(bool));
    public static readonly SpelTypeCode Byte = new ("BYTE", typeof(byte));
    public static readonly SpelTypeCode Sbyte = new ("SBYTE", typeof(sbyte));
    public static readonly SpelTypeCode Char = new ("CHAR", typeof(char));
    public static readonly SpelTypeCode Double = new ("DOUBLE", typeof(double));
    public static readonly SpelTypeCode Float = new ("FLOAT", typeof(float));
    public static readonly SpelTypeCode Int = new ("INT", typeof(int));
    public static readonly SpelTypeCode Uint = new ("UINT", typeof(uint));
    public static readonly SpelTypeCode Long = new ("LONG", typeof(long));
    public static readonly SpelTypeCode Ulong = new ("ULONG", typeof(ulong));
    public static readonly SpelTypeCode Short = new ("SHORT", typeof(short));
    public static readonly SpelTypeCode Ushort = new ("USHORT", typeof(ushort));

    private SpelTypeCode(string name, Type type)
    {
        Type = type;
        Name = name;
        Names.Add(Name, this);
        Types.Add(Type, this);
    }

    public Type Type { get; }

    public string Name { get; }

    public static SpelTypeCode ForName(string name)
    {
        if (!Names.TryGetValue(name.ToUpper(), out var result))
        {
            return Object;
        }

        return result;
    }

    public static SpelTypeCode ForType(Type clazz)
    {
        if (!Types.TryGetValue(clazz, out var result))
        {
            return Object;
        }

        return result;
    }
}
