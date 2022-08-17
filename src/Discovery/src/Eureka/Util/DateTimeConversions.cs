// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Eureka.Util;

public static class DateTimeConversions
{
    private static readonly DateTime BaseTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static long ToJavaMillis(DateTime dateTime)
    {
        if (dateTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("DateTime kind must be UTC.", nameof(dateTime));
        }

        if (dateTime.Ticks <= 0)
        {
            return 0;
        }

        long javaTicks = dateTime.Ticks - BaseTime.Ticks;
        return javaTicks / 10000;
    }

    public static DateTime FromJavaMillis(long javaMillis)
    {
        long dotNetTicks = javaMillis * 10000 + BaseTime.Ticks;
        return new DateTime(dotNetTicks, DateTimeKind.Utc);
    }
}
