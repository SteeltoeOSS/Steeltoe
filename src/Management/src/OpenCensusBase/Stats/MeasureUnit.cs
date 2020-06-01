// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Census.Stats
{
    public static class MeasureUnit
    {
        public static string Bytes { get; } = "bytes";

        public static string Kilobyte { get; } = "kilobytes";

        public static string Megabyte { get; } = "megabytes";

        public static string Gigabyte { get; } = "gigabytes";

        public static string Tarabyte { get; } = "terabytes";

        public static string Seconds { get; } = "seconds";

        public static string MicroSeconds { get; } = "microseconds";

        public static string MilliSeconds { get; } = "milliseconds";

        public static string NanoSeconds { get; } = "nanoseconds";

        private static string Minutes { get; } = "minutes";

        public static bool IsTimeUnit(string unit)
        {
            if (string.IsNullOrEmpty(unit))
            {
                return false;
            }

            return unit == MilliSeconds ||
                unit == NanoSeconds ||
                unit == Seconds ||
                unit == Minutes ||
                unit == MicroSeconds;
        }
    }
}
