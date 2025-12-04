// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

// ReSharper disable all
#pragma warning disable

namespace Steeltoe.Configuration.RandomValue.Test;

public sealed class CrashDumpTest
{
    [Fact]
    public void CrashTestHostUsingStackOverflow()
    {
        CrashTestHostUsingStackOverflow();

        true.Should().BeTrue();
    }
}
