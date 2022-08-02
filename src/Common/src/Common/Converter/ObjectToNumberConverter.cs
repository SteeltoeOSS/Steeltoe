// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Converter;

public class ObjectToNumberConverter : AbstractToNumberConverter
{
    public ObjectToNumberConverter()
        : base(GetConvertiblePairs())
    {
    }

    private static ISet<(Type Source, Type Target)> GetConvertiblePairs()
    {
        return new HashSet<(Type Source, Type Target)>
        {
            (typeof(object), typeof(int)),
            (typeof(object), typeof(float)),
            (typeof(object), typeof(uint)),
            (typeof(object), typeof(ulong)),
            (typeof(object), typeof(long)),
            (typeof(object), typeof(double)),
            (typeof(object), typeof(short)),
            (typeof(object), typeof(ushort)),
            (typeof(object), typeof(decimal)),
            (typeof(object), typeof(byte)),
            (typeof(object), typeof(sbyte))
        };
    }
}
