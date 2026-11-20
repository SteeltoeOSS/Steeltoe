// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Configuration;

namespace Steeltoe.Common.Test.Configuration;

public sealed class ConfigurationKeyConverterTest
{
    [Theory]
    [InlineData("unchanged", "unchanged")]
    [InlineData("unchanged:four:seven", "unchanged:four:seven")]
    [InlineData("one.four.seven", "one:four:seven")]
    [InlineData("one__four__seven", "one:four:seven")]
    [InlineData("one__four__seven__", "one:four:seven:")]
    [InlineData("_one__four__and_seven_", "_one:four:and_seven_")]
    [InlineData("one[1]", "one:1")]
    [InlineData("one[12][3456]", "one:12:3456")]
    [InlineData("one.four.seven[0][1].twelve.thirteen[12]", "one:four:seven:0:1:twelve:thirteen:12")]
    [InlineData(@"one\.four\\.seven", @"one.four\:seven")]
    [InlineData(@"one\_four\", @"one\_four\")]
    [InlineData("one[key]", "one:key")]
    [InlineData("one[four.seven]", "one:four.seven")]
    [InlineData("one[four.seven].twelve", "one:four.seven:twelve")]
    [InlineData("[one.four].seven", "one.four:seven")]
    [InlineData("one.[four.seven]", "one:four.seven")]
    [InlineData("one[four][seven.twelve]", "one:four:seven.twelve")]
    [InlineData("one[four__seven]", "one:four__seven")]
    [InlineData("one[four[0]]", "one:four[0]")]
    [InlineData("one[0]four", "one:0:four")]
    [InlineData("one[0]:four", "one:0:four")]
    [InlineData("one:four[0]:seven", "one:four:0:seven")]
    [InlineData("logging.level[Steeltoe.Common]", "logging:level:Steeltoe.Common")]
    [InlineData(@"one\[four.seven]", "one[four:seven]")]
    [InlineData("one[]", "one[]")]
    [InlineData("one[four.seven", "one[four:seven")]
    [InlineData("one]four.seven", "one]four:seven")]
    public void AsDotNetConfigurationKey_ProducesExpected(string input, string expectedOutput)
    {
        _ = ConfigurationKeyConverter.AsDotNetConfigurationKey(input).Should().Be(expectedOutput);
    }
}
