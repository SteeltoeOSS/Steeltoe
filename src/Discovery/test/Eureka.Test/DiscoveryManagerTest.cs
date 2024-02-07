// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class DiscoveryManagerTest : AbstractBaseTest
{
    [Fact]
    public void DiscoveryManager_Uninitialized()
    {
        Assert.Null(EurekaDiscoveryManager.SharedInstance.Client);
        Assert.Null(EurekaDiscoveryManager.SharedInstance.ClientOptions);
        Assert.Null(EurekaDiscoveryManager.SharedInstance.InstanceOptions);
    }
}
