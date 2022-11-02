// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Steeltoe.Common;

namespace Steeltoe.Discovery.Consul.Util;

public static class DateTimeConversions
{
    public static TimeSpan ToTimeSpan(string time)
    {
        ArgumentGuard.NotNullOrWhiteSpace(time);

        time = time.ToLowerInvariant();

        if (time.EndsWith("ms", StringComparison.Ordinal))
        {
            return ToTimeSpan(int.Parse(time.Substring(0, time.Length - 2), CultureInfo.InvariantCulture), "ms");
        }

        if (time.EndsWith('s'))
        {
            return ToTimeSpan(int.Parse(time.Substring(0, time.Length - 1), CultureInfo.InvariantCulture), "s");
        }

        if (time.EndsWith('m'))
        {
            return ToTimeSpan(int.Parse(time.Substring(0, time.Length - 1), CultureInfo.InvariantCulture), "m");
        }

        if (time.EndsWith('h'))
        {
            return ToTimeSpan(int.Parse(time.Substring(0, time.Length - 1), CultureInfo.InvariantCulture), "h");
        }

        throw new InvalidOperationException($"Incorrect format:{time}");
    }

    public static TimeSpan ToTimeSpan(int value, string unit)
    {
        ArgumentGuard.NotNullOrWhiteSpace(unit);

        return unit switch
        {
            "ms" => TimeSpan.FromMilliseconds(value),
            "s" => TimeSpan.FromSeconds(value),
            "m" => TimeSpan.FromMinutes(value),
            "h" => TimeSpan.FromHours(value),
            _ => throw new InvalidOperationException($"Incorrect unit:{unit}")
        };
    }
}
