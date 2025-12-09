// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Test;

public sealed class CalculatorTest
{
    [Fact]
    public void Add_Returns_Correct_Sum()
    {
        var calculator = new Calculator();
        int result = calculator.Add(3, 5);
        result.Should().Be(8);
    }

    [Fact(Skip = "ExampleSkippedTest")]
    public void SkippedTest()
    {
        true.Should().BeFalse();
    }
}
