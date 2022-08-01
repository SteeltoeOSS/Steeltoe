// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Common.Test;

public class PlatformTest
{
    [Fact]
    public void IsCloudFoundry_ReturnsExpected()
    {
        Assert.False(Platform.IsCloudFoundry);
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", "somevalue");
        Assert.True(Platform.IsCloudFoundry);
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Assert.False(Platform.IsCloudFoundry);
    }
}
