// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

#if !NET6_0
#pragma warning disable CS0618 // Type or member is obsolete
#endif

public sealed class TestClock : ISystemClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2013, 6, 11, 12, 34, 56, 789, TimeSpan.Zero);

    public void Add(TimeSpan timeSpan)
    {
        UtcNow += timeSpan;
    }
}
