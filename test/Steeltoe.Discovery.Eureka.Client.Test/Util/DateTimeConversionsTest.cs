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
using Xunit;

namespace Steeltoe.Discovery.Eureka.Util.Test
{
    public class DateTimeConversionsTest
    {
        [Fact]
        public void ToJavaMillis_Throws_IfNotUTC()
        {
            DateTime dt = new DateTime(2016, 3, 14, 16, 42, 21, DateTimeKind.Local).AddMilliseconds(708);
            var ex = Assert.Throws<ArgumentException>(() => DateTimeConversions.ToJavaMillis(dt));
            Assert.Contains("Kind != UTC", ex.Message);
        }

        [Fact]
        public void ToJavaMillis()
        {
            DateTime dt = new DateTime(2016, 3, 14, 16, 42, 21, DateTimeKind.Utc).AddMilliseconds(708);
            long millis = DateTimeConversions.ToJavaMillis(dt);
            Assert.Equal(1457973741708, millis);
        }

        [Fact]
        public void FromJavaMillis()
        {
            long millis = 1457973741708;
            DateTime dt = DateTimeConversions.FromJavaMillis(millis);
            Assert.Equal(3, dt.Month);
            Assert.Equal(14, dt.Day);
            Assert.Equal(2016, dt.Year);
            Assert.Equal(16, dt.Hour);
            Assert.Equal(42, dt.Minute);
            Assert.Equal(21, dt.Second);
            Assert.Equal(708, dt.Millisecond);
        }
    }
}
