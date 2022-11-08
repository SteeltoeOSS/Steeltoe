// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace Steeltoe.Common.Util;

internal static class ObjectEquality
{
    public static bool ObjectOrCollectionEquals(object obj1, object obj2)
    {
        if (ReferenceEquals(obj1, obj2))
        {
            return true;
        }

        if (obj1 is null)
        {
            return false;
        }

        if (obj1 is IEnumerable enumerable1 && obj2 is IEnumerable enumerable2)
        {
            return enumerable1.Cast<object>().SequenceEqual(enumerable2.Cast<object>());
        }

        return obj1.Equals(obj2);
    }

    public static int GetObjectOrCollectionHashCode(object obj)
    {
        if (obj is null)
        {
            return 0;
        }

        if (obj is IEnumerable enumerable)
        {
            var hashCode = default(HashCode);

            foreach (object item in enumerable)
            {
                hashCode.Add(item);
            }

            return hashCode.ToHashCode();
        }

        return obj.GetHashCode();
    }
}
