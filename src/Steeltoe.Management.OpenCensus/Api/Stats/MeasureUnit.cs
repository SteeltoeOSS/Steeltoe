using System;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public static class MeasureUnit
    {
        public static string Bytes { get; } = "bytes";

        public static string Kilobyte { get; } = "kilobytes";

        public static string Megabyte { get; } = "megabytes";

        public static string Gigabyte { get; } = "gigabytes";

        public static string Tarabyte { get; } = "terabytes";

        private static string Minutes { get; } = "minutes";

        public static string Seconds { get; } = "seconds";

        public static string MicroSeconds { get; } = "microseconds";

        public static string MilliSeconds { get; } = "milliseconds";

        public static string NanoSeconds { get; } = "nanoseconds";

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
