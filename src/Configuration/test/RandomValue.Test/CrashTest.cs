// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#pragma warning disable
// Resharper disable All

namespace Steeltoe.Configuration.RandomValue.Test;

public sealed class CrashTest
{
    [Fact]
    public void AlwaysCrash()
    {
        InfiniteRecursion();
        true.Should().BeTrue();
    }

    private static void InfiniteRecursion()
    {
        InfiniteRecursion();
    }
}
