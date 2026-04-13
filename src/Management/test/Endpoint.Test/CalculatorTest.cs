// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Test;

public sealed class CalculatorTest
{
    [Fact]
    public void Can_add()
    {
        var calculator = new Calculator();
        int result = calculator.Add(3, 5);
        result.Should().Be(8);
    }
}
