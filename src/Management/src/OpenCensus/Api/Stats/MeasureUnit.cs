// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
