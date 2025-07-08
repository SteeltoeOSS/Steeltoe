// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Util;

namespace Steeltoe.Discovery.Eureka.Test.Util;

public sealed class DateTimeConversionsTest
{
    [Fact]
    public void ToJavaMillis_Throws_IfNotUTC()
    {
        DateTime dateTime = new DateTime(2016, 3, 14, 16, 42, 21, DateTimeKind.Local).AddMilliseconds(708);

        Action action = () => DateTimeConversions.ToJavaMilliseconds(dateTime);

        action.Should().ThrowExactly<ArgumentException>().WithMessage("DateTime kind must be UTC.*");
    }

    [Fact]
    public void ToJavaMillis()
    {
        DateTime dateTime = new DateTime(2016, 3, 14, 16, 42, 21, DateTimeKind.Utc).AddMilliseconds(708);
        long millis = DateTimeConversions.ToJavaMilliseconds(dateTime);

        millis.Should().Be(1_457_973_741_708);
    }

    [Fact]
    public void FromJavaMillis()
    {
        const long millis = 1_457_973_741_708;
        DateTime dateTime = DateTimeConversions.FromJavaMilliseconds(millis);

        dateTime.Month.Should().Be(3);
        dateTime.Day.Should().Be(14);
        dateTime.Year.Should().Be(2016);
        dateTime.Hour.Should().Be(16);
        dateTime.Minute.Should().Be(42);
        dateTime.Second.Should().Be(21);
        dateTime.Millisecond.Should().Be(708);
    }
}
