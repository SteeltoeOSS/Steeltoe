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

namespace Steeltoe.Discovery.Eureka.Util
{
    public static class DateTimeConversions
    {
        private static readonly DateTime BaseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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
}
