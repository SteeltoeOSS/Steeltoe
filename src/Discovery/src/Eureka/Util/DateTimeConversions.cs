// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Eureka.Util;

internal static class DateTimeConversions
{
    private static readonly DateTime ZeroDateTimeUtc = DateTime.UnixEpoch;

    public static long? ToNullableJavaMilliseconds(DateTime? dateTime)
    {
        return dateTime == null ? null : ToJavaMilliseconds(dateTime.Value);
    }

    public static long ToJavaMilliseconds(DateTime dateTime)
    {
        if (dateTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("DateTime kind must be UTC.", nameof(dateTime));
        }

        if (dateTime.Ticks <= 0)
        {
            return 0;
        }

        long javaTicks = dateTime.Ticks - ZeroDateTimeUtc.Ticks;
        return javaTicks / 10_000;
    }

    public static DateTime? FromNullableJavaMilliseconds(long? javaMilliseconds)
    {
        return javaMilliseconds == null ? null : FromJavaMilliseconds(javaMilliseconds.Value);
    }

    public static DateTime FromJavaMilliseconds(long javaMilliseconds)
    {
        long dotNetTicks = javaMilliseconds * 10_000 + ZeroDateTimeUtc.Ticks;
        return new DateTime(dotNetTicks, DateTimeKind.Utc);
    }
}
