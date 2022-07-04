// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Common.Util;

public static class ObjectUtils
{
    private const int InitialHash = 7;
    private const int Multiplier = 31;

    [Obsolete("Use IsNullOrEmpty instead.")]
    public static bool IsEmpty(object[] array)
    {
        return IsNullOrEmpty(array);
    }

    public static bool IsNullOrEmpty<T>(IEnumerable<T> source)
    {
        return source == null || !source.Any();
    }

    public static bool NullSafeEquals(object o1, object o2)
    {
        if (o1 == o2)
        {
            return true;
        }

        if (o1 == null || o2 == null)
        {
            return false;
        }

        if (o1.Equals(o2))
        {
            return true;
        }

        if (o1.GetType().IsArray && o2.GetType().IsArray)
        {
            return ArrayEquals(o1, o2);
        }

        return false;
    }

    public static int NullSafeHashCode(object obj)
    {
        if (obj == null)
        {
            return 0;
        }

        if (obj.GetType().IsArray)
        {
            return ArrayHashCode(obj);
        }

        return obj.GetHashCode();
    }

    // Add other .NET types?
    internal static int ArrayHashCode(object o1)
    {
        return o1 switch
        {
            object[] v => ArrayItemsHashCode(v),
            bool[] v => ArrayItemsHashCode(v),
            byte[] v => ArrayItemsHashCode(v),
            char[] v => ArrayItemsHashCode(v),
            double[] v => ArrayItemsHashCode(v),
            float[] v => ArrayItemsHashCode(v),
            int[] v => ArrayItemsHashCode(v),
            long[] v => ArrayItemsHashCode(v),
            short[] v => ArrayItemsHashCode(v),
            _ => 0
        };
    }

    // Add other .NET types?
    internal static bool ArrayEquals(object o1, object o2)
    {
        return o1 switch
        {
            object[] v => o2 is object[] v1 && ArrayItemsEqual(v, v1),
            bool[] v => o2 is bool[] v1 && ArrayItemsEqual(v, v1),
            byte[] v => o2 is byte[] v1 && ArrayItemsEqual(v, v1),
            char[] v => o2 is char[] v1 && ArrayItemsEqual(v, v1),
            double[] v => o2 is double[] v1 && ArrayItemsEqual(v, v1),
            float[] v => o2 is float[] v1 && ArrayItemsEqual(v, v1),
            int[] v => o2 is int[] v1 && ArrayItemsEqual(v, v1),
            long[] v => o2 is long[] v1 && ArrayItemsEqual(v, v1),
            short[] v => o2 is short[] v1 && ArrayItemsEqual(v, v1),
            _ => false
        };
    }

    internal static bool ArrayItemsEqual<T>(T[] o1, T[] o2)
    {
        if (o1 == o2)
        {
            return true;
        }

        if (o1 == null || o2 == null)
        {
            return false;
        }

        if (o2.Length != o1.Length)
        {
            return false;
        }

        for (var i = 0; i < o1.Length; i++)
        {
            var item1 = o1[i];
            var item2 = o2[i];
            if (!(item1 == null ? item2 == null : item1.Equals(item2)))
            {
                return false;
            }
        }

        return true;
    }

    internal static int ArrayItemsHashCode<T>(T[] array)
    {
        if (array == null)
        {
            return 0;
        }

        var hash = InitialHash;
        foreach (var element in array)
        {
            hash = (Multiplier * hash) + NullSafeHashCode(element);
        }

        return hash;
    }
}
