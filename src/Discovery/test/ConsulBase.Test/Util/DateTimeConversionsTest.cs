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
using Xunit;

namespace Steeltoe.Consul.Util.Test
{
    public class DateTimeConversionsTest
    {
        [Fact]
        public void ToTimeSpan_ReturnsExpected()
        {
            Assert.Throws<ArgumentNullException>(() => DateTimeConversions.ToTimeSpan(string.Empty));
            Assert.Throws<InvalidOperationException>(() => DateTimeConversions.ToTimeSpan("foobar"));
            Assert.Equal(TimeSpan.FromMilliseconds(1000), DateTimeConversions.ToTimeSpan("1000ms"));
            Assert.Equal(TimeSpan.FromSeconds(1000), DateTimeConversions.ToTimeSpan("1000s"));
            Assert.Equal(TimeSpan.FromHours(1), DateTimeConversions.ToTimeSpan("1h"));
            Assert.Equal(TimeSpan.FromMinutes(1), DateTimeConversions.ToTimeSpan("1m"));
            Assert.Equal(TimeSpan.FromMilliseconds(1000), DateTimeConversions.ToTimeSpan("1000Ms"));
            Assert.Equal(TimeSpan.FromSeconds(1000), DateTimeConversions.ToTimeSpan("1000S"));
            Assert.Equal(TimeSpan.FromHours(1), DateTimeConversions.ToTimeSpan("1H"));
            Assert.Equal(TimeSpan.FromMinutes(1), DateTimeConversions.ToTimeSpan("1M"));
        }
    }
}
