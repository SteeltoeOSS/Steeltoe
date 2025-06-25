// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Test;

public sealed class SampleTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TestCoverage(bool condition)
    {
        var sample = new Sample();

        string result = sample.Covered(condition);

        if (condition)
        {
            result.Should().Be("yes");
        }
        else
        {
            result.Should().Be("no");
        }
    }
}
