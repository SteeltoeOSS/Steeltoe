// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Steeltoe.Consul.Util
{
    public class DateTimeConversions
    {
        public static TimeSpan ToTimeSpan(string time)
        {
            if (string.IsNullOrWhiteSpace(time))
            {
                throw new ArgumentNullException(nameof(time));
            }

            time = time.ToLower();

            if (time.EndsWith("ms"))
            {
                return ToTimeSpan(int.Parse(time.Substring(0, time.Length - 2)), "ms");
            }

            if (time.EndsWith("s"))
            {
                return ToTimeSpan(int.Parse(time.Substring(0, time.Length - 1)), "s");
            }

            if (time.EndsWith("m"))
            {
                return ToTimeSpan(int.Parse(time.Substring(0, time.Length - 1)), "m");
            }

            if (time.EndsWith("h"))
            {
                return ToTimeSpan(int.Parse(time.Substring(0, time.Length - 1)), "h");
            }

            throw new InvalidOperationException("Incorrect format:" + time);
        }

        public static TimeSpan ToTimeSpan(int value, string unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
            {
                throw new ArgumentNullException(nameof(unit));
            }

            switch (unit)
            {
                case "ms":
                    return TimeSpan.FromMilliseconds(value);

                case "s":
                    return TimeSpan.FromSeconds(value);

                case "m":
                    return TimeSpan.FromMinutes(value);

                case "h":
                    return TimeSpan.FromHours(value);
            }

            throw new InvalidOperationException("Incorrect unit:" + unit);
        }
    }
}