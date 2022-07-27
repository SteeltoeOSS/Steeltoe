// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Discovery.Eureka.Util;

public static class DateTimeConversions
{
    private static readonly DateTime BaseTime = new (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static long ToJavaMillis(DateTime dt)
    {
        if (dt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Kind != UTC");
        }

        if (dt.Ticks <= 0)
        {
            return 0;
        }

        var javaTicks = dt.Ticks - BaseTime.Ticks;
        return javaTicks / 10000;
    }

    public static DateTime FromJavaMillis(long javaMillis)
    {
        var dotNetTicks = (javaMillis * 10000) + BaseTime.Ticks;
        return new DateTime(dotNetTicks, DateTimeKind.Utc);
    }
}