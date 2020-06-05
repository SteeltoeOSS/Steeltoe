// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Steeltoe.Discovery.Consul.Util.Test
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
