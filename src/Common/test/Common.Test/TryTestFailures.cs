// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#pragma warning disable

namespace Steeltoe.Common.Test;

public sealed class TryTestFailures
{
    [Fact]
    public void HangTestHostByLoopingForever()
    {
        while (DateTime.UtcNow.Year < 2099)
        {
            // Comment to silence warnings.
        }

        true.Should().BeFalse();
    }
}
